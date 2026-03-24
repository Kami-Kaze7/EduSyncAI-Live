using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;

namespace EduSyncAI
{
    public partial class SessionManagementView : UserControl
    {
        // Screen recording state (stays here — recording is session-level)
        private Process? _screenRecordProcess;
        private bool _isScreenRecording = false;
        private string _screenRecordingPath = "";
        private int _recordingSessionId = 0;
        private DispatcherTimer? _recBlinkTimer;

        // Reference to auto-opened whiteboard
        private WhiteboardWindow? _activeWhiteboard;

        // Course card selection
        private System.Windows.Controls.Border? _selectedCardBorder;

        public SessionManagementView()
        {
            InitializeComponent();
            DataContextChanged += SessionManagementView_DataContextChanged;
        }

        private void CourseCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.Tag is Course course)
            {
                var viewModel = DataContext as SessionManagementViewModel;
                if (viewModel != null)
                {
                    viewModel.SelectedCourse = course;
                }

                // Reset previous selection
                if (_selectedCardBorder != null)
                {
                    _selectedCardBorder.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8F9FA"));
                    _selectedCardBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BDC3C7"));
                }

                // Highlight selected card
                border.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EBF5FB"));
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498DB"));
                _selectedCardBorder = border;
            }
        }

        private void SessionManagementView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SessionManagementViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is SessionManagementViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SessionManagementViewModel.HasActiveSession))
            {
                var viewModel = DataContext as SessionManagementViewModel;
                if (viewModel != null)
                {
                    if (viewModel.HasActiveSession && viewModel.ActiveSession != null)
                    {
                        // Session started → start screen recording + auto-open whiteboard
                        StartScreenRecording(viewModel.ActiveSession.Id);
                        AutoOpenWhiteboard(viewModel);
                    }
                    else
                    {
                        // Session ended → stop recording
                        if (_isScreenRecording)
                        {
                            await StopScreenRecordingAsync();
                        }
                    }
                }
            }
        }

        private void AutoOpenWhiteboard(SessionManagementViewModel viewModel)
        {
            try
            {
                if (viewModel.ActiveSession == null) return;

                _activeWhiteboard = new WhiteboardWindow(
                    viewModel.ActiveSession.Id,
                    viewModel,
                    OnWhiteboardSessionEnded);
                _activeWhiteboard.Closed += (s, e) => RestoreMainWindow();
                _activeWhiteboard.Show();

                // Minimize the main window so only the whiteboard is visible
                MinimizeMainWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening whiteboard: {ex.Message}");
            }
        }

        private async void OnWhiteboardSessionEnded()
        {
            // When session is ended from the whiteboard, stop recording here
            if (_isScreenRecording)
            {
                await StopScreenRecordingAsync();
            }
            // Restore the main window
            RestoreMainWindow();
        }

        private void MinimizeMainWindow()
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                mainWindow.WindowState = WindowState.Minimized;
            }
        }

        private void RestoreMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Window.GetWindow(this);
                if (mainWindow != null)
                {
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            });
        }

        private void OpenWhiteboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewModel = DataContext as SessionManagementViewModel;
                if (viewModel?.ActiveSession != null)
                {
                    // If whiteboard is already open, just bring it to front
                    if (_activeWhiteboard != null && _activeWhiteboard.IsLoaded)
                    {
                        _activeWhiteboard.WindowState = WindowState.Normal;
                        _activeWhiteboard.Activate();
                        MinimizeMainWindow();
                        return;
                    }
                    _activeWhiteboard = new WhiteboardWindow(
                        viewModel.ActiveSession.Id,
                        viewModel,
                        OnWhiteboardSessionEnded);
                    _activeWhiteboard.Closed += (s, args) => RestoreMainWindow();
                    _activeWhiteboard.Show();
                    MinimizeMainWindow();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No active session. Please start a session first.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening whiteboard: {ex.Message}");
            }
        }

        // ==================== SESSION SCREEN RECORDING ====================

        private void StartScreenRecording(int sessionId)
        {
            try
            {
                var ffmpegPath = FindFFmpeg();
                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    RecStatusText.Text = "NO REC";
                    return;
                }

                var recordingsDir = System.IO.Path.Combine(AppConfig.DataDir, "Recordings");
                System.IO.Directory.CreateDirectory(recordingsDir);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _screenRecordingPath = System.IO.Path.Combine(recordingsDir, $"Session_{sessionId}_{timestamp}.mp4");
                _recordingSessionId = sessionId;

                // Try to find an audio device for recording system audio
                string audioArgs = "";
                try
                {
                    var audioDevice = FindAudioDevice(ffmpegPath);
                    if (!string.IsNullOrEmpty(audioDevice))
                    {
                        audioArgs = $"-f dshow -i audio=\"{audioDevice}\" ";
                        System.Diagnostics.Debug.WriteLine($"Audio capture device found: {audioDevice}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No audio capture device found, recording video only");
                    }
                }
                catch { /* Fall back to video-only */ }

                // Build ffmpeg arguments: video + optional audio
                string ffmpegArgs;
                if (!string.IsNullOrEmpty(audioArgs))
                {
                    // Record screen + system audio
                    ffmpegArgs = $"-y -f gdigrab -framerate 10 -i desktop {audioArgs}-c:v libx264 -preset ultrafast -crf 28 -pix_fmt yuv420p -c:a aac -b:a 128k -movflags frag_keyframe+empty_moov \"{_screenRecordingPath}\"";
                }
                else
                {
                    // Video only (no audio device available)
                    ffmpegArgs = $"-y -f gdigrab -framerate 10 -i desktop -c:v libx264 -preset ultrafast -crf 28 -pix_fmt yuv420p -movflags frag_keyframe+empty_moov \"{_screenRecordingPath}\"";
                }

                _screenRecordProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                _screenRecordProcess.Start();
                _isScreenRecording = true;
                StartRecBlink();
                System.Diagnostics.Debug.WriteLine($"Screen recording started: {_screenRecordingPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start screen recording: {ex.Message}");
                RecStatusText.Text = "NO REC";
            }
        }

        /// <summary>
        /// Detects an available audio output device for recording system audio.
        /// Looks for "Stereo Mix", "What U Hear", or any audio capture device.
        /// </summary>
        private string? FindAudioDevice(string ffmpegPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-list_devices true -f dshow -i dummy",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);
                if (!process.HasExited) process.Kill();

                // Parse output for audio devices
                var lines = output.Split('\n');
                bool inAudioSection = false;
                var preferredDevices = new[] { "Stereo Mix", "What U Hear", "Loopback", "CABLE Output" };
                string? fallbackDevice = null;

                foreach (var line in lines)
                {
                    if (line.Contains("DirectShow audio devices"))
                    {
                        inAudioSection = true;
                        continue;
                    }
                    if (inAudioSection && line.Contains("\""))
                    {
                        var start = line.IndexOf('"') + 1;
                        var end = line.IndexOf('"', start);
                        if (end > start)
                        {
                            var deviceName = line.Substring(start, end - start);
                            
                            // Prefer virtual/loopback audio devices
                            foreach (var preferred in preferredDevices)
                            {
                                if (deviceName.Contains(preferred, StringComparison.OrdinalIgnoreCase))
                                    return deviceName;
                            }
                            
                            // Use any microphone as fallback
                            if (deviceName.Contains("Microphone", StringComparison.OrdinalIgnoreCase) ||
                                deviceName.Contains("Mic", StringComparison.OrdinalIgnoreCase))
                            {
                                fallbackDevice = deviceName;
                            }
                        }
                    }
                }
                return fallbackDevice;
            }
            catch
            {
                return null;
            }
        }

        private async Task StopScreenRecordingAsync()
        {
            try
            {
                _isScreenRecording = false;
                StopRecBlink();
                try { RecStatusText.Text = "SAVING..."; } catch { }

                if (_screenRecordProcess != null && !_screenRecordProcess.HasExited)
                {
                    try
                    {
                        _screenRecordProcess.StandardInput.Write("q");
                        _screenRecordProcess.StandardInput.Flush();
                        _screenRecordProcess.StandardInput.Close();
                    }
                    catch { }

                    await Task.Run(() => _screenRecordProcess.WaitForExit(30000));
                    if (!_screenRecordProcess.HasExited)
                    {
                        _screenRecordProcess.Kill();
                        await Task.Delay(500);
                    }
                    _screenRecordProcess.Dispose();
                    _screenRecordProcess = null;
                }

                if (System.IO.File.Exists(_screenRecordingPath))
                {
                    var fileInfo = new System.IO.FileInfo(_screenRecordingPath);
                    System.Diagnostics.Debug.WriteLine($"Screen recording saved: {_screenRecordingPath} ({fileInfo.Length / 1024}KB)");
                    if (fileInfo.Length > 0)
                    {
                        var fileName = System.IO.Path.GetFileName(_screenRecordingPath);
                        await UploadRecordingAsync(_screenRecordingPath, fileName, _recordingSessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping screen recording: {ex.Message}");
            }
        }

        private async Task UploadRecordingAsync(string filePath, string fileName, int sessionId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    client.BaseAddress = new Uri($"{AppConfig.ServerUrl}/");
                    using (var content = new MultipartFormDataContent())
                    {
                        var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                        fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("video/mp4");
                        content.Add(fileContent, "file", fileName);
                        var response = await client.PostAsync($"api/materials/session/{sessionId}", content);
                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"Recording uploaded successfully: {fileName}");
                        }
                        else
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"Recording upload failed! Status: {response.StatusCode} {errorBody}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Recording upload error: {ex.Message}");
            }
        }

        private string? FindFFmpeg()
        {
            var locations = new[]
            {
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
                @"C:\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            };

            foreach (var loc in locations)
            {
                if (System.IO.File.Exists(loc)) return loc;
            }

            try
            {
                var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
                foreach (var dir in pathDirs)
                {
                    var ffmpegPath = System.IO.Path.Combine(dir.Trim(), "ffmpeg.exe");
                    if (System.IO.File.Exists(ffmpegPath)) return ffmpegPath;
                }
            }
            catch { }

            return null;
        }

        private void StartRecBlink()
        {
            _recBlinkTimer?.Stop();
            _recBlinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            bool visible = true;
            _recBlinkTimer.Tick += (s, e) =>
            {
                visible = !visible;
                RecDot.Opacity = visible ? 1.0 : 0.2;
            };
            _recBlinkTimer.Start();
            RecDot.Opacity = 1.0;
            RecStatusText.Text = "RECORDING";
        }

        private void StopRecBlink()
        {
            _recBlinkTimer?.Stop();
            RecDot.Opacity = 1.0;
        }
    }
}

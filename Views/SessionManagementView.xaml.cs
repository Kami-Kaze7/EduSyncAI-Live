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
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Drawing = System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace EduSyncAI
{
    public partial class SessionManagementView : UserControl
    {
        private readonly GeminiFaceRecognitionService _faceService;
        private readonly DatabaseService _dbService;
        private DispatcherTimer _cameraTimer;
        private DispatcherTimer _recognitionTimer;
        private VideoCapture? _capture;
        private bool _isCameraActive = false;
        private bool _isRecognizing = false;
        private HashSet<int> _markedStudents = new HashSet<int>();
        
        // Live streaming state
        private bool _isStreamingFrames = false;
        private int _liveSessionId = 0;
        private int _frameCounter = 0;
        private static readonly HttpClient _streamClient = new HttpClient();

        // Screen recording state
        private Process? _screenRecordProcess;
        private bool _isScreenRecording = false;
        private string _screenRecordingPath = "";
        private int _recordingSessionId = 0;
        private DispatcherTimer? _recBlinkTimer;

        public SessionManagementView()
        {
            InitializeComponent();
            _faceService = new GeminiFaceRecognitionService();
            _dbService = new DatabaseService();
            
            // Subscribe to DataContext changes to handle session end
            DataContextChanged += SessionManagementView_DataContextChanged;
        }

        private void SessionManagementView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (e.OldValue is SessionManagementViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            // Subscribe to new ViewModel
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
                        // Session started → start screen recording
                        StartScreenRecording(viewModel.ActiveSession.Id);
                    }
                    else
                    {
                        // Session ended → stop camera and recording
                        if (_isCameraActive) StopCameraIfActive();
                        if (_isScreenRecording)
                        {
                            // Await the stop to ensure FFmpeg finalizes the file
                            await StopScreenRecordingAsync();
                        }
                    }
                }
            }
        }

        public void StopCameraIfActive()
        {
            if (_isCameraActive)
            {
                _ = StopCameraAsync();
            }
        }

        private void OpenWhiteboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewModel = DataContext as SessionManagementViewModel;
                if (viewModel?.ActiveSession != null)
                {
                    var whiteboard = new WhiteboardWindow(viewModel.ActiveSession.Id);
                    whiteboard.Show();
                }
                else
                {
                    MessageBox.Show("No active session. Please start a session first.",
                        "No Active Session", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening whiteboard:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Whiteboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FacialStatusText.Text = "Starting native camera...";
                StartButton.Visibility = Visibility.Collapsed;
                StopButton.Visibility = Visibility.Visible;
                
                _capture = new VideoCapture(0); // Open default camera
                if (!_capture.IsOpened)
                {
                    MessageBox.Show("Failed to open camera. Please check if camera is available.", 
                        "Camera Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    FacialStatusText.Text = "Camera failed to start";
                    StartButton.Visibility = Visibility.Visible;
                    StopButton.Visibility = Visibility.Collapsed;
                    return;
                }

                _isCameraActive = true;
                _markedStudents.Clear();
                FacialStatusText.Text = "🔴 LIVE: Native recognition active. Students will be marked as they appear.";

                // Start camera preview timer (fast - 33ms for ~30fps)
                _cameraTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _cameraTimer.Tick += (s, args) => UpdateCameraPreview();
                _cameraTimer.Start();

                // Start recognition timer (slower - every 10 seconds)
                _recognitionTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };
                _recognitionTimer.Tick += async (s, args) => await AutoRecognizeFaces();
                _recognitionTimer.Start();

                MessageBox.Show("✅ Native recognition started!\n\nSnapshots will be taken every 10 seconds and matched against student profile pictures.", 
                    "Recognition Active", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting camera: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FacialStatusText.Text = "Error starting camera";
                StartButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateCameraPreview()
        {
            try
            {
                if (_capture == null || !_capture.IsOpened) return;

                using (var frame = _capture.QueryFrame())
                {
                    if (frame != null)
                    {
                        CameraPreview.Source = MatToBitmapSource(frame);
                        
                        // Send frame to Web API for PiP streaming (~5fps: every 6th frame from 30fps)
                        _frameCounter++;
                        if (_isStreamingFrames && _frameCounter % 6 == 0)
                        {
                            _ = SendFrameAsync(frame);
                        }
                    }
                }
            }
            catch
            {
                // Ignore preview errors
            }
        }

        private async Task AutoRecognizeFaces()
        {
            if (_isRecognizing || _capture == null || !_capture.IsOpened)
                return;

            try
            {
                _isRecognizing = true;
                if (DataContext is not SessionManagementViewModel viewModel || viewModel.ActiveSession == null) return;

                FacialStatusText.Text = "📸 Taking snapshot for recognition...";

                string base64Snapshot = "";
                using (var frame = _capture.QueryFrame())
                {
                    if (frame == null) return;
                    
                    // Convert Mat to Base64
                    using (var mem = new MemoryStream())
                    {
                        using (Drawing.Bitmap bitmap = frame.ToBitmap())
                        {
                            bitmap.Save(mem, ImageFormat.Jpeg);
                            base64Snapshot = Convert.ToBase64String(mem.ToArray());
                        }
                    }
                }

                if (string.IsNullOrEmpty(base64Snapshot)) return;

                FacialStatusText.Text = "🔍 Matching faces against profile pictures...";

                // Recognize faces natively
                var result = await _faceService.RecognizeFacesAsync(viewModel.ActiveSession.Id, base64Snapshot);

                if (!result.Success)
                {
                    FacialStatusText.Text = $"🔴 LIVE: Recognition error - {result.Error}";
                    return;
                }

                // Process new matches only
                var newMatches = result.Matches.Where(m => !_markedStudents.Contains(m.StudentId)).ToList();

                if (newMatches.Count > 0)
                {
                    // Mark attendance for new students
                    var markedCount = await _faceService.MarkAttendanceAsync(viewModel.ActiveSession.Id, newMatches);

                    if (markedCount > 0)
                    {
                        // Add to marked list
                        foreach (var match in newMatches)
                        {
                            _markedStudents.Add(match.StudentId);

                            // Add to UI
                            var studentPanel = new Border
                            {
                                Background = System.Windows.Media.Brushes.White,
                                Padding = new Thickness(10),
                                Margin = new Thickness(0, 0, 0, 5),
                                CornerRadius = new CornerRadius(3)
                            };

                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            var nameText = new TextBlock
                            {
                                Text = match.Name,
                                FontWeight = FontWeights.SemiBold
                            };
                            Grid.SetColumn(nameText, 0);

                            var confidencePanel = new StackPanel
                            {
                                Orientation = System.Windows.Controls.Orientation.Horizontal
                            };
                            var confidenceText = new TextBlock
                            {
                                Text = $"{(match.Confidence * 100):F0}%",
                                Foreground = System.Windows.Media.Brushes.Green,
                                FontWeight = FontWeights.Bold,
                                Margin = new Thickness(0, 0, 5, 0)
                            };
                            var checkText = new TextBlock
                            {
                                Text = "✓",
                                Foreground = System.Windows.Media.Brushes.Green,
                                FontWeight = FontWeights.Bold
                            };
                            confidencePanel.Children.Add(confidenceText);
                            confidencePanel.Children.Add(checkText);
                            Grid.SetColumn(confidencePanel, 1);

                            grid.Children.Add(nameText);
                            grid.Children.Add(confidencePanel);
                            studentPanel.Child = grid;

                            RecognizedStudentsList.Children.Insert(0, studentPanel);
                        }

                        // Play success sound
                        System.Media.SystemSounds.Exclamation.Play();
                        
                        // Update UI Count
                        viewModel.ActiveSession.AttendanceCount += markedCount;
                        _dbService.UpdateClassSession(viewModel.ActiveSession);
                        
                        RecognizedStudentsPanel.Visibility = Visibility.Visible;
                        FacialStatusText.Text = $"✅ {_markedStudents.Count} student(s) marked present. Scanning continues...";
                    }
                    else
                    {
                        FacialStatusText.Text = "🔴 LIVE: Recognition succeeded, but database save failed.";
                    }
                }
                else
                {
                    // No new matches
                    if (_markedStudents.Count > 0)
                    {
                        FacialStatusText.Text = $"🔴 LIVE: {_markedStudents.Count} student(s) marked. Scanning for more...";
                    }
                    else
                    {
                        FacialStatusText.Text = "🔴 LIVE: Scanning... No students recognized yet.";
                    }
                }
            }
            catch (Exception ex)
            {
                FacialStatusText.Text = $"⚠️ Error: {ex.Message}. Continuing to scan...";
            }
            finally
            {
                _isRecognizing = false;
            }
        }

        private async void StopCamera_Click(object sender, RoutedEventArgs e)
        {
            await StopCameraAsync();
        }

        private async Task StopCameraAsync()
        {
            try
            {
                _cameraTimer?.Stop();
                _cameraTimer = null;
                _recognitionTimer?.Stop();
                _recognitionTimer = null;
                _isCameraActive = false;
                _isRecognizing = false;

                if (_capture != null)
                {
                    _capture.Dispose();
                    _capture = null;
                }
                
                CameraPreview.Source = null;
                StartButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Collapsed;

                var totalMarked = _markedStudents.Count;
                FacialStatusText.Text = $"Recognition stopped. Total students marked: {totalMarked}";

                if (totalMarked > 0)
                {
                    MessageBox.Show($"✅ Recognition session ended!\n\nTotal students marked present: {totalMarked}", 
                        "Session Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping camera: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapSource MatToBitmapSource(Mat mat)
        {
            using (Drawing.Bitmap bitmap = mat.ToBitmap())
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private BitmapImage Base64ToImage(string base64String)
        {
            // Remove data:image prefix if present
            if (base64String.Contains(","))
            {
                base64String = base64String.Split(',')[1];
            }

            byte[] imageBytes = Convert.FromBase64String(base64String);
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }

        // ==================== FRAME STREAMING ====================

        private async Task SendFrameAsync(Mat frame)
        {
            try
            {
                using (Drawing.Bitmap bitmap = frame.ToBitmap())
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    var content = new ByteArrayContent(ms.ToArray());
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    await _streamClient.PostAsync($"http://localhost:5152/api/stream/{_liveSessionId}/frame", content);
                }
            }
            catch
            {
                // Ignore frame send errors silently
            }
        }

        // ==================== LIVE CLASSROOM ====================

        private async void GoLive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewModel = DataContext as SessionManagementViewModel;
                if (viewModel?.ActiveSession == null)
                {
                    MessageBox.Show("No active session. Please start a session first.",
                        "No Active Session", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var authService = new AuthenticationService();
                var lecturer = authService.GetCurrentLecturer();
                var lecturerName = lecturer?.FullName ?? "Lecturer";
                var lecturerId = lecturer?.Id ?? 0;
                var courseName = viewModel.ActiveSession.CourseName ?? "Class";

                GoLiveBtn.Visibility = Visibility.Collapsed;
                StopLiveBtn.Visibility = Visibility.Visible;
                LivePanel.Visibility = Visibility.Visible;

                await LivePanel.StartBroadcastAsync(
                    viewModel.ActiveSession.Id,
                    lecturerName,
                    courseName,
                    lecturerId);

                // Enable PiP frame streaming if camera is running
                _liveSessionId = viewModel.ActiveSession.Id;
                _isStreamingFrames = _isCameraActive;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting broadcast:\n\n{ex.Message}",
                    "Broadcast Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GoLiveBtn.Visibility = Visibility.Visible;
                StopLiveBtn.Visibility = Visibility.Collapsed;
                LivePanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void StopLive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isStreamingFrames = false;
                await LivePanel.StopBroadcastAsync();
                GoLiveBtn.Visibility = Visibility.Visible;
                StopLiveBtn.Visibility = Visibility.Collapsed;
                LivePanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping broadcast: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Create output directory and file path
                var recordingsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Recordings");
                System.IO.Directory.CreateDirectory(recordingsDir);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _screenRecordingPath = System.IO.Path.Combine(recordingsDir, $"Session_{sessionId}_{timestamp}.mp4");
                _recordingSessionId = sessionId;

                // Start FFmpeg gdigrab — captures entire desktop
                // -movflags frag_keyframe+empty_moov writes the moov atom at start and
                // embeds keyframe metadata inline, ensuring a playable file even if interrupted
                _screenRecordProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-y -f gdigrab -framerate 10 -i desktop -c:v libx264 -preset ultrafast -crf 28 -pix_fmt yuv420p -movflags frag_keyframe+empty_moov \"{_screenRecordingPath}\"",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                _screenRecordProcess.Start();
                _isScreenRecording = true;

                // Start REC indicator blink
                StartRecBlink();

                System.Diagnostics.Debug.WriteLine($"Screen recording started: {_screenRecordingPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start screen recording: {ex.Message}");
                RecStatusText.Text = "NO REC";
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
                    // Close stdin to signal FFmpeg to stop (EOF on input)
                    // Then send 'q' as backup before closing
                    try
                    {
                        _screenRecordProcess.StandardInput.Write("q");
                        _screenRecordProcess.StandardInput.Flush();
                        _screenRecordProcess.StandardInput.Close();
                    }
                    catch { }

                    // Wait for FFmpeg to finalize the MP4 (max 30 seconds)
                    await Task.Run(() => _screenRecordProcess.WaitForExit(30000));
                    if (!_screenRecordProcess.HasExited)
                    {
                        _screenRecordProcess.Kill();
                        await Task.Delay(500);
                    }
                    _screenRecordProcess.Dispose();
                    _screenRecordProcess = null;
                }

                // Upload the recording
                if (System.IO.File.Exists(_screenRecordingPath))
                {
                    var fileInfo = new System.IO.FileInfo(_screenRecordingPath);
                    System.Diagnostics.Debug.WriteLine($"Screen recording saved: {_screenRecordingPath} ({fileInfo.Length / 1024}KB)");
                    
                    if (fileInfo.Length > 0)
                    {
                        var fileName = System.IO.Path.GetFileName(_screenRecordingPath);
                        await UploadRecordingAsync(_screenRecordingPath, fileName, _recordingSessionId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Recording file is empty — FFmpeg may have failed.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Recording file not found: {_screenRecordingPath}");
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
                    client.BaseAddress = new Uri("http://localhost:5152/");

                    using (var content = new MultipartFormDataContent())
                    {
                        var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                        fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("video/mp4");
                        content.Add(fileContent, "file", fileName);

                        var response = await client.PostAsync($"api/materials/session/{sessionId}", content);
                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"Recording uploaded: {fileName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error uploading recording: {ex.Message}");
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

            // Check PATH
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

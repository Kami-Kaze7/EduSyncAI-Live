using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace EduSyncAI
{
    public partial class LiveClassroomPanel : UserControl
    {
        private int _sessionId;
        private string _roomName = "";
        private string _lecturerName = "";
        private string _courseName = "";
        private bool _isLive = false;

        public event EventHandler? BroadcastEnded;

        public LiveClassroomPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize and start the Jitsi Meet video conference
        /// </summary>
        public async Task StartBroadcastAsync(int sessionId, string lecturerName, string courseName, int lecturerId)
        {
            _sessionId = sessionId;
            _lecturerName = lecturerName;
            _courseName = courseName;

            SessionInfoText.Text = $"{courseName} — Session #{sessionId}";
            StatusText.Text = "Starting live broadcast...";

            try
            {
                // 1. Notify Web API that a stream is starting
                _roomName = await NotifyStreamStartAsync(sessionId, lecturerId, courseName, lecturerName);

                // 2. Initialize WebView2
                await JitsiWebView.EnsureCoreWebView2Async();

                // 3. Grant camera/mic permissions automatically
                JitsiWebView.CoreWebView2.PermissionRequested += (s, args) =>
                {
                    if (args.PermissionKind == CoreWebView2PermissionKind.Camera ||
                        args.PermissionKind == CoreWebView2PermissionKind.Microphone)
                    {
                        args.State = CoreWebView2PermissionState.Allow;
                    }
                };

                // 4. Navigate directly to Jitsi Meet HTTPS URL (fixes WebRTC issue)
                var jitsiUrl = BuildJitsiUrl(_roomName, lecturerName);
                JitsiWebView.CoreWebView2.Navigate(jitsiUrl);

                _isLive = true;
                StatusText.Text = $"🔴 Broadcasting live — Room: {_roomName}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to start live broadcast:\n\n{ex.Message}", 
                    "Broadcast Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Build the Jitsi Meet URL with config parameters in the hash
        /// Navigating directly to https://meet.jit.si ensures WebRTC works (secure context)
        /// </summary>
        private string BuildJitsiUrl(string roomName, string displayName)
        {
            // Jitsi Meet supports config via URL hash parameters
            // See: https://jitsi.github.io/handbook/docs/dev-guide/dev-guide-iframe/
            var safeDisplayName = Uri.EscapeDataString(displayName + " (Lecturer)");
            var safeSubject = Uri.EscapeDataString(_courseName + " — Live Class");

            return $"https://meet.jit.si/{roomName}" +
                   $"#config.prejoinPageEnabled=false" +
                   $"&config.startWithAudioMuted=false" +
                   $"&config.startWithVideoMuted=false" +
                   $"&config.disableDeepLinking=true" +
                   $"&config.hideConferenceSubject=false" +
                   $"&config.subject=%22{safeSubject}%22" +
                   $"&userInfo.displayName=%22{safeDisplayName}%22" +
                   $"&interfaceConfig.SHOW_JITSI_WATERMARK=false" +
                   $"&interfaceConfig.SHOW_WATERMARK_FOR_GUESTS=false" +
                   $"&interfaceConfig.TOOLBAR_ALWAYS_VISIBLE=true" +
                   $"&interfaceConfig.TOOLBAR_BUTTONS=%5B%22microphone%22,%22camera%22,%22chat%22,%22raisehand%22,%22tileview%22%5D";
        }

        /// <summary>
        /// Notify the Web API to register the stream as active
        /// </summary>
        private async Task<string> NotifyStreamStartAsync(int sessionId, int lecturerId, string courseName, string lecturerName)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri($"{AppConfig.ServerUrl}/");

            var payload = new
            {
                SessionId = sessionId,
                LecturerId = lecturerId,
                CourseName = courseName,
                LecturerName = lecturerName
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/stream/start", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseJson);
                return result.RootElement.GetProperty("roomName").GetString() ?? $"EduSync-{sessionId}";
            }

            throw new Exception("Failed to register stream with server");
        }

        /// <summary>
        /// Notify the Web API to mark the stream as ended
        /// </summary>
        private async Task NotifyStreamStopAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri($"{AppConfig.ServerUrl}/");

                var payload = new { SessionId = _sessionId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync("api/stream/stop", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LiveStream] Error notifying stream stop: {ex.Message}");
            }
        }

        private async void EndBroadcast_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "End the live broadcast? All remote students will be disconnected.",
                "End Broadcast",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await StopBroadcastAsync();
            }
        }

        public async Task StopBroadcastAsync()
        {
            if (!_isLive) return;

            _isLive = false;
            StatusText.Text = "Ending broadcast...";

            // Notify the server
            await NotifyStreamStopAsync();

            // Navigate to a blank page
            JitsiWebView.CoreWebView2?.Navigate("about:blank");

            StatusText.Text = "Broadcast ended";
            BroadcastEnded?.Invoke(this, EventArgs.Empty);
        }
    }
}

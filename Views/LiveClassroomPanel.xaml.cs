using System;
using System.IO;
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
        private bool _webViewInitialized = false;

        public event EventHandler? BroadcastEnded;

        public LiveClassroomPanel()
        {
            InitializeComponent();
        }

        public async Task StartBroadcastAsync(int sessionId, string lecturerName, string courseName, int lecturerId)
        {
            _sessionId = sessionId;
            _lecturerName = lecturerName;
            _courseName = courseName;

            SessionInfoText.Text = $"{courseName} — Session #{sessionId}";
            StatusText.Text = "Starting live broadcast...";

            try
            {
                _roomName = await NotifyStreamStartAsync(sessionId, lecturerId, courseName, lecturerName);

                if (!_webViewInitialized)
                {
                    StatusText.Text = "Initializing video engine...";
                    await InitializeWebView2Async();
                    _webViewInitialized = true;
                }

                StatusText.Text = "Connecting to live classroom...";

                // Direct navigation to Jitsi room URL
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

        private async Task InitializeWebView2Async()
        {
            var userDataFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Data", "WebView2_LiveClassroom");
            Directory.CreateDirectory(userDataFolder);

            var envOptions = new CoreWebView2EnvironmentOptions(
                "--autoplay-policy=no-user-gesture-required --use-fake-ui-for-media-stream");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, envOptions);
            await JitsiWebView.EnsureCoreWebView2Async(env);

            var coreWebView = JitsiWebView.CoreWebView2;

            // Auto-grant ALL permissions
            coreWebView.PermissionRequested += (s, args) =>
            {
                args.State = CoreWebView2PermissionState.Allow;
            };

            // Block all popups — just suppress them, do NOT navigate
            coreWebView.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
            };

            // CSS injection after page loads to remove branding
            coreWebView.NavigationCompleted += async (s, args) =>
            {
                if (!args.IsSuccess || !_isLive) return;
                try
                {
                    await Task.Delay(3000);
                    if (JitsiWebView.CoreWebView2 != null)
                    {
                        await JitsiWebView.CoreWebView2.ExecuteScriptAsync(@"
                            var s = document.createElement('style');
                            s.textContent = '.leftwatermark,.rightwatermark,.watermark,[class*=""oink""]{display:none!important}';
                            document.head.appendChild(s);
                        ");
                    }
                }
                catch { }
            };

            coreWebView.Settings.AreDefaultContextMenusEnabled = false;
            coreWebView.Settings.AreBrowserAcceleratorKeysEnabled = false;
            coreWebView.Settings.IsStatusBarEnabled = false;
        }

        private string BuildJitsiUrl(string roomName, string displayName)
        {
            var safeDisplayName = Uri.EscapeDataString(displayName + " (Lecturer)");
            var safeSubject = Uri.EscapeDataString(_courseName + " — Live Class");

            return $"https://meet.viicsoft.dev/{roomName}" +
                   $"#config.prejoinConfig.enabled=false" +
                   $"&config.startWithAudioMuted=false" +
                   $"&config.startWithVideoMuted=false" +
                   $"&config.disableDeepLinking=true" +
                   $"&config.subject=%22{safeSubject}%22" +
                   $"&config.enableInsecureRoomNameWarning=false" +
                   $"&config.enableClosePage=false" +
                   $"&config.disableInviteFunctions=true" +
                   $"&config.p2p.enabled=true" +
                   $"&userInfo.displayName=%22{safeDisplayName}%22" +
                   $"&interfaceConfig.SHOW_JITSI_WATERMARK=false" +
                   $"&interfaceConfig.SHOW_WATERMARK_FOR_GUESTS=false" +
                   $"&interfaceConfig.SHOW_BRAND_WATERMARK=false" +
                   $"&interfaceConfig.SHOW_POWERED_BY=false" +
                   $"&interfaceConfig.DEFAULT_LOGO_URL=''" +
                   $"&interfaceConfig.DEFAULT_WELCOME_PAGE_LOGO_URL=''" +
                   $"&interfaceConfig.JITSI_WATERMARK_LINK=''" +
                   $"&interfaceConfig.APP_NAME=%22EduSync%20AI%22" +
                   $"&interfaceConfig.NATIVE_APP_NAME=%22EduSync%20AI%22" +
                   $"&interfaceConfig.PROVIDER_NAME=%22EduSync%20AI%22" +
                   $"&interfaceConfig.MOBILE_APP_PROMO=false" +
                   $"&interfaceConfig.TOOLBAR_ALWAYS_VISIBLE=true";
        }

        private async Task<string> NotifyStreamStartAsync(int sessionId, int lecturerId, string courseName, string lecturerName)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri($"{AppConfig.ServerUrl}/");
            var payload = new { SessionId = sessionId, LecturerId = lecturerId, CourseName = courseName, LecturerName = lecturerName };
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
                "End Broadcast", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) await StopBroadcastAsync();
        }

        public async Task StopBroadcastAsync()
        {
            if (!_isLive) return;
            _isLive = false;
            StatusText.Text = "Ending broadcast...";
            await NotifyStreamStopAsync();
            try
            {
                if (JitsiWebView.CoreWebView2 != null)
                {
                    await JitsiWebView.CoreWebView2.ExecuteScriptAsync(
                        "if(typeof APP!=='undefined'&&APP.conference){APP.conference.hangup();}");
                    await Task.Delay(500);
                    JitsiWebView.CoreWebView2.Navigate("about:blank");
                }
            }
            catch { }
            StatusText.Text = "Broadcast ended";
            BroadcastEnded?.Invoke(this, EventArgs.Empty);
        }
    }
}

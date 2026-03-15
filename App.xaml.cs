using System;
using System.Threading.Tasks;
using System.Windows;

namespace EduSyncAI
{
    public partial class App : Application
    {
        private ServiceManager? _serviceManager;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize database on startup
                new DatabaseService();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Database initialization failed:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Show splash screen and start services
            var splash = new SplashWindow();
            splash.Show();

            try
            {
                _serviceManager = new ServiceManager();
                _serviceManager.StatusChanged += (msg) => splash.UpdateStatus(msg);
                _serviceManager.ErrorOccurred += (msg) => splash.ShowError(msg);

                await _serviceManager.StartAllAsync();

                splash.MarkComplete();
                await Task.Delay(800); // Brief pause to show "Ready!" message
            }
            catch (Exception ex)
            {
                splash.ShowError($"Startup error: {ex.Message}");
                await Task.Delay(2000);
            }

            splash.Close();

            // Show the login window (original entry point)
            var loginWindow = new WelcomeWindow();
            loginWindow.Show();

            // Set the main window so the app doesn't close when splash closes
            MainWindow = loginWindow;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop all background services
            _serviceManager?.StopAll();
            _serviceManager?.Dispose();
            base.OnExit(e);
        }
    }
}

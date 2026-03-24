using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace EduSyncAI
{
    public partial class App : Application
    {
        private ServiceManager? _serviceManager;

        // Portable crash log path — works on any PC
        private static readonly string CrashLogPath = Path.Combine(AppConfig.DataDir, "crash_log.txt");

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Prevent premature shutdown during async startup
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            base.OnStartup(e);
            
            try { File.AppendAllText(CrashLogPath, $"\n===STARTUP ENTERED [{DateTime.Now}]===\n"); } catch { }
            // Global crash logger
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                try { File.AppendAllText(CrashLogPath, $"\n\n===UNHANDLED [{DateTime.Now}]===\n{ex?.ToString()}"); } catch { }
            };
            DispatcherUnhandledException += (s, args) =>
            {
                try { File.AppendAllText(CrashLogPath, $"\n\n===DISPATCHER [{DateTime.Now}]===\n{args.Exception.ToString()}"); } catch { }
                MessageBox.Show(args.Exception.ToString(), "EduSync Crash", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                try { File.AppendAllText(CrashLogPath, $"\n\n===TASK [{DateTime.Now}]===\n{args.Exception.ToString()}"); } catch { }
            };
            try
            {
                // Initialize database on startup
                new DatabaseService();
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(CrashLogPath, $"\n\n===DB INIT [{DateTime.Now}]===\n{ex}"); } catch { }
                MessageBox.Show(
                    $"Database initialization failed:\n\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            try
            {
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
                    await Task.Delay(800);
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(CrashLogPath, $"\n\n===SERVICE [{DateTime.Now}]===\n{ex}"); } catch { }
                    splash.ShowError($"Startup error: {ex.Message}");
                    await Task.Delay(2000);
                }

                splash.Close();

                // Show the login window
                var loginWindow = new WelcomeWindow();
                loginWindow.Show();
                MainWindow = loginWindow;
                ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(CrashLogPath, $"\n\n===STARTUP [{DateTime.Now}]===\n{ex}"); } catch { }
                MessageBox.Show($"Startup Error:\n\n{ex.Message}\n\nSee crash_log.txt for details.", "EduSync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

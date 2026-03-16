using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace EduSyncAI
{
    public partial class App : Application
    {
        private ServiceManager? _serviceManager;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Prevent premature shutdown during async startup
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            base.OnStartup(e);
            
            Console.Error.WriteLine("[EduSync] OnStartup entered");
            File.AppendAllText(@"c:\EduSyncAI\crash_log.txt", $"\n===STARTUP ENTERED [{DateTime.Now}]===\n");
            // Global crash logger
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "crash_log.txt");
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                File.AppendAllText(logPath, $"\n\n===UNHANDLED [{DateTime.Now}]===\n{ex?.ToString()}");
            };
            DispatcherUnhandledException += (s, args) =>
            {
                File.AppendAllText(logPath, $"\n\n===DISPATCHER [{DateTime.Now}]===\n{args.Exception.ToString()}");
                MessageBox.Show(args.Exception.ToString(), "EduSync Crash", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                File.AppendAllText(logPath, $"\n\n===TASK [{DateTime.Now}]===\n{args.Exception.ToString()}");
            };
            try
            {
                // Initialize database on startup
                new DatabaseService();
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"c:\EduSyncAI\crash_log.txt", $"\n\n===DB INIT [{DateTime.Now}]===\n{ex}");
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
                    File.AppendAllText(@"c:\EduSyncAI\crash_log.txt", $"\n\n===SERVICE [{DateTime.Now}]===\n{ex}");
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
                File.AppendAllText(@"c:\EduSyncAI\crash_log.txt", $"\n\n===STARTUP [{DateTime.Now}]===\n{ex}");
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

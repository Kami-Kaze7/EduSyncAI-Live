using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace EduSyncAI
{
    /// <summary>
    /// Manages the lifecycle of all background services:
    /// - ASP.NET Core WebAPI (port 5152)
    /// - Next.js Web Frontend (port 3000)
    /// - Python Flask Face Recognition (port 5001)
    /// </summary>
    public class ServiceManager : IDisposable
    {
        private Process? _webApiProcess;
        private Process? _nextJsProcess;
        private Process? _pythonProcess;
        private readonly string _basePath;
        private bool _disposed;

        public event Action<string>? StatusChanged;
        public event Action<string>? ErrorOccurred;

        public ServiceManager()
        {
            // Determine base path — works for both dev and installed scenarios
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Starts all services and waits for them to become healthy.
        /// </summary>
        public async Task StartAllAsync()
        {
            await StartWebApiAsync();
            await StartNextJsAsync();
            await StartPythonBackendAsync();
        }

        // ==================== WEB API ====================

        private async Task StartWebApiAsync()
        {
            StatusChanged?.Invoke("Starting Web API server...");

            // Look for published WebAPI first, then fall back to dotnet run
            var publishedExe = Path.Combine(_basePath, "webapi", "EduSyncAI.WebAPI.exe");
            var projectPath = FindProjectPath("EduSyncAI.WebAPI", "EduSyncAI.WebAPI.csproj");

            if (File.Exists(publishedExe))
            {
                // Use published self-contained EXE
                _webApiProcess = StartProcess(publishedExe, "", Path.GetDirectoryName(publishedExe)!);
            }
            else if (projectPath != null)
            {
                // Development mode — use dotnet run
                _webApiProcess = StartProcess("dotnet", $"run --project \"{projectPath}\" --no-launch-profile", Path.GetDirectoryName(projectPath)!);
            }
            else
            {
                ErrorOccurred?.Invoke("WebAPI project not found. Web dashboards will not work.");
                return;
            }

            // Wait for WebAPI to be healthy
            var healthy = await WaitForHealthAsync("http://localhost:5152/api/sessions", 30);
            if (healthy)
            {
                StatusChanged?.Invoke("Web API started ✓");
            }
            else
            {
                ErrorOccurred?.Invoke("Web API failed to start within 30 seconds.");
            }
        }

        // ==================== NEXT.JS ====================

        private async Task StartNextJsAsync()
        {
            StatusChanged?.Invoke("Starting Web Dashboard...");

            var webDir = FindDirectoryPath("edusync-web");
            if (webDir == null)
            {
                ErrorOccurred?.Invoke("Web frontend folder not found. Student/Lecturer dashboards will not work.");
                return;
            }

            var nodeExe = FindNode();
            var npxCmd = FindNpx();

            if (string.IsNullOrEmpty(nodeExe))
            {
                ErrorOccurred?.Invoke("Node.js not found. Web dashboards will not work.");
                return;
            }

            // Check if production build exists
            var nextBuildDir = Path.Combine(webDir, ".next");
            var nodeModules = Path.Combine(webDir, "node_modules");

            if (!Directory.Exists(nodeModules))
            {
                StatusChanged?.Invoke("Installing web dependencies (first run)...");
                var npmCmd = Path.Combine(Path.GetDirectoryName(nodeExe)!, "npm.cmd");
                if (!File.Exists(npmCmd)) npmCmd = "npm";
                var npmInstall = StartProcess("cmd.exe", $"/c \"{npmCmd}\" install", webDir);
                npmInstall.WaitForExit(120000); // max 2 minutes
            }

            if (Directory.Exists(nextBuildDir))
            {
                // Production mode — use next start via bundled npx
                _nextJsProcess = StartProcess("cmd.exe", $"/c \"{npxCmd}\" next start -p 3000", webDir);
            }
            else
            {
                // Dev mode — use npm run dev
                var npmCmd = Path.Combine(Path.GetDirectoryName(nodeExe)!, "npm.cmd");
                if (!File.Exists(npmCmd)) npmCmd = "npm";
                _nextJsProcess = StartProcess("cmd.exe", $"/c \"{npmCmd}\" run dev", webDir);
            }

            var healthy = await WaitForHealthAsync("http://localhost:3000", 30);
            if (healthy)
            {
                StatusChanged?.Invoke("Web Dashboard started ✓");
            }
            else
            {
                ErrorOccurred?.Invoke("Web Dashboard failed to start.");
            }
        }

        // ==================== PYTHON BACKEND ====================

        private async Task StartPythonBackendAsync()
        {
            StatusChanged?.Invoke("Starting Face Recognition service...");

            var backendDir = FindDirectoryPath("backend");
            if (backendDir == null)
            {
                ErrorOccurred?.Invoke("Python backend folder not found. Face recognition will not work.");
                return;
            }

            var pythonExe = FindPython();
            if (string.IsNullOrEmpty(pythonExe))
            {
                ErrorOccurred?.Invoke("Python not found. Face recognition will not work.\nInstall Python 3.10+ from https://python.org");
                return;
            }

            // Install pip dependencies if needed
            var requirementsFile = Path.Combine(backendDir, "requirements_facial.txt");
            if (File.Exists(requirementsFile))
            {
                StatusChanged?.Invoke("Checking Python dependencies...");
                var pipInstall = StartProcess(pythonExe, $"-m pip install -r \"{requirementsFile}\" --quiet", backendDir);
                pipInstall.WaitForExit(60000);
            }

            var scriptPath = Path.Combine(backendDir, "gemini_face_service.py");
            if (!File.Exists(scriptPath))
            {
                ErrorOccurred?.Invoke("gemini_face_service.py not found.");
                return;
            }

            _pythonProcess = StartProcess(pythonExe, $"\"{scriptPath}\"", backendDir);

            var healthy = await WaitForHealthAsync("http://127.0.0.1:5001/health", 15);
            if (healthy)
            {
                StatusChanged?.Invoke("Face Recognition service started ✓");
            }
            else
            {
                // Flask may not have /health — just check if process is running
                if (_pythonProcess != null && !_pythonProcess.HasExited)
                {
                    StatusChanged?.Invoke("Face Recognition service started ✓");
                }
                else
                {
                    ErrorOccurred?.Invoke("Face Recognition service may not be running.");
                }
            }
        }

        // ==================== HELPERS ====================

        private Process StartProcess(string fileName, string arguments, string workingDir)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            process.Start();

            // Log output asynchronously
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[{Path.GetFileNameWithoutExtension(fileName)}] {e.Data}");
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[{Path.GetFileNameWithoutExtension(fileName)} ERR] {e.Data}");
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private async Task<bool> WaitForHealthAsync(string url, int maxSeconds)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            for (int i = 0; i < maxSeconds; i++)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if ((int)response.StatusCode < 500) // Any non-server-error response means it's running
                        return true;
                }
                catch { }
                await Task.Delay(1000);
            }
            return false;
        }

        private string? FindProjectPath(string folderName, string fileName)
        {
            // Check relative to base path (development)
            var devPath = Path.Combine(_basePath, "..", "..", "..", "..", folderName, fileName);
            if (File.Exists(devPath)) return Path.GetFullPath(devPath);

            // Check relative to base (installed: basePath/webapi/project.csproj)
            var installPath = Path.Combine(_basePath, folderName, fileName);
            if (File.Exists(installPath)) return Path.GetFullPath(installPath);

            // Walk up from base trying to find the project
            var dir = new DirectoryInfo(_basePath);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, folderName, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            return null;
        }

        private string? FindDirectoryPath(string folderName)
        {
            // Check relative to base path (development)
            var devPath = Path.GetFullPath(Path.Combine(_basePath, "..", "..", "..", "..", folderName));
            if (Directory.Exists(devPath)) return devPath;

            // Check relative to base (installed)
            var installPath = Path.Combine(_basePath, folderName);
            if (Directory.Exists(installPath)) return installPath;

            // Walk up from base
            var dir = new DirectoryInfo(_basePath);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, folderName);
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            return null;
        }

        private string? FindNode()
        {
            // Check bundled Node.js first
            var bundled = Path.Combine(_basePath, "node", "node.exe");
            if (File.Exists(bundled)) return bundled;

            // Fallback to system PATH
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });
                proc?.WaitForExit(5000);
                if (proc?.ExitCode == 0) return "node";
            }
            catch { }
            return null;
        }

        private string FindNpx()
        {
            // Check bundled npx first
            var bundled = Path.Combine(_basePath, "node", "npx.cmd");
            if (File.Exists(bundled)) return bundled;
            return "npx";
        }

        private string? FindPython()
        {
            // Check bundled Python first
            var bundled = Path.Combine(_basePath, "python", "python.exe");
            if (File.Exists(bundled)) return bundled;

            // Fallback to system PATH
            var names = new[] { "python", "python3", "py" };
            foreach (var name in names)
            {
                try
                {
                    var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = "--version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    });
                    proc?.WaitForExit(5000);
                    if (proc?.ExitCode == 0) return name;
                }
                catch { }
            }
            return null;
        }

        // ==================== SHUTDOWN ====================

        public void StopAll()
        {
            KillProcess(ref _pythonProcess, "Python Backend");
            KillProcess(ref _nextJsProcess, "Next.js");
            KillProcess(ref _webApiProcess, "WebAPI");
        }

        private void KillProcess(ref Process? process, string name)
        {
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                {
                    // For cmd.exe processes, we need to kill the entire process tree
                    try
                    {
                        using var killer = Process.Start(new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = $"/PID {process.Id} /T /F",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        });
                        killer?.WaitForExit(5000);
                    }
                    catch
                    {
                        process.Kill(entireProcessTree: true);
                    }

                    Debug.WriteLine($"[ServiceManager] Stopped {name} (PID {process.Id})");
                }
                process.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ServiceManager] Error stopping {name}: {ex.Message}");
            }
            finally
            {
                process = null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopAll();
                _disposed = true;
            }
        }
    }
}

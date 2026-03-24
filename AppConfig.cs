using System;

namespace EduSyncAI
{
    /// <summary>
    /// Centralized configuration for API server URLs.
    /// Toggle between local development and remote production server.
    /// </summary>
    public static class AppConfig
    {
        // ===== TOGGLE THIS FOR LOCAL vs REMOTE =====
        // Set to true to use the remote Contabo VPS server
        // Set to false to use localhost for development
        public static bool UseRemoteServer = true;

        private const string LocalServer = "http://localhost:5152";
        private const string RemoteServer = "https://173-212-248-253.nip.io";

        /// <summary>
        /// Base server URL (e.g., "http://173.212.248.253" or "http://localhost:5152")
        /// </summary>
        public static string ServerUrl => UseRemoteServer ? RemoteServer : LocalServer;

        /// <summary>
        /// API base URL (e.g., "http://173.212.248.253/api")
        /// </summary>
        public static string ApiUrl => $"{ServerUrl}/api";

        /// <summary>
        /// SignalR Hub URL (e.g., "http://173.212.248.253/hubs/classroom")
        /// </summary>
        public static string HubUrl => $"{ServerUrl}/hubs/classroom";

        /// <summary>
        /// Writable data directory for storing local files (DB, whiteboard images, recordings, etc.).
        /// Uses LocalAppData for installed apps (Program Files is read-only).
        /// Uses project Data folder for development.
        /// </summary>
        public static string DataDir
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // If running from Program Files (installed), use LocalAppData
                if (baseDir.Contains("Program Files", StringComparison.OrdinalIgnoreCase))
                {
                    var appDataDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "EduSync AI", "Data");
                    if (!System.IO.Directory.Exists(appDataDir))
                        System.IO.Directory.CreateDirectory(appDataDir);
                    return appDataDir;
                }

                // Development: use project root Data folder
                var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", ".."));
                var devDataDir = System.IO.Path.Combine(projectRoot, "Data");
                if (!System.IO.Directory.Exists(devDataDir))
                    System.IO.Directory.CreateDirectory(devDataDir);
                return devDataDir;
            }
        }
    }
}

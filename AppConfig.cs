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
        private const string RemoteServer = "http://173.212.248.253";

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
    }
}

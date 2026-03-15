using Microsoft.AspNetCore.SignalR;
using EduSyncAI.WebAPI.Services;

namespace EduSyncAI.WebAPI.Hubs
{
    /// <summary>
    /// SignalR hub for real-time classroom notifications.
    /// Students connect to receive live stream status updates.
    /// </summary>
    public class ClassroomHub : Hub
    {
        private readonly LiveStreamService _streamService;
        private readonly ILogger<ClassroomHub> _logger;

        public ClassroomHub(LiveStreamService streamService, ILogger<ClassroomHub> logger)
        {
            _streamService = streamService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            
            // Send the currently active streams to the newly connected client
            var activeStreams = _streamService.GetActiveStreams();
            await Clients.Caller.SendAsync("ActiveStreams", activeStreams.Select(s => new
            {
                s.SessionId,
                s.RoomName,
                s.CourseName,
                s.LecturerId,
                s.StartedAt,
                viewerCount = s.ViewerCount
            }));

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Called by a student when they join a live stream
        /// </summary>
        public async Task JoinStream(int sessionId, string studentName)
        {
            _streamService.IncrementViewers(sessionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
            
            var stream = _streamService.GetStream(sessionId);
            
            // Notify the session group that a student joined
            await Clients.Group($"session-{sessionId}").SendAsync("StudentJoined", new
            {
                studentName,
                viewerCount = stream?.ViewerCount ?? 0
            });

            _logger.LogInformation("Student {StudentName} joined session {SessionId}", studentName, sessionId);
        }

        /// <summary>
        /// Called by a student when they leave a live stream
        /// </summary>
        public async Task LeaveStream(int sessionId, string studentName)
        {
            _streamService.DecrementViewers(sessionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
            
            var stream = _streamService.GetStream(sessionId);
            
            await Clients.Group($"session-{sessionId}").SendAsync("StudentLeft", new
            {
                studentName,
                viewerCount = stream?.ViewerCount ?? 0
            });

            _logger.LogInformation("Student {StudentName} left session {SessionId}", studentName, sessionId);
        }
    }
}

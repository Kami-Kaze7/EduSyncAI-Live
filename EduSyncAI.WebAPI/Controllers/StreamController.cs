using Microsoft.AspNetCore.Mvc;
using EduSyncAI.WebAPI.Services;
using Microsoft.AspNetCore.SignalR;
using EduSyncAI.WebAPI.Hubs;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly LiveStreamService _streamService;
        private readonly IHubContext<ClassroomHub> _hubContext;
        private readonly ILogger<StreamController> _logger;

        public StreamController(
            LiveStreamService streamService,
            IHubContext<ClassroomHub> hubContext,
            ILogger<StreamController> logger)
        {
            _streamService = streamService;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Start a live stream for a session (called by the desktop app)
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult> StartStream([FromBody] StartStreamRequest request)
        {
            try
            {
                var roomName = $"EduSync-{request.SessionId}-{Guid.NewGuid().ToString("N")[..8]}";
                
                var success = _streamService.StartStream(
                    request.SessionId, 
                    roomName, 
                    request.LecturerId,
                    request.CourseName);

                if (!success)
                {
                    return BadRequest(new { error = "Stream already active for this session" });
                }

                _logger.LogInformation("Stream started for session {SessionId}, room {RoomName}", 
                    request.SessionId, roomName);

                // Notify all connected students that a new stream is live
                await _hubContext.Clients.All.SendAsync("StreamStarted", new
                {
                    sessionId = request.SessionId,
                    roomName,
                    courseName = request.CourseName,
                    lecturerName = request.LecturerName
                });

                return Ok(new { roomName, sessionId = request.SessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting stream");
                return StatusCode(500, new { error = "Failed to start stream" });
            }
        }

        /// <summary>
        /// Stop a live stream (called by the desktop app)
        /// </summary>
        [HttpPost("stop")]
        public async Task<ActionResult> StopStream([FromBody] StopStreamRequest request)
        {
            try
            {
                _streamService.StopStream(request.SessionId);

                _logger.LogInformation("Stream stopped for session {SessionId}", request.SessionId);

                // Notify all connected students that the stream has ended
                await _hubContext.Clients.All.SendAsync("StreamEnded", new
                {
                    sessionId = request.SessionId
                });

                return Ok(new { message = "Stream stopped" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping stream");
                return StatusCode(500, new { error = "Failed to stop stream" });
            }
        }

        /// <summary>
        /// Get all currently active live streams
        /// </summary>
        [HttpGet("active")]
        public ActionResult GetActiveStreams()
        {
            var streams = _streamService.GetActiveStreams();
            return Ok(streams.Select(s => new
            {
                s.SessionId,
                s.RoomName,
                s.CourseName,
                s.LecturerId,
                s.StartedAt,
                viewerCount = s.ViewerCount,
                hasVideo = _streamService.HasFrames(s.SessionId)
            }));
        }

        /// <summary>
        /// Receive a JPEG frame from the desktop app camera
        /// </summary>
        [HttpPost("{sessionId}/frame")]
        [RequestSizeLimit(5_000_000)] // 5MB max per frame
        public async Task<ActionResult> UploadFrame(int sessionId)
        {
            try
            {
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);
                var frameData = ms.ToArray();

                if (frameData.Length == 0)
                    return BadRequest(new { error = "Empty frame" });

                _streamService.UpdateFrame(sessionId, frameData);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving frame for session {SessionId}", sessionId);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Serve an MJPEG stream of the lecturer's camera for PiP viewing
        /// </summary>
        [HttpGet("{sessionId}/video")]
        public async Task GetVideoStream(int sessionId, CancellationToken ct)
        {
            Response.ContentType = "multipart/x-mixed-replace; boundary=--frame";
            Response.Headers["Cache-Control"] = "no-cache, no-store";
            Response.Headers["Connection"] = "keep-alive";

            _logger.LogInformation("MJPEG viewer connected for session {SessionId}", sessionId);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var frame = await _streamService.GetFrameAsync(sessionId, ct);
                    if (frame == null) continue;

                    var header = $"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {frame.Length}\r\n\r\n";
                    var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);

                    await Response.Body.WriteAsync(headerBytes, ct);
                    await Response.Body.WriteAsync(frame, ct);
                    await Response.Body.WriteAsync(System.Text.Encoding.ASCII.GetBytes("\r\n"), ct);
                    await Response.Body.FlushAsync(ct);

                    // ~5fps rate limit for viewers
                    await Task.Delay(200, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MJPEG stream ended for session {SessionId}", sessionId);
            }
        }
    }

    public class StartStreamRequest
    {
        public int SessionId { get; set; }
        public int LecturerId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
    }

    public class StopStreamRequest
    {
        public int SessionId { get; set; }
    }
}

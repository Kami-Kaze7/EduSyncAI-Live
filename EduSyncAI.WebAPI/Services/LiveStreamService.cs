using System.Collections.Concurrent;

namespace EduSyncAI.WebAPI.Services
{
    /// <summary>
    /// In-memory singleton that tracks which sessions are currently streaming live
    /// and stores the latest camera frame per session for MJPEG relay.
    /// </summary>
    public class LiveStreamService
    {
        private readonly ConcurrentDictionary<int, LiveStreamInfo> _activeStreams = new();
        private readonly ConcurrentDictionary<int, FrameBuffer> _frameBuffers = new();

        public bool StartStream(int sessionId, string roomName, int lecturerId, string courseName)
        {
            var info = new LiveStreamInfo
            {
                SessionId = sessionId,
                RoomName = roomName,
                LecturerId = lecturerId,
                CourseName = courseName,
                StartedAt = DateTime.UtcNow,
                ViewerCount = 0
            };
            _frameBuffers.TryAdd(sessionId, new FrameBuffer());
            return _activeStreams.TryAdd(sessionId, info);
        }

        public bool StopStream(int sessionId)
        {
            _frameBuffers.TryRemove(sessionId, out _);
            return _activeStreams.TryRemove(sessionId, out _);
        }

        public LiveStreamInfo? GetStream(int sessionId)
        {
            _activeStreams.TryGetValue(sessionId, out var info);
            return info;
        }

        public List<LiveStreamInfo> GetActiveStreams()
        {
            return _activeStreams.Values.ToList();
        }

        public void IncrementViewers(int sessionId)
        {
            if (_activeStreams.TryGetValue(sessionId, out var info))
            {
                Interlocked.Increment(ref info.ViewerCount);
            }
        }

        public void DecrementViewers(int sessionId)
        {
            if (_activeStreams.TryGetValue(sessionId, out var info))
            {
                Interlocked.Decrement(ref info.ViewerCount);
            }
        }

        /// <summary>
        /// Store the latest camera frame for a session
        /// </summary>
        public void UpdateFrame(int sessionId, byte[] jpegData)
        {
            if (_frameBuffers.TryGetValue(sessionId, out var buffer))
            {
                buffer.Update(jpegData);
            }
        }

        /// <summary>
        /// Get the latest camera frame for a session (blocks until available or timeout)
        /// </summary>
        public async Task<byte[]?> GetFrameAsync(int sessionId, CancellationToken ct)
        {
            if (_frameBuffers.TryGetValue(sessionId, out var buffer))
            {
                return await buffer.WaitForFrameAsync(ct);
            }
            return null;
        }

        public bool HasFrames(int sessionId)
        {
            return _frameBuffers.TryGetValue(sessionId, out var buffer) && buffer.HasFrame;
        }
    }

    public class LiveStreamInfo
    {
        public int SessionId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int LecturerId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int ViewerCount;
    }

    /// <summary>
    /// Thread-safe frame buffer that stores the latest JPEG frame
    /// and notifies waiting consumers when a new frame arrives.
    /// </summary>
    public class FrameBuffer
    {
        private byte[]? _currentFrame;
        private readonly SemaphoreSlim _signal = new(0, 1);
        private readonly object _lock = new();

        public bool HasFrame => _currentFrame != null;

        public void Update(byte[] jpegData)
        {
            lock (_lock)
            {
                _currentFrame = jpegData;
            }
            // Signal that a new frame is available
            if (_signal.CurrentCount == 0)
            {
                try { _signal.Release(); } catch { }
            }
        }

        public async Task<byte[]?> WaitForFrameAsync(CancellationToken ct)
        {
            // Wait up to 2 seconds for a new frame
            await _signal.WaitAsync(2000, ct);
            lock (_lock)
            {
                return _currentFrame;
            }
        }
    }
}

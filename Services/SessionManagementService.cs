using System;
using System.Collections.Generic;

namespace EduSyncAI
{
    public class SessionManagementService
    {
        private readonly DatabaseService _dbService;

        public SessionManagementService()
        {
            _dbService = new DatabaseService();
        }

        /// <summary>
        /// Creates a new session in Ready state
        /// </summary>
        public int CreateSession(int courseId, int lectureId, string? sessionCode = null, int? lecturerId = null)
        {
            var session = new ClassSession
            {
                CourseId = courseId,
                LectureId = lectureId,
                LecturerId = lecturerId,
                SessionCode = sessionCode,
                State = SessionState.Ready,
                CreatedAt = DateTime.Now,
                AttendanceCount = 0,
                Duration = 0
            };

            return _dbService.CreateClassSession(session);
        }

        /// <summary>
        /// Starts a session (Ready → Live)
        /// Business Rule: Only one Live session allowed at a time
        /// </summary>
        public bool StartSession(int sessionId)
        {
            // Check if there's already a Live session
            var activeSession = _dbService.GetActiveClassSession();
            if (activeSession != null && activeSession.Id != sessionId)
            {
                throw new InvalidOperationException($"Cannot start session. Session #{activeSession.Id} is already live.");
            }

            var session = _dbService.GetClassSessionById(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session #{sessionId} not found.");
            }

            if (session.State != SessionState.Ready)
            {
                throw new InvalidOperationException($"Session must be in Ready state to start. Current state: {session.State}");
            }

            session.State = SessionState.Live;
            session.StartTime = DateTime.Now;
            _dbService.UpdateClassSession(session);

            return true;
        }

        /// <summary>
        /// Ends a session (Live → Ended)
        /// Calculates duration automatically
        /// </summary>
        public bool EndSession(int sessionId)
        {
            var session = _dbService.GetClassSessionById(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session #{sessionId} not found.");
            }

            if (session.State != SessionState.Live)
            {
                throw new InvalidOperationException($"Session must be Live to end. Current state: {session.State}");
            }

            session.State = SessionState.Ended;
            session.EndTime = DateTime.Now;

            // Calculate duration in seconds
            if (session.StartTime.HasValue && session.EndTime.HasValue)
            {
                session.Duration = (int)(session.EndTime.Value - session.StartTime.Value).TotalSeconds;
            }

            _dbService.UpdateClassSession(session);

            return true;
        }

        /// <summary>
        /// Updates recording file paths for a session
        /// </summary>
        public void UpdateRecordingPaths(int sessionId, string? audioPath = null, string? videoPath = null, string? boardPath = null, string? snapshotFolder = null)
        {
            var session = _dbService.GetClassSessionById(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session #{sessionId} not found.");
            }

            if (audioPath != null) session.AudioFilePath = audioPath;
            if (videoPath != null) session.VideoFilePath = videoPath;
            if (boardPath != null) session.BoardExportPath = boardPath;
            if (snapshotFolder != null) session.BoardSnapshotFolder = snapshotFolder;

            _dbService.UpdateClassSession(session);
        }

        /// <summary>
        /// Updates attendance count for a session
        /// </summary>
        public void UpdateAttendanceCount(int sessionId, int count)
        {
            var session = _dbService.GetClassSessionById(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session #{sessionId} not found.");
            }

            session.AttendanceCount = count;
            _dbService.UpdateClassSession(session);
        }

        /// <summary>
        /// Gets the currently active (Live) session
        /// </summary>
        public ClassSession GetActiveSession()
        {
            return _dbService.GetActiveClassSession();
        }

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        public ClassSession GetSessionById(int sessionId)
        {
            return _dbService.GetClassSessionById(sessionId);
        }

        /// <summary>
        /// Gets all sessions ordered by creation date (newest first)
        /// </summary>
        public List<ClassSession> GetAllSessions()
        {
            return _dbService.GetAllClassSessions();
        }

        /// <summary>
        /// Gets formatted duration string (e.g., "1h 23m 45s")
        /// </summary>
        public string GetFormattedDuration(int durationSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(durationSeconds);
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{timeSpan.Seconds}s";
            }
        }
    }
}

using System;

namespace EduSyncAI
{
    public class ClassSession
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int LectureId { get; set; }
        public int? LecturerId { get; set; }
        public string? SessionCode { get; set; }
        public SessionState State { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? AudioFilePath { get; set; }
        public string? VideoFilePath { get; set; }
        public string? BoardExportPath { get; set; }
        public string? BoardSnapshotFolder { get; set; }
        public int AttendanceCount { get; set; }
        public int Duration { get; set; } // in seconds
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties (for display)
        public string? CourseName { get; set; }
        public string? LectureTopic { get; set; }
    }

    public enum SessionState
    {
        Ready,
        Live,
        Ended
    }
}

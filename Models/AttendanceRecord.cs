using System;

namespace EduSyncAI
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int StudentId { get; set; }
        public DateTime CheckInTime { get; set; }
        public string CheckInMethod { get; set; }  // "Fingerprint" or "Manual"
        public int? VerifiedBy { get; set; }  // LecturerId for manual override
        
        // Navigation properties for display
        public string StudentName { get; set; }
        public string MatricNumber { get; set; }
    }

    public enum CheckInMethod
    {
        Fingerprint,
        Manual
    }
}

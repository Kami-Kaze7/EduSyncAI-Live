using System;

namespace EduSyncAI
{
    public class Student
    {
        public int Id { get; set; }
        public string MatricNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? WindowsUsername { get; set; }  // For fingerprint mapping
        
        // Authentication fields
        public string? PasswordHash { get; set; }
        public string? PIN { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        
        // Photo
        public string? PhotoPath { get; set; }
    }
}

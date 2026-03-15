using System;

namespace EduSyncAI
{
    public class Lecturer
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }
        public string PIN { get; set; }  // 4-6 digit PIN for quick login
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

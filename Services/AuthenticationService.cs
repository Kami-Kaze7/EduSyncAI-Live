using System;
using System.Security.Cryptography;
using System.Text;

namespace EduSyncAI
{
    public class AuthenticationService
    {
        private readonly DatabaseService _dbService;
        private static Lecturer? _currentLecturer;
        private static Student? _currentStudent;
        private static string _currentUserType = "";

        public AuthenticationService()
        {
            _dbService = new DatabaseService();
        }

        /// <summary>
        /// Authenticates lecturer with username and password
        /// </summary>
        public Lecturer? AuthenticateWithPassword(string username, string password)
        {
            Console.WriteLine($"[DEBUG AUTH] Attempting lecturer login with username: '{username}'");
            var lecturer = _dbService.GetLecturerByUsername(username);
            if (lecturer == null)
            {
                Console.WriteLine($"[DEBUG AUTH] Lecturer NOT FOUND for username: '{username}'");
                return null;
            }

            Console.WriteLine($"[DEBUG AUTH] Found lecturer: Id={lecturer.Id}, Username='{lecturer.Username}', FullName='{lecturer.FullName}', IsActive={lecturer.IsActive}");

            // Check if lecturer is active (important for admin-created accounts)
            if (!lecturer.IsActive)
            {
                Console.WriteLine($"[DEBUG AUTH] Lecturer '{username}' is INACTIVE");
                return null;
            }

            Console.WriteLine($"[DEBUG AUTH] Stored hash: '{lecturer.PasswordHash}'");
            Console.WriteLine($"[DEBUG AUTH] Hex hash of input: '{HashPassword(password)}'");
            Console.WriteLine($"[DEBUG AUTH] Base64 hash of input: '{HashPasswordBase64(password)}'");

            if (VerifyPassword(password, lecturer.PasswordHash))
            {
                Console.WriteLine($"[DEBUG AUTH] Password VERIFIED for '{username}'");
                _currentLecturer = lecturer;
                return lecturer;
            }

            Console.WriteLine($"[DEBUG AUTH] Password MISMATCH for '{username}'");
            return null;
        }

        /// <summary>
        /// Authenticates lecturer with PIN (4-6 digits)
        /// </summary>
        public Lecturer? AuthenticateWithPIN(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4 || pin.Length > 6)
            {
                return null;
            }

            var lecturer = _dbService.GetLecturerByPIN(pin);
            if (lecturer != null)
            {
                _currentLecturer = lecturer;
            }
            return lecturer;
        }

        /// <summary>
        /// Creates a new lecturer account
        /// </summary>
        public int CreateLecturer(string username, string email, string fullName, string password, string? pin = null)
        {
            var lecturer = new Lecturer
            {
                Username = username,
                Email = email,
                FullName = fullName,
                PasswordHash = HashPassword(password),
                PIN = pin,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            return _dbService.CreateLecturer(lecturer);
        }

        /// <summary>
        /// Gets the currently logged-in lecturer
        /// </summary>
        public Lecturer? GetCurrentLecturer()
        {
            return _currentLecturer;
        }

        /// <summary>
        /// Checks if a lecturer is currently logged in
        /// </summary>
        public bool IsAuthenticated()
        {
            return _currentLecturer != null;
        }

        /// <summary>
        /// Logs out the current lecturer
        /// </summary>
        public void Logout()
        {
            _currentLecturer = null;
        }

        /// <summary>
        /// Hashes a password using SHA256 (simple but secure for this use case)
        /// </summary>
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Verifies a password against a hash (supports both hex and Base64 formats)
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            // Try hex format first (desktop app format)
            string hexHash = HashPassword(password);
            if (hexHash.Equals(hash, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Try Base64 format (web admin format)
            string base64Hash = HashPasswordBase64(password);
            if (base64Hash.Equals(hash, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Hashes a password using SHA256 and returns Base64 (for web admin compatibility)
        /// </summary>
        private string HashPasswordBase64(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        // ==================== STUDENT AUTHENTICATION ====================

        public int RegisterStudent(string matricNumber, string fullName, string email, string password, string? pin = null)
        {
            var passwordHash = HashPassword(password);
            var student = new Student
            {
                MatricNumber = matricNumber,
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHash,
                PIN = pin,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            return _dbService.CreateStudent(student);
        }

        public Student? AuthenticateStudentWithPassword(string matricNumber, string password)
        {
            var student = _dbService.GetStudentByMatricNumber(matricNumber);
            if (student == null)
            {
                System.Diagnostics.Debug.WriteLine($"Student not found: {matricNumber}");
                return null;
            }
            
            // Check if student is active (important for admin-created accounts)
            if (!student.IsActive)
            {
                System.Diagnostics.Debug.WriteLine($"Student {matricNumber} is inactive");
                return null;
            }

            if (student.PasswordHash == null)
            {
                System.Diagnostics.Debug.WriteLine($"Student {matricNumber} has no password hash");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"Authenticating student: {matricNumber}");
            System.Diagnostics.Debug.WriteLine($"Stored hash: {student.PasswordHash}");
            System.Diagnostics.Debug.WriteLine($"Input password: {password}");
            System.Diagnostics.Debug.WriteLine($"Computed hash: {HashPassword(password)}");

            if (VerifyPassword(password, student.PasswordHash))
            {
                _currentStudent = student;
                _currentUserType = "Student";
                System.Diagnostics.Debug.WriteLine("Authentication SUCCESS");
                return student;
            }
            
            System.Diagnostics.Debug.WriteLine("Authentication FAILED - password mismatch");
            return null;
        }

        public Student? AuthenticateStudentWithPIN(string pin)
        {
            var student = _dbService.GetStudentByPIN(pin);
            if (student != null)
            {
                _currentStudent = student;
                _currentUserType = "Student";
            }
            return student;
        }

        public Student? GetCurrentStudent()
        {
            return _currentStudent;
        }

        public string GetCurrentUserType()
        {
            return _currentUserType;
        }

        public void LogoutStudent()
        {
            _currentStudent = null;
            _currentUserType = "";
        }
    }
}

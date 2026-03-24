using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduSyncAI
{
    public class AuthenticationService
    {
        private readonly DatabaseService _dbService;
        private static Lecturer? _currentLecturer;
        private static Student? _currentStudent;
        private static string _currentUserType = "";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string? _authToken;

        public static string? AuthToken => _authToken;

        public AuthenticationService()
        {
            _dbService = new DatabaseService();
        }

        /// <summary>
        /// Authenticates lecturer with username and password.
        /// Uses remote API when AppConfig.UseRemoteServer is true.
        /// </summary>
        public Lecturer? AuthenticateWithPassword(string username, string password)
        {
            if (AppConfig.UseRemoteServer)
            {
                return AuthenticateViaRemoteApiAsync(username, password).GetAwaiter().GetResult();
            }

            return AuthenticateLocally(username, password);
        }

        /// <summary>
        /// Authenticates lecturer via the remote WebAPI POST /api/auth/login
        /// </summary>
        private async Task<Lecturer?> AuthenticateViaRemoteApiAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"[AUTH] Attempting remote login for '{username}' at {AppConfig.ApiUrl}/auth/login");

                var requestBody = new { Username = username, Password = password };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{AppConfig.ApiUrl}/auth/login", content).ConfigureAwait(false);
                var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                Console.WriteLine($"[AUTH] Remote login response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                    _authToken = result.GetProperty("token").GetString();

                    var user = result.GetProperty("user");
                    var lecturer = new Lecturer
                    {
                        Id = user.GetProperty("id").GetInt32(),
                        Username = user.GetProperty("username").GetString() ?? username,
                        FullName = user.GetProperty("fullName").GetString() ?? "",
                        Email = user.GetProperty("email").GetString() ?? "",
                        PasswordHash = "",
                        IsActive = true
                    };

                    Console.WriteLine($"[AUTH] Remote login SUCCESS: {lecturer.FullName} (ID: {lecturer.Id})");
                    _currentLecturer = lecturer;
                    _currentUserType = "Lecturer";
                    return lecturer;
                }
                else
                {
                    Console.WriteLine($"[AUTH] Remote login FAILED: {responseText}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH] Remote login ERROR: {ex.Message}");
                // If remote fails, fall back to local DB
                Console.WriteLine("[AUTH] Falling back to local database...");
                return AuthenticateLocally(username, password);
            }
        }

        /// <summary>
        /// Original local database authentication
        /// </summary>
        private Lecturer? AuthenticateLocally(string username, string password)
        {
            Console.WriteLine($"[DEBUG AUTH] Attempting local lecturer login with username: '{username}'");
            var lecturer = _dbService.GetLecturerByUsername(username);
            if (lecturer == null)
            {
                Console.WriteLine($"[DEBUG AUTH] Lecturer NOT FOUND for username: '{username}'");
                return null;
            }

            Console.WriteLine($"[DEBUG AUTH] Found lecturer: Id={lecturer.Id}, Username='{lecturer.Username}', FullName='{lecturer.FullName}', IsActive={lecturer.IsActive}");

            if (!lecturer.IsActive)
            {
                Console.WriteLine($"[DEBUG AUTH] Lecturer '{username}' is INACTIVE");
                return null;
            }

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
            if (AppConfig.UseRemoteServer)
            {
                return AuthenticateStudentViaRemoteApiAsync(matricNumber, password).GetAwaiter().GetResult();
            }

            return AuthenticateStudentLocally(matricNumber, password);
        }

        /// <summary>
        /// Authenticates student via the remote WebAPI POST /api/students/login
        /// </summary>
        private async Task<Student?> AuthenticateStudentViaRemoteApiAsync(string matricNumber, string password)
        {
            try
            {
                Console.WriteLine($"[AUTH] Attempting remote student login for '{matricNumber}'");

                var requestBody = new { Username = matricNumber, Password = password };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{AppConfig.ApiUrl}/students/login", content).ConfigureAwait(false);
                var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                Console.WriteLine($"[AUTH] Remote student login response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                    _authToken = result.GetProperty("token").GetString();

                    var user = result.GetProperty("student");
                    var student = new Student
                    {
                        Id = user.GetProperty("id").GetInt32(),
                        MatricNumber = user.GetProperty("matricNumber").GetString() ?? matricNumber,
                        FullName = user.GetProperty("fullName").GetString() ?? "",
                        Email = user.GetProperty("email").GetString() ?? "",
                        PasswordHash = "",
                        IsActive = true
                    };

                    Console.WriteLine($"[AUTH] Remote student login SUCCESS: {student.FullName} (ID: {student.Id})");
                    _currentStudent = student;
                    _currentUserType = "Student";
                    return student;
                }
                else
                {
                    Console.WriteLine($"[AUTH] Remote student login FAILED: {responseText}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH] Remote student login ERROR: {ex.Message}");
                Console.WriteLine("[AUTH] Falling back to local database...");
                return AuthenticateStudentLocally(matricNumber, password);
            }
        }

        /// <summary>
        /// Original local database student authentication
        /// </summary>
        private Student? AuthenticateStudentLocally(string matricNumber, string password)
        {
            var student = _dbService.GetStudentByMatricNumber(matricNumber);
            if (student == null)
            {
                System.Diagnostics.Debug.WriteLine($"Student not found: {matricNumber}");
                return null;
            }
            
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

            if (VerifyPassword(password, student.PasswordHash))
            {
                _currentStudent = student;
                _currentUserType = "Student";
                return student;
            }
            
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

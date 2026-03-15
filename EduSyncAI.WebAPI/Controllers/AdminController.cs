using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using System.Security.Cryptography;
using System.Text;
using OfficeOpenXml;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(EduSyncDbContext context, ILogger<AdminController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // POST: api/admin/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] EduSyncAI.WebAPI.Controllers.LoginRequest request)
        {
            try
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Username == request.Username);

                if (admin == null || !VerifyPassword(request.Password, admin.PasswordHash))
                {
                    return Unauthorized(new { error = "Invalid credentials" });
                }

                var token = GenerateJwtToken(admin.Id, admin.Username, "Admin");

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = admin.Id,
                        username = admin.Username,
                        fullName = admin.FullName,
                        role = "Admin"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login");
                return StatusCode(500, new { error = "Login failed" });
            }
        }

        // GET: api/admin/lecturers
        [HttpGet("lecturers")]
        public async Task<ActionResult<IEnumerable<Lecturer>>> GetLecturers()
        {
            try
            {
                var lecturers = await _context.Lecturers.ToListAsync();
                return Ok(lecturers.Select(l => new
                {
                    l.Id,
                    l.Username,
                    l.FullName,
                    l.Email,
                    l.IsActive
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lecturers");
                return StatusCode(500, new { error = "Failed to fetch lecturers" });
            }
        }

        // POST: api/admin/lecturers
        [HttpPost("lecturers")]
        public async Task<ActionResult> CreateLecturer([FromBody] CreateLecturerRequest request)
        {
            try
            {
                // Check if username already exists
                if (await _context.Lecturers.AnyAsync(l => l.Username == request.Username))
                {
                    return BadRequest(new { error = "Username already exists" });
                }

                // Generate password if not provided
                var password = string.IsNullOrEmpty(request.Password) 
                    ? GenerateRandomPassword() 
                    : request.Password;

                var lecturer = new Lecturer
                {
                    Username = request.Username,
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = HashPassword(password),
                    IsActive = true
                };

                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = lecturer.Id,
                    username = lecturer.Username,
                    fullName = lecturer.FullName,
                    email = lecturer.Email,
                    generatedPassword = password
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lecturer");
                return StatusCode(500, new { error = "Failed to create lecturer" });
            }
        }

        // POST: api/admin/lecturers/import
        [HttpPost("lecturers/import")]
        public async Task<ActionResult> ImportLecturers([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                var results = new List<object>();
                var errors = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var fullName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            var email = worksheet.Cells[row, 2].Value?.ToString()?.Trim();

                            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
                            {
                                errors.Add($"Row {row}: Missing required fields");
                                continue;
                            }

                            // Generate username from email
                            var username = email.Split('@')[0];
                            
                            // Check if already exists
                            if (await _context.Lecturers.AnyAsync(l => l.Username == username))
                            {
                                errors.Add($"Row {row}: Username {username} already exists");
                                continue;
                            }

                            var password = GenerateRandomPassword();
                            var lecturer = new Lecturer
                            {
                                Username = username,
                                FullName = fullName,
                                Email = email,
                                PasswordHash = HashPassword(password),
                                IsActive = true
                            };

                            _context.Lecturers.Add(lecturer);
                            results.Add(new
                            {
                                username,
                                fullName,
                                email,
                                generatedPassword = password
                            });
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new
                {
                    success = results.Count,
                    failed = errors.Count,
                    lecturers = results,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing lecturers");
                return StatusCode(500, new { error = $"Failed to import lecturers: {ex.Message}" });
            }
        }

        // DELETE: api/admin/lecturers/5
        [HttpDelete("lecturers/{id}")]
        public async Task<IActionResult> DeleteLecturer(int id)
        {
            try
            {
                var lecturer = await _context.Lecturers.FindAsync(id);
                if (lecturer == null)
                {
                    return NotFound(new { error = "Lecturer not found" });
                }

                _context.Lecturers.Remove(lecturer);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lecturer {LecturerId}", id);
                return StatusCode(500, new { error = "Failed to delete lecturer" });
            }
        }

        // GET: api/admin/students
        [HttpGet("students")]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            try
            {
                var students = await _context.Students.ToListAsync();
                return Ok(students.Select(s => new
                {
                    s.Id,
                    s.MatricNumber,
                    s.FullName,
                    s.Email,
                    s.IsActive
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students");
                return StatusCode(500, new { error = "Failed to fetch students" });
            }
        }

        // POST: api/admin/students
        [HttpPost("students")]
        public async Task<ActionResult> CreateStudent([FromBody] CreateStudentRequest request)
        {
            try
            {
                // Check if matric number already exists
                if (await _context.Students.AnyAsync(s => s.MatricNumber == request.MatricNumber))
                {
                    return BadRequest(new { error = "Matric number already exists" });
                }

                // Generate password if not provided (use matric number as password)
                var password = string.IsNullOrEmpty(request.Password) 
                    ? request.MatricNumber 
                    : request.Password;

                var student = new Student
                {
                    MatricNumber = request.MatricNumber,
                    FullName = request.FullName,
                    Email = request.Email ?? "",
                    PasswordHash = HashPassword(password),
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = student.Id,
                    matricNumber = student.MatricNumber,
                    fullName = student.FullName,
                    email = student.Email,
                    generatedPassword = password
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return StatusCode(500, new { error = "Failed to create student" });
            }
        }

        // POST: api/admin/students/import
        [HttpPost("students/import")]
        public async Task<ActionResult> ImportStudents([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                var results = new List<object>();
                var errors = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var fullName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            var matricNumber = worksheet.Cells[row, 2].Value?.ToString()?.Trim();

                            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(matricNumber))
                            {
                                errors.Add($"Row {row}: Missing required fields");
                                continue;
                            }

                            // Check if already exists
                            if (await _context.Students.AnyAsync(s => s.MatricNumber == matricNumber))
                            {
                                errors.Add($"Row {row}: Matric number {matricNumber} already exists");
                                continue;
                            }

                            var password = matricNumber; // Use matric number as password
                            var student = new Student
                            {
                                MatricNumber = matricNumber,
                                FullName = fullName,
                                Email = "",
                                PasswordHash = HashPassword(password),
                                IsActive = true
                            };

                            _context.Students.Add(student);
                            results.Add(new
                            {
                                matricNumber,
                                fullName,
                                generatedPassword = password
                            });
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new
                {
                    success = results.Count,
                    failed = errors.Count,
                    students = results,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing students");
                return StatusCode(500, new { error = $"Failed to import students: {ex.Message}" });
            }
        }

        // DELETE: api/admin/students/5
        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound(new { error = "Student not found" });
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", id);
                return StatusCode(500, new { error = "Failed to delete student" });
            }
        }

        // Helper methods
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateJwtToken(int userId, string username, string role)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key-min-32-characters-long");
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("id", userId.ToString()),
                    new System.Security.Claims.Claim("username", username),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpirationHours"] ?? "24")),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class CreateLecturerRequest
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
    }

    public class CreateStudentRequest
    {
        public string MatricNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}

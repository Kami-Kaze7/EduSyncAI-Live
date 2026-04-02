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

                // --- Pure raw SQL cascade delete for maximum reliability ---
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get all course IDs for this lecturer
                    var lecturerCourseIds = await _context.Courses
                        .Where(c => c.LecturerId == id)
                        .Select(c => c.Id)
                        .ToListAsync();

                    // LAYER 1: Clean ALL FK references via raw SQL (handles tracked + untracked tables)
                    // Attendance (via sessions)
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM Attendance WHERE SessionId IN (SELECT Id FROM ClassSessions WHERE CourseId IN (SELECT Id FROM Courses WHERE LecturerId = {id}) OR LecturerId = {id})");
                    // Nullify VerifiedBy references
                    await _context.Database.ExecuteSqlRawAsync(
                        $"UPDATE Attendance SET VerifiedBy = NULL WHERE VerifiedBy = {id}");
                    // LectureNotes (via sessions)
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM LectureNotes WHERE SessionId IN (SELECT Id FROM ClassSessions WHERE CourseId IN (SELECT Id FROM Courses WHERE LecturerId = {id}) OR LecturerId = {id})");
                    // LectureMaterials (via sessions)
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM LectureMaterials WHERE SessionId IN (SELECT Id FROM ClassSessions WHERE CourseId IN (SELECT Id FROM Courses WHERE LecturerId = {id}) OR LecturerId = {id})");
                    // StudentWeeklySummaries (via WeeklySummaries)
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM StudentWeeklySummaries WHERE WeeklySummaryId IN (SELECT Id FROM WeeklySummaries WHERE LecturerId = {id})");
                    // LecturePreps (via Lectures, untracked)
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM LecturePreps WHERE LectureId IN (SELECT Id FROM Lectures WHERE CourseId IN (SELECT Id FROM Courses WHERE LecturerId = {id}))");
                    // Lectures (untracked)
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM Lectures WHERE CourseId IN (SELECT Id FROM Courses WHERE LecturerId = {id})");

                    // LAYER 2: Intermediate tables
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM WeeklySummaries WHERE LecturerId = {id}");
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM CourseSyllabi WHERE LecturerId = {id}");
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM ClassSummaries WHERE LecturerId = {id}");
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM ClassSessions WHERE LecturerId = {id}");

                    // LAYER 3: Course-level children
                    if (lecturerCourseIds.Any())
                    {
                        var ids = string.Join(",", lecturerCourseIds);
                        await _context.Database.ExecuteSqlRawAsync(
                            $"DELETE FROM ClassSessions WHERE CourseId IN ({ids})");
                        await _context.Database.ExecuteSqlRawAsync(
                            $"DELETE FROM CourseEnrollments WHERE CourseId IN ({ids})");
                        await _context.Database.ExecuteSqlRawAsync(
                            $"DELETE FROM ClassSummaries WHERE CourseId IN ({ids})");
                        await _context.Database.ExecuteSqlRawAsync(
                            $"DELETE FROM CourseSyllabi WHERE CourseId IN ({ids})");
                        await _context.Database.ExecuteSqlRawAsync(
                            $"DELETE FROM WeeklySummaries WHERE CourseId IN ({ids})");
                    }

                    // LAYER 4: Courses owned by this lecturer
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM Courses WHERE LecturerId = {id}");

                    // LAYER 5: The lecturer itself
                    await _context.Database.ExecuteSqlRawAsync(
                        $"DELETE FROM Lecturers WHERE Id = {id}");

                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.LogError(ex, "Error deleting lecturer {LecturerId}", id);
                return StatusCode(500, new { error = $"Failed to delete lecturer: {inner}" });
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

                // Manually cascade deletes for dependent tables to avoid DB foreign key constraints
                var enrollments = await _context.CourseEnrollments.Where(e => e.StudentId == id).ToListAsync();
                if (enrollments.Any()) _context.CourseEnrollments.RemoveRange(enrollments);

                var attendance = await _context.Attendance.Where(a => a.StudentId == id).ToListAsync();
                if (attendance.Any()) _context.Attendance.RemoveRange(attendance);

                var studentSummaries = await _context.StudentWeeklySummaries.Where(s => s.StudentId == id).ToListAsync();
                if (studentSummaries.Any()) _context.StudentWeeklySummaries.RemoveRange(studentSummaries);

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

        // GET: api/admin/debug-delete
        [HttpGet("debug-delete")]
        public async Task<IActionResult> DebugDelete()
        {
            try 
            {
                var studentId = await _context.Attendance.Select(a => a.StudentId).FirstOrDefaultAsync();
                if (studentId == 0) return Ok("No student with attendance");
                
                using var tx = await _context.Database.BeginTransactionAsync();
                try 
                {
                    var student = await _context.Students.FindAsync(studentId);
                    
                    var enrollments = await _context.CourseEnrollments.Where(e => e.StudentId == studentId).ToListAsync();
                    if (enrollments.Any()) _context.CourseEnrollments.RemoveRange(enrollments);

                    var attendance = await _context.Attendance.Where(a => a.StudentId == studentId).ToListAsync();
                    if (attendance.Any()) _context.Attendance.RemoveRange(attendance);

                    var studentSummaries = await _context.StudentWeeklySummaries.Where(s => s.StudentId == studentId).ToListAsync();
                    if (studentSummaries.Any()) _context.StudentWeeklySummaries.RemoveRange(studentSummaries);
                    
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();
                    
                    await tx.RollbackAsync();
                    return Ok($"SUCCESS: Deleted {studentId}");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return StatusCode(500, new { 
                        error = ex.Message, 
                        inner = ex.InnerException?.Message ?? "No inner exception",
                        stack = ex.StackTrace 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/admin/course-list
        [HttpGet("course-list")]
        public async Task<ActionResult> GetCourses()
        {
            try
            {
                var courses = await _context.Courses.ToListAsync();
                var lecturers = await _context.Lecturers.ToListAsync();

                var result = courses.Select(c => new
                {
                    c.Id,
                    c.CourseCode,
                    c.CourseName,
                    c.Description,
                    c.CreditHours,
                    c.LecturerId,
                    c.SyllabusPath,
                    c.CreatedAt,
                    lecturerName = lecturers.FirstOrDefault(l => l.Id == c.LecturerId)?.FullName ?? "Unassigned"
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses");
                return StatusCode(500, new { error = "Failed to fetch courses" });
            }
        }

        // POST: api/admin/course-create
        [HttpPost("course-create")]
        public async Task<ActionResult> CreateCourse([FromBody] CreateAdminCourseRequest request)
        {
            try
            {
                var lecturer = await _context.Lecturers.FindAsync(request.LecturerId);
                if (lecturer == null)
                    return BadRequest(new { error = "Lecturer not found" });

                var course = new Course
                {
                    CourseCode = request.CourseCode,
                    CourseName = request.CourseName,
                    Description = request.Description ?? "",
                    CreditHours = request.CreditHours,
                    LecturerId = request.LecturerId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    course.Id,
                    course.CourseCode,
                    course.CourseName,
                    course.Description,
                    course.CreditHours,
                    course.LecturerId,
                    course.CreatedAt,
                    lecturerName = lecturer.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new { error = "Failed to create course" });
            }
        }

        // DELETE: api/admin/course-delete/5
        [HttpDelete("course-delete/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                    return NotFound(new { error = "Course not found" });

                // --- Full cascade delete for course ---
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Get all session IDs for this course
                    var sessionIds = await _context.ClassSessions
                        .Where(s => s.CourseId == id)
                        .Select(s => s.Id)
                        .ToListAsync();

                    // LAYER 1: Deepest children
                    var attendance = await _context.Attendance.Where(a => sessionIds.Contains(a.SessionId)).ToListAsync();
                    if (attendance.Any()) _context.Attendance.RemoveRange(attendance);

                    var lectureNotes = await _context.LectureNotes.Where(n => sessionIds.Contains(n.SessionId)).ToListAsync();
                    if (lectureNotes.Any()) _context.LectureNotes.RemoveRange(lectureNotes);

                    var lectureMaterials = await _context.LectureMaterials.Where(m => sessionIds.Contains(m.SessionId)).ToListAsync();
                    if (lectureMaterials.Any()) _context.LectureMaterials.RemoveRange(lectureMaterials);

                    var weeklySummaryIds = await _context.WeeklySummaries
                        .Where(ws => ws.CourseId == id)
                        .Select(ws => ws.Id)
                        .ToListAsync();

                    var studentWeeklySummaries = await _context.StudentWeeklySummaries
                        .Where(sws => weeklySummaryIds.Contains(sws.WeeklySummaryId)).ToListAsync();
                    if (studentWeeklySummaries.Any()) _context.StudentWeeklySummaries.RemoveRange(studentWeeklySummaries);

                    await _context.SaveChangesAsync();

                    // RAW SQL: Delete from untracked tables (Lectures/LecturePreps not in DbContext)
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM LecturePreps WHERE LectureId IN (SELECT Id FROM Lectures WHERE CourseId = {id})");
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM Lectures WHERE CourseId = {id}");

                    // LAYER 2: WeeklySummaries
                    var weeklySummaries = await _context.WeeklySummaries.Where(ws => ws.CourseId == id).ToListAsync();
                    if (weeklySummaries.Any()) _context.WeeklySummaries.RemoveRange(weeklySummaries);
                    await _context.SaveChangesAsync();

                    // LAYER 3: Intermediate children
                    var sessions = await _context.ClassSessions.Where(s => s.CourseId == id).ToListAsync();
                    if (sessions.Any()) _context.ClassSessions.RemoveRange(sessions);

                    var enrollments = await _context.CourseEnrollments.Where(e => e.CourseId == id).ToListAsync();
                    if (enrollments.Any()) _context.CourseEnrollments.RemoveRange(enrollments);

                    var classSummaries = await _context.ClassSummaries.Where(cs => cs.CourseId == id).ToListAsync();
                    if (classSummaries.Any()) _context.ClassSummaries.RemoveRange(classSummaries);

                    var syllabi = await _context.CourseSyllabi.Where(cs => cs.CourseId == id).ToListAsync();
                    if (syllabi.Any()) _context.CourseSyllabi.RemoveRange(syllabi);

                    await _context.SaveChangesAsync();

                    // LAYER 4: Course
                    _context.Courses.Remove(course);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                return StatusCode(500, new { error = $"Failed to delete course: {inner}" });
            }
        }

        // POST: api/admin/students/{id}/enroll-all
        [HttpPost("students/{id}/enroll-all")]
        public async Task<ActionResult> EnrollStudentInAllCourses(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                    return NotFound(new { error = "Student not found" });

                // The 10 standard course codes
                var standardCodes = new[] { "CSC301","CSC303","CSC305","CSC307","CSC309","MTH301","CSC311","CSC313","EEE301","GST301" };

                var courses = await _context.Courses
                    .Where(c => standardCodes.Contains(c.CourseCode))
                    .ToListAsync();

                int enrolled = 0, skipped = 0;
                foreach (var course in courses)
                {
                    var exists = await _context.CourseEnrollments
                        .AnyAsync(e => e.CourseId == course.Id && e.StudentId == id);
                    if (!exists)
                    {
                        _context.CourseEnrollments.Add(new CourseEnrollment
                        {
                            CourseId = course.Id,
                            StudentId = id,
                            EnrolledAt = DateTime.UtcNow
                        });

                        // Also link all existing summaries to this student
                        var existingSummaries = await _context.WeeklySummaries
                            .Where(ws => ws.CourseId == course.Id)
                            .ToListAsync();
                        foreach (var summary in existingSummaries)
                        {
                            _context.StudentWeeklySummaries.Add(new StudentWeeklySummary
                            {
                                StudentId = id,
                                WeeklySummaryId = summary.Id,
                                SentAt = DateTime.UtcNow
                            });
                        }
                        enrolled++;
                    }
                    else skipped++;
                }
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Enrolled in {enrolled} course(s). {skipped} already enrolled.", enrolled, skipped, totalFound = courses.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in all courses", id);
                return StatusCode(500, new { error = "Failed to enroll student" });
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

    public class CreateAdminCourseRequest
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CreditHours { get; set; } = 3;
        public int LecturerId { get; set; }
    }
}

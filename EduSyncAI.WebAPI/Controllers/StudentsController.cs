using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            EduSyncDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<StudentsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        // POST: api/students/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.MatricNumber == request.Username);

            if (student == null || !VerifyPassword(request.Password, student.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }

            if (!student.IsActive)
            {
                return Unauthorized(new { error = "Account is inactive. Please contact admin." });
            }

            var token = GenerateJwtToken(student);

            return Ok(new
            {
                token,
                student = new
                {
                    student.Id,
                    student.MatricNumber,
                    student.FullName,
                    student.Email,
                    student.PhotoPath
                }
            });
        }

        // GET: api/students/profile
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized();

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound();

            return Ok(new
            {
                student.Id,
                student.MatricNumber,
                student.FullName,
                student.Email,
                student.PhotoPath,
                student.Age,
                student.Hobbies,
                student.Bio
            });
        }

        // PUT: api/students/profile
        [HttpPost("profile")]
        public async Task<ActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == null)
                    return Unauthorized(new { error = "Not authenticated" });

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { error = "Student not found" });

                // Update basic info
                if (!string.IsNullOrEmpty(request.FullName))
                    student.FullName = request.FullName;

                if (!string.IsNullOrEmpty(request.Email))
                    student.Email = request.Email;

                if (request.Age.HasValue)
                    student.Age = request.Age;

                if (request.Hobbies != null)
                    student.Hobbies = request.Hobbies;

                if (request.Bio != null)
                    student.Bio = request.Bio;

                // Handle photo upload
                if (request.Photo != null && request.Photo.Length > 0)
                {
                    // Validate file type
                    var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var ext = Path.GetExtension(request.Photo.FileName).ToLowerInvariant();
                    if (!allowedTypes.Contains(ext))
                        return BadRequest(new { error = $"Invalid file type '{ext}'. Allowed: jpg, jpeg, png, gif, webp" });

                    // Validate file size (max 5MB)
                    if (request.Photo.Length > 5 * 1024 * 1024)
                        return BadRequest(new { error = "File too large. Maximum size is 5MB." });

                    // Resolve the full absolute path (avoids issues with '..' in FileStream)
                    var relativeUploadsPath = Path.Combine(_environment.ContentRootPath, "..", "Data", "uploads", "students");
                    var uploadsFolder = Path.GetFullPath(relativeUploadsPath);

                    _logger.LogInformation("Photo upload path resolved to: {Path}", uploadsFolder);
                    Directory.CreateDirectory(uploadsFolder);

                    var sanitizedMatric = student.MatricNumber.Replace("/", "_").Replace("\\", "_");
                    var fileName = $"{sanitizedMatric}_{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.Photo.CopyToAsync(stream);
                    }

                    student.PhotoPath = $"/uploads/students/{fileName}";
                    _logger.LogInformation("Photo saved successfully: {FileName}", fileName);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    student.Id,
                    student.MatricNumber,
                    student.FullName,
                    student.Email,
                    student.PhotoPath,
                    student.Age,
                    student.Hobbies,
                    student.Bio
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for student");
                // Return detailed error for debugging (you can remove 'detail' in production)
                return StatusCode(500, new { error = "Failed to update profile", detail = ex.Message });
            }
        }

        // GET: api/students/courses
        [HttpGet("courses")]
        public async Task<ActionResult> GetCourses()
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized();

            var courses = await _context.Courses.ToListAsync();
            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var coursesWithLecturers = new List<object>();
            foreach (var course in courses)
            {
                // Get lecturer info from ClassSessions
                var session = await _context.ClassSessions
                    .Where(s => s.CourseId == course.Id)
                    .FirstOrDefaultAsync();

                string lecturerName = "TBA";
                if (session != null)
                {
                    var lecturer = await _context.Lecturers.FindAsync(session.LecturerId);
                    if (lecturer != null)
                        lecturerName = lecturer.FullName;
                }

                coursesWithLecturers.Add(new
                {
                    course.Id,
                    course.CourseCode,
                    course.CourseTitle,
                    LecturerName = lecturerName,
                    IsEnrolled = enrolledCourseIds.Contains(course.Id)
                });
            }

            return Ok(coursesWithLecturers);
        }

        // GET: api/students/my-courses
        [HttpGet("my-courses")]
        public async Task<ActionResult> GetMyCourses()
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized();

            var enrollments = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId)
                .ToListAsync();

            var myCourses = new List<object>();
            foreach (var enrollment in enrollments)
            {
                var course = await _context.Courses.FindAsync(enrollment.CourseId);
                if (course != null)
                {
                    var session = await _context.ClassSessions
                        .Where(s => s.CourseId == course.Id)
                        .FirstOrDefaultAsync();

                    string lecturerName = "TBA";
                    if (session != null)
                    {
                        var lecturer = await _context.Lecturers.FindAsync(session.LecturerId);
                        if (lecturer != null)
                            lecturerName = lecturer.FullName;
                    }

                    myCourses.Add(new
                    {
                        course.Id,
                        course.CourseCode,
                        course.CourseTitle,
                        LecturerName = lecturerName,
                        enrollment.EnrolledAt
                    });
                }
            }

            return Ok(myCourses);
        }

        // POST: api/students/enroll/{courseId}
        [HttpPost("enroll/{courseId}")]
        public async Task<ActionResult> EnrollInCourse(int courseId)
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound(new { error = "Course not found" });

            var existingEnrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

            if (existingEnrollment != null)
                return BadRequest(new { error = "Already enrolled in this course" });

            var enrollment = new CourseEnrollment
            {
                StudentId = studentId.Value,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // AUTOMATION: Give student access to all existing weekly summaries for this course
            var existingSummaries = await _context.WeeklySummaries
                .Where(ws => ws.CourseId == courseId)
                .ToListAsync();

            foreach (var summary in existingSummaries)
            {
                _context.StudentWeeklySummaries.Add(new StudentWeeklySummary
                {
                    StudentId = studentId.Value,
                    WeeklySummaryId = summary.Id,
                    SentAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully enrolled in course" });
        }

        // DELETE: api/students/unenroll/{courseId}
        [HttpDelete("unenroll/{courseId}")]
        public async Task<ActionResult> UnenrollFromCourse(int courseId)
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized();

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

            if (enrollment == null)
                return NotFound(new { error = "Enrollment not found" });

            _context.CourseEnrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully unenrolled from course" });
        }

        // GET: api/students/class-summaries
        [HttpGet("class-summaries")]
        public async Task<ActionResult> GetClassSummaries()
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null)
                return Unauthorized();

            // Get enrolled course IDs
            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            // 1. Get traditional class summaries for enrolled courses
            var summaries = await _context.ClassSummaries
                .Where(s => enrolledCourseIds.Contains(s.CourseId))
                .OrderByDescending(s => s.ClassDate)
                .ToListAsync();

            var resultList = new List<object>();

            foreach (var summary in summaries)
            {
                var course = await _context.Courses.FindAsync(summary.CourseId);
                var lecturer = await _context.Lecturers.FindAsync(summary.LecturerId);

                // Try to find the session this summary belongs to
                var session = await _context.ClassSessions
                    .Where(s => s.CourseId == summary.CourseId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
                
                // Match by date (ignoring time)
                var matchingSession = session.FirstOrDefault(s => 
                    DateTime.TryParse(s.StartTime, out var sDate) && sDate.Date == summary.ClassDate.Date);

                resultList.Add(new
                {
                    summary.Id,
                    Title = summary.Title,
                    Summary = summary.Summary,
                    KeyTopics = summary.KeyTopics,
                    PreparationNotes = summary.PreparationNotes,
                    ClassDate = summary.ClassDate,
                    Type = "Class",
                    CourseName = course?.CourseTitle ?? "Unknown",
                    CourseCode = course?.CourseCode ?? "N/A",
                    LecturerName = lecturer?.FullName ?? "Unknown",
                    CourseId = summary.CourseId,
                    SessionId = matchingSession?.Id
                });
            }

            // 2. Get AI Weekly Summaries specifically sent to this student
            var studentWeeklySummaries = await _context.StudentWeeklySummaries
                .Where(sws => sws.StudentId == studentId)
                .Include(sws => sws.WeeklySummary)
                .ToListAsync();

            foreach (var sws in studentWeeklySummaries)
            {
                if (sws.WeeklySummary == null) continue;

                var summary = sws.WeeklySummary;
                var course = await _context.Courses.FindAsync(summary.CourseId);
                var lecturer = await _context.Lecturers.FindAsync(summary.LecturerId);

                resultList.Add(new
                {
                    summary.Id,
                    Title = summary.WeekTitle ?? $"Week {summary.WeekNumber} Summary",
                    Summary = summary.Summary,
                    KeyTopics = summary.KeyTopics, // This is a JSON string in WeeklySummary
                    PreparationNotes = summary.PreparationNotes,
                    ClassDate = sws.SentAt,
                    Type = "Weekly",
                    CourseName = course?.CourseTitle ?? "Unknown",
                    CourseCode = course?.CourseCode ?? "N/A",
                    LecturerName = lecturer?.FullName ?? "Unknown",
                    WeekNumber = summary.WeekNumber,
                    CourseId = summary.CourseId
                });
            }

            // Order by Week Number ascending for Weekly summaries, then by ClassDate descending for regular class summaries
    var finalResult = resultList.OrderBy(x => 
    {
        var dynamicX = (dynamic)x;
        // If it's a Weekly summary, use WeekNumber for top priority
        if (dynamicX.Type == "Weekly")
            return (int)dynamicX.WeekNumber;
        // Otherwise, push to the end (or handle differently)
        return 999;
    }).ThenByDescending(x => ((dynamic)x).ClassDate);

    return Ok(finalResult);
        }

        // Helper methods
        private int? GetStudentIdFromToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                var studentIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "studentId");
                if (studentIdClaim != null && int.TryParse(studentIdClaim.Value, out int studentId))
                    return studentId;
            }
            catch
            {
                return null;
            }

            return null;
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = Convert.ToBase64String(hashedBytes);
            return hash == passwordHash;
        }

        private string GenerateJwtToken(Student student)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Secret"] ?? "EduSyncAI-Super-Secret-Key-For-JWT-Authentication-Min-32-Chars"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("studentId", student.Id.ToString()),
                new Claim("matricNumber", student.MatricNumber),
                new Claim("role", "Student"),
                new Claim(JwtRegisteredClaimNames.Sub, student.MatricNumber),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "EduSyncAI",
                audience: "EduSyncAI-Web",
                claims: claims,
                expires: DateTime.Now.AddHours(int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class UpdateProfileRequest
        {
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public int? Age { get; set; }
            public string? Hobbies { get; set; }
            public string? Bio { get; set; }
            public IFormFile? Photo { get; set; }
        }
    }
}

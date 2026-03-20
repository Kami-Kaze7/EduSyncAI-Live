using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using OfficeOpenXml;
using System.Security.Cryptography;
using System.Text;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly ILogger<CoursesController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly EduSyncAI.WebAPI.Services.GeminiSummarizationService _geminiService;

        public CoursesController(
            EduSyncDbContext context, 
            ILogger<CoursesController> logger, 
            IWebHostEnvironment environment,
            EduSyncAI.WebAPI.Services.GeminiSummarizationService geminiService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _geminiService = geminiService;
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Resolves a stored file path (which may be a Windows or Linux absolute path)
        /// to the correct location on the current server.
        /// </summary>
        private string ResolveFilePath(string storedPath)
        {
            var normalized = storedPath.Replace('\\', '/');
            var dataIndex = normalized.IndexOf("/Data/", StringComparison.OrdinalIgnoreCase);
            if (dataIndex >= 0)
            {
                var relativePart = normalized.Substring(dataIndex + "/Data/".Length);
                var dataDir = Path.Combine(_environment.ContentRootPath, "..", "Data");
                return Path.GetFullPath(Path.Combine(dataDir, relativePart));
            }
            return storedPath;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            try
            {
                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null)
                {
                    // If not a lecturer, maybe return all courses for student view?
                    // Let's check if the request is from a student.
                    // For now, let's assume if it's not a lecturer, it's a general request.
                    // But the lecturer dashboard specifically calls this.
                    // If lecturerId is present, we filter.
                    var allCourses = await _context.Courses.ToListAsync();
                    return Ok(allCourses);
                }

                var courses = await _context.Courses
                    .Where(c => c.LecturerId == lecturerId)
                    .ToListAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses");
                return StatusCode(500, new { error = "Failed to fetch courses" });
            }
        }

        // GET: api/courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                // Check ownership if it's a lecturer
                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId != null && course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have access to this course" });
                }

                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to fetch course" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Course>> CreateCourse(Course course)
        {
            try
            {
                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null)
                {
                    return Unauthorized(new { error = "Only lecturers can create courses" });
                }

                course.LecturerId = lecturerId.Value;
                course.CreatedAt = DateTime.UtcNow;
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new { error = "Failed to create course" });
            }
        }

        // PUT: api/courses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, Course course)
        {
            if (id != course.Id)
            {
                return BadRequest(new { error = "Course ID mismatch" });
            }

            try
            {
                var existingCourse = await _context.Courses.FindAsync(id);
                if (existingCourse == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || existingCourse.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to update this course" });
                }

                // Update properties
                existingCourse.CourseCode = course.CourseCode;
                existingCourse.CourseTitle = course.CourseTitle;
                // Don't allow changing LecturerId via PUT
                
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CourseExists(id))
                {
                    return NotFound(new { error = "Course not found" });
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to update course" });
            }
        }

        // DELETE: api/courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to delete this course" });
                }

                // Delete related sessions (no need to delete notes/materials as those tables don't exist)
                var sessions = await _context.ClassSessions
                    .Where(s => s.CourseId == id)
                    .ToListAsync();
                _context.ClassSessions.RemoveRange(sessions);

                // Delete related enrollments
                var enrollments = await _context.CourseEnrollments
                    .Where(e => e.CourseId == id)
                    .ToListAsync();
                _context.CourseEnrollments.RemoveRange(enrollments);

                // Now delete the course
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                return StatusCode(500, new { error = $"Failed to delete course: {ex.Message}" });
            }
        }

        [HttpPost("{id}/syllabus")]
        public async Task<IActionResult> UploadSyllabus(int id, IFormFile file)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to modify this course" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = "Invalid file type. Allowed: .doc, .docx, .xls, .xlsx, .pdf" });
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { error = "File too large. Maximum size is 10MB" });
                }

                // Create directory
                var syllabusDir = Path.Combine(_environment.ContentRootPath, "..", "Data", "Syllabi", id.ToString());
                Directory.CreateDirectory(syllabusDir);

                // Save file
                var fileName = $"syllabus{extension}";
                var filePath = Path.Combine(syllabusDir, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update course
                course.SyllabusPath = filePath;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Syllabus uploaded successfully", fileName = fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading syllabus for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to upload syllabus" });
            }
        }

        // GET: api/courses/5/syllabus
        [HttpGet("{id}/syllabus")]
        public async Task<IActionResult> DownloadSyllabus(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null || string.IsNullOrEmpty(course.SyllabusPath))
                {
                    return NotFound(new { error = "Syllabus not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                // Both owners and students can download but we need to check if it's the right owner if a lecturer token is provided.
                // Wait, if it's a student, GetLecturerIdFromToken returns null.
                // For now let's just ensure if they ARE a lecturer, they own it.
                if (lecturerId != null && course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have access to this syllabus" });
                }
                // Students should be checked if they are enrolled, but let's stick to the lecturer isolation for now as requested.

                var resolvedPath = ResolveFilePath(course.SyllabusPath);
                if (!System.IO.File.Exists(resolvedPath))
                {
                    return NotFound(new { error = "Syllabus file not found" });
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(resolvedPath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                var contentType = GetContentType(resolvedPath);
                var fileName = Path.GetFileName(resolvedPath);

                return File(memory, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading syllabus for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to download syllabus" });
            }
        }

        [HttpPost("{id}/students/import")]
        public async Task<IActionResult> ImportStudents(int id, IFormFile file)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to import students to this course" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                // Validate file type
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".csv")
                {
                    return BadRequest(new { error = "Invalid file type. Allowed: .xlsx, .csv" });
                }

                var results = new
                {
                    created = 0,
                    enrolled = 0,
                    skipped = 0,
                    errors = new List<string>()
                };

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;

                        if (rowCount < 2)
                        {
                            return BadRequest(new { error = "Excel file is empty or has no data rows" });
                        }

                        // Expected columns: Full Name, Matric Number, Email
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var fullName = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                                var matricNumber = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                                var email = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(matricNumber) || string.IsNullOrEmpty(email))
                                {
                                    ((List<string>)results.errors).Add($"Row {row}: Missing required fields");
                                    continue;
                                }

                                // Check if student exists
                                var student = await _context.Students.FirstOrDefaultAsync(s => s.MatricNumber == matricNumber);
                                
                                if (student == null)
                                {
                                    // Create new student
                                    student = new Student
                                    {
                                        FullName = fullName,
                                        MatricNumber = matricNumber,
                                        Email = email
                                    };
                                    _context.Students.Add(student);
                                    await _context.SaveChangesAsync();
                                    results = new { created = results.created + 1, results.enrolled, results.skipped, results.errors };
                                }

                                // Check if already enrolled
                                var existingEnrollment = await _context.CourseEnrollments
                                    .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == student.Id);

                                if (existingEnrollment == null)
                        {
                            // Enroll student
                            var enrollment = new CourseEnrollment
                            {
                                CourseId = id,
                                StudentId = student.Id,
                                EnrolledAt = DateTime.UtcNow
                            };
                            _context.CourseEnrollments.Add(enrollment);
                            await _context.SaveChangesAsync();

                            // AUTOMATION: Give student access to all existing weekly summaries for this course
                            var existingSummaries = await _context.WeeklySummaries
                                .Where(ws => ws.CourseId == id)
                                .ToListAsync();

                            foreach (var summary in existingSummaries)
                            {
                                _context.StudentWeeklySummaries.Add(new StudentWeeklySummary
                                {
                                    StudentId = student.Id,
                                    WeeklySummaryId = summary.Id,
                                    SentAt = DateTime.UtcNow
                                });
                            }
                            await _context.SaveChangesAsync();

                            results = new { results.created, enrolled = results.enrolled + 1, results.skipped, results.errors };
                        }
                                else
                                {
                                    results = new { results.created, results.enrolled, skipped = results.skipped + 1, results.errors };
                                }
                            }
                            catch (Exception ex)
                            {
                                ((List<string>)results.errors).Add($"Row {row}: {ex.Message}");
                            }
                        }
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing students for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to import students" });
            }
        }

        [HttpPost("{id}/students")]
        public async Task<IActionResult> AddStudent(int id, [FromBody] AddStudentRequest request)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to add students to this course" });
                }

                // Check if student exists
                var student = await _context.Students.FirstOrDefaultAsync(s => s.MatricNumber == request.MatricNumber);
                
                if (student == null)
                {
                    // Create new student
                    student = new Student
                    {
                        FullName = request.FullName,
                        MatricNumber = request.MatricNumber,
                        Email = request.Email
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                }

                // Check if already enrolled
                var existingEnrollment = await _context.CourseEnrollments
                    .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == student.Id);

                if (existingEnrollment != null)
                {
                    return BadRequest(new { error = "Student already enrolled in this course" });
                }

                // Enroll student
        var enrollment = new CourseEnrollment
        {
            CourseId = id,
            StudentId = student.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // AUTOMATION: Give student access to all existing weekly summaries for this course
        var existingSummaries = await _context.WeeklySummaries
            .Where(ws => ws.CourseId == id)
            .ToListAsync();

        foreach (var summary in existingSummaries)
        {
            _context.StudentWeeklySummaries.Add(new StudentWeeklySummary
            {
                StudentId = student.Id,
                WeeklySummaryId = summary.Id,
                SentAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

                return Ok(new { message = "Student added successfully", studentId = student.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student to course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to add student" });
            }
        }

        // GET: api/courses/5/enrollments
        [HttpGet("{id}/enrollments")]
        public async Task<ActionResult<IEnumerable<CourseEnrollment>>> GetEnrollments(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to view enrollments for this course" });
                }

                var enrollments = await _context.CourseEnrollments
                    .Where(e => e.CourseId == id)
                    .Include(e => e.Student)
                    .ToListAsync();

                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to fetch enrollments" });
            }
        }

        // POST: api/courses/5/enrollments
        [HttpPost("{id}/enrollments")]
        public async Task<ActionResult> EnrollStudents(int id, [FromBody] List<int> studentIds)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to enroll students in this course" });
                }

                foreach (var studentId in studentIds)
                {
                    // Check if already enrolled
                    var existing = await _context.CourseEnrollments
                        .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == studentId);

                    if (existing == null)
                    {
                        _context.CourseEnrollments.Add(new CourseEnrollment
                        {
                            CourseId = id,
                            StudentId = studentId,
                            EnrolledAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Students enrolled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling students in course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to enroll students" });
            }
        }

        [HttpPost("{id}/syllabus/analyze")]
        public async Task<IActionResult> AnalyzeSyllabus(int id, IFormFile file)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(new { error = "Course not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || course.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to analyze this syllabus" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".docx", ".txt" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = "Invalid file type. Allowed: .pdf, .docx, .txt" });
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { error = "File too large. Maximum size is 10MB" });
                }

                // Create directory
                var syllabusDir = Path.Combine(_environment.ContentRootPath, "..", "Data", "Syllabi", id.ToString());
                Directory.CreateDirectory(syllabusDir);

                // Save file
                var fileName = $"syllabus_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(syllabusDir, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Extract text from file
                var docProcessor = new EduSyncAI.WebAPI.Services.DocumentProcessingService();
                var extractedText = await docProcessor.ExtractTextFromFileAsync(filePath);

                // Analyze with Gemini AI
                var analysis = await _geminiService.AnalyzeSyllabusAsync(extractedText);

                // Analysis done

                // Save to database
                var syllabus = new CourseSyllabus
                {
                    CourseId = id,
                    LecturerId = lecturerId.Value,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileType = extension.TrimStart('.'),
                    ExtractedText = extractedText,
                    TotalWeeks = analysis.TotalWeeks,
                    UploadedAt = DateTime.UtcNow
                };

                _context.CourseSyllabi.Add(syllabus);
                await _context.SaveChangesAsync();

                // AUTOMATION: Generate individual day summaries for each week (3 days per week = 36 total for 12 weeks)
                _logger.LogInformation("Starting batch day summary generation for course {CourseId}, {WeekCount} weeks x 3 days", id, analysis.TotalWeeks);
                
                var enrolledStudentIds = await _context.CourseEnrollments
                    .Where(e => e.CourseId == id)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                int successCount = 0;
                int failCount = 0;

                foreach (var week in analysis.Weeks)
                {
                    for (int dayNumber = 1; dayNumber <= 3; dayNumber++)
                    {
                        // Rate-limit protection: wait between API calls to avoid hitting Gemini limits
                        if (successCount + failCount > 0)
                        {
                            await Task.Delay(2500); // 2.5 seconds between calls
                        }

                        bool generated = false;
                        for (int retry = 0; retry < 3 && !generated; retry++)
                        {
                            try
                            {
                                if (retry > 0)
                                {
                                    _logger.LogWarning("Retry {Retry}/3 for Week {WeekNumber} Day {DayNumber}", retry + 1, week.WeekNumber, dayNumber);
                                    await Task.Delay(retry * 5000); // Exponential backoff: 5s, 10s
                                }

                                _logger.LogInformation("Generating summary for Week {WeekNumber} Day {DayNumber} ({Num}/{Total})", 
                                    week.WeekNumber, dayNumber, successCount + failCount + 1, analysis.TotalWeeks * 3);
                                var summaryResult = await _geminiService.SummarizeDayAsync(extractedText, week.WeekNumber, dayNumber);

                                var weeklySummary = new WeeklySummary
                                {
                                    SyllabusId = syllabus.Id,
                                    CourseId = id,
                                    LecturerId = syllabus.LecturerId,
                                    WeekNumber = week.WeekNumber,
                                    DayNumber = dayNumber,
                                    WeekTitle = summaryResult.WeekTitle,
                                    Summary = summaryResult.Summary,
                                    KeyTopics = System.Text.Json.JsonSerializer.Serialize(summaryResult.KeyTopics),
                                    LearningObjectives = System.Text.Json.JsonSerializer.Serialize(summaryResult.LearningObjectives),
                                    PreparationNotes = summaryResult.PreparationNotes,
                                    GeneratedAt = DateTime.UtcNow,
                                    SentToStudents = true,
                                    SentAt = DateTime.UtcNow
                                };

                                _context.WeeklySummaries.Add(weeklySummary);
                                await _context.SaveChangesAsync();

                                // Distribute to currently enrolled students
                                foreach (var studentId in enrolledStudentIds)
                                {
                                    _context.StudentWeeklySummaries.Add(new StudentWeeklySummary
                                    {
                                        StudentId = studentId,
                                        WeeklySummaryId = weeklySummary.Id,
                                        SentAt = DateTime.UtcNow
                                    });
                                }
                                await _context.SaveChangesAsync();
                                
                                generated = true;
                                successCount++;
                            }
                            catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("RESOURCE_EXHAUSTED"))
                            {
                                _logger.LogWarning("Rate limit hit for Week {WeekNumber} Day {DayNumber}, waiting before retry...", week.WeekNumber, dayNumber);
                                await Task.Delay(15000); // Wait 15 seconds on rate limit
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to generate summary for Week {WeekNumber} Day {DayNumber} (attempt {Retry})", week.WeekNumber, dayNumber, retry + 1);
                                if (retry == 2) failCount++; // Only count as failed after all retries exhausted
                            }
                        }
                    }
                }
                _logger.LogInformation("Batch summary generation complete: {Success} succeeded, {Failed} failed out of {Total}", successCount, failCount, analysis.TotalWeeks * 3);

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Syllabus analyzed and weekly summaries generated automatically.", 
                    syllabusId = syllabus.Id,
                    totalWeeks = analysis.TotalWeeks,
                    weeks = analysis.Weeks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing syllabus for course {CourseId}", id);
                return StatusCode(500, new { error = $"Failed to analyze syllabus: {ex.Message}" });
            }
        }

        // GET: api/courses/5/syllabus/info
        [HttpGet("{id}/syllabus/info")]
        public async Task<IActionResult> GetSyllabusInfo(int id)
        {
            try
            {
                var syllabus = await _context.CourseSyllabi
                    .Where(s => s.CourseId == id)
                    .OrderByDescending(s => s.UploadedAt)
                    .FirstOrDefaultAsync();

                if (syllabus == null)
                {
                    return NotFound(new { error = "No syllabus found for this course" });
                }

                return Ok(new
                {
                    id = syllabus.Id,
                    fileName = syllabus.FileName,
                    fileType = syllabus.FileType,
                    totalWeeks = syllabus.TotalWeeks,
                    uploadedAt = syllabus.UploadedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching syllabus info for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to fetch syllabus info" });
            }
        }

        // GET: api/courses/5/syllabus/download
        [HttpGet("{id}/syllabus/download")]
        public async Task<IActionResult> DownloadAnalyzedSyllabus(int id)
        {
            try
            {
                var syllabus = await _context.CourseSyllabi
                    .Where(s => s.CourseId == id)
                    .OrderByDescending(s => s.UploadedAt)
                    .FirstOrDefaultAsync();

                if (syllabus == null)
                {
                    return NotFound(new { error = "No syllabus found" });
                }

                if (!System.IO.File.Exists(syllabus.FilePath))
                {
                    return NotFound(new { error = "Syllabus file not found on server" });
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(syllabus.FilePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                var contentType = GetContentType(syllabus.FilePath);
                return File(memory, contentType, syllabus.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading syllabus for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to download syllabus" });
            }
        }

        // POST: api/courses/5/syllabus/summarize
        [HttpPost("{id}/syllabus/summarize")]
        public async Task<IActionResult> SummarizeWeek(int id, [FromBody] SummarizeWeekRequest request)
        {
            try
            {
                var syllabus = await _context.CourseSyllabi
                    .Where(s => s.CourseId == id)
                    .OrderByDescending(s => s.UploadedAt)
                    .FirstOrDefaultAsync();

                if (syllabus == null)
                {
                    return NotFound(new { error = "No syllabus found. Please upload and analyze a syllabus first." });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || syllabus.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to generate summaries for this course" });
                }

                if (string.IsNullOrEmpty(syllabus.ExtractedText))
                {
                    return BadRequest(new { error = "Syllabus text not available" });
                }

                // Check if summary already exists
                var existingSummary = await _context.WeeklySummaries
                    .FirstOrDefaultAsync(ws => ws.SyllabusId == syllabus.Id && ws.WeekNumber == request.WeekNumber);

                if (existingSummary != null)
                {
                    return Ok(new
                    {
                        message = "Summary already exists",
                        summaryId = existingSummary.Id,
                        weekNumber = existingSummary.WeekNumber,
                        weekTitle = existingSummary.WeekTitle,
                        summary = existingSummary.Summary,
                        keyTopics = existingSummary.KeyTopics,
                        learningObjectives = existingSummary.LearningObjectives,
                        preparationNotes = existingSummary.PreparationNotes
                    });
                }

                // Generate summary with Gemini AI
                var summaryResult = await _geminiService.SummarizeWeekAsync(syllabus.ExtractedText, request.WeekNumber);

                // Save to database
                var weeklySummary = new WeeklySummary
                {
                    SyllabusId = syllabus.Id,
                    CourseId = id,
                    LecturerId = syllabus.LecturerId,
                    WeekNumber = request.WeekNumber,
                    WeekTitle = summaryResult.WeekTitle,
                    Summary = summaryResult.Summary,
                    KeyTopics = System.Text.Json.JsonSerializer.Serialize(summaryResult.KeyTopics),
                    LearningObjectives = System.Text.Json.JsonSerializer.Serialize(summaryResult.LearningObjectives),
                    PreparationNotes = summaryResult.PreparationNotes,
                    GeneratedAt = DateTime.UtcNow
                };

                _context.WeeklySummaries.Add(weeklySummary);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Summary generated successfully",
                    summaryId = weeklySummary.Id,
                    weekNumber = weeklySummary.WeekNumber,
                    weekTitle = weeklySummary.WeekTitle,
                    summary = weeklySummary.Summary,
                    keyTopics = summaryResult.KeyTopics,
                    learningObjectives = summaryResult.LearningObjectives,
                    preparationNotes = weeklySummary.PreparationNotes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing week for course {CourseId}", id);
                return StatusCode(500, new { error = $"Failed to generate summary: {ex.Message}" });
            }
        }

        // GET: api/courses/5/summaries
        [HttpGet("{id}/summaries")]
        public async Task<IActionResult> GetCourseSummaries(int id)
        {
            try
            {
                var summaries = await _context.WeeklySummaries
                    .Where(ws => ws.CourseId == id)
                    .OrderBy(ws => ws.WeekNumber)
                    .ToListAsync();

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId != null)
                {
                    // If a lecturer is asking, make sure they own the course
                    var course = await _context.Courses.FindAsync(id);
                    if (course != null && course.LecturerId != lecturerId)
                    {
                        return Unauthorized(new { error = "You do not have access to these summaries" });
                    }
                }

                var result = summaries.Select(s => new
                {
                    id = s.Id,
                    weekNumber = s.WeekNumber,
                    weekTitle = s.WeekTitle,
                    summary = s.Summary,
                    keyTopics = s.KeyTopics,
                    learningObjectives = s.LearningObjectives,
                    preparationNotes = s.PreparationNotes,
                    sentToStudents = s.SentToStudents,
                    sentAt = s.SentAt,
                    generatedAt = s.GeneratedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching summaries for course {CourseId}", id);
                return StatusCode(500, new { error = "Failed to fetch summaries" });
            }
        }

        [HttpPost("{courseId}/summaries/{summaryId}/send")]
        public async Task<IActionResult> SendSummaryToStudents(int courseId, int summaryId, [FromBody] SendSummaryRequest request)
        {
            try
            {
                var summary = await _context.WeeklySummaries
                    .FirstOrDefaultAsync(ws => ws.Id == summaryId && ws.CourseId == courseId);

                if (summary == null)
                {
                    return NotFound(new { error = "Summary not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || summary.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to send this summary" });
                }

                if (request?.StudentIds == null || !request.StudentIds.Any())
                {
                    return BadRequest(new { error = "No students selected" });
                }

                _logger.LogInformation("Sending summary {SummaryId} to {Count} students", summaryId, request.StudentIds.Count);

                // Simulate sending to selected students
                foreach (var studentId in request.StudentIds)
                {
                    // In a real app, you would fetch the student email and send the notification here
                    _logger.LogInformation("Simulating sending summary to student {StudentId}", studentId);
                }

                // Record sending to selected students
                foreach (var studentId in request.StudentIds)
                {
                    // Check if already sent to this student to avoid duplicates
                    var exists = await _context.StudentWeeklySummaries
                        .AnyAsync(sws => sws.StudentId == studentId && sws.WeeklySummaryId == summaryId);
                    
                    if (!exists)
                    {
                        _context.StudentWeeklySummaries.Add(new StudentWeeklySummary
                        {
                            StudentId = studentId,
                            WeeklySummaryId = summaryId,
                            SentAt = DateTime.UtcNow
                        });
                    }
                    
                    _logger.LogInformation("Recording summary sent to student {StudentId}", studentId);
                }

                // Mark as sent
                summary.SentToStudents = true;
                summary.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Summary sent to {request.StudentIds.Count} students successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending summary {SummaryId}", summaryId);
                return StatusCode(500, new { error = "Failed to send summary" });
            }
        }

        public class SendSummaryRequest
        {
            public List<int> StudentIds { get; set; } = new List<int>();
        }

        [HttpDelete("{courseId}/summaries/{summaryId}")]
        public async Task<IActionResult> DeleteSummary([FromRoute] int courseId, [FromRoute] int summaryId)
        {
            _logger.LogInformation("Attempting to delete summary {SummaryId} for course {CourseId}", summaryId, courseId);
            try
            {
                var summary = await _context.WeeklySummaries
                    .FirstOrDefaultAsync(ws => ws.Id == summaryId && ws.CourseId == courseId);

                if (summary == null)
                {

                    return NotFound(new { error = "Summary not found" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || summary.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to delete this summary" });
                }

                // Delete related student records first to avoid FK constraint issues
                var studentSummaries = await _context.StudentWeeklySummaries
                    .Where(sws => sws.WeeklySummaryId == summaryId)
                    .ToListAsync();
                
                if (studentSummaries.Any())
                {
                    _context.StudentWeeklySummaries.RemoveRange(studentSummaries);
                }

                _context.WeeklySummaries.Remove(summary);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Summary deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting summary {SummaryId}", summaryId);
                var errorMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                return StatusCode(500, new { error = $"Failed to delete summary: {errorMsg}" });
            }
        }

        [HttpDelete("{id}/syllabus")]
        public async Task<IActionResult> DeleteSyllabus([FromRoute] int id)
        {
            _logger.LogInformation("Attempting to delete syllabus for course {CourseId}", id);
            try
            {
                var syllabus = await _context.CourseSyllabi
                    .Where(s => s.CourseId == id)
                    .OrderByDescending(s => s.UploadedAt)
                    .FirstOrDefaultAsync();

                if (syllabus == null)
                {
                    return NotFound(new { error = "No syllabus found for this course" });
                }

                var lecturerId = GetLecturerIdFromToken();
                if (lecturerId == null || syllabus.LecturerId != lecturerId)
                {
                    return Unauthorized(new { error = "You do not have permission to delete this syllabus" });
                }

                // 1. Get ALL summaries for this course (by both SyllabusId and CourseId to catch all rows)
                var summaries = await _context.WeeklySummaries
                    .Where(ws => ws.SyllabusId == syllabus.Id || ws.CourseId == id)
                    .ToListAsync();

                var summaryIds = summaries.Select(s => s.Id).ToList();

                // 2. Delete all StudentWeeklySummaries child records first (FK constraint)
                if (summaryIds.Any())
                {
                    var studentSummaries = await _context.StudentWeeklySummaries
                        .Where(sws => summaryIds.Contains(sws.WeeklySummaryId))
                        .ToListAsync();
                    if (studentSummaries.Any())
                        _context.StudentWeeklySummaries.RemoveRange(studentSummaries);
                    await _context.SaveChangesAsync();
                }

                // 3. Delete WeeklySummaries
                if (summaries.Any())
                {
                    _context.WeeklySummaries.RemoveRange(summaries);
                    await _context.SaveChangesAsync();
                }

                // 4. Delete physical file
                if (System.IO.File.Exists(syllabus.FilePath))
                {
                    try {
                        System.IO.File.Delete(syllabus.FilePath);
                        _logger.LogInformation("Deleted syllabus file: {FilePath}", syllabus.FilePath);
                    } catch (Exception ex) {
                        _logger.LogWarning(ex, "Could not delete physical file {FilePath}", syllabus.FilePath);
                    }
                }

                // 5. Delete syllabus record
                _logger.LogInformation("Removing syllabus record for syllabus ID {SyllabusId}", syllabus.Id);
                _context.CourseSyllabi.Remove(syllabus);
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted syllabus and associated data");

                return Ok(new { message = "Syllabus and all associated summaries deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting syllabus for course {CourseId}", id);
                var errorMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                return StatusCode(500, new { error = $"Failed to delete syllabus: {errorMsg}" });
            }
        }

        private async Task<bool> CourseExists(int id)
        {
            return await _context.Courses.AnyAsync(e => e.Id == id);
        }

        private int? GetLecturerIdFromToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                // AuthController uses ClaimTypes.NameIdentifier for the lecturer ID
                var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier) ??
                            jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid") ??
                            jwtToken.Claims.FirstOrDefault(c => c.Type == "id");

                if (claim != null && int.TryParse(claim.Value, out int id))
                    return id;
            }
            catch
            {
                return null;
            }

            return null;
        }

        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }

    public class AddStudentRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string MatricNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class SummarizeWeekRequest
    {
        public int WeekNumber { get; set; }
    }
}

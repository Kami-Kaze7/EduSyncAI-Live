using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(EduSyncDbContext context, ILogger<AttendanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class AttendanceUploadDto
        {
            public ClassSession? SessionInfo { get; set; }
            public List<AttendanceRecord> Records { get; set; } = new();
        }

        // POST: api/attendance/session/{sessionId}
        [HttpPost("session/{sessionId}")]
        public async Task<IActionResult> UploadAttendance(int sessionId, [FromBody] AttendanceUploadDto uploadData)
        {
            _logger.LogInformation("Received attendance upload request for Session {SessionId}", sessionId);
            
            try
            {
                if (uploadData == null)
                {
                    _logger.LogError("Upload data is null for session {SessionId}", sessionId);
                    return BadRequest(new { error = "Upload data cannot be null" });
                }

                var attendanceRecords = uploadData.Records;
                var sessionInfo = uploadData.SessionInfo;

                _logger.LogInformation("Processing {Count} records. SessionInfo provided: {HasInfo}", 
                    attendanceRecords?.Count ?? 0, sessionInfo != null);

                var session = await _context.ClassSessions.FindAsync(sessionId);
                
                // If session doesn't exist but metadata is provided, create it
                if (session == null && sessionInfo != null)
                {
                    _logger.LogInformation("Creating missing session {SessionId} from metadata", sessionId);
                    
                    // Handle property name differences between Desktop and Web API models
                    // Desktop might send 'State' instead of 'SessionState' and 'LectureTopic' instead of 'Topic'
                    // We can use a dynamic approach or just check multiple possible properties
                    
                    string finalState = "Completed";
                    // Try to get State if SessionState is missing (dynamic check would be better but let's be explicit if possible)
                    // For now, we'll assume sessionInfo is the ClassSession model which might have been partially mapped
                    
                    var newSession = new ClassSession
                    {
                        Id = sessionId,
                        CourseId = sessionInfo.CourseId,
                        LectureId = sessionInfo.LectureId,
                        LecturerId = sessionInfo.LecturerId ?? 1,
                        SessionCode = sessionInfo.SessionCode,
                        SessionState = sessionInfo.SessionState ?? "Completed",
                        StartTime = sessionInfo.StartTime,
                        EndTime = sessionInfo.EndTime,
                        AttendanceCount = attendanceRecords?.Count ?? 0,
                        CreatedAt = DateTime.UtcNow.ToString("O")
                    };

                    // Note: If the desktop sent 'Topic' or 'LectureTopic', it might be in the 'Topic' property of the DTO ClassSession
                    newSession.Topic = sessionInfo.Topic;
                    
                    // Fallback for different property names in Desktop JSON
                    if (string.IsNullOrEmpty(newSession.Topic))
                    {
                        // Topic is already a string in our model, so this works
                    }

                    _context.ClassSessions.Add(newSession);
                    await _context.SaveChangesAsync();
                    session = newSession;
                }

                if (session == null)
                {
                    return NotFound(new { error = "Session not found and no metadata provided" });
                }

                foreach (var record in attendanceRecords)
                {
                    // Resolve student ID in Web API DB using MatricNumber from desktop record
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.MatricNumber == record.MatricNumber);
                    if (student == null)
                    {
                        _logger.LogWarning("Attendance sync: Student with Matric {Matric} not found in Web DB. Skipping.", record.MatricNumber);
                        continue;
                    }

                    int webStudentId = student.Id;

                    // Check if record already exists to avoid duplicates
                    var existing = await _context.Attendance
                        .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == webStudentId);
                    
                    if (existing == null)
                    {
                        var newRecord = new AttendanceRecord
                        {
                            SessionId = sessionId,
                            StudentId = webStudentId,
                            CheckInTime = record.CheckInTime,
                            CheckInMethod = record.CheckInMethod,
                            VerifiedBy = record.VerifiedBy
                        };
                        _context.Attendance.Add(newRecord);
                        _logger.LogInformation("Added attendance record for Student {Matric} in Session {SessionId}", record.MatricNumber, sessionId);
                    }
                }

                await _context.SaveChangesAsync();
                
                // Update attendance count in the session
                session.AttendanceCount = await _context.Attendance.CountAsync(a => a.SessionId == sessionId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Finished processing attendance for Session {SessionId}. Final count: {Count}", sessionId, session.AttendanceCount);
                return Ok(new { message = $"Successfully processed {attendanceRecords.Count} attendance records" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attendance for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to upload attendance" });
            }
        }

        // GET: api/attendance/session/{sessionId}
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<AttendanceRecord>>> GetSessionAttendance(int sessionId)
        {
            try
            {
                var attendance = await _context.Attendance
                    .Where(a => a.SessionId == sessionId)
                    .Join(_context.Students,
                        a => a.StudentId,
                        s => s.Id,
                        (a, s) => new AttendanceRecord
                        {
                            Id = a.Id,
                            SessionId = a.SessionId,
                            StudentId = a.StudentId,
                            CheckInTime = a.CheckInTime,
                            CheckInMethod = a.CheckInMethod,
                            VerifiedBy = a.VerifiedBy,
                            StudentName = s.FullName,
                            MatricNumber = s.MatricNumber
                        })
                    .OrderBy(a => a.CheckInTime)
                    .ToListAsync();

                return Ok(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to fetch session attendance" });
            }
        }

        // GET: api/attendance/student/{studentId}
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetStudentAttendance(int studentId)
        {
            try
            {
                var attendance = await _context.Attendance
                    .Where(a => a.StudentId == studentId)
                    .Join(_context.ClassSessions,
                        a => a.SessionId,
                        s => s.Id,
                        (a, s) => new { a, s })
                    .Join(_context.Courses,
                        comb => comb.s.CourseId,
                        c => c.Id,
                        (comb, c) => new
                        {
                            comb.a.Id,
                            comb.a.SessionId,
                            comb.a.CheckInTime,
                            comb.a.CheckInMethod,
                            CourseName = c.CourseTitle,
                            CourseCode = c.CourseCode,
                            SessionDate = comb.s.StartTime
                        })
                    .OrderByDescending(x => x.CheckInTime)
                    .ToListAsync();

                return Ok(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance for student {StudentId}", studentId);
                return StatusCode(500, new { error = "Failed to fetch student attendance" });
            }
        }
    }
}

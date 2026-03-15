using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly ILogger<SessionsController> _logger;

        public SessionsController(EduSyncDbContext context, ILogger<SessionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/sessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClassSession>>> GetSessions(
            [FromQuery] int? courseId,
            [FromQuery] int? lecturerId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.ClassSessions.AsQueryable();

                if (courseId.HasValue)
                {
                    query = query.Where(s => s.CourseId == courseId.Value);
                }

                if (lecturerId.HasValue)
                {
                    query = query.Where(s => s.LecturerId == lecturerId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.ScheduledDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.ScheduledDate <= endDate.Value);
                }

                var sessions = await query
                    .OrderByDescending(s => s.Id) // Order by recent sessions
                    .Include(s => s.Course)
                    .Include(s => s.Notes)
                    .Include(s => s.Materials)
                    .ToListAsync();

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sessions");
                return StatusCode(500, new { error = "Failed to fetch sessions" });
            }
        }

        // GET: api/sessions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassSession>> GetSession(int id)
        {
            try
            {
                var session = await _context.ClassSessions
                    .Include(s => s.Course)
                    .Include(s => s.Notes)
                    .Include(s => s.Materials)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching session {SessionId}", id);
                return StatusCode(500, new { error = "Failed to fetch session" });
            }
        }

        // POST: api/sessions
        [HttpPost]
        public async Task<ActionResult<ClassSession>> CreateSession(ClassSession session)
        {
            try
            {
                _context.ClassSessions.Add(session);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session");
                return StatusCode(500, new { error = "Failed to create session" });
            }
        }

        // PUT: api/sessions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, ClassSession session)
        {
            if (id != session.Id)
            {
                return BadRequest(new { error = "Session ID mismatch" });
            }

            try
            {
                _context.Entry(session).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SessionExists(id))
                {
                    return NotFound(new { error = "Session not found" });
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session {SessionId}", id);
                return StatusCode(500, new { error = "Failed to update session" });
            }
        }

        // DELETE: api/sessions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            try
            {
                var session = await _context.ClassSessions.FindAsync(id);
                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                _context.ClassSessions.Remove(session);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", id);
                return StatusCode(500, new { error = "Failed to delete session" });
            }
        }

        // PUT: api/sessions/5/notes
        [HttpPut("{id}/notes")]
        public async Task<IActionResult> UpdateNotes(int id, [FromBody] string content)
        {
            try
            {
                var session = await _context.ClassSessions
                    .Include(s => s.Notes)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                if (session.Notes == null)
                {
                    session.Notes = new LectureNotes
                    {
                        SessionId = id,
                        Content = content,
                        LastModified = DateTime.UtcNow
                    };
                    _context.LectureNotes.Add(session.Notes);
                }
                else
                {
                    session.Notes.Content = content;
                    session.Notes.LastModified = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Notes updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notes for session {SessionId}", id);
                return StatusCode(500, new { error = "Failed to update notes" });
            }
        }

        private async Task<bool> SessionExists(int id)
        {
            return await _context.ClassSessions.AnyAsync(e => e.Id == id);
        }
    }
}

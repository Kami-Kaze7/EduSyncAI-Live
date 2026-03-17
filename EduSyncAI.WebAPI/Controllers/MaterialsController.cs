using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialsController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly ILogger<MaterialsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public MaterialsController(
            EduSyncDbContext context,
            ILogger<MaterialsController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Resolves a stored file path (which may be a Windows or Linux absolute path)
        /// to the correct location on the current server.
        /// </summary>
        private string ResolveFilePath(string storedPath)
        {
            // Extract the relative portion after "Data" directory
            // e.g., "C:\EduSyncAI\Data\LectureMaterials\file.mp4" -> "LectureMaterials/file.mp4"
            // e.g., "/opt/edusyncai/publish/Data/LectureMaterials/file.mp4" -> "LectureMaterials/file.mp4"
            var normalized = storedPath.Replace('\\', '/');
            var dataIndex = normalized.IndexOf("/Data/", StringComparison.OrdinalIgnoreCase);
            if (dataIndex >= 0)
            {
                var relativePart = normalized.Substring(dataIndex + "/Data/".Length);
                var dataDir = Path.Combine(_environment.ContentRootPath, "..", "Data");
                return Path.GetFullPath(Path.Combine(dataDir, relativePart));
            }
            // Fallback: just use the stored path as-is
            return storedPath;
        }

        // GET: api/materials/session/5
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<LectureMaterial>>> GetMaterials(int sessionId)
        {
            try
            {
                var materials = await _context.LectureMaterials
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.UploadedAt)
                    .ToListAsync();

                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching materials for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to fetch materials" });
            }
        }

        // GET: api/materials/student/{studentId}
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetStudentMaterials(int studentId)
        {
            try
            {
                var courseIds = await _context.CourseEnrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                var materials = await _context.LectureMaterials
                    .Join(_context.ClassSessions,
                        m => m.SessionId,
                        s => s.Id,
                        (m, s) => new { Material = m, Session = s })
                    .Join(_context.Courses,
                        ms => ms.Session.CourseId,
                        c => c.Id,
                        (ms, c) => new { ms.Material, ms.Session, Course = c })
                    .Where(x => courseIds.Contains(x.Course.Id))
                    .OrderByDescending(x => x.Material.UploadedAt)
                    .Select(x => new
                    {
                        x.Material.Id,
                        x.Material.FileName,
                        x.Material.FileType,
                        x.Material.FileSize,
                        x.Material.UploadedAt,
                        CourseName = x.Course.CourseTitle,
                        CourseCode = x.Course.CourseCode,
                        SessionDate = x.Session.StartTime
                    })
                    .ToListAsync();

                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching materials for student {StudentId}", studentId);
                return StatusCode(500, new { error = "Failed to fetch student materials" });
            }
        }

        // GET: api/materials/lecturer/{lecturerId}
        [HttpGet("lecturer/{lecturerId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetLecturerMaterials(int lecturerId)
        {
            try
            {
                var materials = await _context.LectureMaterials
                    .Join(_context.ClassSessions,
                        m => m.SessionId,
                        s => s.Id,
                        (m, s) => new { Material = m, Session = s })
                    .Join(_context.Courses,
                        ms => ms.Session.CourseId,
                        c => c.Id,
                        (ms, c) => new { ms.Material, ms.Session, Course = c })
                    .Where(x => x.Course.LecturerId == lecturerId)
                    .OrderByDescending(x => x.Material.UploadedAt)
                    .Select(x => new
                    {
                        x.Material.Id,
                        x.Material.FileName,
                        x.Material.FileType,
                        x.Material.FileSize,
                        x.Material.UploadedAt,
                        CourseName = x.Course.CourseTitle,
                        CourseCode = x.Course.CourseCode,
                        SessionDate = x.Session.StartTime
                    })
                    .ToListAsync();

                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching materials for lecturer {LecturerId}", lecturerId);
                return StatusCode(500, new { error = "Failed to fetch lecturer materials" });
            }
        }

        // POST: api/materials/session/5
        [HttpPost("session/{sessionId}")]
        public async Task<ActionResult<LectureMaterial>> UploadMaterial(int sessionId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                // Validate file size (max 200MB for video recordings)
                if (file.Length > 200 * 1024 * 1024)
                {
                    return BadRequest(new { error = "File size exceeds 200MB limit" });
                }

                // Ensure the session exists on the server (desktop app creates sessions locally)
                var session = await _context.ClassSessions.FindAsync(sessionId);
                if (session == null)
                {
                    _logger.LogInformation("Session {SessionId} not found on server, creating placeholder.", sessionId);
                    var placeholder = new ClassSession
                    {
                        Id = sessionId,
                        CourseId = 1,
                        LectureId = 1,
                        SessionState = "Ended",
                        StartTime = DateTime.UtcNow.ToString("O"),
                        EndTime = DateTime.UtcNow.ToString("O"),
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        Topic = "Desktop Recording Session"
                    };
                    _context.ClassSessions.Add(placeholder);
                    await _context.SaveChangesAsync();
                }

                // Create materials directory
                var materialsPath = Path.Combine(_environment.ContentRootPath, "..", "Data", "LectureMaterials");
                Directory.CreateDirectory(materialsPath);

                // Generate unique filename
                var fileName = $"{sessionId}_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
                var filePath = Path.Combine(materialsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create database record
                var material = new LectureMaterial
                {
                    SessionId = sessionId,
                    FileName = file.FileName,
                    FilePath = Path.GetFullPath(filePath),
                    FileType = Path.GetExtension(file.FileName),
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow
                };

                _context.LectureMaterials.Add(material);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMaterial), new { id = material.Id }, material);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading material for session {SessionId}", sessionId);
                var innerMsg = ex.InnerException?.Message ?? "No inner exception";
                return StatusCode(500, new { error = $"Failed to upload material: {ex.Message} | Inner: {innerMsg}" });
            }
        }

        // GET: api/materials/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LectureMaterial>> GetMaterial(int id)
        {
            try
            {
                var material = await _context.LectureMaterials.FindAsync(id);

                if (material == null)
                {
                    return NotFound(new { error = "Material not found" });
                }

                return Ok(material);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching material {MaterialId}", id);
                return StatusCode(500, new { error = "Failed to fetch material" });
            }
        }

        // GET: api/materials/5/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadMaterial(int id)
        {
            try
            {
                var material = await _context.LectureMaterials.FindAsync(id);

                if (material == null)
                {
                    return NotFound(new { error = "Material not found" });
                }

                var resolvedPath = ResolveFilePath(material.FilePath);
                _logger.LogInformation("Resolved path: {StoredPath} -> {ResolvedPath}", material.FilePath, resolvedPath);
                if (!System.IO.File.Exists(resolvedPath))
                {
                    return NotFound(new { error = $"File not found on server. Path: {resolvedPath}" });
                }

                var contentType = material.FileType.ToLower() switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".pdf" => "application/pdf",
                    ".mp4" => "video/mp4",
                    ".webm" => "video/webm",
                    ".avi" => "video/x-msvideo",
                    ".mov" => "video/quicktime",
                    _ => "application/octet-stream"
                };

                // Use PhysicalFile with enableRangeProcessing for proper video streaming
                // Range processing enables HTTP 206 Partial Content which HTML5 video needs
                return PhysicalFile(resolvedPath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading material {MaterialId}", id);
                return StatusCode(500, new { error = "Failed to download material" });
            }
        }

        // DELETE: api/materials/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            try
            {
                var material = await _context.LectureMaterials.FindAsync(id);
                if (material == null)
                {
                    return NotFound(new { error = "Material not found" });
                }

                // Delete file from disk
                var deletePath = ResolveFilePath(material.FilePath);
                if (System.IO.File.Exists(deletePath))
                {
                    System.IO.File.Delete(deletePath);
                }

                // Delete database record
                _context.LectureMaterials.Remove(material);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material {MaterialId}", id);
                return StatusCode(500, new { error = "Failed to delete material" });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;

namespace EduSyncAI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseVideosController : ControllerBase
    {
        private readonly EduSyncDbContext _context;

        public CourseVideosController(EduSyncDbContext context)
        {
            _context = context;
        }

        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<IEnumerable<CourseVideo>>> GetVideosForCourse(int courseId)
        {
            return await _context.CourseVideos
                .Where(v => v.CourseId == courseId)
                .OrderBy(v => v.OrderIndex)
                .ToListAsync();
        }

        public class VideoUploadDto
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string VideoUrl { get; set; } = string.Empty;
        }

        [HttpPost("course/{courseId}")]
        public async Task<ActionResult<CourseVideo>> AddVideoToCourse(int courseId, [FromBody] VideoUploadDto dto)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound("Course not found");

            var currentCount = await _context.CourseVideos.CountAsync(v => v.CourseId == courseId);

            var video = new CourseVideo
            {
                CourseId = courseId,
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = dto.VideoUrl,
                OrderIndex = currentCount, // Place at the end
                AddedAt = DateTime.UtcNow
            };

            _context.CourseVideos.Add(video);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Video link added safely.", video });
        }

        [HttpDelete("{videoId}")]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            var video = await _context.CourseVideos.FindAsync(videoId);
            if (video == null) return NotFound();

            _context.CourseVideos.Remove(video);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Video removed from course." });
        }
    }
}

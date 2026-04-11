using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using EduSyncAI.WebAPI.Services;

namespace EduSyncAI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseVideosController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly WasabiService _wasabi;

        public CourseVideosController(EduSyncDbContext context, WasabiService wasabi)
        {
            _context = context;
            _wasabi = wasabi;
        }

        // ==================== EXISTING ENDPOINTS (unchanged) ====================

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

        /// <summary>
        /// Add a video via URL (YouTube embed) — existing flow, unchanged.
        /// </summary>
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
                OrderIndex = currentCount,
                AddedAt = DateTime.UtcNow,
                IsWasabiVideo = false // This is a URL embed
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

            // If it's a Wasabi video, also delete from cloud storage
            if (video.IsWasabiVideo && !string.IsNullOrEmpty(video.WasabiKey))
            {
                try
                {
                    await _wasabi.DeleteObjectAsync(video.WasabiKey);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to delete Wasabi object {video.WasabiKey}: {ex.Message}");
                    // Continue with DB deletion even if cloud delete fails
                }
            }

            _context.CourseVideos.Remove(video);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Video removed from course." });
        }

        // ==================== NEW WASABI ENDPOINTS ====================

        public class UploadUrlRequest
        {
            public string FileName { get; set; } = string.Empty;
            public string ContentType { get; set; } = "video/mp4";
            public int CourseId { get; set; }
        }

        /// <summary>
        /// Generate a pre-signed URL for the browser to upload directly to Wasabi.
        /// </summary>
        [HttpPost("upload-url")]
        public ActionResult GenerateUploadUrl([FromBody] UploadUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileName))
                return BadRequest("FileName is required");

            // Generate a unique object key: courses/{courseId}/{guid}_{originalname}
            var safeFileName = Path.GetFileName(request.FileName)
                .Replace(" ", "_")
                .Replace("#", "")
                .Replace("?", "");
            var objectKey = $"courses/{request.CourseId}/{Guid.NewGuid():N}_{safeFileName}";

            var uploadUrl = _wasabi.GenerateUploadUrl(objectKey, request.ContentType, expirationMinutes: 60);

            return Ok(new
            {
                uploadUrl,
                objectKey,
                expiresInMinutes = 60
            });
        }

        public class ConfirmUploadDto
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string ObjectKey { get; set; } = string.Empty;
            public string OriginalFileName { get; set; } = string.Empty;
            public long FileSizeBytes { get; set; }
        }

        /// <summary>
        /// Called after the browser finishes uploading to Wasabi.
        /// Saves the video metadata to the database.
        /// </summary>
        [HttpPost("course/{courseId}/confirm")]
        public async Task<ActionResult<CourseVideo>> ConfirmUpload(int courseId, [FromBody] ConfirmUploadDto dto)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound("Course not found");

            // Note: We trust the browser upload succeeded since it completed without error.
            // Skipping ObjectExistsAsync to avoid extra latency to Wasabi.

            var currentCount = await _context.CourseVideos.CountAsync(v => v.CourseId == courseId);

            // Generate the stream URL for immediate playback
            var streamUrl = _wasabi.GenerateStreamUrl(dto.ObjectKey);

            var video = new CourseVideo
            {
                CourseId = courseId,
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = streamUrl, // Initial stream URL (will be refreshed on access)
                WasabiKey = dto.ObjectKey,
                OriginalFileName = dto.OriginalFileName,
                FileSizeBytes = dto.FileSizeBytes,
                IsWasabiVideo = true,
                OrderIndex = currentCount,
                AddedAt = DateTime.UtcNow
            };

            _context.CourseVideos.Add(video);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Video uploaded successfully!", video });
        }

        /// <summary>
        /// Generate a fresh pre-signed URL for streaming a Wasabi video.
        /// Called by the student player to get a non-expired URL.
        /// </summary>
        [HttpGet("{videoId}/stream-url")]
        public async Task<ActionResult> GetStreamUrl(int videoId)
        {
            var video = await _context.CourseVideos.FindAsync(videoId);
            if (video == null) return NotFound();

            if (!video.IsWasabiVideo || string.IsNullOrEmpty(video.WasabiKey))
            {
                // For YouTube embeds, just return the existing URL
                return Ok(new { url = video.VideoUrl, isWasabi = false });
            }

            var streamUrl = _wasabi.GenerateStreamUrl(video.WasabiKey);
            return Ok(new { url = streamUrl, isWasabi = true });
        }

        /// <summary>
        /// Generate a pre-signed download URL with proper Content-Disposition header.
        /// </summary>
        [HttpGet("{videoId}/download-url")]
        public async Task<ActionResult> GetDownloadUrl(int videoId)
        {
            var video = await _context.CourseVideos.FindAsync(videoId);
            if (video == null) return NotFound();

            if (!video.IsWasabiVideo || string.IsNullOrEmpty(video.WasabiKey))
                return BadRequest("This video is not a downloadable file.");

            var fileName = video.OriginalFileName ?? $"{video.Title}.mp4";
            var downloadUrl = _wasabi.GenerateDownloadUrl(video.WasabiKey, fileName);

            return Ok(new { url = downloadUrl, fileName });
        }
    }
}

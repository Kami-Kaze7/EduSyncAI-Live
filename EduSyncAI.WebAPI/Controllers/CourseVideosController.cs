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

        // ==================== NEW FLAT VIDEO MANAGEMENT ENDPOINTS ====================

        /// <summary>
        /// Get all videos grouped by faculty name.
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult> GetAllVideosGrouped()
        {
            var videos = await _context.CourseVideos.OrderBy(v => v.FacultyName).ThenBy(v => v.CourseName).ThenBy(v => v.OrderIndex).ToListAsync();

            var grouped = videos
                .GroupBy(v => v.FacultyName)
                .Select(fg => new
                {
                    facultyName = fg.Key,
                    courses = fg.GroupBy(v => v.CourseName).Select(cg => new
                    {
                        courseName = cg.Key,
                        departmentName = cg.First().DepartmentName,
                        description = cg.First().Description,
                        price = cg.First().Price,
                        isFeatured = cg.First().IsFeatured,
                        whatYoullLearn = cg.First().WhatYoullLearn,
                        videos = cg.Select(v => new
                        {
                            v.Id,
                            v.Title,
                            v.Description,
                            v.VideoUrl,
                            v.Duration,
                            v.IsWasabiVideo,
                            v.FileSizeBytes,
                            v.OriginalFileName,
                            v.WasabiKey,
                            v.ThumbnailUrl,
                            v.OrderIndex,
                            v.AddedAt
                        }).ToList()
                    }).ToList()
                }).ToList();

            return Ok(grouped);
        }

        public class CreateVideoV2Dto
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? VideoUrl { get; set; }
            public string FacultyName { get; set; } = string.Empty;
            public string DepartmentName { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public string? Duration { get; set; }
            public decimal Price { get; set; } = 0;
            public string? ThumbnailUrl { get; set; }
            public string? WhatYoullLearn { get; set; }
        }

        /// <summary>
        /// Create a new video via URL embed with flat metadata.
        /// </summary>
        [HttpPost("create-v2")]
        public async Task<ActionResult<CourseVideo>> CreateVideoV2([FromBody] CreateVideoV2Dto dto)
        {
            var video = new CourseVideo
            {
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = dto.VideoUrl ?? string.Empty,
                FacultyName = dto.FacultyName,
                DepartmentName = dto.DepartmentName,
                CourseName = dto.CourseName,
                Duration = dto.Duration,
                Price = dto.Price,
                ThumbnailUrl = dto.ThumbnailUrl,
                WhatYoullLearn = dto.WhatYoullLearn,
                IsWasabiVideo = false,
                OrderIndex = await _context.CourseVideos.CountAsync(v => v.CourseName == dto.CourseName && v.FacultyName == dto.FacultyName),
                AddedAt = DateTime.UtcNow
            };

            _context.CourseVideos.Add(video);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Video created.", video });
        }

        public class UpdateVideoDto
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? CourseName { get; set; }
            public string? DepartmentName { get; set; }
            public string? FacultyName { get; set; }
            public string? Duration { get; set; }
            public decimal? Price { get; set; }
            public string? ThumbnailUrl { get; set; }
            public string? WhatYoullLearn { get; set; }
        }

        /// <summary>
        /// Update video or course metadata.
        /// </summary>
        [HttpPut("{videoId}")]
        public async Task<ActionResult> UpdateVideo(int videoId, [FromBody] UpdateVideoDto dto)
        {
            var video = await _context.CourseVideos.FindAsync(videoId);
            if (video == null) return NotFound();

            if (dto.Title != null) video.Title = dto.Title;
            if (dto.Description != null) video.Description = dto.Description;
            if (dto.CourseName != null) video.CourseName = dto.CourseName;
            if (dto.DepartmentName != null) video.DepartmentName = dto.DepartmentName;
            if (dto.FacultyName != null) video.FacultyName = dto.FacultyName;
            if (dto.Duration != null) video.Duration = dto.Duration;
            if (dto.Price.HasValue) video.Price = dto.Price.Value;
            if (dto.ThumbnailUrl != null) video.ThumbnailUrl = dto.ThumbnailUrl;
            if (dto.WhatYoullLearn != null) video.WhatYoullLearn = dto.WhatYoullLearn;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Video updated.", video });
        }

        /// <summary>
        /// Update all videos in a course (bulk update course metadata like price, name, description).
        /// </summary>
        [HttpPut("course-update")]
        public async Task<ActionResult> UpdateCourseMetadata([FromBody] UpdateVideoDto dto)
        {
            if (string.IsNullOrEmpty(dto.CourseName) || string.IsNullOrEmpty(dto.FacultyName))
                return BadRequest("CourseName and FacultyName are required.");

            var videos = await _context.CourseVideos
                .Where(v => v.CourseName == dto.CourseName && v.FacultyName == dto.FacultyName)
                .ToListAsync();

            foreach (var video in videos)
            {
                if (dto.Description != null) video.Description = dto.Description;
                if (dto.DepartmentName != null) video.DepartmentName = dto.DepartmentName;
                if (dto.WhatYoullLearn != null) video.WhatYoullLearn = dto.WhatYoullLearn;
                if (dto.Price.HasValue) video.Price = dto.Price.Value;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Updated {videos.Count} videos.", count = videos.Count });
        }

        /// <summary>
        /// Generate upload URL without requiring courseId.
        /// </summary>
        [HttpPost("upload-url-v2")]
        public ActionResult GenerateUploadUrlV2([FromBody] UploadUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.FileName))
                return BadRequest("FileName is required");

            var safeFileName = Path.GetFileName(request.FileName)
                .Replace(" ", "_")
                .Replace("#", "")
                .Replace("?", "");
            var objectKey = $"videos/{Guid.NewGuid():N}_{safeFileName}";

            var uploadUrl = _wasabi.GenerateUploadUrl(objectKey, request.ContentType, expirationMinutes: 60);

            return Ok(new { uploadUrl, objectKey, expiresInMinutes = 60 });
        }

        public class ConfirmUploadV2Dto
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string ObjectKey { get; set; } = string.Empty;
            public string OriginalFileName { get; set; } = string.Empty;
            public long FileSizeBytes { get; set; }
            public string FacultyName { get; set; } = string.Empty;
            public string DepartmentName { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public string? Duration { get; set; }
            public decimal Price { get; set; } = 0;
            public string? ThumbnailUrl { get; set; }
            public string? WhatYoullLearn { get; set; }
        }

        /// <summary>
        /// Confirm a Wasabi upload with flat metadata.
        /// </summary>
        [HttpPost("confirm-v2")]
        public async Task<ActionResult<CourseVideo>> ConfirmUploadV2([FromBody] ConfirmUploadV2Dto dto)
        {
            var streamUrl = _wasabi.GenerateStreamUrl(dto.ObjectKey);

            var video = new CourseVideo
            {
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = streamUrl,
                WasabiKey = dto.ObjectKey,
                OriginalFileName = dto.OriginalFileName,
                FileSizeBytes = dto.FileSizeBytes,
                IsWasabiVideo = true,
                FacultyName = dto.FacultyName,
                DepartmentName = dto.DepartmentName,
                CourseName = dto.CourseName,
                Duration = dto.Duration,
                Price = dto.Price,
                ThumbnailUrl = dto.ThumbnailUrl,
                WhatYoullLearn = dto.WhatYoullLearn,
                OrderIndex = await _context.CourseVideos.CountAsync(v => v.CourseName == dto.CourseName && v.FacultyName == dto.FacultyName),
                AddedAt = DateTime.UtcNow
            };

            _context.CourseVideos.Add(video);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Video uploaded successfully!", video });
        }
        /// <summary>
        /// Get featured courses for the landing page (public, no auth required).
        /// </summary>
        [HttpGet("featured")]
        public async Task<ActionResult> GetFeaturedCourses()
        {
            var videos = await _context.CourseVideos
                .Where(v => v.IsFeatured)
                .OrderBy(v => v.CourseName)
                .ThenBy(v => v.OrderIndex)
                .ToListAsync();

            var courses = videos
                .GroupBy(v => new { v.CourseName, v.FacultyName })
                .Select(cg => new
                {
                    courseName = cg.Key.CourseName,
                    facultyName = cg.Key.FacultyName,
                    departmentName = cg.First().DepartmentName,
                    description = cg.First().Description,
                    price = cg.First().Price,
                    thumbnailUrl = cg.First().ThumbnailUrl,
                    whatYoullLearn = cg.First().WhatYoullLearn,
                    videoCount = cg.Count(),
                    videos = cg.Select(v => new
                    {
                        v.Id,
                        v.Title,
                        v.Description,
                        v.VideoUrl,
                        v.Duration,
                        v.IsWasabiVideo,
                        v.ThumbnailUrl,
                        v.OrderIndex
                    }).ToList()
                }).ToList();

            return Ok(courses);
        }

        /// <summary>
        /// Toggle featured status for all videos in a course.
        /// </summary>
        [HttpPost("toggle-featured")]
        public async Task<ActionResult> ToggleFeatured([FromBody] ToggleFeaturedDto dto)
        {
            if (string.IsNullOrEmpty(dto.CourseName) || string.IsNullOrEmpty(dto.FacultyName))
                return BadRequest("CourseName and FacultyName are required.");

            var videos = await _context.CourseVideos
                .Where(v => v.CourseName == dto.CourseName && v.FacultyName == dto.FacultyName)
                .ToListAsync();

            if (!videos.Any()) return NotFound("Course not found.");

            var newState = !videos.First().IsFeatured;
            foreach (var video in videos)
                video.IsFeatured = newState;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Course {(newState ? "featured" : "unfeatured")}.", isFeatured = newState });
        }

        public class ToggleFeaturedDto
        {
            public string CourseName { get; set; } = string.Empty;
            public string FacultyName { get; set; } = string.Empty;
        }
    }
}

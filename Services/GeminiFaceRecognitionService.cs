using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EduSyncAI
{
    /// <summary>
    /// Service for facial recognition using Gemini AI natively in C#
    /// </summary>
    public class GeminiFaceRecognitionService
    {
        private readonly GeminiVisionService _visionService;
        private readonly DatabaseService _dbService;

        public GeminiFaceRecognitionService()
        {
            _visionService = new GeminiVisionService();
            _dbService = new DatabaseService();
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await Task.FromResult(true);
        }

        public async Task<FaceRecognitionResult> RecognizeFacesAsync(int sessionId, string base64Image)
        {
            try
            {
                var session = _dbService.GetClassSessionById(sessionId);
                if (session == null) return new FaceRecognitionResult { Success = false, Error = "Session not found." };

                var enrolledStudents = _dbService.GetEnrolledStudents(session.CourseId);
                var studentsWithPhotos = enrolledStudents
                    .Where(s => !string.IsNullOrEmpty(s.PhotoPath))
                    .Select(s => new StudentProfile
                    {
                        Id = s.Id,
                        FullName = s.FullName,
                        PhotoPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", s.PhotoPath.TrimStart('/')))
                    })
                    .ToList();

                var validStudents = studentsWithPhotos.Where(s => File.Exists(s.PhotoPath)).ToList();
                if (validStudents.Count == 0)
                {
                    return new FaceRecognitionResult { Success = false, Error = "No students with profile pictures found for this course." };
                }

                byte[] classroomBytes = Convert.FromBase64String(base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image);
                return await _visionService.RecognizeFacesAsync(classroomBytes, validStudents);
            }
            catch (Exception ex)
            {
                return new FaceRecognitionResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<int> MarkAttendanceAsync(int sessionId, List<FaceMatch> matches)
        {
            try
            {
                int markedCount = 0;
                foreach (var match in matches)
                {
                    var record = new AttendanceRecord
                    {
                        SessionId = sessionId,
                        StudentId = match.StudentId,
                        CheckInTime = DateTime.Now,
                        CheckInMethod = "Facial"
                    };
                    
                    int id = _dbService.MarkAttendance(record);
                    if (id > 0) markedCount++;
                }
                return await Task.FromResult(markedCount);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> StartCameraAsync() => await Task.FromResult(true);
        public async Task<string?> CaptureFrameAsync() => await Task.FromResult<string?>(null);
        public async Task<bool> StopCameraAsync() => await Task.FromResult(true);
        public async Task<string?> GetCurrentFrameAsync() => await Task.FromResult<string?>(null);
    }

    #region Response Models

    public class FaceRecognitionResult
    {
        public bool Success { get; set; }
        public List<FaceMatch> Matches { get; set; } = new();
        public int TotalDetectedFaces { get; set; }
        public int UnmatchedFaces { get; set; }
        public string? Error { get; set; }
    }

    public class FaceMatch
    {
        [Newtonsoft.Json.JsonProperty("student_id")]
        public int StudentId { get; set; }
        
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("confidence")]
        public double Confidence { get; set; }
    }

    public class FaceRecognitionResponse
    {
        [Newtonsoft.Json.JsonProperty("matches")]
        public List<FaceMatch> Matches { get; set; } = new();
        
        [Newtonsoft.Json.JsonProperty("total_detected_faces")]
        public int TotalDetectedFaces { get; set; }
        
        [Newtonsoft.Json.JsonProperty("unmatched_faces")]
        public int UnmatchedFaces { get; set; }
    }

    public class CameraFrameResponse
    {
        public string Image { get; set; } = string.Empty;
    }

    public class MarkAttendanceResponse
    {
        public string Status { get; set; } = string.Empty;
        public int MarkedCount { get; set; }
    }

    #endregion
}

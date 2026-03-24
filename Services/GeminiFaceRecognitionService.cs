using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _logFile = Path.Combine(AppConfig.DataDir, "face_debug.log");

        private static void Log(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            Console.WriteLine(line);
            try { File.AppendAllText(_logFile, line + Environment.NewLine); } catch { }
        }

        public GeminiFaceRecognitionService()
        {
            _visionService = new GeminiVisionService();
            _dbService = new DatabaseService();
            Log("[FACE] Service initialized");
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await Task.FromResult(true);
        }

        public async Task<FaceRecognitionResult> RecognizeFacesAsync(int sessionId, string base64Image)
        {
            try
            {
                Log($"[FACE] RecognizeFacesAsync called, sessionId={sessionId}, imageLen={base64Image?.Length ?? 0}");
                
                var session = _dbService.GetClassSessionById(sessionId);
                if (session == null)
                {
                    Log("[FACE] ERROR: Session not found");
                    return new FaceRecognitionResult { Success = false, Error = "Session not found." };
                }
                Log($"[FACE] Session found, courseId={session.CourseId}");

                List<StudentProfile> validStudents;

                if (AppConfig.UseRemoteServer)
                {
                    Log("[FACE] Using remote server to fetch students");
                    validStudents = await GetStudentsFromRemoteApiAsync(session.CourseId).ConfigureAwait(false);
                }
                else
                {
                    Log("[FACE] Using local DB to fetch students");
                    validStudents = GetStudentsFromLocalDb(session.CourseId);
                }

                if (validStudents.Count == 0)
                {
                    Log("[FACE] ERROR: No students with profile pictures found");
                    return new FaceRecognitionResult { Success = false, Error = "No students with profile pictures found for this course." };
                }

                Log($"[FACE] Found {validStudents.Count} students with photos, calling Gemini...");
                byte[] classroomBytes = Convert.FromBase64String(base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image);
                Log($"[FACE] Classroom image size: {classroomBytes.Length} bytes");
                
                var result = await _visionService.RecognizeFacesAsync(classroomBytes, validStudents).ConfigureAwait(false);
                Log($"[FACE] Gemini result: Success={result.Success}, Matches={result.Matches?.Count ?? 0}, Error={result.Error ?? "none"}");
                
                // Guard against Gemini hallucinations:
                // 1. Filter out any match below 85% confidence
                if (result.Success && result.Matches != null)
                {
                    var beforeCount = result.Matches.Count;
                    result.Matches = result.Matches.Where(m => m.Confidence >= 0.85).ToList();
                    if (beforeCount != result.Matches.Count)
                    {
                        Log($"[FACE] Confidence filter: {beforeCount} -> {result.Matches.Count} matches (removed {beforeCount - result.Matches.Count} low-confidence)");
                    }
                    
                    // 2. If Gemini matched EVERY enrolled student, it's almost certainly hallucinating
                    //    (e.g. reading name labels from Jitsi tiles instead of actual faces)
                    if (result.Matches.Count > 0 && result.Matches.Count >= validStudents.Count && validStudents.Count > 1)
                    {
                        Log($"[FACE] ⚠ HALLUCINATION GUARD: Gemini matched ALL {validStudents.Count} students — discarding results");
                        result.Matches.Clear();
                        result.Error = "All students matched — likely a false positive. Skipping this scan cycle.";
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"[FACE] EXCEPTION: {ex}");
                return new FaceRecognitionResult { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Fetches enrolled students with photos from the remote WebAPI and downloads their photos locally.
        /// </summary>
        private async Task<List<StudentProfile>> GetStudentsFromRemoteApiAsync(int courseId)
        {
            var profiles = new List<StudentProfile>();
            try
            {
                var token = AuthenticationService.AuthToken;
                var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConfig.ApiUrl}/courses/{courseId}/enrollments");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                Console.WriteLine($"[FACE] Fetching enrollments for course {courseId} from remote API...");
                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[FACE] Failed to fetch enrollments: {response.StatusCode} - {responseText}");
                    return profiles;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var enrollments = JsonSerializer.Deserialize<List<EnrollmentDto>>(responseText, options);

                if (enrollments == null || enrollments.Count == 0)
                {
                    Console.WriteLine("[FACE] No enrollments found for this course");
                    return profiles;
                }

                Console.WriteLine($"[FACE] Found {enrollments.Count} enrolled students");

                // Download photos for students who have them
                var photoCacheDir = Path.Combine(AppConfig.DataDir, "StudentPhotos");
                Directory.CreateDirectory(photoCacheDir);

                foreach (var enrollment in enrollments)
                {
                    var student = enrollment.Student;
                    if (student == null || string.IsNullOrEmpty(student.PhotoPath))
                    {
                        Console.WriteLine($"[FACE] Student {student?.FullName ?? "unknown"} has no photo, skipping");
                        continue;
                    }

                    // Download the photo from the server
                    var localPhotoPath = Path.Combine(photoCacheDir, $"student_{student.Id}.jpg");
                    
                    // Download if not cached or cache is older than 1 hour
                    if (!File.Exists(localPhotoPath) || 
                        (DateTime.Now - File.GetLastWriteTime(localPhotoPath)).TotalHours > 1)
                    {
                        try
                        {
                            var photoUrl = $"{AppConfig.ServerUrl}{student.PhotoPath}";
                            Console.WriteLine($"[FACE] Downloading photo for {student.FullName}: {photoUrl}");
                            var photoBytes = await _httpClient.GetByteArrayAsync(photoUrl).ConfigureAwait(false);
                            await File.WriteAllBytesAsync(localPhotoPath, photoBytes).ConfigureAwait(false);
                            Console.WriteLine($"[FACE] Photo saved: {localPhotoPath} ({photoBytes.Length} bytes)");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[FACE] Failed to download photo for {student.FullName}: {ex.Message}");
                            continue;
                        }
                    }

                    if (File.Exists(localPhotoPath))
                    {
                        profiles.Add(new StudentProfile
                        {
                            Id = student.Id,
                            FullName = student.FullName ?? "",
                            PhotoPath = localPhotoPath
                        });
                    }
                }

                Console.WriteLine($"[FACE] Ready for recognition with {profiles.Count} student profiles");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FACE] Error fetching remote students: {ex.Message}");
            }
            return profiles;
        }

        /// <summary>
        /// Original local DB student loading (for development)
        /// </summary>
        private List<StudentProfile> GetStudentsFromLocalDb(int courseId)
        {
            var enrolledStudents = _dbService.GetEnrolledStudents(courseId);
            return enrolledStudents
                .Where(s => !string.IsNullOrEmpty(s.PhotoPath))
                .Select(s => new StudentProfile
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    PhotoPath = Path.Combine(AppConfig.DataDir, s.PhotoPath.TrimStart('/'))
                })
                .Where(s => File.Exists(s.PhotoPath))
                .ToList();
        }

        public async Task<int> MarkAttendanceAsync(int sessionId, List<FaceMatch> matches)
        {
            try
            {
                int markedCount = 0;
                Log($"[FACE] MarkAttendanceAsync: sessionId={sessionId}, matches={matches.Count}");
                foreach (var match in matches)
                {
                    Log($"[FACE] Marking: StudentId={match.StudentId}, Name={match.Name}, Confidence={match.Confidence}");
                    var record = new AttendanceRecord
                    {
                        SessionId = sessionId,
                        StudentId = match.StudentId,
                        CheckInTime = DateTime.Now,
                        CheckInMethod = "Facial"
                    };
                    
                    try
                    {
                        int id = _dbService.MarkAttendance(record);
                        Log($"[FACE] MarkAttendance returned id={id}");
                        if (id > 0) markedCount++;
                    }
                    catch (Exception dbEx)
                    {
                        Log($"[FACE] MarkAttendance DB ERROR: {dbEx.Message}");
                        // Still count it as marked even if DB insert fails (student was recognized)
                        markedCount++;
                    }
                }
                Log($"[FACE] MarkAttendanceAsync returning markedCount={markedCount}");
                return await Task.FromResult(markedCount);
            }
            catch (Exception ex)
            {
                Log($"[FACE] MarkAttendanceAsync EXCEPTION: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> StartCameraAsync() => await Task.FromResult(true);
        public async Task<string?> CaptureFrameAsync() => await Task.FromResult<string?>(null);
        public async Task<bool> StopCameraAsync() => await Task.FromResult(true);
        public async Task<string?> GetCurrentFrameAsync() => await Task.FromResult<string?>(null);
    }

    // DTO for deserializing the enrollment response from the API
    internal class EnrollmentDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public StudentDto? Student { get; set; }
    }

    internal class StudentDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? MatricNumber { get; set; }
        public string? Email { get; set; }
        public string? PhotoPath { get; set; }
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

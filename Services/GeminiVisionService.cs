using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EduSyncAI
{
    public class GeminiVisionService
    {
        private static readonly string API_KEY = "AIzaSyAvSaTdksyJd1H2IaSGWbBrD1WJd1zLXSA";
        private static readonly string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key=" + API_KEY;
        
        private readonly HttpClient _httpClient;

        public GeminiVisionService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<FaceRecognitionResult> RecognizeFacesAsync(byte[] classroomImage, List<StudentProfile> enrolledStudents)
        {
            try
            {
                if (enrolledStudents == null || enrolledStudents.Count == 0)
                {
                    return new FaceRecognitionResult { Success = false, Error = "No enrolled students found." };
                }

                // Prepare parts for Gemini API
                var parts = new List<object>();

                // 1. Instructions Prompt
                string studentList = string.Join("\n", enrolledStudents.Select((s, i) => $"{i + 1}. {s.FullName} (ID: {s.Id})"));
                
                string prompt = $@"You are a facial recognition system for classroom attendance.

TASK: Compare faces in the 'CLASSROOM_SNAPSHOT' with the provided 'REFERENCE_STUDENT_PHOTOS'.

REFERENCE STUDENTS:
{studentList}

INSTRUCTIONS:
1. Examine the 'CLASSROOM_SNAPSHOT' (the first image provided).
2. Compare each detected face with the 'REFERENCE_STUDENT_PHOTOS' (provided in order after the snapshot).
3. Only return matches if you are VERY confident (>80%).
4. If a face in the classroom does not clearly match any reference photo, do NOT match it.
5. Be strict - false positives are not acceptable.

RESPONSE FORMAT (JSON only, no other text):
{{
  ""matches"": [
    {{""student_id"": 1, ""name"": ""John Doe"", ""confidence"": 0.95}},
    {{""student_id"": 2, ""name"": ""Jane Smith"", ""confidence"": 0.88}}
  ],
  ""total_detected_faces"": 5,
  ""unmatched_faces"": 2
}}

Return ONLY the JSON, nothing else.";

                parts.Add(new { text = prompt });

                // 2. Classroom Snapshot
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = "image/jpeg",
                        data = Convert.ToBase64String(classroomImage)
                    }
                });

                // 3. Reference Student Photos
                foreach (var student in enrolledStudents)
                {
                    if (File.Exists(student.PhotoPath))
                    {
                        byte[] photoBytes = File.ReadAllBytes(student.PhotoPath);
                        parts.Add(new
                        {
                            inline_data = new
                            {
                                mime_type = GetMimeType(student.PhotoPath),
                                data = Convert.ToBase64String(photoBytes)
                            }
                        });
                    }
                }

                // Construct Request Body
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = parts.ToArray() }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        topP = 0.95,
                        topK = 40
                    }
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GEMINI_API_URL, content);
                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new FaceRecognitionResult { Success = false, Error = $"API Error ({response.StatusCode}): {responseJson}" };
                }

                // Parse Gemini Response
                var geminiResult = JObject.Parse(responseJson);
                string? textResponse = geminiResult["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                if (string.IsNullOrEmpty(textResponse))
                {
                    return new FaceRecognitionResult { Success = false, Error = "Empty response from Gemini." };
                }

                // Robust JSON extraction: Find the first '{' and the last '}'
                // This handles cases where Gemini wraps the response in markdown code blocks like ```json ... ```
                var firstBrace = textResponse.IndexOf('{');
                var lastBrace = textResponse.LastIndexOf('}');

                if (firstBrace != -1 && lastBrace != -1 && lastBrace > firstBrace)
                {
                    textResponse = textResponse.Substring(firstBrace, lastBrace - firstBrace + 1);
                }

                // Parse the inner JSON that Gemini returned
                var result = JsonConvert.DeserializeObject<FaceRecognitionResponse>(textResponse);

                return new FaceRecognitionResult
                {
                    Success = true,
                    Matches = result?.Matches ?? new List<FaceMatch>(),
                    TotalDetectedFaces = result?.TotalDetectedFaces ?? 0,
                    UnmatchedFaces = result?.UnmatchedFaces ?? 0
                };
            }
            catch (Exception ex)
            {
                return new FaceRecognitionResult { Success = false, Error = ex.Message };
            }
        }

        private string GetMimeType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }
    }

    public class StudentProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string PhotoPath { get; set; } = "";
    }
}

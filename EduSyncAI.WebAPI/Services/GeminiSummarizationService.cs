using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace EduSyncAI.WebAPI.Services
{
    public class GeminiSummarizationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string GEMINI_MODEL = "gemini-2.5-flash";
        private const string GEMINI_API_URL = $"https://generativelanguage.googleapis.com/v1/models/{GEMINI_MODEL}:generateContent";

        public GeminiSummarizationService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = (configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "").Trim();
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                Console.WriteLine("⚠️ WARNING: Gemini API Key not found in configuration or environment variables");
            }
            else
            {
                Console.WriteLine($"✅ GeminiSummarizationService initialized with key length: {_apiKey.Length}");
            }
        }

        public async Task<SyllabusAnalysisResult> AnalyzeSyllabusAsync(string extractedText)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                Console.WriteLine("⚠️ WARNING: Attempted to analyze empty syllabus text");
                return new SyllabusAnalysisResult { TotalWeeks = 0, Weeks = new List<WeekInfo>() };
            }

            var prompt = $@"Analyze this course syllabus and identify the week-by-week structure.

Return a JSON object with this exact format:
{{
  ""totalWeeks"": <number>,
  ""weeks"": [
    {{""weekNumber"": 1, ""title"": ""Week 1 title""}},
    {{""weekNumber"": 2, ""title"": ""Week 2 title""}}
  ]
}}

Syllabus content:
{extractedText}

IMPORTANT: Return ONLY the JSON object, no additional text.";

            try
            {
                var response = await CallGeminiAPIAsync(prompt);
                var result = JsonSerializer.Deserialize<SyllabusAnalysisResult>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result ?? new SyllabusAnalysisResult { TotalWeeks = 0, Weeks = new List<WeekInfo>() };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in AnalyzeSyllabusAsync: {ex.Message}");
                throw; // Rethrow so the controller can handle the error
            }
        }

        public async Task<WeeklySummaryResult> SummarizeWeekAsync(string syllabusText, int weekNumber)
        {
            if (string.IsNullOrWhiteSpace(syllabusText))
            {
                Console.WriteLine($"⚠️ WARNING: Attempted to summarize Week {weekNumber} with empty syllabus text");
                return new WeeklySummaryResult { WeekTitle = $"Week {weekNumber}", Summary = "Syllabus text is empty" };
            }

            var prompt = $@"You are a helpful and experienced university lecturer. Your task is to create a weekly summary for Week {weekNumber} based on the course syllabus.

Instead of a dry summary, you should write as if you are teaching the student. Explain each concept clearly, use relatable examples, and maintain an encouraging, academic tone.

Requirements for the 'summary' field:
1. Identify each day the class meets (e.g., Monday, Wednesday, Friday) and provide a descriptive header for that day.
2. For each day, identify the main topics and subtopics.
3. Provide a narrative explanation for each topic. Explain it like a lecturer teaching a student in a classroom.
4. Use bullet points for specific examples, lists, or technical breakdowns.
5. Use bolding to emphasize key terms and definitions.
6. For each day, provide a short concluding sentence or a 'Main Lesson' to wrap up the concepts.
7. The formatting should be clean and readable, using Markdown extensively (headers, bolding, bullet points).

Return a JSON object with this exact format:
{{
  ""weekTitle"": ""Title of the week"",
  ""summary"": ""Detailed, narrative summary written in the persona of a lecturer teaching the material"",
  ""keyTopics"": [""topic1"", ""topic2"", ""topic3""],
  ""learningObjectives"": [""objective1"", ""objective2""],
  ""preparationNotes"": ""What students should do to prepare""
}}

Syllabus content:
{syllabusText}

Focus specifically on Week {weekNumber}. Return ONLY the JSON object, no additional text.";

            try
            {
                var response = await CallGeminiAPIAsync(prompt);
                var result = JsonSerializer.Deserialize<WeeklySummaryResult>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result ?? new WeeklySummaryResult 
                { 
                    WeekTitle = $"Week {weekNumber}",
                    Summary = "",
                    KeyTopics = new List<string>(),
                    LearningObjectives = new List<string>(),
                    PreparationNotes = ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SummarizeWeekAsync: {ex.Message}");
                throw; // Rethrow so the controller can handle the error
            }
        }

        public async Task<string> ChatWithAIAsync(string context, string question)
        {
            var prompt = $@"You are a helpful and experienced university lecturer. You are currently discussing a specific week of course material with a student.

Context of the material being discussed:
{context}

The student has asked the following question:
{question}

Provide a helpful, clear, and encouraging explanation as a lecturer. Use Markdown for formatting if helpful. If the question is unrelated to the course or the specific material, try to gently bring the student back to the topic while still being helpful.";

            try
            {
                var response = await CallGeminiAPIAsync(prompt);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ChatWithAIAsync: {ex.Message}");
                return "I'm sorry, I'm having trouble connecting to my teaching resources right now. Please try again in a moment.";
            }
        }

        private async Task<string> CallGeminiAPIAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured");
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{GEMINI_API_URL}?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"\n--- Gemini API Error ---");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Error Body: {errorBody}");
                Console.WriteLine($"Debug Key Length: {_apiKey.Length}");
                Console.WriteLine($"------------------------\n");
                throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {errorBody}");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var generatedText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
    
            // Robust JSON extraction: Find the first '{' and the last '}'
            var firstBrace = generatedText.IndexOf('{');
            var lastBrace = generatedText.LastIndexOf('}');

            if (firstBrace != -1 && lastBrace != -1 && lastBrace > firstBrace)
            {
                generatedText = generatedText.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return generatedText;
        }
    }

    // Response models
    public class SyllabusAnalysisResult
    {
        public int TotalWeeks { get; set; }
        public List<WeekInfo> Weeks { get; set; } = new();
    }

    public class WeekInfo
    {
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class WeeklySummaryResult
    {
        public string WeekTitle { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyTopics { get; set; } = new();
        public List<string> LearningObjectives { get; set; } = new();
        public string PreparationNotes { get; set; } = string.Empty;
    }

    // Gemini API response models
    public class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        public Content? Content { get; set; }
    }

    public class Content
    {
        public List<Part>? Parts { get; set; }
    }

    public class Part
    {
        public string? Text { get; set; }
    }
}

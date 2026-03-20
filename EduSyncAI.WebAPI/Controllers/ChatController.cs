using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using EduSyncAI.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace EduSyncAI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly EduSyncDbContext _context;
        private readonly GeminiSummarizationService _geminiService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(EduSyncDbContext context, GeminiSummarizationService geminiService, ILogger<ChatController> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskQuestion([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new { error = "Question cannot be empty" });
                }

                string context = "";

                // If summaryId is provided, get the context from that summary
                if (request.SummaryId.HasValue)
                {
                    var summary = await _context.WeeklySummaries.FindAsync(request.SummaryId.Value);
                    if (summary != null)
                    {
                        context = $@"Week Title: {summary.WeekTitle}
Summary: {summary.Summary}
Key Topics: {summary.KeyTopics}
Learning Objectives: {summary.LearningObjectives}
Preparation Notes: {summary.PreparationNotes}";
                    }
                }

                _logger.LogInformation("Student asking AI: {Question} with context length: {ContextLength}", 
                    request.Question, context.Length);

                var response = await _geminiService.ChatWithAIAsync(context, request.Question);

                return Ok(new { response });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in ChatController.AskQuestion");
                return StatusCode(500, new { error = "Failed to get response from AI" });
            }
        }

        [HttpPost("quiz")]
        public async Task<IActionResult> GenerateQuiz([FromBody] QuizRequest request)
        {
            try
            {
                if (!request.SummaryId.HasValue)
                    return BadRequest(new { error = "SummaryId is required" });

                var summary = await _context.WeeklySummaries.FindAsync(request.SummaryId.Value);
                if (summary == null)
                    return NotFound(new { error = "Summary not found" });

                var prompt = $@"You are an expert university lecturer creating a quiz for your students.

Based on the following lecture summary, create a quiz with {request.QuestionCount} multiple-choice questions.

Lecture: {summary.WeekTitle}
Summary: {summary.Summary}
Key Topics: {summary.KeyTopics}
Learning Objectives: {summary.LearningObjectives}

Rules:
- Each question should test understanding, not just memorization.
- Provide 4 options (A, B, C, D) for each question.
- One option must be correct.
- Include a brief explanation for the correct answer.
- Vary difficulty: some easy, some moderate, some challenging.

Return ONLY a JSON array with this exact format:
[
  {{
    ""question"": ""What is..."",
    ""options"": [""A) ..."", ""B) ..."", ""C) ..."", ""D) ...""],
    ""correctIndex"": 0,
    ""explanation"": ""Brief explanation of why this is correct""
  }}
]

Return ONLY the JSON array, no additional text.";

                var response = await _geminiService.ChatWithAIAsync("", prompt);

                // Try to parse as JSON array, clean up if needed
                var cleaned = response.Trim();
                if (cleaned.StartsWith("```json")) cleaned = cleaned[7..];
                if (cleaned.StartsWith("```")) cleaned = cleaned[3..];
                if (cleaned.EndsWith("```")) cleaned = cleaned[..^3];
                cleaned = cleaned.Trim();

                return Ok(new { quiz = cleaned });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz");
                return StatusCode(500, new { error = "Failed to generate quiz" });
            }
        }
    }

    public class ChatRequest
    {
        public int? SummaryId { get; set; }
        public string Question { get; set; } = string.Empty;
    }

    public class QuizRequest
    {
        public int? SummaryId { get; set; }
        public int QuestionCount { get; set; } = 5;
    }
}

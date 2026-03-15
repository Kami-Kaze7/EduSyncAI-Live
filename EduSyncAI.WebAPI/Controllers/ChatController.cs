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
Key Topics: {summary.KeyTopics}";
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
    }

    public class ChatRequest
    {
        public int? SummaryId { get; set; }
        public string Question { get; set; } = string.Empty;
    }
}

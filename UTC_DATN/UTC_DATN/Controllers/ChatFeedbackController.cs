using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Common;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers;

/// <summary>
/// API để quản lý feedback của chatbot
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatFeedbackController : ControllerBase
{
    private readonly UTC_DATNContext _context;
    private readonly ILogger<ChatFeedbackController> _logger;

    public ChatFeedbackController(UTC_DATNContext context, ILogger<ChatFeedbackController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gửi phản hồi về tin nhắn chatbot
    /// </summary>
    /// <param name="dto">Thông tin phản hồi</param>
    /// <returns>Phản hồi vừa được tạo</returns>
    [HttpPost("submit")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatFeedbackResponseDto>> SubmitFeedbackAsync([FromBody] ChatFeedbackDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest("Feedback data is required");

            // Tạo ChatFeedback entity
            var feedback = new ChatFeedback
            {
                FeedbackId = Guid.NewGuid(),
                ChatSessionId = dto.ChatSessionId,
                MessageId = dto.MessageId,
                Rating = dto.Rating,
                IsHelpful = dto.IsHelpful,
                Category = dto.Category,
                Comments = dto.Comments,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[Feedback] Submitted feedback {feedback.FeedbackId} for message {dto.MessageId}");

            return Ok(new ChatFeedbackResponseDto
            {
                FeedbackId = feedback.FeedbackId,
                Rating = feedback.Rating,
                IsHelpful = feedback.IsHelpful,
                Category = feedback.Category,
                Comments = feedback.Comments,
                CreatedAt = feedback.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy phản hồi theo ID
    /// </summary>
    /// <param name="feedbackId">ID phản hồi</param>
    /// <returns>Chi tiết phản hồi</returns>
    [HttpGet("{feedbackId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatFeedbackResponseDto>> GetFeedbackAsync(Guid feedbackId)
    {
        try
        {
            var feedback = await _context.ChatFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

            if (feedback == null)
                return NotFound($"Feedback {feedbackId} not found");

            return Ok(new ChatFeedbackResponseDto
            {
                FeedbackId = feedback.FeedbackId,
                Rating = feedback.Rating,
                IsHelpful = feedback.IsHelpful,
                Category = feedback.Category,
                Comments = feedback.Comments,
                CreatedAt = feedback.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy danh sách feedback cho tin nhắn
    /// </summary>
    /// <param name="messageId">ID tin nhắn</param>
    /// <returns>Danh sách feedback</returns>
    [HttpGet("message/{messageId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ChatFeedbackResponseDto>>> GetMessageFeedbackAsync(Guid messageId)
    {
        try
        {
            var feedbacks = await _context.ChatFeedbacks
                .Where(f => f.MessageId == messageId)
                .Select(f => new ChatFeedbackResponseDto
                {
                    FeedbackId = f.FeedbackId,
                    Rating = f.Rating,
                    IsHelpful = f.IsHelpful,
                    Category = f.Category,
                    Comments = f.Comments,
                    CreatedAt = f.CreatedAt
                })
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message feedback");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy danh sách feedback cho phiên hội thoại
    /// </summary>
    /// <param name="sessionId">ID phiên</param>
    /// <returns>Danh sách feedback</returns>
    [HttpGet("session/{sessionId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ChatFeedbackResponseDto>>> GetSessionFeedbackAsync(Guid sessionId)
    {
        try
        {
            var feedbacks = await _context.ChatFeedbacks
                .Where(f => f.ChatSessionId == sessionId)
                .Select(f => new ChatFeedbackResponseDto
                {
                    FeedbackId = f.FeedbackId,
                    Rating = f.Rating,
                    IsHelpful = f.IsHelpful,
                    Category = f.Category,
                    Comments = f.Comments,
                    CreatedAt = f.CreatedAt
                })
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session feedback");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy thống kê feedback hàng tuần
    /// </summary>
    /// <returns>Thống kê</returns>
    [HttpGet("stats/weekly")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, object>>> GetWeeklyStatsAsync()
    {
        try
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);

            var totalFeedback = await _context.ChatFeedbacks
                .Where(f => f.CreatedAt >= weekAgo)
                .CountAsync();

            var avgRating = await _context.ChatFeedbacks
                .Where(f => f.CreatedAt >= weekAgo && f.Rating.HasValue)
                .AverageAsync(f => (double?)f.Rating) ?? 0;

            var helpfulCount = await _context.ChatFeedbacks
                .Where(f => f.CreatedAt >= weekAgo && f.IsHelpful == true)
                .CountAsync();

            var unhelpfulCount = await _context.ChatFeedbacks
                .Where(f => f.CreatedAt >= weekAgo && f.IsHelpful == false)
                .CountAsync();

            var categoryCounts = await _context.ChatFeedbacks
                .Where(f => f.CreatedAt >= weekAgo && f.Category != null)
                .GroupBy(f => f.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var stats = new Dictionary<string, object>
            {
                { "period", "last_7_days" },
                { "total_feedback", totalFeedback },
                { "average_rating", Math.Round(avgRating, 2) },
                { "helpful_count", helpfulCount },
                { "unhelpful_count", unhelpfulCount },
                { "helpful_percentage", totalFeedback > 0 ? Math.Round(100.0 * helpfulCount / totalFeedback, 2) : 0 },
                { "by_category", categoryCounts }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

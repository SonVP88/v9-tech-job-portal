using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UTC_DATN.DTOs.Common;
using UTC_DATN.Services.Implements;

namespace UTC_DATN.Controllers;

/// <summary>
/// API để lấy analytics của chatbot
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatAnalyticsController : ControllerBase
{
    private readonly ChatAnalyticsService _analyticsService;
    private readonly ILogger<ChatAnalyticsController> _logger;

    public ChatAnalyticsController(ChatAnalyticsService analyticsService, ILogger<ChatAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy báo cáo analytics hàng tuần
    /// </summary>
    /// <returns>Báo cáo hàng tuần</returns>
    [HttpGet("weekly")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatAnalyticsReportDto>> GetWeeklyReportAsync()
    {
        try
        {
            var report = await _analyticsService.GenerateWeeklyReportAsync();
            _logger.LogInformation("[Analytics] Generated weekly report");
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating weekly report");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy top N câu hỏi được hỏi nhiều nhất
    /// </summary>
    /// <param name="limit">Số lượng (default 10)</param>
    /// <returns>Danh sách top questions</returns>
    [HttpGet("top-questions")]
    [AllowAnonymous]
    public async Task<ActionResult<List<TopQuestionDto>>> GetTopQuestionsAsync(int limit = 10)
    {
        try
        {
            if (limit <= 0 || limit > 100)
                limit = 10;

            var questions = await _analyticsService.GetTopQuestionsAsync(null, limit);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top questions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy các truy vấn có độ tin cậy thấp
    /// </summary>
    /// <param name="limit">Số lượng (default 20)</param>
    /// <returns>Danh sách low confidence queries</returns>
    [HttpGet("low-confidence")]
    [AllowAnonymous]
    public async Task<ActionResult<List<LowConfidenceQueryDto>>> GetLowConfidenceQueriesAsync(int limit = 20)
    {
        try
        {
            if (limit <= 0 || limit > 100)
                limit = 20;

            var queries = await _analyticsService.GetLowConfidenceQueriesAsync(null, limit);
            return Ok(queries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low confidence queries");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy độ chính xác intent per intent type
    /// </summary>
    /// <returns>Dictionary of intent -> accuracy</returns>
    [HttpGet("intent-accuracy")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, double>>> GetIntentAccuracyAsync()
    {
        try
        {
            var accuracy = await _analyticsService.GetIntentAccuracyAsync();
            return Ok(accuracy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting intent accuracy");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy thống kê thời gian phản hồi (P50, P95, P99)
    /// </summary>
    /// <returns>Dictionary of percentile -> value</returns>
    [HttpGet("response-time")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, double>>> GetResponseTimeStatsAsync()
    {
        try
        {
            var stats = await _analyticsService.GetResponseTimeStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting response time stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy CSAT (Customer Satisfaction Score)
    /// </summary>
    /// <returns>CSAT data</returns>
    [HttpGet("csat")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, object>>> GetCSATAsync()
    {
        try
        {
            var csat = await _analyticsService.GetCSATAsync();
            return Ok(csat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CSAT");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy danh sách các chủ đề được thảo luận nhiều nhất
    /// </summary>
    /// <param name="limit">Số lượng (default 20)</param>
    /// <returns>Danh sách topics</returns>
    [HttpGet("topics")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> GetTopicsAsync(int limit = 20)
    {
        try
        {
            if (limit <= 0 || limit > 100)
                limit = 20;

            var topics = await _analyticsService.GetTopicsAsync(null, limit);
            return Ok(topics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting topics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Dashboard overview - Tóm tắt all metrics
    /// </summary>
    /// <returns>Dashboard data</returns>
    [HttpGet("dashboard")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetDashboardAsync()
    {
        try
        {
            var report = await _analyticsService.GenerateWeeklyReportAsync();
            var accuracy = await _analyticsService.GetIntentAccuracyAsync();
            var responseTime = await _analyticsService.GetResponseTimeStatsAsync();
            var csat = await _analyticsService.GetCSATAsync();

            var dashboard = new
            {
                summary = new
                {
                    total_conversations = report.TotalConversations,
                    average_response_time_ms = Math.Round(report.AverageResponseTimeMs, 2),
                    intent_accuracy = Math.Round(report.IntentAccuracy * 100, 2),
                    resolution_rate = Math.Round(report.ResolutionRate * 100, 2),
                    escalation_rate = Math.Round(report.EscalationRate * 100, 2),
                    average_csat = Math.Round(report.AverageCsat * 5, 2)
                },
                top_questions = report.TopQuestions,
                low_confidence_queries = report.LowConfidenceQueries.Take(5),
                intent_accuracy_breakdown = accuracy,
                response_time_percentiles = responseTime,
                csat = csat
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

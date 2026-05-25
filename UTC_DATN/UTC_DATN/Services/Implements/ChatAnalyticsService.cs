using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Common;

namespace UTC_DATN.Services.Implements;

/// <summary>
/// Dịch vụ analytics cho chatbot
/// Cung cấp các báo cáo, metrics và insights từ chat logs
/// </summary>
public class ChatAnalyticsService
{
    private readonly UTC_DATNContext _context;
    private readonly ILogger<ChatAnalyticsService> _logger;

    public ChatAnalyticsService(UTC_DATNContext context, ILogger<ChatAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tạo báo cáo analytics hàng tuần
    /// </summary>
    public async Task<ChatAnalyticsReportDto> GenerateWeeklyReportAsync()
    {
        var report = new ChatAnalyticsReportDto();

        try
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);

            // 1. Tổng số cuộc hội thoại
            report.TotalConversations = await _context.ChatSessions
                .Where(cs => cs.StartedAt >= weekAgo)
                .CountAsync();

            // 2. Thời gian phản hồi trung bình
            var responseTimeAvg = await _context.ChatAnalytics
                .Where(ca => ca.CreatedAt >= weekAgo && ca.ResponseTimeMs.HasValue)
                .AverageAsync(ca => (double?)ca.ResponseTimeMs);
            report.AverageResponseTimeMs = responseTimeAvg ?? 0;

            // 3. Độ chính xác Intent
            var totalAnalytics = await _context.ChatAnalytics
                .Where(ca => ca.CreatedAt >= weekAgo && ca.IntentConfidence.HasValue)
                .CountAsync();
            if (totalAnalytics > 0)
            {
                var correctIntents = await _context.ChatAnalytics
                    .Where(ca => ca.CreatedAt >= weekAgo && ca.IntentConfidence >= 0.8m)
                    .CountAsync();
                report.IntentAccuracy = (double)correctIntents / totalAnalytics;
            }

            // 4. Tỉ lệ phân quyết (không escalate)
            var totalMessages = await _context.ChatMessages
                .Where(cm => cm.CreatedAt >= weekAgo)
                .CountAsync();
            if (totalMessages > 0)
            {
                var escalatedMessages = await _context.ChatAnalytics
                    .Where(ca => ca.CreatedAt >= weekAgo && ca.WasEscalated)
                    .CountAsync();
                report.ResolutionRate = (double)(totalMessages - escalatedMessages) / totalMessages;
            }

            // 5. Tỉ lệ Escalation
            if (totalMessages > 0)
            {
                var escalations = await _context.ChatAnalytics
                    .Where(ca => ca.CreatedAt >= weekAgo && ca.WasEscalated)
                    .CountAsync();
                report.EscalationRate = (double)escalations / totalMessages;
            }

            // 6. Điểm CSAT trung bình (từ feedback)
            var csatAvg = await _context.ChatFeedbacks
                .Where(cf => cf.CreatedAt >= weekAgo && cf.Rating.HasValue)
                .AverageAsync(cf => (double?)cf.Rating);
            report.AverageCsat = (csatAvg ?? 0) / 5.0; // Normalize to 0-1

            // 7. Top 10 câu hỏi
            report.TopQuestions = await GetTopQuestionsAsync(weekAgo, 10);

            // 8. Low confidence queries
            report.LowConfidenceQueries = await GetLowConfidenceQueriesAsync(weekAgo, 20);

            _logger.LogInformation($"[Analytics] Generated weekly report - {report.TotalConversations} conversations");
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating weekly report");
            return report;
        }
    }

    /// <summary>
    /// Lấy top N câu hỏi được hỏi nhiều nhất
    /// </summary>
    public async Task<List<TopQuestionDto>> GetTopQuestionsAsync(DateTime? from = null, int limit = 10)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);

        try
        {
            var questions = await _context.ChatMessages
                .Where(cm => cm.Sender == "user" && cm.CreatedAt >= startDate)
                .GroupBy(cm => cm.Message)
                .Select(g => new TopQuestionDto
                {
                    Question = g.Key ?? "",
                    Count = g.Count()
                })
                .OrderByDescending(q => q.Count)
                .Take(limit)
                .ToListAsync();

            // Populate ratings separately
            foreach (var q in questions)
            {
                var avgRating = await _context.ChatAnalytics
                    .Where(ca => ca.UserRating.HasValue)
                    .AverageAsync(ca => (double?)ca.UserRating);
                q.AverageRating = avgRating ?? 0;
            }

            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top questions");
            return new List<TopQuestionDto>();
        }
    }

    /// <summary>
    /// Lấy các truy vấn có độ tin cậy thấp
    /// </summary>
    public async Task<List<LowConfidenceQueryDto>> GetLowConfidenceQueriesAsync(DateTime? from = null, int limit = 20)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);

        try
        {
            var queries = await (from ca in _context.ChatAnalytics
                                 join cm in _context.ChatMessages on ca.MessageId equals cm.ChatMessageId
                                 where ca.IntentConfidence < 0.7m && ca.CreatedAt >= startDate
                                 select new LowConfidenceQueryDto
                                 {
                                     MessageId = cm.ChatMessageId,
                                     Message = cm.Message ?? "",
                                     Intent = ca.Intent ?? "UNKNOWN",
                                     Confidence = (double?)ca.IntentConfidence ?? 0,
                                     UserRating = ca.UserRating
                                 })
                .OrderBy(q => q.Confidence)
                .Take(limit)
                .ToListAsync();

            return queries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low confidence queries");
            return new List<LowConfidenceQueryDto>();
        }
    }

    /// <summary>
    /// Lấy độ chính xác intent per intent type
    /// </summary>
    public async Task<Dictionary<string, double>> GetIntentAccuracyAsync(DateTime? from = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);

        try
        {
            var accuracy = await _context.ChatAnalytics
                .Where(ca => ca.Intent != null && ca.CreatedAt >= startDate && ca.IntentConfidence.HasValue)
                .GroupBy(ca => ca.Intent!)
                .Select(g => new { Intent = g.Key, Accuracy = g.Average(ca => (double?)ca.IntentConfidence) })
                .ToDictionaryAsync(x => x.Intent, x => x.Accuracy ?? 0);

            return accuracy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting intent accuracy");
            return new Dictionary<string, double>();
        }
    }

    /// <summary>
    /// Lấy thống kê thời gian phản hồi (P50, P95, P99)
    /// </summary>
    public async Task<Dictionary<string, double>> GetResponseTimeStatsAsync(DateTime? from = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);

        try
        {
            var responseTimes = await _context.ChatAnalytics
                .Where(ca => ca.ResponseTimeMs.HasValue && ca.CreatedAt >= startDate)
                .Select(ca => (double)ca.ResponseTimeMs!.Value)
                .OrderBy(rt => rt)
                .ToListAsync();

            if (!responseTimes.Any())
                return new Dictionary<string, double>();

            var count = responseTimes.Count;
            return new Dictionary<string, double>
            {
                { "min", responseTimes.First() },
                { "p50", GetPercentile(responseTimes, 50) },
                { "p95", GetPercentile(responseTimes, 95) },
                { "p99", GetPercentile(responseTimes, 99) },
                { "max", responseTimes.Last() },
                { "avg", responseTimes.Average() }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting response time stats");
            return new Dictionary<string, double>();
        }
    }

    /// <summary>
    /// Lấy điểm CSAT (Customer Satisfaction)
    /// </summary>
    public async Task<Dictionary<string, object>> GetCSATAsync(DateTime? from = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);

        try
        {
            var feedbacks = await _context.ChatFeedbacks
                .Where(cf => cf.Rating.HasValue && cf.CreatedAt >= startDate)
                .ToListAsync();

            if (!feedbacks.Any())
                return new Dictionary<string, object> { { "average", 0 }, { "count", 0 } };

            return new Dictionary<string, object>
            {
                { "average", feedbacks.Average(f => f.Rating ?? 0) },
                { "count", feedbacks.Count },
                { "distribution", feedbacks.GroupBy(f => f.Rating).ToDictionary(g => g.Key, g => g.Count()) }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CSAT");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Lấy danh sách các chủ đề được thảo luận nhiều nhất
    /// </summary>
    public async Task<List<string>> GetTopicsAsync(DateTime? from = null, int limit = 20)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-7);

        try
        {
            var topics = await _context.ChatAnalytics
                .Where(ca => ca.Intent != null && ca.CreatedAt >= startDate)
                .GroupBy(ca => ca.Intent!)
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => $"{g.Key} ({g.Count()} times)")
                .ToListAsync();

            return topics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting topics");
            return new List<string>();
        }
    }

    /// <summary>
    /// Helper: Tính percentile
    /// </summary>
    private double GetPercentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0)
            return 0;

        var index = (percentile / 100.0) * sortedValues.Count;
        if (index < 1)
            return sortedValues.First();
        if (index >= sortedValues.Count)
            return sortedValues.Last();

        var lower = (int)index - 1;
        var upper = lower + 1;
        var weight = index - lower - 1;

        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }
}

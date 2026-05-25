using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UTC_DATN.DTOs.Common;

namespace UTC_DATN.Services.Implements;

/// <summary>
/// Dịch vụ phân loại intent từ tin nhắn của người dùng
/// Hỗ trợ 15+ loại intent khác nhau
/// </summary>
public class IntentClassifier
{
    private readonly ILogger<IntentClassifier> _logger;
    private readonly Dictionary<string, IntentRule> _intentRules;

    public IntentClassifier(ILogger<IntentClassifier> logger)
    {
        _logger = logger;
        _intentRules = InitializeIntentRules();
    }

    /// <summary>
    /// Phân loại intent từ tin nhắn
    /// </summary>
    public async Task<IntentClassificationResultDto> ClassifyAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return new IntentClassificationResultDto 
            { 
                Intent = "UNKNOWN", 
                Confidence = 0 
            };

        try
        {
            // Chiến lược 1: Dựa trên quy tắc (nhanh, 80% trường hợp)
            var ruleResult = ClassifyByRules(message);
            if (ruleResult.Confidence >= 0.85)
            {
                _logger.LogDebug($"[Intent] Classified as {ruleResult.Intent} (rule-based, confidence: {ruleResult.Confidence})");
                return ruleResult;
            }

            // Chiến lược 2: Fallback nếu độ tin cậy thấp
            var fallbackResult = ClassifyFallback(message);
            _logger.LogDebug($"[Intent] Classified as {fallbackResult.Intent} (fallback, confidence: {fallbackResult.Confidence})");
            return fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during intent classification");
            return new IntentClassificationResultDto 
            { 
                Intent = "UNKNOWN", 
                Confidence = 0 
            };
        }
    }

    /// <summary>
    /// Phân loại dựa trên quy tắc (rule-based)
    /// </summary>
    private IntentClassificationResultDto ClassifyByRules(string message)
    {
        var lower = message.ToLower().Trim();
        var words = lower.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Điểm số cho mỗi intent
        var scores = new Dictionary<string, double>();

        foreach (var rule in _intentRules.Values)
        {
            double score = 0;

            // Kiểm tra keywords
            if (rule.Keywords.Any(k => lower.Contains(k)))
            {
                score += 0.6;
            }

            // Kiểm tra patterns
            foreach (var pattern in rule.Patterns)
            {
                if (Regex.IsMatch(lower, pattern, RegexOptions.IgnoreCase))
                {
                    score += 0.4;
                    break;
                }
            }

            // Kiểm tra excluded keywords (nếu chứa, giảm điểm)
            if (rule.ExcludedKeywords.Any(k => lower.Contains(k)))
            {
                score *= 0.5;
            }

            if (score > 0)
            {
                scores[rule.Intent] = score;
            }
        }

        // Tìm intent có điểm cao nhất
        if (scores.Any())
        {
            var topIntent = scores.OrderByDescending(x => x.Value).First();
            return new IntentClassificationResultDto
            {
                Intent = topIntent.Key,
                Confidence = Math.Min(topIntent.Value, 1.0)
            };
        }

        return ClassifyFallback(message);
    }

    /// <summary>
    /// Fallback nếu quy tắc không match
    /// </summary>
    private IntentClassificationResultDto ClassifyFallback(string message)
    {
        var lower = message.ToLower();

        // Kiểm tra greeting
        if (Regex.IsMatch(lower, @"\b(xin chào|chào|hi|hello|alo|ê)\b"))
            return new IntentClassificationResultDto { Intent = "SMALL_TALK", Confidence = 0.75 };

        // Kiểm tra farewell
        if (Regex.IsMatch(lower, @"\b(bye|tạm biệt|hẹn gặp|cáo biệt|tạm thôi)\b"))
            return new IntentClassificationResultDto { Intent = "SMALL_TALK", Confidence = 0.75 };

        // Default: general inquiry
        return new IntentClassificationResultDto 
        { 
            Intent = "GENERAL_INQUIRY", 
            Confidence = 0.5 
        };
    }

    private Dictionary<string, IntentRule> InitializeIntentRules()
    {
        return new Dictionary<string, IntentRule>
        {
            // 1. COMPANY_INFO
            ["COMPANY_INFO"] = new IntentRule
            {
                Intent = "COMPANY_INFO",
                Keywords = new string[] { "công ty", "v9tech", "về chúng tôi", "giới thiệu", "văn hóa", "quyền lợi", "lợi ích" },
                Patterns = new string[] { @"\b(công ty|v9|về|giới thiệu)" },
                ExcludedKeywords = new string[] { "vị trí", "job", "tuyển" }
            },

            // 2. JOB_SEARCH
            ["JOB_SEARCH"] = new IntentRule
            {
                Intent = "JOB_SEARCH",
                Keywords = new string[] { "tuyển", "job", "vị trí", "công việc", "developer", "engineer", "backend", "frontend", "devops" },
                Patterns = new string[] { @"\b(tuyển|job|vị trí|công việc|backend|frontend|developer)" },
                ExcludedKeywords = new string[] { }
            },

            // 3. JOB_DETAIL
            ["JOB_DETAIL"] = new IntentRule
            {
                Intent = "JOB_DETAIL",
                Keywords = new string[] { "chi tiết", "yêu cầu", "mô tả", "requirements", "description" },
                Patterns = new string[] { @"\b(chi tiết|yêu cầu|mô tả|requirements)" },
                ExcludedKeywords = new string[] { }
            },

            // 4. SALARY_INQUIRY
            ["SALARY_INQUIRY"] = new IntentRule
            {
                Intent = "SALARY_INQUIRY",
                Keywords = new string[] { "lương", "salary", "pay", "mức", "bao nhiêu", "giá" },
                Patterns = new string[] { @"\b(lương|salary|pay|mức|bao nhiêu)\b" },
                ExcludedKeywords = new string[] { }
            },

            // 5. BENEFITS_INQUIRY
            ["BENEFITS_INQUIRY"] = new IntentRule
            {
                Intent = "BENEFITS_INQUIRY",
                Keywords = new string[] { "quyền lợi", "benefit", "bảo hiểm", "phúc lợi", "thưởng" },
                Patterns = new string[] { @"\b(quyền lợi|benefit|bảo hiểm|phúc lợi|thưởng)" },
                ExcludedKeywords = new string[] { }
            },

            // 6. SKILL_INQUIRY
            ["SKILL_INQUIRY"] = new IntentRule
            {
                Intent = "SKILL_INQUIRY",
                Keywords = new string[] { "kỹ năng", "skill", "yêu cầu", "công nghệ", "framework", "ngôn ngữ" },
                Patterns = new string[] { @"\b(kỹ năng|skill|công nghệ|framework|ngôn ngữ)" },
                ExcludedKeywords = new string[] { }
            },

            // 7. SKILL_LEARNING
            ["SKILL_LEARNING"] = new IntentRule
            {
                Intent = "SKILL_LEARNING",
                Keywords = new string[] { "học", "learning", "roadmap", "đường dẫn", "cách thành" },
                Patterns = new string[] { @"\b(học|learning|roadmap|cách thành)" },
                ExcludedKeywords = new string[] { }
            },

            // 8. CAREER_PATH
            ["CAREER_PATH"] = new IntentRule
            {
                Intent = "CAREER_PATH",
                Keywords = new string[] { "sự nghiệp", "career", "phát triển", "nâng cấp", "junior", "senior", "middle" },
                Patterns = new string[] { @"\b(sự nghiệp|career|phát triển|junior|senior|middle)" },
                ExcludedKeywords = new string[] { }
            },

            // 9. PROCESS_INQUIRY
            ["PROCESS_INQUIRY"] = new IntentRule
            {
                Intent = "PROCESS_INQUIRY",
                Keywords = new string[] { "quy trình", "process", "các bước", "flow", "phỏng vấn", "apply" },
                Patterns = new string[] { @"\b(quy trình|process|các bước|flow|phỏng vấn)" },
                ExcludedKeywords = new string[] { }
            },

            // 10. INTERVIEW_PREP
            ["INTERVIEW_PREP"] = new IntentRule
            {
                Intent = "INTERVIEW_PREP",
                Keywords = new string[] { "phỏng vấn", "interview", "chuẩn bị", "câu hỏi", "tips", "luyện" },
                Patterns = new string[] { @"\b(phỏng vấn|interview|chuẩn bị|luyện tập)" },
                ExcludedKeywords = new string[] { }
            },

            // 11. CV_TIPS
            ["CV_TIPS"] = new IntentRule
            {
                Intent = "CV_TIPS",
                Keywords = new string[] { "cv", "resume", "hồ sơ", "viết", "tips", "mẫu" },
                Patterns = new string[] { @"\b(cv|resume|hồ sơ|viết|profile)" },
                ExcludedKeywords = new string[] { }
            },

            // 12. ESCALATION
            ["ESCALATION"] = new IntentRule
            {
                Intent = "ESCALATION",
                Keywords = new string[] { "hr", "quản lý", "manager", "nói chuyện", "gặp", "contact" },
                Patterns = new string[] { @"\b(hr|quản lý|manager|nói chuyện|gặp)" },
                ExcludedKeywords = new string[] { }
            }
        };
    }

    /// <summary>
    /// Lớp nội bộ để định nghĩa quy tắc phân loại
    /// </summary>
    private class IntentRule
    {
        public string Intent { get; set; } = "";
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string[] Patterns { get; set; } = Array.Empty<string>();
        public string[] ExcludedKeywords { get; set; } = Array.Empty<string>();
    }
}

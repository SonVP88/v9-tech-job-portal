namespace UTC_DATN.DTOs.Common;

/// <summary>
/// DTO nhận phản hồi từ người dùng
/// </summary>
public class ChatFeedbackDto
{
    /// <summary>
    /// ID phiên hội thoại
    /// </summary>
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// ID tin nhắn
    /// </summary>
    public Guid? MessageId { get; set; }

    /// <summary>
    /// Xếp hạng 1-5
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Có hữu ích không (true/false)
    /// </summary>
    public bool? IsHelpful { get; set; }

    /// <summary>
    /// Danh mục phản hồi
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Bình luận chi tiết
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// DTO trả về phản hồi
/// </summary>
public class ChatFeedbackResponseDto
{
    public Guid FeedbackId { get; set; }
    public int? Rating { get; set; }
    public bool? IsHelpful { get; set; }
    public string? Category { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO cho Q&A training data
/// </summary>
public class ChatQaTrainingDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = "";
    public string Question { get; set; } = "";
    public List<string> SimilarQuestions { get; set; } = new();
    public string Answer { get; set; } = "";
    public string Intent { get; set; } = "";
    public List<string> ContextNeeded { get; set; } = new();
    public string Difficulty { get; set; } = "medium";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO cho câu hỏi phỏng vấn training
/// </summary>
public class InterviewQuestionTrainingDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Difficulty { get; set; } = "medium";
    public string Role { get; set; } = "";
    public string ExperienceLevel { get; set; } = "";
    public List<string> ExpectedTopics { get; set; } = new();
    public string SampleAnswerGood { get; set; } = "";
    public string SampleAnswerMediocre { get; set; } = "";
    public string SampleAnswerPoor { get; set; } = "";
    public Dictionary<string, string> EvaluationCriteria { get; set; } = new();
    public List<string> FollowUpQuestions { get; set; } = new();
}

/// <summary>
/// DTO cho Analytics report
/// </summary>
public class ChatAnalyticsReportDto
{
    public int TotalConversations { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double IntentAccuracy { get; set; }
    public double ResolutionRate { get; set; }
    public double EscalationRate { get; set; }
    public double AverageCsat { get; set; }
    public List<TopQuestionDto> TopQuestions { get; set; } = new();
    public List<LowConfidenceQueryDto> LowConfidenceQueries { get; set; } = new();
}

public class TopQuestionDto
{
    public string Question { get; set; } = "";
    public string Intent { get; set; } = "";
    public int Count { get; set; }
    public double AverageRating { get; set; }
}

public class LowConfidenceQueryDto
{
    public Guid MessageId { get; set; }
    public string Message { get; set; } = "";
    public string Intent { get; set; } = "";
    public double Confidence { get; set; }
    public int? UserRating { get; set; }
}

/// <summary>
/// DTO cho Intent Classification result
/// </summary>
public class IntentClassificationResultDto
{
    public string Intent { get; set; } = "";
    public double Confidence { get; set; }
    public List<ExtractedEntityDto> Entities { get; set; } = new();
}

/// <summary>
/// DTO cho Extracted Entity
/// </summary>
public class ExtractedEntityDto
{
    public string Type { get; set; } = ""; // ROLE, SALARY_RANGE, LOCATION, etc.
    public string Value { get; set; } = "";
    public double Confidence { get; set; }
}

using System;
using System.Collections.Generic;

namespace UTC_DATN.Entities;

/// <summary>
/// Đại diện cho một tin nhắn trong chat session
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// ID tin nhắn (Guid)
    /// </summary>
    public Guid ChatMessageId { get; set; }

    /// <summary>
    /// ID phiên hội thoại
    /// </summary>
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Người gửi: "user" hoặc "assistant"
    /// </summary>
    public string Sender { get; set; } = "";

    /// <summary>
    /// Nội dung tin nhắn
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Intent được phân loại (từ Phase 2)
    /// </summary>
    public string? Intent { get; set; }

    /// <summary>
    /// Độ tin cậy của intent classification (0-1)
    /// </summary>
    public decimal? IntentConfidence { get; set; }

    /// <summary>
    /// Thời gian response tính bằng milliseconds
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Có được escalate tới HR không
    /// </summary>
    public bool WasEscalated { get; set; }

    /// <summary>
    /// ID feedback liên quan (nếu có)
    /// </summary>
    public Guid? FeedbackId { get; set; }

    /// <summary>
    /// Dữ liệu meta (JSON)
    /// </summary>
    public string? MetaJson { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Phiên hội thoại chứa tin nhắn này
    /// </summary>
    public virtual ChatSession? ChatSession { get; set; }

    /// <summary>
    /// Phản hồi từ người dùng (nếu có)
    /// </summary>
    public virtual ChatFeedback? Feedback { get; set; }

    /// <summary>
    /// Analytics liên quan (có thể nhiều)
    /// </summary>
    public virtual ICollection<ChatAnalytics>? ChatAnalyticsCollection { get; set; }
}
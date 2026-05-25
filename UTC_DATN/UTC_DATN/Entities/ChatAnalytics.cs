using System;
using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.Entities;

/// <summary>
/// Lưu trữ dữ liệu phân tích cho mỗi tin nhắn chatbot
/// </summary>
public partial class ChatAnalytics
{
    /// <summary>
    /// ID analytics duy nhất
    /// </summary>
    [Key]
    public Guid AnalyticsId { get; set; }

    /// <summary>
    /// ID tin nhắn
    /// </summary>
    public Guid? MessageId { get; set; }

    /// <summary>
    /// Loại intent được phân loại: JOB_SEARCH, SALARY_INQUIRY, etc.
    /// </summary>
    public string? Intent { get; set; }

    /// <summary>
    /// Độ tin cậy của phân loại intent (0-1)
    /// </summary>
    public decimal? IntentConfidence { get; set; }

    /// <summary>
    /// Thời gian phản hồi tính bằng milliseconds
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Độ dài phản hồi (số ký tự)
    /// </summary>
    public int? ResponseLength { get; set; }

    /// <summary>
    /// Xếp hạng từ người dùng (1-5) nếu có
    /// </summary>
    public int? UserRating { get; set; }

    /// <summary>
    /// Có được escalate (chuyển sang HR) không
    /// </summary>
    public bool WasEscalated { get; set; }

    /// <summary>
    /// Lý do escalate
    /// </summary>
    public string? EscalationReason { get; set; }

    /// <summary>
    /// Số entity được trích xuất
    /// </summary>
    public int? EntityCount { get; set; }

    /// <summary>
    /// Độ tin cậy entity trích xuất (0-1)
    /// </summary>
    public decimal? EntityConfidence { get; set; }

    /// <summary>
    /// Người dùng
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Tin nhắn liên quan
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.ForeignKey("MessageId")]
    public virtual ChatMessage? Message { get; set; }

    /// <summary>
    /// Người dùng
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

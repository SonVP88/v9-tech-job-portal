using System;

namespace UTC_DATN.Entities;

/// <summary>
/// Lưu trữ phản hồi của người dùng về câu trả lời từ chatbot
/// </summary>
public partial class ChatFeedback
{
    /// <summary>
    /// ID feedback duy nhất
    /// </summary>
    public Guid FeedbackId { get; set; }

    /// <summary>
    /// ID phiên hội thoại
    /// </summary>
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// ID tin nhắn cụ thể
    /// </summary>
    public Guid? MessageId { get; set; }

    /// <summary>
    /// Xếp hạng từ 1-5 (5 = rất hữu ích, 1 = không hữu ích)
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Có hữu ích không (thumbs up/down)
    /// </summary>
    public bool? IsHelpful { get; set; }

    /// <summary>
    /// Danh mục phản hồi: "chính_xác", "hữu_ích", "tông_điệu", "tốc_độ", "khác"
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Bình luận chi tiết từ người dùng
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Cảm xúc người dùng: "tích_cực", "trung_lập", "tiêu_cực"
    /// </summary>
    public string? UserSentiment { get; set; }

    /// <summary>
    /// Người dùng (nếu được xác thực)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ChatSession? ChatSession { get; set; }
    public virtual ChatMessage? Message { get; set; }
    public virtual User? User { get; set; }
}

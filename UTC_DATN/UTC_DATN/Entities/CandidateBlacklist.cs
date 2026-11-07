#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTC_DATN.Entities;

/// <summary>
/// Bảng Danh sách đen ứng viên (CandidateBlacklists)
/// Phục vụ chặn tự động ứng viên vi phạm kỷ luật nghiêm trọng hoặc tự ý bỏ việc.
/// </summary>
[Table("CandidateBlacklists")]
public class CandidateBlacklist
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Số CCCD 12 số (có Index)</summary>
    [MaxLength(20)]
    public string? CitizenId { get; set; }

    /// <summary>SĐT chuẩn hóa dạng 09xxxxxxxx (có Index)</summary>
    [MaxLength(20)]
    public string? PhoneNormalized { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Lý do cấm tuyển</summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>'PERMANENT' | 'TEMPORARY_6M'</summary>
    [Required]
    [MaxLength(50)]
    public string BanType { get; set; } = "PERMANENT";

    public DateTime? BannedUntil { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


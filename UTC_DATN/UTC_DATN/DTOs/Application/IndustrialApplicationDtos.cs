using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UTC_DATN.DTOs.Application;

/// <summary>
/// Checklist kết quả phỏng vấn miệng nhanh tại cổng/bàn tuyển dụng công nhân (MASS_LABOR)
/// </summary>
public class OralInterviewChecklistDto
{
    public bool HandEyePass { get; set; } = true;
    public bool VisionPass { get; set; } = true;
    public bool Shift12hConfirmed { get; set; } = true;
    public bool AttitudePass { get; set; } = true;
    public string? QuickNote { get; set; }
}

/// <summary>
/// Bộ lọc truy vấn danh sách Hồ Sơ Ứng Tuyển Nhà Máy (GET /api/applications)
/// </summary>
public class IndustrialApplicationFilterQuery
{
    public Guid? JobPostingId { get; set; }
    public Guid? DepartmentId { get; set; }
    /// <summary>'PROFESSIONAL' | 'MASS_LABOR'</summary>
    public string? RecruitmentTrack { get; set; }
    /// <summary>'PDF_UPLOAD' | 'GOOGLE_FORM_QR' | 'WALK_IN'</summary>
    public string? ApplicationSource { get; set; }
    public int? MinAiScore { get; set; }
    /// <summary>Tìm kiếm theo Tên / SĐT / CCCD / Email</summary>
    public string? Keyword { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Request duyệt / xếp lịch / từ chối hàng loạt (POST /api/applications/bulk-action)
/// </summary>
public class BulkApplicationActionRequest
{
    [Required(ErrorMessage = "Danh sách ApplicationIds không được để trống")]
    public List<Guid> ApplicationIds { get; set; } = new();

    /// <summary>'SentToManager' | 'ScheduledInterview' | 'InterviewPassed' | 'Rejected'</summary>
    [Required(ErrorMessage = "TargetStatus là bắt buộc")]
    public string TargetStatus { get; set; } = string.Empty;

    public string? InterviewDate { get; set; }
    public string? InterviewTime { get; set; }
    public string? InterviewLocation { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Request phỏng vấn miệng nhanh công nhân xưởng (POST /api/applications/quick-walkin-eval)
/// Hỗ trợ cả cập nhật ứng viên có sẵn từ QR Form hoặc tạo mới nhanh ứng viên Walk-in tại cổng
/// </summary>
public class QuickWalkInEvalRequest
{
    /// <summary>Null nếu ứng viên đến trực tiếp tại cổng (tạo mới), có giá trị nếu chấm từ danh sách QR Form</summary>
    public Guid? ApplicationId { get; set; }

    public Guid? JobPostingId { get; set; }

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? YearOfBirth { get; set; }
    public string? Cccd { get; set; }
    public string? Hometown { get; set; }
    public string? EducationLevel { get; set; }
    public string? ExperienceSummary { get; set; }

    public OralInterviewChecklistDto Checklist { get; set; } = new();

    /// <summary>'InterviewPassed' (Đạt ngay -> chuyển Onboarding) hoặc 'Rejected' (Loại)</summary>
    public string TargetStatus { get; set; } = "InterviewPassed";
}

/// <summary>
/// DTO trả về cho màn hình Hồ Sơ Ứng Tuyển Công Nghiệp (Dual-Track: PROFESSIONAL & MASS_LABOR)
/// </summary>
public class IndustrialApplicationResponseDto
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string JobCode { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string CandidatePhone { get; set; } = string.Empty;
    public string? YearOfBirth { get; set; }
    public string? Cccd { get; set; }
    public string? Hometown { get; set; }
    public string? EducationLevel { get; set; }

    /// <summary>'PROFESSIONAL' | 'MASS_LABOR'</summary>
    public string RecruitmentTrack { get; set; } = "MASS_LABOR";

    /// <summary>'PDF_UPLOAD' | 'GOOGLE_FORM_QR' | 'WALK_IN'</summary>
    public string ApplicationSource { get; set; } = "GOOGLE_FORM_QR";

    /// <summary>'GEMINI_AI' | 'RULE_ENGINE'</summary>
    public string ScoringMethod { get; set; } = "RULE_ENGINE";

    public int AiScore { get; set; }
    public int SkillMatchPercent { get; set; }
    public int ExperienceMatchPercent { get; set; }
    public int ShiftHealthPercent { get; set; }
    public string AiEvaluationSummary { get; set; } = string.Empty;

    public List<string> SkillsOrCommitments { get; set; } = new();
    public bool IsShift12hReady { get; set; }
    public bool IsRuleRejected { get; set; }

    public string? CvFileUrl { get; set; }
    public string? CvUrl { get; set; }
    public string? RawResumeText { get; set; }
    public OralInterviewChecklistDto OralChecklist { get; set; } = new();
    public Dictionary<string, string> FormFields { get; set; } = new();

    public string Status { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}


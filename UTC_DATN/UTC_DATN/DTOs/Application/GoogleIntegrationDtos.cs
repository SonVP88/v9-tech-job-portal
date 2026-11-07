using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UTC_DATN.DTOs.Application;

/// <summary>
/// DTO chứa thông tin ứng viên công nhân trích xuất từ Google Sheets / Forms
/// </summary>
public class GoogleSheetCandidateDto
{
    // Cho phép trích xuất động từ bất kỳ tên cột nào của Form/Sheet
    public string FullName { get; set; } = string.Empty;

    // Cho phép trích xuất động SĐT hoặc dùng Email/định danh thay thế nếu Form không hỏi SĐT
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Năm sinh (dạng số hoặc chuỗi, ví dụ: 1995, 2002)
    /// </summary>
    public string? YearOfBirth { get; set; }

    /// <summary>
    /// Quê quán / Thường trú (Huyện, Tỉnh)
    /// </summary>
    public string? Hometown { get; set; }

    /// <summary>
    /// Ghi chú kinh nghiệm làm việc (ví dụ: 1 năm đứng chuyền may, biết hàn cơ bản)
    /// </summary>
    public string? ExperienceNotes { get; set; }

    /// <summary>
    /// Sẵn sàng làm việc xoay 3 ca / tăng ca (Chấp nhận true/false, "Có"/"Không", "1"/"0")
    /// </summary>
    public object? ShiftReady { get; set; }

    /// <summary>
    /// Email (nếu có thu thập, nếu không sẽ tự động sinh email danh danh)
    /// </summary>
    public string? Email { get; set; }
    public string? CvUrl { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();

    /// <summary>
    /// Helper chuyển đổi ShiftReady sang boolean linh hoạt
    /// </summary>
    [JsonIgnore]
    public bool IsShiftReady
    {
        get
        {
            if (ShiftReady == null) return false;
            var val = ShiftReady.ToString()?.Trim().ToLowerInvariant();
            return val == "true" || val == "có" || val == "co" || val == "1" || val == "yes" || val == "y";
        }
    }
}

/// <summary>
/// Request Import danh sách ứng viên hàng loạt từ Google Sheets
/// </summary>
public class ImportGoogleSheetsRequest
{
    [Required(ErrorMessage = "JobPostingId (JobId) là bắt buộc")]
    public Guid JobId { get; set; }

    [Required(ErrorMessage = "Danh sách ứng viên không được để trống")]
    public List<GoogleSheetCandidateDto> Candidates { get; set; } = new();
}

/// <summary>
/// Kết quả sau khi thực hiện Import từ Google Sheets
/// </summary>
public class ImportGoogleSheetsResultDto
{
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int TotalReceived { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> DuplicatePhones { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ImportedApplicationSummaryDto> ImportedApplications { get; set; } = new();
}

/// <summary>
/// Thông tin tóm tắt hồ sơ vừa được import
/// </summary>
public class ImportedApplicationSummaryDto
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

/// <summary>
/// Payload gửi từ Webhook Google Apps Script khi có ứng viên bấm nút Gửi Form
/// </summary>
public class GoogleFormWebhookRequest
{
    [Required(ErrorMessage = "JobPostingId là bắt buộc")]
    public Guid JobId { get; set; }

    // Cho phép trích xuất động từ bất kỳ tên cột nào của Form/Sheet
    public string FullName { get; set; } = string.Empty;

    // Cho phép trích xuất động SĐT hoặc dùng Email/định danh thay thế nếu Form không hỏi SĐT
    public string Phone { get; set; } = string.Empty;

    public string? YearOfBirth { get; set; }
    public string? Hometown { get; set; }
    public string? ExperienceNotes { get; set; }
    public object? ShiftReady { get; set; }
    public string? Email { get; set; }
    public string? CvUrl { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();

    [JsonIgnore]
    public bool IsShiftReady
    {
        get
        {
            if (ShiftReady == null) return false;
            var val = ShiftReady.ToString()?.Trim().ToLowerInvariant();
            return val == "true" || val == "có" || val == "co" || val == "1" || val == "yes" || val == "y";
        }
    }
}

/// <summary>
/// Kết quả xử lý Webhook
/// </summary>
public class GoogleFormWebhookResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? CandidateId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Status { get; set; }
    public DateTime? AppliedAt { get; set; }
}

/// <summary>
/// Request đồng bộ trực tiếp từ đường link Google Sheet
/// </summary>
public class SyncGoogleSheetRequest
{
    [Required(ErrorMessage = "JobId là bắt buộc")]
    public Guid JobId { get; set; }

    [Required(ErrorMessage = "Đường link Google Sheet không được để trống")]
    public string SheetUrl { get; set; } = string.Empty;
    public GoogleSheetColumnMappingDto? ColumnMapping { get; set; }
}

public class GoogleSheetColumnMappingDto
{
    public string? FullNameColumn { get; set; }
    public string? PhoneColumn { get; set; }
    public string? EmailColumn { get; set; }
    public string? YearOfBirthColumn { get; set; }
    public string? HometownColumn { get; set; }
    public string? ExperienceColumn { get; set; }
    public string? ShiftReadyColumn { get; set; }
    public string? CvUrlColumn { get; set; }
}

public class GoogleSheetPreviewResultDto
{
    public List<string> Headers { get; set; } = new();
    public Dictionary<string, string> SampleRow { get; set; } = new();
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();
    public int TotalRows { get; set; }
    public GoogleSheetColumnMappingDto SuggestedMapping { get; set; } = new();
}

public class PreviewGoogleSheetRequest
{
    [Required(ErrorMessage = "Đường link Google Sheet không được để trống")]
    public string SheetUrl { get; set; } = string.Empty;

}


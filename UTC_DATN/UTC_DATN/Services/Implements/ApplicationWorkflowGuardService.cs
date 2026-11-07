using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Services.Implements;

#region DTOs cho 7 Logic Nghiệp Vụ Kiểm Soát & Onboarding

public class CandidateIntakeValidationResult
{
    public bool IsAllowed { get; set; } = true;
    public bool ShouldSkipAiQueue { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public bool IsMergedDuplicate { get; set; }
    public int DuplicateCount { get; set; } = 1;
    public Guid? ExistingApplicationId { get; set; }
    public string PhoneNormalized { get; set; } = string.Empty;
    public string? CitizenIdNormalized { get; set; }
    public string AssignedStatus { get; set; } = "PENDING";
    public string Message { get; set; } = string.Empty;
}

public class PdfExtractionValidationResult
{
    public bool IsValidForGeminiBatch { get; set; }
    public bool IsScannedOrUnparseablePdf { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public string AssignedStatus { get; set; } = "PENDING";
    public string? WarningSummary { get; set; }
}

public class QuotaEvaluationResult
{
    public bool CanProceedToOnboardingOrHired { get; set; }
    public bool MovedToWaitlist { get; set; }
    public bool ExceededWaitlistBuffer { get; set; }
    public string AssignedStatus { get; set; } = string.Empty;
    public int CurrentHiredCount { get; set; }
    public int TargetQuantity { get; set; }
    public int CurrentWaitlistCount { get; set; }
    public int MaxWaitlistAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SlaOverrideRequestDto
{
    public string SlaOverrideReason { get; set; } = string.Empty;
    public string TargetStatus { get; set; } = "ScheduledInterview";
}

public class BulkOnboardingConvertRequest
{
    public List<Guid> ApplicationIds { get; set; } = new();
    public bool CreateSystemAccount { get; set; }
    public Dictionary<Guid, bool>? PerCandidateCreateAccount { get; set; }
}

public class BulkOnboardingConvertItemResult
{
    public Guid ApplicationId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ConvertedEmployeeCode { get; set; }
    public bool SystemAccountCreated { get; set; }
    public string? SystemAccountEmail { get; set; }
    public bool MovedToWaitlist { get; set; }
    public bool Skipped { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkOnboardingConvertResult
{
    public bool Success { get; set; }
    public int ConvertedCount { get; set; }
    public int WaitlistedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BulkOnboardingConvertItemResult> Items { get; set; } = new();
}

public class WaitlistSuggestionDto
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CitizenId { get; set; }
    public int AiMatchingScore { get; set; }
    public string MedicalCheckStatus { get; set; } = "Passed";
    public string JobTitle { get; set; } = string.Empty;
}

public class MarkNoShowResult
{
    public bool Success { get; set; }
    public Guid ApplicationId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string? RevokedEmployeeCode { get; set; }
    public int UpdatedHiredCount { get; set; }
    public int TargetQuantity { get; set; }
    public string Message { get; set; } = string.Empty;
    public WaitlistSuggestionDto? SuggestedWaitlistCandidate { get; set; }
}

#endregion

public interface IApplicationWorkflowGuardService
{
    string NormalizePhone(string? rawPhone);
    string? NormalizeCitizenId(string? rawCitizenId);
    Task<CandidateIntakeValidationResult> ValidateCandidateIntakeAsync(Application application, string? rawPhone, string? rawCitizenId, DateTime? dateOfBirth = null, int? birthYear = null);
    PdfExtractionValidationResult ExtractAndValidatePdfText(Application application, string? pdfFilePath = null, Stream? pdfStream = null);
    void ApplyScoreVersionSnapshot(Application application, Job jobPosting, string? scoringConfigJson = null);
    bool ComputeIsOutdatedScore(Application application, Job? jobPosting);
    (bool IsValid, string ErrorMessage) ValidateStrictStateTransition(Application application, string targetStatus);
    Task<QuotaEvaluationResult> EvaluateQuotaAndWaitlistAsync(Application application, Job jobPosting, string requestedStatus);
    Task<(bool Success, string Message, Application? UpdatedApplication)> SlaOverrideApproveAsync(Guid applicationId, string overrideReason, string targetStatus, Guid? actorUserId, bool isAuthorizedHrOrAdmin);
    Task<BulkOnboardingConvertResult> BulkConvertOnboardingAsync(BulkOnboardingConvertRequest request, Guid? actorUserId);
    Task<MarkNoShowResult> MarkNoShowAsync(Guid applicationId, string? reason, Guid? actorUserId);
    Task<(bool Success, string Message, string? NewStatus)> PromoteWaitlistedCandidateAsync(Guid waitlistApplicationId, Guid? actorUserId);
    Task<(bool Success, string Message)> OverrideAiAsync(Guid applicationId, bool overrideToPassed, string overrideReason, string? targetStatus, Guid? actorUserId);
    Task<(bool Success, string Message)> SendOfferAsync(Guid applicationId, decimal offeredSalary, string? note, Guid? actorUserId);
    Task<(bool Success, string Message)> RespondOfferAsync(Guid applicationId, bool accepted, string? responseNote);
    Task<(bool Success, string Message)> TransferJobAsync(Guid applicationId, Guid targetJobId, string transferReason, Guid? actorUserId);
}

/// <summary>
/// Service xử lý 7 Logic Nghiệp Vụ chặt chẽ cho hệ thống ATS Khu Công Nghiệp (Dual-Track: PROFESSIONAL & MASS_LABOR)
/// </summary>
public class ApplicationWorkflowGuardService : IApplicationWorkflowGuardService
{
    private readonly UTC_DATNContext _context;
    private readonly ILogger<ApplicationWorkflowGuardService> _logger;

    public ApplicationWorkflowGuardService(
        UTC_DATNContext context,
        ILogger<ApplicationWorkflowGuardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================================================
    // LOGIC 1: Chuẩn hóa SĐT/CCCD, Chặn Blacklist & Chống trùng lặp 60 ngày
    // =========================================================================================
    public string NormalizePhone(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return string.Empty;

        // Xóa khoảng trắng, dấu gạch ngang, dấu chấm, dấu ngoặc
        var cleaned = Regex.Replace(rawPhone.Trim(), @"[\s\-\.\(\)]", "");

        // Chuẩn hóa +84 hoặc 84 ở đầu thành 0
        if (cleaned.StartsWith("+84"))
        {
            cleaned = "0" + cleaned.Substring(3);
        }
        else if (cleaned.StartsWith("84") && cleaned.Length >= 11)
        {
            cleaned = "0" + cleaned.Substring(2);
        }

        return cleaned;
    }

    public string? NormalizeCitizenId(string? rawCitizenId)
    {
        if (string.IsNullOrWhiteSpace(rawCitizenId))
            return null;

        var digitsOnly = Regex.Replace(rawCitizenId.Trim(), @"\D", "");
        return string.IsNullOrWhiteSpace(digitsOnly) ? null : digitsOnly;
    }

    public async Task<CandidateIntakeValidationResult> ValidateCandidateIntakeAsync(
        Application application,
        string? rawPhone,
        string? rawCitizenId,
        DateTime? dateOfBirth = null,
        int? birthYear = null)
    {
        var phoneNorm = NormalizePhone(rawPhone ?? application.ContactPhone);
        var citizenNorm = NormalizeCitizenId(rawCitizenId ?? application.CitizenId);

        application.PhoneNormalized = phoneNorm;
        if (!string.IsNullOrWhiteSpace(citizenNorm))
        {
            application.CitizenId = citizenNorm;
        }

        if (dateOfBirth.HasValue)
        {
            application.DateOfBirth = dateOfBirth.Value.Date;
        }

        var result = new CandidateIntakeValidationResult
        {
            PhoneNormalized = phoneNorm,
            CitizenIdNormalized = citizenNorm,
            AssignedStatus = string.IsNullOrWhiteSpace(application.Status) ? "PENDING" : application.Status
        };

        // 1.0. Kiểm tra Độ tuổi Lao động theo Bộ luật Lao động Việt Nam (ValidateLaborLawAge)
        var job = application.Job ?? (application.JobId != Guid.Empty ? await _context.Jobs.FindAsync(application.JobId) : null);
        if (job != null && job.RequireNightShift)
        {
            int? age = null;
            var today = DateTime.UtcNow.Date;

            if (application.DateOfBirth.HasValue)
            {
                age = today.Year - application.DateOfBirth.Value.Year;
                if (application.DateOfBirth.Value.Date > today.AddYears(-age.Value))
                    age--;
            }
            else if (birthYear.HasValue && birthYear.Value > 1900)
            {
                age = today.Year - birthYear.Value;
            }

            if (age.HasValue && age.Value < 18)
            {
                var under18Msg = "Vi phạm điều kiện Bộ luật Lao động: Không sử dụng lao động chưa thành niên (dưới 18 tuổi) làm việc ban đêm (22h-06h) hoặc dây chuyền nặng nhọc";
                application.Status = "Rejected_LaborLaw_Under18";
                application.AiAnalysisSummary = under18Msg;

                result.IsAllowed = false;
                result.ShouldSkipAiQueue = true;
                result.AssignedStatus = "Rejected_LaborLaw_Under18";
                result.Message = under18Msg;

                _logger.LogWarning("⛔ [LABOR LAW GUARD] Chặn ứng viên {Name} ({Age} tuổi) nộp vào Job ca đêm {JobTitle}",
                    application.ContactName, age.Value, job.Title);

                return result;
            }
        }

        // 1.1. Tra cứu CitizenId hoặc PhoneNormalized trong bảng CandidateBlacklists
        CandidateBlacklist? matchedBlacklist = null;
        if (!string.IsNullOrWhiteSpace(citizenNorm) || !string.IsNullOrWhiteSpace(phoneNorm))
        {
            matchedBlacklist = await _context.CandidateBlacklists
                .AsNoTracking()
                .Where(b =>
                    (!string.IsNullOrWhiteSpace(citizenNorm) && b.CitizenId == citizenNorm) ||
                    (!string.IsNullOrWhiteSpace(phoneNorm) && b.PhoneNormalized == phoneNorm))
                .OrderByDescending(b => b.BanType == "PERMANENT")
                .ThenByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (matchedBlacklist != null)
        {
            bool isBanActive = string.Equals(matchedBlacklist.BanType, "PERMANENT", StringComparison.OrdinalIgnoreCase)
                || (matchedBlacklist.BannedUntil.HasValue && matchedBlacklist.BannedUntil.Value > DateTime.UtcNow);

            if (isBanActive)
            {
                application.Status = "Rejected_Blacklisted";
                application.IsBlacklisted = true;
                application.BlacklistWarningNote = $"[🚫 Blacklist - {matchedBlacklist.BanType}] {matchedBlacklist.Reason}";

                result.IsAllowed = false;
                result.IsBlacklisted = true;
                result.ShouldSkipAiQueue = true;
                result.BlacklistReason = application.BlacklistWarningNote;
                result.AssignedStatus = "Rejected_Blacklisted";
                result.Message = $"Ứng viên nằm trong danh sách cấm tuyển ({matchedBlacklist.BanType}): {matchedBlacklist.Reason}";

                _logger.LogWarning(
                    "🚫 [BLACKLIST GUARD] Chặn tự động ứng viên {Name} (SĐT: {Phone}, CCCD: {CCCD}) - Lý do: {Reason}",
                    application.ContactName, phoneNorm, citizenNorm, matchedBlacklist.Reason);

                return result;
            }
        }

        // 1.2. Kiểm tra trùng lặp trong 60 ngày gần nhất tại cùng JobId
        var cutoff60Days = DateTime.UtcNow.AddDays(-60);
        if (application.JobId != Guid.Empty && (!string.IsNullOrWhiteSpace(phoneNorm) || !string.IsNullOrWhiteSpace(citizenNorm)))
        {
            var existingDuplicate = await _context.Applications
                .Where(a => a.JobId == application.JobId
                         && a.ApplicationId != application.ApplicationId
                         && a.AppliedAt >= cutoff60Days
                         && ((!string.IsNullOrWhiteSpace(phoneNorm) && (a.PhoneNormalized == phoneNorm || a.ContactPhone == phoneNorm))
                          || (!string.IsNullOrWhiteSpace(citizenNorm) && a.CitizenId == citizenNorm)))
                .OrderByDescending(a => a.AppliedAt)
                .FirstOrDefaultAsync();

            if (existingDuplicate != null)
            {
                existingDuplicate.DuplicateCount = Math.Max(1, existingDuplicate.DuplicateCount) + 1;
                existingDuplicate.LastStageChangedAt = DateTime.UtcNow;

                application.DuplicateCount = existingDuplicate.DuplicateCount;

                result.IsMergedDuplicate = true;
                result.DuplicateCount = existingDuplicate.DuplicateCount;
                result.ExistingApplicationId = existingDuplicate.ApplicationId;
                result.Message = $"[⚠️ Nộp trùng {existingDuplicate.DuplicateCount} lần] trong vòng 60 ngày tại cùng vị trí tuyển dụng. Đã gộp vào hồ sơ hiện tại.";

                _logger.LogInformation(
                    "⚠️ [DUPLICATE GUARD] Phát hiện nộp trùng lần thứ {Count} cho SĐT {Phone} / CCCD {CCCD} tại Job {JobId}",
                    existingDuplicate.DuplicateCount, phoneNorm, citizenNorm, application.JobId);
            }
        }

        return result;
    }

    // =========================================================================================
    // LOGIC 2: Bắt lỗi File PDF Scan dạng ảnh (ExtractAndValidatePdfText)
    // =========================================================================================
    public PdfExtractionValidationResult ExtractAndValidatePdfText(
        Application application,
        string? pdfFilePath = null,
        Stream? pdfStream = null)
    {
        var extractedBuilder = new StringBuilder();

        try
        {
            if (pdfStream != null)
            {
                if (pdfStream.CanSeek) pdfStream.Position = 0;
                using var document = PdfDocument.Open(pdfStream);
                foreach (var page in document.GetPages())
                {
                    if (!string.IsNullOrWhiteSpace(page.Text))
                        extractedBuilder.AppendLine(page.Text);
                }
            }
            else if (!string.IsNullOrWhiteSpace(pdfFilePath) && System.IO.File.Exists(pdfFilePath))
            {
                using var document = PdfDocument.Open(pdfFilePath);
                foreach (var page in document.GetPages())
                {
                    if (!string.IsNullOrWhiteSpace(page.Text))
                        extractedBuilder.AppendLine(page.Text);
                }
            }
            else if (!string.IsNullOrWhiteSpace(application.RawResumeText))
            {
                extractedBuilder.Append(application.RawResumeText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [PDF GUARD] Không thể đọc cấu trúc PDF cho Application {AppId}", application.ApplicationId);
        }

        var extractedText = extractedBuilder.ToString().Trim();
        application.RawResumeText = extractedText;

        // Nếu text bóc ra < 50 ký tự -> File PDF scan dạng ảnh hoặc lỗi font không đọc được
        if (extractedText.Length < 50)
        {
            const string scanWarning = "File PDF dạng ảnh scan không có lớp văn bản, cần kiểm tra trực tiếp hoặc OCR.";
            application.IsScannedOrUnparseablePdf = true;
            application.Status = "Need_Manual_Review";
            application.AiAnalysisSummary = scanWarning;

            return new PdfExtractionValidationResult
            {
                IsValidForGeminiBatch = false,
                IsScannedOrUnparseablePdf = true,
                ExtractedText = extractedText,
                CharacterCount = extractedText.Length,
                AssignedStatus = "Need_Manual_Review",
                WarningSummary = scanWarning
            };
        }

        application.IsScannedOrUnparseablePdf = false;
        return new PdfExtractionValidationResult
        {
            IsValidForGeminiBatch = true,
            IsScannedOrUnparseablePdf = false,
            ExtractedText = extractedText,
            CharacterCount = extractedText.Length,
            AssignedStatus = string.IsNullOrWhiteSpace(application.Status) ? "PENDING" : application.Status
        };
    }

    // =========================================================================================
    // LOGIC 3: Snapshot Phiên bản Trọng số AI (Score Versioning)
    // =========================================================================================
    public void ApplyScoreVersionSnapshot(Application application, Job jobPosting, string? scoringConfigJson = null)
    {
        var currentVersion = jobPosting.CurrentScoringWeightVersion > 0
            ? jobPosting.CurrentScoringWeightVersion
            : 1;

        application.AppliedScoringVersion = currentVersion;
        application.AppliedScoringConfigJson = !string.IsNullOrWhiteSpace(scoringConfigJson)
            ? scoringConfigJson
            : $"{{\"version\":{currentVersion},\"track\":\"{jobPosting.RecruitmentTrack ?? "PROFESSIONAL"}\",\"capturedAt\":\"{DateTime.UtcNow:O}\"}}";
    }

    public bool ComputeIsOutdatedScore(Application application, Job? jobPosting)
    {
        if (jobPosting == null) return false;
        var appliedVer = application.AppliedScoringVersion ?? 1;
        var jobVer = jobPosting.CurrentScoringWeightVersion > 0 ? jobPosting.CurrentScoringWeightVersion : 1;
        return appliedVer < jobVer;
    }

    // =========================================================================================
    // LOGIC 4: Ràng buộc Máy trạng thái (Strict State Machine) & Điều kiện cứng trước Onboarding
    // =========================================================================================
    public (bool IsValid, string ErrorMessage) ValidateStrictStateTransition(Application application, string targetStatus)
    {
        var current = (application.Status ?? "PENDING").Trim();
        var next = (targetStatus ?? string.Empty).Trim();

        bool isTargetHired = string.Equals(next, "Hired", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(next, "HIRED", StringComparison.OrdinalIgnoreCase);

        bool isCurrentRejectedOrBlacklisted =
            string.Equals(current, "Rejected", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(current, "REJECTED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(current, "Rejected_Blacklisted", StringComparison.OrdinalIgnoreCase);

        if (isCurrentRejectedOrBlacklisted && isTargetHired)
        {
            return (false, $"Vi phạm quy tắc Máy trạng thái (Strict State Machine): Không được phép chuyển nhảy cóc trực tiếp từ trạng thái '{current}' sang 'Hired'.");
        }

        if (application.IsBlacklisted && isTargetHired)
        {
            return (false, "Ứng viên đang nằm trong Danh sách đen (Blacklist), nghiêm cấm cấp Mã nhân viên (Hired).");
        }

        if (isTargetHired)
        {
            // Kiểm tra Offer Letter cho khối Kỹ thuật / Văn phòng (PROFESSIONAL)
            var jobTrack = application.Job?.RecruitmentTrack ?? "PROFESSIONAL";
            if (string.Equals(jobTrack, "PROFESSIONAL", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(application.OfferStatus, "Offer_Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Ứng viên khối Kỹ thuật/Văn phòng (PROFESSIONAL) bắt buộc phải qua bước gửi Offer và được xác nhận 'Offer_Accepted' trước khi cấp Mã nhân viên / Tiếp nhận.");
                }
            }

            bool medicalPassed = string.Equals(application.MedicalCheckStatus, "Passed", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(application.MedicalCheckStatus, "PASSED", StringComparison.OrdinalIgnoreCase);

            if (!medicalPassed)
            {
                return (false, "Điều kiện cứng trước Onboarding: Chỉ cho phép chuyển sang 'Hired' (Cấp mã nhân viên) khi trạng thái Khám sức khỏe đạt ('Passed').");
            }

            if (string.IsNullOrWhiteSpace(application.CitizenId))
            {
                return (false, "Điều kiện cứng trước Onboarding: Ứng viên bắt buộc phải bổ sung đầy đủ Số CCCD (CitizenId) trước khi cấp Mã nhân viên.");
            }
        }

        return (true, string.Empty);
    }

    // =========================================================================================
    // LOGIC 5: Khóa Chỉ tiêu (Quota Lock) & Danh sách Dự bị tự động (Waitlist Pool)
    // =========================================================================================
    public async Task<QuotaEvaluationResult> EvaluateQuotaAndWaitlistAsync(
        Application application,
        Job jobPosting,
        string requestedStatus)
    {
        int targetQty = jobPosting.TargetQuantity > 0 ? jobPosting.TargetQuantity : (jobPosting.NumberOfPositions ?? 50);
        int hiredCount = Math.Max(0, jobPosting.HiredCount);
        int bufferPercent = jobPosting.WaitlistBufferPercent > 0 ? jobPosting.WaitlistBufferPercent : 15;
        int maxWaitlistAllowed = Math.Max(1, (int)Math.Ceiling(targetQty * (bufferPercent / 100.0)));

        int currentWaitlistCount = await _context.Applications
            .CountAsync(a => a.JobId == jobPosting.JobId
                          && a.ApplicationId != application.ApplicationId
                          && a.Status == "Waitlisted");

        var result = new QuotaEvaluationResult
        {
            CurrentHiredCount = hiredCount,
            TargetQuantity = targetQty,
            CurrentWaitlistCount = currentWaitlistCount,
            MaxWaitlistAllowed = maxWaitlistAllowed,
            AssignedStatus = requestedStatus
        };

        // Nếu chỉ tiêu đã đầy (HiredCount >= TargetQuantity)
        if (hiredCount >= targetQty)
        {
            application.Status = "Waitlisted";
            application.LastStageChangedAt = DateTime.UtcNow;

            result.CanProceedToOnboardingOrHired = false;
            result.MovedToWaitlist = true;
            result.ExceededWaitlistBuffer = currentWaitlistCount >= maxWaitlistAllowed;
            result.AssignedStatus = "Waitlisted";
            result.Message = $"Chỉ tiêu xưởng đã chạm trần định biên ({hiredCount}/{targetQty}). Đã tự động đưa ứng viên vào Danh sách Dự bị (Waitlisted - Dự phòng {bufferPercent}%). Khóa cấp Mã nhân viên vượt trần.";

            return result;
        }

        result.CanProceedToOnboardingOrHired = true;
        result.Message = $"Định biên còn trống ({hiredCount}/{targetQty}). Cho phép tiếp tục tiếp nhận.";
        return result;
    }

    // =========================================================================================
    // LOGIC 6: Duyệt thay khi quá hạn SLA (POST /api/applications/{id}/sla-override)
    // =========================================================================================
    public async Task<(bool Success, string Message, Application? UpdatedApplication)> SlaOverrideApproveAsync(
        Guid applicationId,
        string overrideReason,
        string targetStatus,
        Guid? actorUserId,
        bool isAuthorizedHrOrAdmin)
    {
        if (!isAuthorizedHrOrAdmin)
        {
            return (false, "Chỉ thành viên Phòng Nhân Sự (HR_TEAM) hoặc SuperAdmin mới có quyền duyệt thay khi quá hạn SLA.", null);
        }

        if (string.IsNullOrWhiteSpace(overrideReason))
        {
            return (false, "Vui lòng nhập Lý do duyệt thay quá hạn SLA (SlaOverrideReason).", null);
        }

        var app = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (app == null)
        {
            return (false, "Không tìm thấy hồ sơ ứng viên.", null);
        }

        // Kiểm tra hạn SLA (cho phép duyệt thay nếu SlaDeadlineAt đã qua hoặc hồ sơ ngâm quá 48h)
        var effectiveDeadline = app.SlaDeadlineAt ?? app.AppliedAt.AddHours(48);
        if (DateTime.UtcNow <= effectiveDeadline && !app.IsSlaEscalated)
        {
            // Vẫn cho phép override nếu HR xác nhận nhưng ghi rõ mốc thời gian
            _logger.LogInformation("ℹ️ [SLA OVERRIDE] Thực hiện duyệt thay sớm cho hồ sơ {AppId}", applicationId);
        }

        var stateCheck = ValidateStrictStateTransition(app, targetStatus);
        if (!stateCheck.IsValid)
        {
            return (false, stateCheck.ErrorMessage, null);
        }

        app.IsSlaEscalated = true;
        app.SlaOverrideReason = overrideReason.Trim();
        app.Status = string.IsNullOrWhiteSpace(targetStatus) ? "ScheduledInterview" : targetStatus.Trim();
        app.LastStageChangedAt = DateTime.UtcNow;

        _context.ApplicationNotes.Add(new ApplicationNote
        {
            NoteId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            Note = $"⚡ [SLA OVERRIDE - DUYỆT THAY QUẢN ĐỐC QUÁ HẠN]: Chuyển sang '{app.Status}'. Lý do: {app.SlaOverrideReason}"
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã duyệt thay quá hạn SLA và chuyển hồ sơ sang trạng thái '{app.Status}'.", app);
    }

    // =========================================================================================
    // LOGIC 7: Onboarding Hàng loạt Idempotent & Xử lý "No-Show"
    // =========================================================================================
    public async Task<BulkOnboardingConvertResult> BulkConvertOnboardingAsync(
        BulkOnboardingConvertRequest request,
        Guid? actorUserId)
    {
        var response = new BulkOnboardingConvertResult();

        if (request.ApplicationIds == null || request.ApplicationIds.Count == 0)
        {
            response.Success = false;
            response.Message = "Danh sách hồ sơ cần cấp Mã nhân viên không được để trống.";
            return response;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var apps = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Where(a => request.ApplicationIds.Contains(a.ApplicationId))
                .ToListAsync();

            // Lấy số thứ tự mã nhân viên lớn nhất hiện tại trong năm
            int year = DateTime.UtcNow.Year;
            string prefix = $"NV-{year}-";
            var existingCodes = await _context.Applications
                .AsNoTracking()
                .Where(a => a.ConvertedEmployeeCode != null && a.ConvertedEmployeeCode.StartsWith(prefix))
                .Select(a => a.ConvertedEmployeeCode)
                .ToListAsync();

            int nextSeq = 1;
            foreach (var code in existingCodes)
            {
                if (code != null && code.Length > prefix.Length && int.TryParse(code.Substring(prefix.Length), out int parsedSeq))
                {
                    if (parsedSeq >= nextSeq) nextSeq = parsedSeq + 1;
                }
            }

            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "STAFF" || r.Code == "CANDIDATE" || r.Code == "INTERVIEWER");

            foreach (var app in apps)
            {
                var candidateName = app.ContactName ?? app.Candidate?.FullName ?? "Ứng viên";
                var job = app.Job;

                // 7.1. Idempotent check: Nếu đã có ConvertedEmployeeCode thì bỏ qua không sinh trùng
                if (!string.IsNullOrWhiteSpace(app.ConvertedEmployeeCode))
                {
                    response.SkippedCount++;
                    response.Items.Add(new BulkOnboardingConvertItemResult
                    {
                        ApplicationId = app.ApplicationId,
                        CandidateName = candidateName,
                        Status = app.Status,
                        ConvertedEmployeeCode = app.ConvertedEmployeeCode,
                        Skipped = true,
                        Message = $"Đã có mã nhân viên ({app.ConvertedEmployeeCode}) - Bỏ qua (Idempotent)."
                    });
                    continue;
                }

                // 7.2. Kiểm tra điều kiện cứng (Logic 4): Đạt KSK & Có CCCD
                var stateValidation = ValidateStrictStateTransition(app, "Hired");
                if (!stateValidation.IsValid)
                {
                    response.SkippedCount++;
                    response.Items.Add(new BulkOnboardingConvertItemResult
                    {
                        ApplicationId = app.ApplicationId,
                        CandidateName = candidateName,
                        Status = app.Status,
                        Skipped = true,
                        Message = stateValidation.ErrorMessage
                    });
                    continue;
                }

                // 7.3. Kiểm tra khóa chỉ tiêu định biên (Logic 5)
                if (job != null)
                {
                    int targetQty = job.TargetQuantity > 0 ? job.TargetQuantity : (job.NumberOfPositions ?? 50);
                    if (job.HiredCount >= targetQty)
                    {
                        app.Status = "Waitlisted";
                        app.LastStageChangedAt = DateTime.UtcNow;
                        response.WaitlistedCount++;
                        response.Items.Add(new BulkOnboardingConvertItemResult
                        {
                            ApplicationId = app.ApplicationId,
                            CandidateName = candidateName,
                            Status = "Waitlisted",
                            MovedToWaitlist = true,
                            Message = $"Xưởng đã đủ định biên ({job.HiredCount}/{targetQty}). Tự động chuyển vào Danh sách Dự bị (Waitlisted)."
                        });
                        continue;
                    }
                }

                // 7.4. Sinh mã nhân viên tuần tự duy nhất (NV-2026-0001...)
                string newEmpCode = $"{prefix}{nextSeq:D4}";
                nextSeq++;

                app.ConvertedEmployeeCode = newEmpCode;
                app.Status = "Hired";
                app.OnboardingDocsCompleted = true;
                app.LastStageChangedAt = DateTime.UtcNow;

                if (job != null)
                {
                    job.HiredCount = Math.Max(0, job.HiredCount) + 1;
                }

                // 7.5. Nếu thuộc khối PROFESSIONAL và bật cờ CreateSystemAccount -> Tạo User đăng nhập
                bool shouldCreateUser = request.CreateSystemAccount;
                if (request.PerCandidateCreateAccount != null && request.PerCandidateCreateAccount.TryGetValue(app.ApplicationId, out bool perFlag))
                {
                    shouldCreateUser = perFlag;
                }

                bool isProfessional = string.Equals(job?.RecruitmentTrack, "PROFESSIONAL", StringComparison.OrdinalIgnoreCase);
                bool accountCreated = false;
                string? accountEmail = null;

                if (isProfessional && shouldCreateUser)
                {
                    var rawEmail = app.ContactEmail ?? app.Candidate?.Email;
                    if (string.IsNullOrWhiteSpace(rawEmail))
                    {
                        rawEmail = $"{newEmpCode.ToLowerInvariant().Replace("-", "")}@factory.com";
                    }

                    var normalizedEmail = rawEmail.Trim().ToUpperInvariant();
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.Email == rawEmail);
                    if (existingUser == null)
                    {
                        var newUser = new User
                        {
                            UserId = Guid.NewGuid(),
                            Email = rawEmail.Trim(),
                            NormalizedEmail = normalizedEmail,
                            FullName = candidateName,
                            Phone = app.PhoneNormalized ?? app.ContactPhone,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Factory@2026"),
                            IsActive = true,
                            AuthProvider = "LOCAL",
                            DepartmentId = job?.DepartmentId,
                            CreatedAt = DateTime.UtcNow
                        };
                        if (staffRole != null)
                        {
                            newUser.UserRoles.Add(new UserRole
                            {
                                UserId = newUser.UserId,
                                RoleId = staffRole.RoleId,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        _context.Users.Add(newUser);
                        accountCreated = true;
                        accountEmail = rawEmail.Trim();
                    }
                }

                _context.ApplicationNotes.Add(new ApplicationNote
                {
                    NoteId = Guid.NewGuid(),
                    ApplicationId = app.ApplicationId,
                    CreatedBy = actorUserId,
                    CreatedAt = DateTime.UtcNow,
                    Note = $"🎉 [ONBOARDING THÀNH CÔNG]: Đã cấp Mã nhân viên {newEmpCode}. KSK: {app.MedicalCheckStatus}, CCCD: {app.CitizenId}"
                         + (accountCreated ? $", Đã cấp tài khoản hệ thống: {accountEmail}" : "")
                });

                response.ConvertedCount++;
                response.Items.Add(new BulkOnboardingConvertItemResult
                {
                    ApplicationId = app.ApplicationId,
                    CandidateName = candidateName,
                    Status = "Hired",
                    ConvertedEmployeeCode = newEmpCode,
                    SystemAccountCreated = accountCreated,
                    SystemAccountEmail = accountEmail,
                    Message = $"Đã cấp mã nhân viên {newEmpCode} thành công."
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            response.Success = true;
            response.Message = $"Hoàn tất xử lý Onboarding: Cấp mới {response.ConvertedCount} Mã nhân viên, Đưa vào Dự bị {response.WaitlistedCount}, Bỏ qua {response.SkippedCount}.";
            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi chạy BulkConvertOnboardingAsync");
            response.Success = false;
            response.Message = $"Lỗi giao dịch khi cấp mã nhân viên hàng loạt: {ex.Message}";
            return response;
        }
    }

    public async Task<MarkNoShowResult> MarkNoShowAsync(Guid applicationId, string? reason, Guid? actorUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var app = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (app == null)
            {
                return new MarkNoShowResult
                {
                    Success = false,
                    ApplicationId = applicationId,
                    Message = "Không tìm thấy hồ sơ ứng viên."
                };
            }

            var candidateName = app.ContactName ?? app.Candidate?.FullName ?? "Ứng viên";
            var revokedCode = app.ConvertedEmployeeCode;
            bool wasHired = !string.IsNullOrWhiteSpace(revokedCode)
                         || string.Equals(app.Status, "Hired", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(app.Status, "HIRED", StringComparison.OrdinalIgnoreCase);

            // Đổi trạng thái sang No_Show_Cancelled và thu hồi ConvertedEmployeeCode
            app.Status = "No_Show_Cancelled";
            app.ConvertedEmployeeCode = null;
            app.LastStageChangedAt = DateTime.UtcNow;

            var job = app.Job;
            if (job != null && wasHired && job.HiredCount > 0)
            {
                job.HiredCount = Math.Max(0, job.HiredCount - 1);
            }

            _context.ApplicationNotes.Add(new ApplicationNote
            {
                NoteId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                CreatedBy = actorUserId,
                CreatedAt = DateTime.UtcNow,
                Note = $"🚫 [NO-SHOW NGÀY ĐẦU]: Ứng viên bỏ việc ngày đầu. Đã thu hồi mã nhân viên '{revokedCode ?? "N/A"}' và hoàn lại 1 chỉ tiêu cho xưởng. Lý do: {reason ?? "Không đến nhận việc theo lịch hẹn"}"
            });

            await _context.SaveChangesAsync();

            // Tìm ứng viên có điểm cao nhất trong danh sách Waitlisted của cùng Job để gợi ý đôn lên thay thế
            WaitlistSuggestionDto? suggestion = null;
            if (app.JobId != Guid.Empty)
            {
                var waitlistedApps = await _context.Applications
                    .AsNoTracking()
                    .Include(a => a.Candidate)
                    .Include(a => a.Job)
                    .Include(a => a.ApplicationAiScores)
                    .Where(a => a.JobId == app.JobId && a.Status == "Waitlisted")
                    .ToListAsync();

                var bestWaitlist = waitlistedApps
                    .Select(w => new
                    {
                        App = w,
                        Score = w.ApplicationAiScores.OrderByDescending(s => s.CreatedAt).Select(s => (int?)s.MatchingScore).FirstOrDefault() ?? 85
                    })
                    .OrderByDescending(x => string.Equals(x.App.MedicalCheckStatus, "Passed", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(x => x.Score)
                    .ThenBy(x => x.App.AppliedAt)
                    .FirstOrDefault();

                if (bestWaitlist != null)
                {
                    suggestion = new WaitlistSuggestionDto
                    {
                        ApplicationId = bestWaitlist.App.ApplicationId,
                        CandidateId = bestWaitlist.App.CandidateId,
                        FullName = bestWaitlist.App.ContactName ?? bestWaitlist.App.Candidate?.FullName ?? "Ứng viên dự bị",
                        Phone = bestWaitlist.App.PhoneNormalized ?? bestWaitlist.App.ContactPhone ?? bestWaitlist.App.Candidate?.Phone ?? "",
                        CitizenId = bestWaitlist.App.CitizenId,
                        AiMatchingScore = bestWaitlist.Score,
                        MedicalCheckStatus = bestWaitlist.App.MedicalCheckStatus ?? "Passed",
                        JobTitle = bestWaitlist.App.Job?.Title ?? ""
                    };
                }
            }

            await transaction.CommitAsync();

            return new MarkNoShowResult
            {
                Success = true,
                ApplicationId = app.ApplicationId,
                CandidateName = candidateName,
                RevokedEmployeeCode = revokedCode,
                UpdatedHiredCount = job?.HiredCount ?? 0,
                TargetQuantity = job?.TargetQuantity ?? 100,
                SuggestedWaitlistCandidate = suggestion,
                Message = $"Đã ghi nhận '{candidateName}' bỏ việc ngày đầu (No_Show_Cancelled), thu hồi mã {revokedCode ?? "N/A"} và hoàn lại 1 chỉ tiêu cho xưởng."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi xử lý MarkNoShowAsync cho Application {AppId}", applicationId);
            return new MarkNoShowResult
            {
                Success = false,
                ApplicationId = applicationId,
                Message = $"Lỗi khi xử lý No-Show: {ex.Message}"
            };
        }
    }

    public async Task<(bool Success, string Message, string? NewStatus)> PromoteWaitlistedCandidateAsync(
        Guid waitlistApplicationId,
        Guid? actorUserId)
    {
        var app = await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == waitlistApplicationId);

        if (app == null)
            return (false, "Không tìm thấy hồ sơ ứng viên dự bị.", null);

        var name = app.ContactName ?? app.Candidate?.FullName ?? "Ứng viên dự bị";
        app.Status = "InterviewPassed";
        app.LastStageChangedAt = DateTime.UtcNow;

        _context.ApplicationNotes.Add(new ApplicationNote
        {
            NoteId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            Note = $"⬆️ [ĐÔN DỰ BỊ LÊN CHÍNH THỨC]: Đã đôn ứng viên '{name}' từ danh sách Waitlisted lên thay thế chỉ tiêu trống."
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã đôn ứng viên dự bị '{name}' lên danh sách Đủ điều kiện tiếp nhận chính thức!", app.Status);
    }

    // =========================================================================================
    // V3 ENTERPRISE COMPLIANCE: HUMAN OVERRIDE AI, OFFER MANAGEMENT & CROSS-DEPARTMENT TRANSFER
    // =========================================================================================

    public async Task<(bool Success, string Message)> OverrideAiAsync(
        Guid applicationId,
        bool overrideToPassed,
        string overrideReason,
        string? targetStatus,
        Guid? actorUserId)
    {
        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null)
            return (false, "Không tìm thấy hồ sơ ứng viên.");

        if (string.IsNullOrWhiteSpace(overrideReason))
            return (false, "Lý do ghi đè kết quả AI (Human Override) là bắt buộc.");

        application.IsAiOverriddenByHuman = true;
        application.AiOverrideReason = overrideReason.Trim();

        if (overrideToPassed)
        {
            application.Status = !string.IsNullOrWhiteSpace(targetStatus) ? targetStatus : "Interview_Pending";
        }
        else
        {
            application.Status = !string.IsNullOrWhiteSpace(targetStatus) ? targetStatus : "Rejected";
        }

        application.LastStageChangedAt = DateTime.UtcNow;

        _context.ApplicationNotes.Add(new ApplicationNote
        {
            NoteId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            Note = $"[⚖️ Human Override] {(overrideToPassed ? "Vớt hồ sơ vào phỏng vấn" : "Loại hồ sơ")}. Lý do: {overrideReason.Trim()}",
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã ghi đè kết quả AI thành công. Trạng thái mới: {application.Status}.");
    }

    public async Task<(bool Success, string Message)> SendOfferAsync(
        Guid applicationId,
        decimal offeredSalary,
        string? note,
        Guid? actorUserId)
    {
        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null)
            return (false, "Không tìm thấy hồ sơ ứng viên.");

        if (offeredSalary <= 0)
            return (false, "Mức lương đề xuất (OfferedSalary) phải lớn hơn 0.");

        application.OfferStatus = "Offer_Sent";
        application.OfferedSalary = offeredSalary;
        application.OfferResponseNote = note?.Trim();
        application.Status = "Offer_Sent";
        application.LastStageChangedAt = DateTime.UtcNow;

        _context.ApplicationNotes.Add(new ApplicationNote
        {
            NoteId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            Note = $"[📩 Gửi Offer Letter] Mức lương thỏa thuận: {offeredSalary:N0} VNĐ. Ghi chú: {note}",
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã gửi Offer Letter thành công với mức lương {offeredSalary:N0} VNĐ.");
    }

    public async Task<(bool Success, string Message)> RespondOfferAsync(
        Guid applicationId,
        bool accepted,
        string? responseNote)
    {
        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null)
            return (false, "Không tìm thấy hồ sơ ứng viên.");

        if (accepted)
        {
            application.OfferStatus = "Offer_Accepted";
            application.Status = "Offer_Accepted";
            application.OfferResponseNote = responseNote?.Trim() ?? "Ứng viên đã đồng ý nhận Offer";
        }
        else
        {
            application.OfferStatus = "Offer_Declined";
            application.Status = "Offer_Declined";
            application.OfferResponseNote = responseNote?.Trim() ?? "Ứng viên từ chối Offer";
        }

        application.LastStageChangedAt = DateTime.UtcNow;

        _context.ApplicationNotes.Add(new ApplicationNote
        {
            NoteId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            Note = $"[Phản hồi Offer] {(accepted ? "✅ Ứng viên ĐỒNG Ý nhận Offer" : "❌ Ứng viên TỪ CHỐI Offer")}. Ghi chú: {responseNote}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, accepted ? "Ứng viên đã đồng ý nhận Offer, đủ điều kiện chuyển sang Onboarding." : "Đã cập nhật trạng thái từ chối Offer.");
    }

    public async Task<(bool Success, string Message)> TransferJobAsync(
        Guid applicationId,
        Guid targetJobId,
        string transferReason,
        Guid? actorUserId)
    {
        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null)
            return (false, "Không tìm thấy hồ sơ ứng viên.");

        var targetJob = await _context.Jobs.FindAsync(targetJobId);
        if (targetJob == null || targetJob.IsDeleted)
            return (false, "Không tìm thấy phân xưởng/chiến dịch đích để điều chuyển.");

        if (string.IsNullOrWhiteSpace(transferReason))
            return (false, "Lý do điều chuyển phân xưởng không được để trống.");

        var oldJobTitle = application.Job?.Title ?? "Chiến dịch cũ";
        application.TransferredFromJobId = application.JobId;
        application.JobId = targetJob.JobId;
        application.TransferReason = transferReason.Trim();
        application.Status = "Transferred_Pending";
        application.LastStageChangedAt = DateTime.UtcNow;

        _context.ApplicationNotes.Add(new ApplicationNote
        {
            NoteId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            Note = $"[🔄 Điều chuyển phân xưởng] Từ '{oldJobTitle}' sang '{targetJob.Title}'. Lý do: {transferReason.Trim()}",
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, $"Đã điều chuyển ứng viên sang chiến dịch '{targetJob.Title}' thành công.");
    }
}


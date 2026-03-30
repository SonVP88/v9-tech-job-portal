using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Application;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

public class ApplicationService : IApplicationService
{
    private readonly UTC_DATNContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ApplicationService> _logger;
    private readonly IAiMatchingService _aiMatchingService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;  // ← NEW: Para crear scope en background

    // Các extension được phép
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };
    
    // Kích thước file tối đa: 5MB
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> TerminalApplicationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "HIRED",
        "REJECTED",
        "WAITLIST"
    };

    private static readonly Dictionary<string, string> StatusToStageCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ACTIVE"] = "NEW_APPLIED",
        ["NEW_APPLIED"] = "NEW_APPLIED",
        ["INTERVIEW"] = "INTERVIEW",
        ["Pending_Offer"] = "OFFER",
        ["Offer_Sent"] = "OFFER",
        ["OFFER_ACCEPTED"] = "OFFER",
        ["REJECTED"] = "REJECTED"
    };

    public ApplicationService(
        UTC_DATNContext context,
        IWebHostEnvironment environment,
        ILogger<ApplicationService> logger,
        IAiMatchingService aiMatchingService,
        IEmailService emailService,
        INotificationService notificationService,
        IServiceProvider serviceProvider)  // ← NEW: Inject service provider
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _aiMatchingService = aiMatchingService;
        _emailService = emailService;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;  // ← NEW: Store service provider
    }

    private sealed class SlaSnapshot
    {
        public DateTime? DueAt { get; init; }
        public string Status { get; init; } = "DISABLED";
        public int? OverdueDays { get; init; }
        public int? MaxDays { get; init; }
        public int? WarnBeforeDays { get; init; }
    }

    private static SlaSnapshot CalculateSlaSnapshot(DateTime enteredStageAt, PipelineStage? stage, string? appStatus)
    {
        if (stage == null)
        {
            return new SlaSnapshot();
        }

        if (!string.IsNullOrWhiteSpace(appStatus) && TerminalApplicationStatuses.Contains(appStatus))
        {
            return new SlaSnapshot();
        }

        if (stage.IsSlaEnabled != true || !stage.SlaMaxDays.HasValue || stage.SlaMaxDays.Value <= 0)
        {
            return new SlaSnapshot
            {
                MaxDays = stage.SlaMaxDays,
                WarnBeforeDays = stage.SlaWarnBeforeDays
            };
        }

        var maxDays = stage.SlaMaxDays.Value;
        var warnBeforeDays = stage.SlaWarnBeforeDays.GetValueOrDefault(1);
        if (warnBeforeDays < 0)
        {
            warnBeforeDays = 0;
        }

        var now = DateTime.UtcNow;
        var dueAt = enteredStageAt.AddDays(maxDays);
        var warningAt = dueAt.AddDays(-warnBeforeDays);

        if (now > dueAt)
        {
            var overdueDays = (int)Math.Ceiling((now - dueAt).TotalDays);
            if (overdueDays < 1)
            {
                overdueDays = 1;
            }

            return new SlaSnapshot
            {
                DueAt = dueAt,
                Status = "OVERDUE",
                OverdueDays = overdueDays,
                MaxDays = maxDays,
                WarnBeforeDays = warnBeforeDays
            };
        }

        return new SlaSnapshot
        {
            DueAt = dueAt,
            Status = now >= warningAt ? "WARNING" : "ON_TRACK",
            OverdueDays = 0,
            MaxDays = maxDays,
            WarnBeforeDays = warnBeforeDays
        };
    }


    public async Task<bool> ApplyJobAsync(ApplyJobRequest request, Guid? userId)
    {
        string? savedFilePath = null;

        try
        {
            // === BƯỚC 1: VALIDATE VÀ LƯU FILE ===
            _logger.LogInformation("Bắt đầu xử lý nộp hồ sơ cho JobId: {JobId}, Email: {Email}, UserId: {UserId}", 
                request.JobId, request.Email, userId?.ToString() ?? "NULL");

            // Kiểm tra file CV
            if (request.CVFile == null || request.CVFile.Length == 0)
            {
                _logger.LogWarning("File CV không hợp lệ");
                throw new ArgumentException("File CV là bắt buộc");
            }

            // Kiểm tra extension
            var fileExtension = Path.GetExtension(request.CVFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Extension không hợp lệ: {Extension}", fileExtension);
                throw new ArgumentException($"Chỉ chấp nhận file PDF hoặc DOCX. File của bạn: {fileExtension}");
            }

            // Kiểm tra kích thước
            if (request.CVFile.Length > MaxFileSize)
            {
                _logger.LogWarning("File quá lớn: {Size} bytes", request.CVFile.Length);
                throw new ArgumentException($"File không được vượt quá {MaxFileSize / 1024 / 1024}MB");
            }

            // Tạo tên file mới
            var newFileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "cvs");
            
            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation("Đã tạo thư mục: {Folder}", uploadsFolder);
            }

            savedFilePath = Path.Combine(uploadsFolder, newFileName);

            // Lưu file vật lý
            using (var fileStream = new FileStream(savedFilePath, FileMode.Create))
            {
                await request.CVFile.CopyToAsync(fileStream);
            }
            _logger.LogInformation("Đã lưu file CV: {FilePath}", savedFilePath);

            // === BẮT ĐẦU TRANSACTION ===
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // === BƯỚC 2: LƯU BẢN GHI FILES ===
                var mimeType = fileExtension == ".pdf" 
                    ? "application/pdf" 
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                var fileEntity = new Entities.File
                {
                    FileId = Guid.NewGuid(),
                    Provider = "LOCAL",
                    OriginalFileName = request.CVFile.FileName,
                    StoredFileName = newFileName,
                    MimeType = mimeType,
                    SizeBytes = request.CVFile.Length,
                    LocalPath = $"/uploads/cvs/{newFileName}",
                    Url = $"/uploads/cvs/{newFileName}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Files.Add(fileEntity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã tạo bản ghi File: {FileId}", fileEntity.FileId);

                // === BƯỚC 3: TÌM/TẠO CANDIDATE (LOGIC MỚI BẢO VỆ DANH TÍNH) ===
                Candidate? candidate = null;

                // Priority 1: Dành cho User ĐÃ ĐĂNG NHẬP
                if (userId.HasValue)
                {
                    var userEntity = await _context.Users.FindAsync(userId.Value);
                    if (userEntity != null)
                    {
                        var normalizedIdentityEmail = userEntity.Email.Trim().ToUpper();

                        // Tìm qua UserId trước
                        candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.UserId == userId.Value);

                        if (candidate == null)
                        {
                            // Tìm theo Identity Email của acc này nếu họ từng apply diện Guest
                            candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.NormalizedEmail == normalizedIdentityEmail);

                            if (candidate != null)
                            {
                                // Auto-link
                                candidate.UserId = userId.Value;
                                _logger.LogInformation("🔗 Auto-link Candidate {CandidateId} với User {UserId} qua Email {Email}", 
                                    candidate.CandidateId, userId.Value, userEntity.Email);
                            }
                            else
                            {
                                // Chưa từng apply -> Tạo mới Candidate từ hồ sơ tài khoản.
                                // Không lấy FullName/Phone từ form apply để tránh ghi đè profile khi apply hộ người khác.
                                candidate = new Candidate
                                {
                                    CandidateId = Guid.NewGuid(),
                                    Email = userEntity.Email,
                                    NormalizedEmail = normalizedIdentityEmail,
                                    FullName = userEntity.FullName,
                                    Phone = userEntity.Phone,
                                    Summary = null,
                                    Source = "CAREER_SITE",
                                    UserId = userId.Value,
                                    CreatedAt = DateTime.UtcNow,
                                    IsDeleted = false
                                };
                                _context.Candidates.Add(candidate);
                                _logger.LogInformation("➕ Tạo mới Candidate: {CandidateId} với UserId: {UserId}", candidate.CandidateId, userId.Value);
                            }
                        }
                        else
                        {
                            // User đã đăng nhập: KHÔNG đồng bộ FullName/Phone từ form apply vào profile Candidate.
                            // Form apply có thể là dữ liệu liên hệ tạm thời cho lần ứng tuyển này.
                            _logger.LogInformation(" Giữ nguyên profile Candidate {CandidateId} cho User đăng nhập", candidate.CandidateId);
                        }
                    }
                }

                // Priority 2: Nếu chưa tìm được (User KHÔNG ĐĂNG NHẬP, hoặc account ko lệ thuộc DB -> GUEST)
                if (candidate == null)
                {
                    var normalizedContactEmail = request.Email.Trim().ToUpper();
                    candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.NormalizedEmail == normalizedContactEmail);

                    if (candidate == null)
                    {
                        candidate = new Candidate
                        {
                            CandidateId = Guid.NewGuid(),
                            Email = request.Email.Trim(), // Form email
                            NormalizedEmail = normalizedContactEmail,
                            FullName = request.FullName,
                            Phone = request.Phone,
                            Summary = request.Introduction,
                            Source = "CAREER_SITE",
                            UserId = null,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        _context.Candidates.Add(candidate);
                        _logger.LogInformation("➕ Tạo mới Candidate Guest: {CandidateId} qua Email: {Email}", candidate.CandidateId, request.Email);
                    }
                    else
                    {
                        candidate.FullName = request.FullName;
                        candidate.Phone = request.Phone;
                        candidate.Summary = request.Introduction;
                        candidate.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation(" Cập nhật Candidate Guest: {CandidateId}", candidate.CandidateId);
                    }
                }

                await _context.SaveChangesAsync();

                // === BƯỚC 4: TẠO CANDIDATEDOCUMENT ===
                var candidateDocument = new CandidateDocument
                {
                    CandidateDocumentId = Guid.NewGuid(),
                    CandidateId = candidate.CandidateId,
                    FileId = fileEntity.FileId,
                    DocType = "CV",
                    CreatedAt = DateTime.UtcNow
                };

                _context.CandidateDocuments.Add(candidateDocument);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã tạo CandidateDocument: {DocumentId}", candidateDocument.CandidateDocumentId);

                // === BƯỚC 5: TẠO APPLICATION ===
                
                // Kiểm tra Job tồn tại
                var job = await _context.Jobs
                    .FirstOrDefaultAsync(j => j.JobId == request.JobId && !j.IsDeleted);
                
                if (job == null)
                {
                    throw new ArgumentException("Công việc không tồn tại hoặc đã bị xóa");
                }

                // Kiểm tra đã apply chưa
                var existingApplication = await _context.Applications
                    .FirstOrDefaultAsync(a => a.JobId == request.JobId && a.CandidateId == candidate.CandidateId);

                if (existingApplication != null)
                {
                    throw new InvalidOperationException("Bạn đã nộp hồ sơ cho công việc này rồi");
                }

                // Lấy PipelineStage đầu tiên (stage có SortOrder thấp nhất)
                var firstStage = await _context.PipelineStages
                    .OrderBy(s => s.SortOrder)
                    .FirstOrDefaultAsync();

                if (firstStage == null)
                {
                    throw new InvalidOperationException("Không tìm thấy PipelineStage trong hệ thống");
                }

                // Tạo Application với Snapshot thông tin liên lạc (Historical Data Integrity)
                var application = new Entities.Application
                {
                    ApplicationId = Guid.NewGuid(),
                    JobId = request.JobId,
                    CandidateId = candidate.CandidateId,
                    CurrentStageId = firstStage.StageId,
                    ResumeDocumentId = candidateDocument.CandidateDocumentId,
                    Source = "CAREER_SITE",
                    Status = "ACTIVE",
                    AppliedAt = DateTime.UtcNow,
                    LastStageChangedAt = DateTime.UtcNow,
                    ContactName = request.FullName?.Trim(),
                    ContactEmail = request.Email?.Trim(),
                    ContactPhone = request.Phone?.Trim()
                };

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã tạo Application: {ApplicationId}", application.ApplicationId);

                // ===== AI SCORING (ASYNC BACKGROUND TASK) =====
                // Fire and forget: AI scoring runs in background, không block response
                _ = Task.Run(() => ScoreApplicationInBackgroundAsync(
                    application.ApplicationId,
                    savedFilePath,
                    job.Title,
                    job.Description,
                    job.Requirements
                ));
                _logger.LogInformation("⚡ Đã gửi AI scoring task vào background cho Application: {ApplicationId}", application.ApplicationId);
                // ===== END AI SCORING =====

                // 9. TẠO THÔNG BÁO CHO ỨNG VIÊN (NEW)
                if (userId.HasValue)
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            userId.Value,
                            "Ứng tuyển thành công",
                            $"Bạn đã nộp hồ sơ thành công cho vị trí {job.Title}. Chúc bạn may mắn!",
                            "APPLICATION_SUBMITTED",
                            application.ApplicationId.ToString()
                        );
                        _logger.LogInformation(" Đã tạo thông báo ứng tuyển thành công cho User {UserId}", userId);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx, " Lỗi khi tạo thông báo cho User {UserId}", userId);
                    }
                }

                // 10. TẠO THÔNG BÁO CHO ADMIN & HR (NEW - USER REQUEST)
                try
                {
                    // Lấy danh sách User có role ADMIN hoặc HR
                    var adminAndHrUsers = await _context.UserRoles
                        .Include(ur => ur.Role)
                        .Where(ur => ur.Role.Code == "ADMIN" || ur.Role.Code == "HR")
                        .Select(ur => ur.UserId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var adminId in adminAndHrUsers)
                    {
                        await _notificationService.CreateNotificationAsync(
                            adminId,
                            "Có hồ sơ ứng tuyển mới",
                            $"Ứng viên {application.ContactName ?? candidate.FullName ?? "Ứng viên"} vừa nộp hồ sơ vào vị trí {job.Title}.",
                            "NEW_APPLICATION",
                            application.ApplicationId.ToString()
                        );
                    }
                    _logger.LogInformation(" Đã tạo thông báo cho {Count} Users (ADMIN/HR)", adminAndHrUsers.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Lỗi khi tạo thông báo cho Admin/HR");
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Hoàn thành nộp hồ sơ thành công cho JobId: {JobId}", request.JobId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý transaction, đang rollback");

                // Xóa file vật lý nếu đã lưu
                if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                {
                    System.IO.File.Delete(savedFilePath);
                    _logger.LogInformation("Đã xóa file: {FilePath}", savedFilePath);
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nộp hồ sơ");
            
            if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
            {
                try
                {
                    System.IO.File.Delete(savedFilePath);
                    _logger.LogInformation("Đã xóa file do lỗi: {FilePath}", savedFilePath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "Không thể xóa file: {FilePath}", savedFilePath);
                }
            }

            throw;
        }
    }
    public async Task<List<ApplicationDto>> GetApplicationsByJobIdAsync(Guid jobId)
    {
        var applications = await _context.Applications
            .AsNoTracking()
            .Where(a => a.JobId == jobId)
            .Select(a => new
            {
                ApplicationId = a.ApplicationId,
                CandidateId = a.CandidateId,
                CandidateName = a.ContactName ?? a.Candidate.FullName ?? "Unknown",
                Email = a.ContactEmail ?? a.Candidate.Email ?? "",
                Phone = a.ContactPhone ?? a.Candidate.Phone ?? "",
                AppliedAt = a.AppliedAt,
                LastStageChangedAt = a.LastStageChangedAt,
                Status = a.Status,
                CvUrl = a.ResumeDocument != null && a.ResumeDocument.File != null 
                    ? a.ResumeDocument.File.Url 
                    : (a.Candidate.CandidateDocuments.Where(d => d.DocType == "CV").OrderByDescending(d => d.IsPrimary).Select(d => d.File.Url).FirstOrDefault() ?? ""),
                JobTitle = a.Job.Title,
                JobId = a.JobId,
                CurrentStageCode = a.CurrentStage.Code,
                CurrentStageName = a.CurrentStage.Name,
                CurrentStage = a.CurrentStage,
                LatestScore = a.ApplicationAiScores
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new { s.MatchingScore, s.MatchedSkillsJson })
                    .FirstOrDefault()
            })
            .OrderByDescending(a => a.LatestScore != null ? a.LatestScore.MatchingScore : -1)
            .ThenByDescending(a => a.AppliedAt)
            .ToListAsync();
        var result = applications.Select(a =>
        {
            string explanation = null;
            var sla = CalculateSlaSnapshot(a.LastStageChangedAt, a.CurrentStage, a.Status);
            if (a.LatestScore != null && !string.IsNullOrEmpty(a.LatestScore.MatchedSkillsJson))
            {
                try
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(a.LatestScore.MatchedSkillsJson))
                    {
                        if (doc.RootElement.TryGetProperty("explanation", out var expElement))
                        {
                            explanation = expElement.GetString();
                        }
                    }
                }
                catch
                {
                }
            }

            return new ApplicationDto
            {
                ApplicationId = a.ApplicationId,
                CandidateId = a.CandidateId,
                CandidateName = a.CandidateName,
                Email = a.Email,
                Phone = a.Phone,
                AppliedAt = a.AppliedAt,
                Status = a.Status,
                CvUrl = a.CvUrl,
                JobTitle = a.JobTitle,
                JobId = a.JobId,
                MatchScore = (int?)a.LatestScore?.MatchingScore,
                AiExplanation = explanation,
                CurrentStageCode = a.CurrentStageCode,
                CurrentStageName = a.CurrentStageName,
                SlaDueAt = sla.DueAt,
                SlaStatus = sla.Status,
                SlaOverdueDays = sla.OverdueDays,
                SlaMaxDays = sla.MaxDays,
                SlaWarnBeforeDays = sla.WarnBeforeDays
            };
        }).ToList();

        return result;
    }

    public async Task<UpdateApplicationStatusResponse?> UpdateStatusAsync(Guid applicationId, string newStatus, bool isHrAction = true, Guid? actorUserId = null)
    {
        var application = await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Job)
                .ThenInclude(j => j.CreatedByNavigation)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
            
        if (application == null) return null;

        var oldStatus = application.Status;
        application.Status = newStatus;

        // Khi đổi status, cố gắng đồng bộ stage tương ứng để SLA reset đúng mốc.
        if (StatusToStageCodeMap.TryGetValue(newStatus, out var mappedStageCode))
        {
            var targetStage = await _context.PipelineStages
                .FirstOrDefaultAsync(s => s.Code == mappedStageCode);

            if (targetStage != null && targetStage.StageId != application.CurrentStageId)
            {
                var oldStageId = application.CurrentStageId;
                application.CurrentStageId = targetStage.StageId;
                application.LastStageChangedAt = DateTime.UtcNow;

                _context.ApplicationStageHistories.Add(new ApplicationStageHistory
                {
                    HistoryId = Guid.NewGuid(),
                    ApplicationId = application.ApplicationId,
                    FromStageId = oldStageId,
                    ToStageId = targetStage.StageId,
                    ChangedBy = actorUserId,
                    Reason = $"Auto sync from status change: {oldStatus} -> {newStatus}",
                    ChangedAt = DateTime.UtcNow
                });
            }
        }
        
        var saveResult = await _context.SaveChangesAsync() > 0;

        // Gửi email tự động nếu status là HIRED hoặc REJECTED
        // NOTE: Theo yêu cầu hiện tại, khi CANDIDATE tự bấm Đồng ý/Từ chối Offer
        // (isHrAction = false) thì KHÔNG gửi email tự động nữa để tránh spam.
        // Chỉ giữ gửi email cho các hành động từ phía HR/Admin.
        if (saveResult && isHrAction && (newStatus == "HIRED" || newStatus == "REJECTED"))
        {
            try
            {
                _logger.LogInformation("📧 Bắt đầu gửi email thông báo cho ứng viên. Status: {Status}", newStatus);
                
                var candidateName = application.ContactName ?? application.Candidate?.FullName ?? "Ứng viên";
                var jobTitle = application.Job?.Title ?? "Vị trí ứng tuyển";
                var companyName = application.Job?.CreatedByNavigation?.FullName ?? "Công ty";

                var emailToSend = !string.IsNullOrEmpty(application.ContactEmail) 
                    ? application.ContactEmail 
                    : application.Candidate?.Email;

                if (string.IsNullOrEmpty(emailToSend))
                {
                    _logger.LogWarning(" Không tìm thấy email của ứng viên. Bỏ qua gửi email.");
                }
                else
                {
                    _logger.LogInformation("📧 Email đích: {Email} (Source: {Source})", 
                        emailToSend, 
                        !string.IsNullOrEmpty(application.ContactEmail) ? "ContactEmail (Snapshot)" : "Candidate.Email (Fallback)");

                    // Bước 1: Tạo nội dung email bằng AI
                    var emailBody = await _aiMatchingService.GenerateEmailContentAsync(
                        candidateName, 
                        jobTitle, 
                        newStatus, 
                        companyName
                    );

                    // Bước 2: Tạo tiêu đề email
                    var emailSubject = newStatus == "HIRED"
                        ? $"Chúc mừng! Bạn đã trúng tuyển vị trí {jobTitle}"
                        : $"Thông báo kết quả ứng tuyển vị trí {jobTitle}";

                    // Bước 3: Gửi email
                    await _emailService.SendEmailAsync(emailToSend, emailSubject, emailBody);
                    
                    _logger.LogInformation(" Đã gửi email thông báo thành công đến: {Email}", emailToSend);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, " Không thể gửi email thông báo, nhưng status đã được cập nhật thành công.");
            }
        }

        // 8. TẠO THÔNG BÁO CHO ỨNG VIÊN (NEW)
        if (saveResult && application.Candidate != null && application.Candidate.UserId.HasValue)
        {
            try
            {
                string notifTitle = "";
                string notifMessage = "";
                string notifType = "APPLICATION_UPDATE";

                switch (newStatus)
                {
                    case "HIRED":
                        notifTitle = "Chúc mừng! Bạn đã trúng tuyển";
                        notifMessage = $"Chúc mừng bạn đã trúng tuyển vị trí {application.Job?.Title}. Vui lòng kiểm tra email để biết thêm chi tiết.";
                        notifType = "OFFER";
                        break;
                    case "OFFER_ACCEPTED":
                        notifTitle = "Đã ghi nhận đồng ý Offer";
                        notifMessage = $"Bạn đã đồng ý Offer cho vị trí {application.Job?.Title}. Bộ phận HR sẽ xác nhận bước nhận việc tiếp theo.";
                        notifType = "OFFER";
                        break;
                    case "REJECTED":
                        // Phân biệt HR từ chối vs Candidate từ chối offer
                        if (isHrAction)
                        {
                            notifTitle = "Thông báo kết quả ứng tuyển";
                            notifMessage = $"Cảm ơn bạn đã quan tâm đến vị trí {application.Job?.Title}. Rất tiếc hiện tại hồ sơ của bạn chưa phù hợp.";
                        }
                        else
                        {
                            notifTitle = "Bạn đã từ chối Offer";
                            notifMessage = $"Chúng tôi đã ghi nhận việc bạn từ chối Offer cho vị trí \"{application.Job?.Title}\". Cảm ơn bạn đã dành thời gian, chúc bạn sớm tìm được công việc phù hợp!";
                        }
                        break;
                    case "Pending_Offer":
                        notifTitle = "Cập nhật trạng thái ứng tuyển";
                        notifMessage = $"Bạn đã vượt qua vòng phỏng vấn vị trí {application.Job?.Title}. Chúng tôi đang chuẩn bị Offer cho bạn.";
                        notifType = "OFFER";
                        break;
                    case "Waitlist":
                        notifTitle = "Cập nhật trạng thái ứng tuyển";
                        notifMessage = $"Hồ sơ ứng tuyển vị trí {application.Job?.Title} của bạn đã được đưa vào danh sách chờ.";
                        break;
                    case "INTERVIEW":
                        notifTitle = "Lịch phỏng vấn mới";
                        notifMessage = $"Bạn có lịch phỏng vấn mới cho vị trí {application.Job?.Title}. Vui lòng kiểm tra email.";
                        notifType = "INTERVIEW";
                        break;
                    case "Offer_Sent":
                        notifTitle = "Bạn nhận được Offer!";
                        notifMessage = $"Công ty đã gửi Offer cho vị trí {application.Job?.Title}. Vui lòng kiểm tra email để xác nhận.";
                        notifType = "OFFER";
                        break;
                    default:
                        // Các status khác có thể không cần thông báo hoặc thông báo chung
                        break;
                }

                if (!string.IsNullOrEmpty(notifTitle))
                {
                    await _notificationService.CreateNotificationAsync(
                        application.Candidate.UserId.Value,
                        notifTitle,
                        notifMessage,
                        notifType,
                        application.ApplicationId.ToString()
                    );
                    _logger.LogInformation("🔔 Đã tạo thông báo '{Title}' cho Candidate UserId: {UserId}", notifTitle, application.Candidate.UserId);
                }
            }
            catch (Exception notifEx)
            {
                _logger.LogError(notifEx, " Lỗi khi tạo thông báo cho User {UserId}", application.Candidate.UserId);
            }
        }

        // 9. THÔNG BÁO NGƯỢC LẠI CHO HR - CHỈ KHI CANDIDATE TỰ PHẢN HỒI OFFER (không gửi khi HR tự reject)
        if (saveResult && !isHrAction && (newStatus == "OFFER_ACCEPTED" || newStatus == "REJECTED"))
        {
            _logger.LogInformation("[DEBUG] Block 9 triggered (candidate action). oldStatus={Old}, newStatus={New}", oldStatus, newStatus);
            try
            {
                var hrUser = application.Job?.CreatedByNavigation;

                if (hrUser != null)
                {
                    var candidateName = application.ContactName ?? application.Candidate?.FullName ?? "Ứng viên";
                    var jobTitle = application.Job?.Title ?? "vị trí ứng tuyển";
                    string hrNotifTitle, hrNotifMessage;

                    if (newStatus == "OFFER_ACCEPTED")
                    {
                        hrNotifTitle = $"{candidateName} đã đồng ý nhận việc";
                        hrNotifMessage = $"{candidateName} đã CHẤP NHẬN offer cho vị trí \"{jobTitle}\". Vui lòng xác nhận nhận việc để hoàn tất tuyển dụng.";
                    }
                    else
                    {
                        hrNotifTitle = $"{candidateName} đã từ chối offer";
                        hrNotifMessage = $"{candidateName} đã TỪ CHỐI offer cho vị trí \"{jobTitle}\". Bạn có thể xem xét ứng viên khác.";
                    }

                    await _notificationService.CreateNotificationAsync(
                        hrUser.UserId,
                        hrNotifTitle,
                        hrNotifMessage,
                        "OFFER",
                        application.ApplicationId.ToString()
                    );
                    _logger.LogInformation("[SUCCESS] Đã thông báo cho HR về phản hồi của candidate. HR UserId: {UserId}", hrUser.UserId);
                }
            }
            catch (Exception hrNotifEx)
            {
                _logger.LogError(hrNotifEx, "[ERROR] Không thể gửi thông báo cho HR.");
            }
        }

        // --- Thêm logic đếm số lượng Hired cho Job ---
        int totalHired = 0;
        if (application.Job != null)
        {
            totalHired = await _context.Applications
                .CountAsync(a => a.JobId == application.JobId && a.Status == "HIRED");
        }

        // --- Thông báo và TỰ ĐỘNG ĐÓNG JOB khi đã tuyển đủ vị trí ---
        if (newStatus == "HIRED"
            && application.Job != null
            && application.Job.NumberOfPositions.HasValue
            && totalHired >= application.Job.NumberOfPositions.Value
            && (application.Job.Status == "OPEN" || application.Job.Status == "PUBLISHED"))
        {
            try
            {
                // Soft mode: đưa các hồ sơ còn active vào Waitlist để HR duyệt batch reject sau.
                var remainingActiveApplications = await _context.Applications
                    .Include(a => a.Candidate)
                    .Where(a => a.JobId == application.JobId
                        && a.ApplicationId != application.ApplicationId
                        && a.Status != "HIRED"
                        && a.Status != "REJECTED"
                        && a.Status != "Waitlist")
                    .ToListAsync();

                foreach (var otherApp in remainingActiveApplications)
                {
                    otherApp.Status = "Waitlist";
                    otherApp.LastStageChangedAt = DateTime.UtcNow;
                }

                // Tự động đóng Job
                application.Job.Status = "CLOSED";
                application.Job.ClosedAt = DateTime.UtcNow;
                application.Job.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation(" Đã TỰ ĐỘNG ĐÓNG Job {JobId} vì đã tuyển đủ {Hired}/{Pos}", application.JobId, totalHired, application.Job.NumberOfPositions.Value);
                _logger.LogInformation(" Đã chuyển {Count} hồ sơ còn lại sang Waitlist cho Job {JobId}", remainingActiveApplications.Count, application.JobId);

                // Thông báo cho các ứng viên bị đưa vào waitlist
                foreach (var otherApp in remainingActiveApplications)
                {
                    if (otherApp.Candidate?.UserId != null)
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                otherApp.Candidate.UserId.Value,
                                "Hồ sơ của bạn đang ở danh sách chờ",
                                $"Vị trí {application.Job.Title} đã tuyển đủ chỉ tiêu hiện tại. Hồ sơ của bạn được chuyển vào danh sách chờ để ưu tiên khi có nhu cầu tiếp theo.",
                                "APPLICATION_UPDATE",
                                otherApp.ApplicationId.ToString()
                            );
                        }
                        catch (Exception candidateNotifEx)
                        {
                            _logger.LogWarning(candidateNotifEx, " Không thể gửi thông báo waitlist cho ApplicationId {ApplicationId}", otherApp.ApplicationId);
                        }
                    }
                }

                var hrUser = application.Job.CreatedByNavigation;
                if (hrUser?.UserId != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        hrUser.UserId,
                        $"Đã tự động đóng tin tuyển dụng \"{application.Job.Title}\"",
                        $"Job \"{application.Job.Title}\" đã tuyển đủ {totalHired}/{application.Job.NumberOfPositions.Value} vị trí, hệ thống đã đóng job và chuyển {remainingActiveApplications.Count} hồ sơ còn lại sang Waitlist để bạn duyệt tiếp.",
                        "APPLICATION_UPDATE",
                        application.JobId.ToString()
                    );
                    _logger.LogInformation("🔔 Đã thông báo cho HR về job đủ vị trí (auto-closed): {JobId}", application.JobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, " Lỗi khi tự động đóng job hoặc gửi thông báo cho HR.");
            }
        }

        return new UpdateApplicationStatusResponse
        {
            Success = true,
            JobId = application.JobId,
            TotalHired = totalHired,
            NumberOfPositions = application.Job?.NumberOfPositions,
            IsJobActive = application.Job?.Status == "PUBLISHED"
        };
    }

    public async Task<List<MyApplicationDto>> GetMyApplicationsAsync(Guid userId)
    {
        _logger.LogInformation(" GetMyApplicationsAsync - UserId: {UserId}", userId);
        
        // TÌM CANDIDATE TRỰC TIẾP QUA UserId (KHÔNG QUA EMAIL NỮA!)
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId);
        
        if (candidate == null)
        {
            _logger.LogWarning(" Candidate NOT FOUND for UserId: {UserId}", userId);
            _logger.LogInformation(" User chưa apply job nào hoặc Candidate chưa được link với tài khoản này");
            return new List<MyApplicationDto>();
        }
        
        _logger.LogInformation(" Found Candidate: CandidateId={CandidateId}, FullName={FullName}", 
            candidate.CandidateId, candidate.FullName);

        // Lấy danh sách Applications của Candidate
        _logger.LogInformation(" Looking for Applications for CandidateId: {CandidateId}", candidate.CandidateId);
        
        var applications = await _context.Applications
            .AsNoTracking()
            .Include(a => a.Job)
                .ThenInclude(j => j.CreatedByNavigation)
            .Include(a => a.ResumeDocument)
                .ThenInclude(rd => rd.File)
            .Where(a => a.CandidateId == candidate.CandidateId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new MyApplicationDto
            {
                ApplicationId = a.ApplicationId,
                JobId = a.JobId,
                JobTitle = a.Job.Title,
                CompanyName = a.Job.CreatedByNavigation != null ? a.Job.CreatedByNavigation.FullName : "Unknown",
                JobLocation = a.Job.Location,
                AppliedAt = a.AppliedAt,
                Status = a.Status,
                LastViewedAt = a.LastViewedAt,
                CvUrl = a.ResumeDocument != null && a.ResumeDocument.File != null 
                    ? a.ResumeDocument.File.Url 
                    : null
            })
            .ToListAsync();

        _logger.LogInformation(" Found {Count} applications for CandidateId: {CandidateId}", 
            applications.Count, candidate.CandidateId);

        return applications;
    }

    public async Task<bool> TrackViewAsync(Guid applicationId, Guid viewerId)
    {
        try
        {
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null) return false;

            // 1. Tạo bản ghi Log chi tiết
            var view = new ApplicationView
            {
                ViewId = Guid.NewGuid(),
                ApplicationId = applicationId,
                ViewerId = viewerId,
                ViewedAt = DateTime.UtcNow
            };
            _context.ApplicationViews.Add(view);

            // 2. Cập nhật Cache field trong bảng Applications (đã thêm vào SQL migration)
            application.LastViewedAt = DateTime.UtcNow;
            application.LastViewedBy = viewerId;

            await _context.SaveChangesAsync();

            // 3. Thông báo cho ứng viên
            if (application.Candidate != null && application.Candidate.UserId.HasValue)
            {
                var viewer = await _context.Users.FindAsync(viewerId);
                var viewerName = viewer?.FullName ?? "Nhà tuyển dụng";

                await _notificationService.CreateNotificationAsync(
                    application.Candidate.UserId.Value,
                    "Hồ sơ đã được xem",
                    $"{viewerName} đã xem hồ sơ của bạn cho vị trí {application.Job?.Title}.",
                    "CV_VIEWED",
                    applicationId.ToString()
                );
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhận lượt xem CV cho Application: {Id}", applicationId);
            return false;
        }
    }

    public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
    {
        // Similar to GetApplicationsByJobIdAsync but WITHOUT jobId filter
        var applications = await _context.Applications
            .AsNoTracking()
            .Select(a => new
            {
                ApplicationId = a.ApplicationId,
                CandidateId = a.CandidateId,
                CandidateName = a.ContactName ?? a.Candidate.FullName ?? "Unknown",
                Email = a.ContactEmail ?? a.Candidate.Email ?? "",
                Phone = a.ContactPhone ?? a.Candidate.Phone ?? "",
                AppliedAt = a.AppliedAt,
                LastStageChangedAt = a.LastStageChangedAt,
                Status = a.Status,
                CvUrl = a.ResumeDocument != null && a.ResumeDocument.File != null 
                    ? a.ResumeDocument.File.Url 
                    : (a.Candidate.CandidateDocuments.Where(d => d.DocType == "CV").OrderByDescending(d => d.IsPrimary).Select(d => d.File.Url).FirstOrDefault() ?? ""),
                JobTitle = a.Job.Title,
                JobId = a.JobId,
                CurrentStageCode = a.CurrentStage.Code,
                CurrentStageName = a.CurrentStage.Name,
                CurrentStage = a.CurrentStage,
                LatestScore = a.ApplicationAiScores
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new { s.MatchingScore, s.MatchedSkillsJson })
                    .FirstOrDefault()
            })
            .OrderByDescending(a => a.AppliedAt) 
            .ToListAsync();

        var result = applications.Select(a =>
        {
            string explanation = null;
            var sla = CalculateSlaSnapshot(a.LastStageChangedAt, a.CurrentStage, a.Status);
            if (a.LatestScore != null && !string.IsNullOrEmpty(a.LatestScore.MatchedSkillsJson))
            {
                try
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(a.LatestScore.MatchedSkillsJson))
                    {
                        if (doc.RootElement.TryGetProperty("explanation", out var expElement))
                        {
                            explanation = expElement.GetString();
                        }
                    }
                }
                catch {
                }
            }

            return new ApplicationDto
            {
                ApplicationId = a.ApplicationId,
                CandidateId = a.CandidateId,
                CandidateName = a.CandidateName,
                Email = a.Email,
                Phone = a.Phone,
                AppliedAt = a.AppliedAt,
                Status = a.Status,
                CvUrl = a.CvUrl,
                JobTitle = a.JobTitle,
                JobId = a.JobId,
                MatchScore = (int?)a.LatestScore?.MatchingScore,
                AiExplanation = explanation,
                CurrentStageCode = a.CurrentStageCode,
                CurrentStageName = a.CurrentStageName,
                SlaDueAt = sla.DueAt,
                SlaStatus = sla.Status,
                SlaOverdueDays = sla.OverdueDays,
                SlaMaxDays = sla.MaxDays,
                SlaWarnBeforeDays = sla.WarnBeforeDays
            };
        }).ToList();

        return result;
    }

    public async Task<List<SlaStageConfigDto>> GetSlaStageConfigsAsync()
    {
        return await _context.PipelineStages
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SlaStageConfigDto
            {
                StageId = s.StageId,
                Code = s.Code,
                Name = s.Name,
                SortOrder = s.SortOrder,
                IsTerminal = s.IsTerminal,
                IsSlaEnabled = s.IsSlaEnabled == true,
                SlaMaxDays = s.SlaMaxDays,
                SlaWarnBeforeDays = s.SlaWarnBeforeDays
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateSlaStageConfigAsync(Guid stageId, UpdateSlaStageConfigRequest request)
    {
        var stage = await _context.PipelineStages.FirstOrDefaultAsync(s => s.StageId == stageId);
        if (stage == null)
        {
            return false;
        }

        if (request.IsSlaEnabled)
        {
            if (!request.SlaMaxDays.HasValue || request.SlaMaxDays.Value <= 0)
            {
                throw new ArgumentException("SlaMaxDays phải lớn hơn 0 khi bật SLA.");
            }

            if (request.SlaWarnBeforeDays.HasValue && request.SlaWarnBeforeDays.Value < 0)
            {
                throw new ArgumentException("SlaWarnBeforeDays không được âm.");
            }
        }

        stage.IsSlaEnabled = request.IsSlaEnabled;
        stage.SlaMaxDays = request.SlaMaxDays;
        stage.SlaWarnBeforeDays = request.SlaWarnBeforeDays;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<SlaDashboardDto> GetSlaDashboardAsync(Guid? recruiterUserId = null)
    {
        const int severeOverdueThresholdDays = 3;

        double CalculateComplianceRate(int onTrackCount, int totalCount)
        {
            if (totalCount <= 0)
            {
                return 0;
            }

            return Math.Round((double)onTrackCount * 100 / totalCount, 2);
        }

        double CalculateHealthScore(int totalCount, int overdueCount, int warningCount, int severeOverdueCount)
        {
            if (totalCount <= 0)
            {
                return 100;
            }

            var overdueRate = (double)overdueCount / totalCount;
            var warningRate = (double)warningCount / totalCount;
            var severeRate = (double)severeOverdueCount / totalCount;

            var score = 100 - overdueRate * 70 - warningRate * 20 - severeRate * 10;
            return Math.Round(Math.Clamp(score, 0, 100), 2);
        }

        string CalculateRiskLevel(double healthScore)
        {
            if (healthScore >= 85)
            {
                return "LOW";
            }

            if (healthScore >= 70)
            {
                return "MEDIUM";
            }

            return "HIGH";
        }

        var baseQuery = _context.Applications
            .AsNoTracking()
            .Where(a => a.Status != "HIRED" && a.Status != "REJECTED")
            .Where(a => a.CurrentStage != null)
            .Select(a => new
            {
                a.ApplicationId,
                a.Status,
                a.LastStageChangedAt,
                RecruiterId = a.Job != null ? a.Job.CreatedBy : (Guid?)null,
                RecruiterName = a.Job != null && a.Job.CreatedByNavigation != null
                    ? a.Job.CreatedByNavigation.FullName
                    : "Unassigned",
                CandidateName = a.Candidate != null ? a.Candidate.FullName : "Unknown",
                JobTitle = a.Job != null ? a.Job.Title : "Unknown",
                StageName = a.CurrentStage != null ? a.CurrentStage.Name : "Unknown",
                StageIsSlaEnabled = a.CurrentStage != null ? a.CurrentStage.IsSlaEnabled : null,
                StageSlaMaxDays = a.CurrentStage != null ? a.CurrentStage.SlaMaxDays : null,
                StageSlaWarnBeforeDays = a.CurrentStage != null ? a.CurrentStage.SlaWarnBeforeDays : null,
            });

        if (recruiterUserId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.RecruiterId == recruiterUserId.Value);
        }

        var rawApps = await baseQuery.ToListAsync();

        var tracked = rawApps
            .Select(a =>
            {
                var stage = new PipelineStage
                {
                    Name = a.StageName,
                    IsSlaEnabled = a.StageIsSlaEnabled,
                    SlaMaxDays = a.StageSlaMaxDays,
                    SlaWarnBeforeDays = a.StageSlaWarnBeforeDays
                };

                return new
                {
                    a.ApplicationId,
                    a.Status,
                    a.LastStageChangedAt,
                    a.RecruiterId,
                    a.RecruiterName,
                    a.CandidateName,
                    a.JobTitle,
                    StageName = a.StageName,
                    Sla = CalculateSlaSnapshot(a.LastStageChangedAt, stage, a.Status)
                };
            })
            .Where(x => x.Sla.Status != "DISABLED")
            .ToList();

        var recruiterStats = tracked
            .GroupBy(x => new { x.RecruiterId, x.RecruiterName })
            .Select(g =>
            {
                var overdue = g.Where(x => x.Sla.Status == "OVERDUE").ToList();
                var warning = g.Count(x => x.Sla.Status == "WARNING");
                var onTrack = g.Count(x => x.Sla.Status == "ON_TRACK");
                var severeOverdue = overdue.Count(x => (x.Sla.OverdueDays ?? 0) >= severeOverdueThresholdDays);
                var avgOverdue = overdue.Count > 0 ? overdue.Average(x => x.Sla.OverdueDays ?? 0) : 0;
                var maxOverdue = overdue.Count > 0 ? overdue.Max(x => x.Sla.OverdueDays ?? 0) : 0;
                var total = g.Count();
                var complianceRate = CalculateComplianceRate(onTrack, total);
                var healthScore = CalculateHealthScore(total, overdue.Count, warning, severeOverdue);

                return new SlaRecruiterBottleneckDto
                {
                    RecruiterId = g.Key.RecruiterId,
                    RecruiterName = g.Key.RecruiterName,
                    TotalApplications = total,
                    OnTrackApplications = onTrack,
                    OverdueApplications = overdue.Count,
                    WarningApplications = warning,
                    SevereOverdueApplications = severeOverdue,
                    ComplianceRate = complianceRate,
                    SlaHealthScore = healthScore,
                    RiskLevel = CalculateRiskLevel(healthScore),
                    AvgOverdueDays = Math.Round(avgOverdue, 2),
                    MaxOverdueDays = maxOverdue
                };
            })
            .OrderBy(x => x.SlaHealthScore)
            .ThenByDescending(x => x.OverdueApplications)
            .ThenByDescending(x => x.WarningApplications)
            .ThenByDescending(x => x.MaxOverdueDays)
            .ToList();

        var stageStats = tracked
            .GroupBy(x => x.StageName)
            .Select(g =>
            {
                var overdue = g.Where(x => x.Sla.Status == "OVERDUE").ToList();
                var warning = g.Count(x => x.Sla.Status == "WARNING");
                var onTrack = g.Count(x => x.Sla.Status == "ON_TRACK");
                var severeOverdue = overdue.Count(x => (x.Sla.OverdueDays ?? 0) >= severeOverdueThresholdDays);
                var avgOverdue = overdue.Count > 0 ? overdue.Average(x => x.Sla.OverdueDays ?? 0) : 0;
                var maxOverdue = overdue.Count > 0 ? overdue.Max(x => x.Sla.OverdueDays ?? 0) : 0;
                var total = g.Count();
                var complianceRate = CalculateComplianceRate(onTrack, total);
                var healthScore = CalculateHealthScore(total, overdue.Count, warning, severeOverdue);

                return new SlaStageBottleneckDto
                {
                    StageName = g.Key,
                    TotalApplications = total,
                    OnTrackApplications = onTrack,
                    OverdueApplications = overdue.Count,
                    WarningApplications = warning,
                    SevereOverdueApplications = severeOverdue,
                    ComplianceRate = complianceRate,
                    RiskLevel = CalculateRiskLevel(healthScore),
                    AvgOverdueDays = Math.Round(avgOverdue, 2),
                    MaxOverdueDays = maxOverdue
                };
            })
            .OrderByDescending(x => x.OverdueApplications)
            .ThenByDescending(x => x.WarningApplications)
            .ThenByDescending(x => x.MaxOverdueDays)
            .ToList();

        var topStuck = tracked
            .Where(x => x.Sla.Status == "OVERDUE")
            .OrderByDescending(x => x.Sla.OverdueDays)
            .Take(10)
            .Select(x => new SlaStuckApplicationDto
            {
                ApplicationId = x.ApplicationId,
                CandidateName = x.CandidateName,
                JobTitle = x.JobTitle,
                StageName = x.StageName,
                RecruiterName = x.RecruiterName,
                EnteredStageAt = x.LastStageChangedAt,
                DueAt = x.Sla.DueAt ?? x.LastStageChangedAt,
                OverdueDays = x.Sla.OverdueDays ?? 0
            })
            .ToList();

        var totalTracked = tracked.Count;
        var overdueCount = tracked.Count(x => x.Sla.Status == "OVERDUE");
        var warningCount = tracked.Count(x => x.Sla.Status == "WARNING");
        var onTrackCount = tracked.Count(x => x.Sla.Status == "ON_TRACK");
        var severeOverdueCount = tracked.Count(x => x.Sla.Status == "OVERDUE" && (x.Sla.OverdueDays ?? 0) >= severeOverdueThresholdDays);

        return new SlaDashboardDto
        {
            TotalTrackedApplications = totalTracked,
            OnTrackApplications = onTrackCount,
            OverdueApplications = overdueCount,
            WarningApplications = warningCount,
            SevereOverdueApplications = severeOverdueCount,
            ComplianceRate = CalculateComplianceRate(onTrackCount, totalTracked),
            SlaHealthScore = CalculateHealthScore(totalTracked, overdueCount, warningCount, severeOverdueCount),
            Recruiters = recruiterStats,
            Stages = stageStats,
            TopStuckApplications = topStuck
        };
    }

    /// <summary>
    /// Background task: Chấm điểm CV bằng AI không block main response
    /// Chạy song song, không đợi hoàn thành để trả response cho client
    /// </summary>
    private async Task ScoreApplicationInBackgroundAsync(
        Guid applicationId,
        string savedFilePath,
        string jobTitle,
        string jobDescription,
        string jobRequirements)
    {
        try
        {
            _logger.LogInformation("🔄 [BACKGROUND] Bắt đầu chấm điểm CV bằng AI cho Application: {ApplicationId}", applicationId);

            // ★ Tạo scope mới để sử dụng DbContext và services trong background thread
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedDbContext = scope.ServiceProvider.GetRequiredService<UTC_DATNContext>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationService>>();
                var scopedAiService = scope.ServiceProvider.GetRequiredService<IAiMatchingService>();

                // Kiểm tra file extension - Chỉ hỗ trợ PDF
                var cvExt = Path.GetExtension(savedFilePath).ToLowerInvariant();
                if (cvExt != ".pdf")
                {
                    scopedLogger.LogWarning("🔄 [BACKGROUND] File không phải PDF ({Extension}), bỏ qua AI scoring. Chỉ hỗ trợ file PDF.", cvExt);
                    return;
                }

                // 1. Lấy Job Description đầy đủ (gộp Title + Description + Requirements)
                var jobContext = new System.Text.StringBuilder();
                jobContext.AppendLine($"Job Title: {jobTitle}");
                jobContext.AppendLine("Job Description:");
                jobContext.AppendLine(jobDescription ?? "");
                jobContext.AppendLine("Job Requirements:");
                jobContext.AppendLine(jobRequirements ?? "");

                var fullJobDescription = jobContext.ToString();

                if (string.IsNullOrWhiteSpace(fullJobDescription.Replace("Job Title: " + jobTitle, "").Trim()))
                {
                    scopedLogger.LogWarning("🔄 [BACKGROUND] Job không có Description/Requirements, bỏ qua AI scoring");
                    return;
                }

                // 2. Gọi AI để chấm điểm
                scopedLogger.LogInformation("🔄 [BACKGROUND] Gửi CV trực tiếp cho AI: {FilePath}", savedFilePath);
                var aiScore = await scopedAiService.ScoreApplicationAsync(savedFilePath, fullJobDescription);

                // 3. Lưu kết quả vào database
                var applicationAiScore = new ApplicationAiScore
                {
                    AiScoreId = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    MatchingScore = aiScore.Score,
                    MatchedSkillsJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        matchedSkills = aiScore.MatchedSkills,
                        missingSkills = aiScore.MissingSkills,
                        explanation = aiScore.Explanation
                    }),
                    Model = "gemini-2.5-flash",
                    CreatedAt = DateTime.UtcNow
                };

                scopedDbContext.ApplicationAiScores.Add(applicationAiScore);
                await scopedDbContext.SaveChangesAsync();

                scopedLogger.LogInformation("✅ [BACKGROUND] Đã lưu AI Score: {Score}/100 cho Application: {ApplicationId}", 
                    aiScore.Score, applicationId);
            }
        }
        catch (Exception aiEx)
        {
            _logger.LogWarning(aiEx, "⚠️ [BACKGROUND] Lỗi khi chấm điểm CV bằng AI - Application: {ApplicationId}, tiếp tục xử lý", applicationId);
            // Không throw, để không làm crash background task
        }
    }
}

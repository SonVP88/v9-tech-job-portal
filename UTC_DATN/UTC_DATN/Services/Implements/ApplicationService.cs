using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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
    private readonly IServiceProvider _serviceProvider;

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
        ["Waitlist"] = "WAITLIST",
        ["REJECTED"] = "REJECTED"
    };

    private static string ResolveCandidateDisplayName(Entities.Application application)
    {
        if (!string.IsNullOrWhiteSpace(application.ContactName))
        {
            return application.ContactName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(application.Candidate?.FullName))
        {
            return application.Candidate.FullName.Trim();
        }

        return "Ứng viên";
    }

    private IQueryable<ApplicationDto> ProjectToApplicationDto(IQueryable<Entities.Application> query)
    {
        return query.Select(a => new ApplicationDto
        {
            ApplicationId = a.ApplicationId,
            JobId = a.JobId,
            JobTitle = a.Job.Title,
            CandidateId = a.CandidateId,
            Status = a.Status,
            AppliedAt = a.AppliedAt,
            CvUrl = a.ResumeDocument.File.Url ?? "",
            CandidateName = a.ContactName ?? a.Candidate.FullName,
            Email = a.ContactEmail ?? a.Candidate.Email,
            Phone = a.ContactPhone ?? a.Candidate.Phone,
            MatchScore = a.ApplicationAiScores.OrderByDescending(x => x.CreatedAt).FirstOrDefault() != null 
                ? (int?)a.ApplicationAiScores.OrderByDescending(x => x.CreatedAt).FirstOrDefault().MatchingScore 
                : null,
            AiExplanation = a.ApplicationAiScores.OrderByDescending(x => x.CreatedAt).FirstOrDefault() != null && a.ApplicationAiScores.OrderByDescending(x => x.CreatedAt).FirstOrDefault().MatchedSkillsJson != null 
                ? "AI đã phân tích kỹ năng ứng viên" 
                : null,
            CurrentStageCode = a.CurrentStage != null ? a.CurrentStage.Code : null,
            CurrentStageName = a.CurrentStage != null ? a.CurrentStage.Name : null,
            SlaMaxDays = a.CurrentStage != null ? a.CurrentStage.SlaMaxDays : null,
            SlaWarnBeforeDays = a.CurrentStage != null ? a.CurrentStage.SlaWarnBeforeDays : null,
            SlaDueAt = (a.CurrentStage != null && a.CurrentStage.SlaMaxDays.HasValue) ? a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays.Value) : null,
            SlaOverdueDays = (a.CurrentStage != null && a.CurrentStage.SlaMaxDays.HasValue && DateTime.UtcNow > a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays.Value)) 
                ? (int)(DateTime.UtcNow - a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays.Value)).TotalDays : 0,
            SlaStatus = (a.CurrentStage == null || !a.CurrentStage.SlaMaxDays.HasValue) ? "DISABLED" : 
                (DateTime.UtcNow > a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays.Value) ? "OVERDUE" : 
                (a.CurrentStage.SlaWarnBeforeDays.HasValue && DateTime.UtcNow > a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays.Value - a.CurrentStage.SlaWarnBeforeDays.Value) ? "WARNING" : "ON_TRACK")),
            JobStatus = a.Job.Status,
            JobNumberOfPositions = a.Job.NumberOfPositions,
            JobTotalHired = a.Job.Applications.Count(app => app.Status == "HIRED")
        });
    }

    /// <summary>
    /// ✅ NEW: Gửi email async (background task) - không block response
    /// </summary>
    private void QueueRejectionEmailInBackground(string toEmail, string subject, string candidateName, string jobTitle, string status, string companyName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedAiService = scope.ServiceProvider.GetRequiredService<IAiMatchingService>();
                var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Tạo nội dung email bằng AI
                var emailBody = await scopedAiService.GenerateEmailContentAsync(candidateName, jobTitle, status, companyName);
                
                // Gửi email
                await scopedEmailService.SendEmailAsync(toEmail, subject, emailBody);
                
                _logger.LogInformation("✅ Đã gửi email background thành công đến: {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Lỗi khi gửi email background đến: {Email}", toEmail);
            }
        });
    }

    public ApplicationService(
        UTC_DATNContext context,
        IWebHostEnvironment environment,
        ILogger<ApplicationService> logger,
        IAiMatchingService aiMatchingService,
        IEmailService emailService,
        INotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _aiMatchingService = aiMatchingService;
        _emailService = emailService;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> ApplyJobAsync(ApplyJobRequest request, Guid? userId)
    {
        string? savedFilePath = null;
        string? fileSha256 = null;

        try
        {
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
            var newFileName = $"{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
            var uploadsFolderPath = Path.Combine(_environment.WebRootPath ?? "/app/wwwroot", "uploads", "cvs");
            Directory.CreateDirectory(uploadsFolderPath);
            savedFilePath = Path.Combine(uploadsFolderPath, newFileName);

            // Tính SHA256 của file
            using (var stream = request.CVFile.OpenReadStream())
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = await sha256.ComputeHashAsync(stream);
                    fileSha256 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

            // Lưu file vào disk
            using (var stream = new FileStream(savedFilePath, FileMode.Create, FileAccess.Write))
            {
                await request.CVFile.CopyToAsync(stream);
            }

            _logger.LogInformation("✅ Lưu file CV thành công: {FileName}, SHA256: {SHA256}", newFileName, fileSha256);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // === BƯỚC 2: QUẢN LÝ CANDIDATE PROFILE ===
                    Entities.Candidate candidate;

                    if (userId.HasValue)
                    {
                        // User đã đăng nhập - Update hồ sơ từ token
                        var user = await _context.Users.FindAsync(userId.Value);
                        if (user == null)
                        {
                            throw new ArgumentException("User không tồn tại");
                        }

                        candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.UserId == userId.Value)
                            ?? throw new ArgumentException("Candidate profile không tồn tại");

                        // Kh�ng c?p nh?t th�ng tin h? so ch�nh c?a ?ng vi�n khi n?p don
                        // candidate.FullName = request.FullName?.Trim() ?? candidate.FullName;
                        // candidate.Email = request.Email?.Trim() ?? candidate.Email;
                        // candidate.Phone = request.Phone?.Trim() ?? candidate.Phone;
                        // candidate.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Guest apply - Tạo candidate mới (không liên kết user)
                        candidate = new Entities.Candidate
                        {
                            CandidateId = Guid.NewGuid(),
                            UserId = null,
                            FullName = request.FullName?.Trim() ?? "Guest",
                            Email = request.Email?.Trim() ?? "",
                            Phone = request.Phone?.Trim() ?? "",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Candidates.Add(candidate);
                        _logger.LogInformation("👤 Tạo Candidate mới cho guest apply: {CandidateId}", candidate.CandidateId);
                    }

                    await _context.SaveChangesAsync();

                    // === BƯỚC 3: TẠO APPLICATION ===
                    
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

                    // === BƯỚC 4: REUSE CANDIDATEDOCUMENT nếu CV trùng hash ===
                    var existingSameCvDocument = await _context.CandidateDocuments
                        .Include(cd => cd.File)
                        .Where(cd => cd.CandidateId == candidate.CandidateId
                            && cd.DocType == "CV"
                            && cd.File != null
                            && cd.File.Sha256 == fileSha256)
                        .OrderByDescending(cd => cd.IsPrimary)
                        .ThenByDescending(cd => cd.CreatedAt)
                        .FirstOrDefaultAsync();

                    CandidateDocument candidateDocument;

                    if (existingSameCvDocument != null)
                    {
                        candidateDocument = existingSameCvDocument;
                        _logger.LogInformation("♻️ Reuse CandidateDocument {DocumentId}", candidateDocument.CandidateDocumentId);

                        // Xóa file vật lý vừa upload nếu trùng hoàn toàn
                        if (!string.IsNullOrWhiteSpace(savedFilePath) && System.IO.File.Exists(savedFilePath))
                        {
                            System.IO.File.Delete(savedFilePath);
                            savedFilePath = Path.Combine(uploadsFolderPath, candidateDocument.File.StoredFileName);
                        }
                    }
                    else
                    {
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
                            Sha256 = fileSha256,
                            LocalPath = $"/uploads/cvs/{newFileName}",
                            Url = $"/uploads/cvs/{newFileName}",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Files.Add(fileEntity);
                        await _context.SaveChangesAsync();

                        var hasCv = await _context.CandidateDocuments
                            .AnyAsync(cd => cd.CandidateId == candidate.CandidateId && cd.DocType == "CV");

                        candidateDocument = new CandidateDocument
                        {
                            CandidateDocumentId = Guid.NewGuid(),
                            CandidateId = candidate.CandidateId,
                            FileId = fileEntity.FileId,
                            DocType = "CV",
                            CreatedAt = DateTime.UtcNow,
                            IsPrimary = !hasCv,
                            DisplayName = request.CVFile.FileName
                        };

                        _context.CandidateDocuments.Add(candidateDocument);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Đã tạo CandidateDocument mới: {DocumentId}", candidateDocument.CandidateDocumentId);
                    }

                    // Lấy PipelineStage đầu tiên
                    var firstStage = await _context.PipelineStages
                        .OrderBy(s => s.SortOrder)
                        .FirstOrDefaultAsync();

                    if (firstStage == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy PipelineStage trong hệ thống");
                    }

                    // Tạo Application
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
                    _ = Task.Run(() => ScoreApplicationInBackgroundAsync(
                        application.ApplicationId,
                        savedFilePath,
                        job.Title,
                        job.Description,
                        job.Requirements,
                        application.ContactEmail ?? candidate.Email
                    ));
                    _logger.LogInformation("⚡ Đã gửi AI scoring task vào background");
                    // ===== END AI SCORING =====

                    // ===== GỬI EMAIL XÁC NHẬN CHO CANDIDATE =====
                    var candidateName = ResolveCandidateDisplayName(application);
                    var candidateEmail = application.ContactEmail ?? candidate.Email;
                    
                    if (!string.IsNullOrWhiteSpace(candidateEmail))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                var scopedAiService = scope.ServiceProvider.GetRequiredService<IAiMatchingService>();

                                // Tạo nội dung email xác nhận
                                var confirmationBody = $@"
<h2>Xác nhận nộp hồ sơ</h2>
<p>Xin chào <strong>{candidateName}</strong>,</p>
<p>Cảm ơn bạn đã nộp hồ sơ ứng tuyển vị trí <strong>{job.Title}</strong>.</p>
<p>Chúng tôi sẽ review hồ sơ của bạn sớm nhất và liên hệ với bạn trong thời gian sớm nhất.</p>
<p>Chúc bạn may mắn!</p>
<hr/>
<p><em>Đây là email tự động từ hệ thống tuyển dụng. Vui lòng không reply email này.</em></p>
";
                                await scopedEmailService.SendEmailAsync(
                                    candidateEmail,
                                    $"Xác nhận nộp hồ sơ - Vị trí {job.Title}",
                                    confirmationBody
                                );
                                _logger.LogInformation("✅ Đã gửi email xác nhận cho candidate: {Email}", candidateEmail);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "❌ Lỗi khi gửi email xác nhận cho candidate: {Email}", candidateEmail);
                            }
                        });
                    }
                    // ===== END EMAIL CONFIRMATION =====

                    // Tạo thông báo cho ứng viên
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
                            _logger.LogInformation(" Đã tạo thông báo cho User {UserId}", userId);
                        }
                        catch (Exception notifEx)
                        {
                            _logger.LogError(notifEx, " Lỗi khi tạo thông báo");
                        }
                    }

                    // Tạo thông báo cho HR/Admin
                    try
                    {
                        var adminAndHrUsers = await _context.UserRoles
                            .Include(ur => ur.Role)
                            .Where(ur => ur.Role.Code == "ADMIN" || ur.Role.Code == "HR")
                            .Select(ur => ur.UserId)
                            .Distinct()
                            .ToListAsync();

                        foreach (var adminId in adminAndHrUsers)
                        {
                            var displayName = ResolveCandidateDisplayName(application);

                            await _notificationService.CreateNotificationAsync(
                                adminId,
                                "Có hồ sơ ứng tuyển mới",
                                $"Ứng viên {displayName} vừa nộp hồ sơ vào vị trí {job.Title}.",
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
                    _logger.LogInformation("Hoàn thành nộp hồ sơ thành công");

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi trong transaction");

                    if (!string.IsNullOrEmpty(savedFilePath) && System.IO.File.Exists(savedFilePath))
                    {
                        System.IO.File.Delete(savedFilePath);
                        _logger.LogInformation("Đã xóa file: {FilePath}", savedFilePath);
                    }

                    throw;
                }
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

    private async Task ScoreApplicationInBackgroundAsync(Guid applicationId, string? savedFilePath, string jobTitle, string? jobDescription, string? jobRequirements, string? candidateEmail = null)
    {
        _logger.LogInformation("🔄 [BACKGROUND] Bắt đầu chấm điểm CV bằng AI cho ApplicationId: {Id}", applicationId);

        using var scope = _serviceProvider.CreateScope();
        var scopedDbContext = scope.ServiceProvider.GetRequiredService<UTC_DATNContext>();
        var scopedLogger    = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationService>>();
        var scopedAiService = scope.ServiceProvider.GetRequiredService<IAiMatchingService>();
        var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        if (string.IsNullOrEmpty(savedFilePath))
        {
            scopedLogger.LogWarning("⚠️ Đường dẫn file rỗng, bỏ qua AI scoring");
            return;
        }

        var cvExt = Path.GetExtension(savedFilePath)?.ToLowerInvariant() ?? "";
        if (cvExt != ".pdf")
        {
            scopedLogger.LogWarning("⚠️ File không phải PDF ({Extension}), bỏ qua AI scoring", cvExt);
            return;
        }

        var fullJobDescription = $"Job Title: {jobTitle}\nJob Description:\n{jobDescription}\nJob Requirements:\n{jobRequirements}";
        if (string.IsNullOrWhiteSpace(jobDescription) && string.IsNullOrWhiteSpace(jobRequirements))
        {
            scopedLogger.LogWarning("🔄 [BACKGROUND] Job không có Description/Requirements, bỏ qua");
            return;
        }

        try
        {
            var application = await scopedDbContext.Applications
                .Include(a => a.Job)
                .Include(a => a.Candidate)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
            
            if (application == null)
            {
                scopedLogger.LogWarning("⚠️ Application {ApplicationId} không tồn tại", applicationId);
                return;
            }

            // Gọi AI chấm điểm
            var aiResult = await scopedAiService.ScoreApplicationAsync(savedFilePath, fullJobDescription);

            // Serialize breakdown thành JSON
            string? breakdownJson = null;
            if (aiResult.Breakdown != null)
            {
                breakdownJson = System.Text.Json.JsonSerializer.Serialize(aiResult.Breakdown);
            }

            // Xóa điểm cũ nếu có
            var existingScore = await scopedDbContext.ApplicationAiScores
                .FirstOrDefaultAsync(s => s.ApplicationId == applicationId);
            if (existingScore != null)
                scopedDbContext.ApplicationAiScores.Remove(existingScore);

            // Lưu điểm mới (kèm BreakdownJson)
            var aiScore = new ApplicationAiScore
            {
                AiScoreId      = Guid.NewGuid(),
                ApplicationId  = applicationId,
                MatchingScore  = aiResult.Score,
                MatchedSkillsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    matched = aiResult.MatchedSkills,
                    missing = aiResult.MissingSkills,
                    explanation = aiResult.Explanation
                }),
                BreakdownJson  = breakdownJson,
                Model          = "gemini-2.5-flash",
                CreatedAt      = DateTime.UtcNow
            };

            scopedDbContext.ApplicationAiScores.Add(aiScore);
            await scopedDbContext.SaveChangesAsync();

            scopedLogger.LogInformation("✅ AI scoring hoàn thành. Score={Score} Breakdown={Breakdown}",
                aiResult.Score, breakdownJson);

            // ===== GỬI EMAIL THÔNG BÁO ADMIN KHI AI CHẤM XONG =====
            try
            {
                var candidateName = ResolveCandidateDisplayName(application);
                var scorePercentage = (int)aiResult.Score;
                
                var adminNotificationBody = $@"
<h2>Chấm điểm CV hoàn thành</h2>
<p><strong>Ứng viên:</strong> {candidateName}</p>
<p><strong>Vị trí:</strong> {jobTitle}</p>
<p><strong>Điểm AI Match:</strong> <span style='color: {(scorePercentage >= 70 ? "green" : scorePercentage >= 50 ? "orange" : "red")}'>{scorePercentage}%</span></p>
<p><strong>Kỹ năng phù hợp:</strong></p>
<ul>
  {string.Join("\n", aiResult.MatchedSkills.Select(s => $"  <li>{s}</li>"))}
</ul>
<p><strong>Kỹ năng thiếu:</strong></p>
<ul>
  {string.Join("\n", aiResult.MissingSkills.Select(s => $"  <li>{s}</li>"))}
</ul>
<p><strong>Giải thích:</strong></p>
<p>{aiResult.Explanation}</p>
<hr/>
<p><em>Vui lòng đăng nhập vào hệ thống để xem chi tiết đầy đủ.</em></p>
";

                // Lấy danh sách HR/Admin emails
                var adminEmails = await scopedDbContext.UserRoles
                    .Where(ur => (ur.Role.Code == "ADMIN" || ur.Role.Code == "HR") && ur.User.IsActive)
                    .Select(ur => ur.User.Email)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToListAsync();

                foreach (var adminEmail in adminEmails)
                {
                    try
                    {
                        await scopedEmailService.SendEmailAsync(
                            adminEmail!,
                            $"[Chấm điểm CV] {candidateName} - {jobTitle} ({scorePercentage}%)",
                            adminNotificationBody
                        );
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, "❌ Lỗi khi gửi email tới admin: {AdminEmail}", adminEmail);
                    }
                }

                scopedLogger.LogInformation("✅ Đã gửi email thông báo cho {Count} Admin", adminEmails.Count);
            }
            catch (Exception emailEx)
            {
                scopedLogger.LogError(emailEx, "❌ Lỗi khi gửi email thông báo Admin");
            }
            // ===== END EMAIL NOTIFICATION =====
        }
        catch (Exception ex)
        {
            scopedLogger.LogError(ex, "❌ Lỗi AI scoring cho ApplicationId: {Id}", applicationId);
        }
    }


    public async Task<List<ApplicationDto>> GetApplicationsByJobIdAsync(Guid jobId)
    {
        var query = _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Job)
            .Include(a => a.CurrentStage)
            .Include(a => a.ApplicationAiScores)
            .Where(a => a.JobId == jobId && !a.Job.IsDeleted)
            .OrderByDescending(a => a.AppliedAt);

        return await ProjectToApplicationDto(query).ToListAsync();
    }

    public async Task<UpdateApplicationStatusResponse?> UpdateStatusAsync(Guid applicationId, string newStatus, bool isHrAction = true, Guid? actorUserId = null)
    {
        var application = await _context.Applications
            .Include(a => a.Job)
            .Include(a => a.CurrentStage)
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null)
            return null;

        application.Status = newStatus;
        application.LastStageChangedAt = DateTime.UtcNow;

        // Xóa (đánh dấu đã đọc) các thông báo SLA cũ của hồ sơ này
        var slaNotifications = await _context.Notifications
            .Where(n => n.RelatedId == applicationId.ToString() && !n.IsRead && n.Type.StartsWith("SLA_"))
            .ToListAsync();
        foreach (var notification in slaNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        // Send rejection email if status is REJECTED
        if (newStatus.Equals("REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            var subject = $"Kết quả ứng tuyển: {application.Job.Title}";
            QueueRejectionEmailInBackground(
                application.ContactEmail ?? application.Candidate.Email,
                subject,
                application.ContactName ?? application.Candidate.FullName,
                application.Job.Title,
                "REJECTED",
                "V9 Tech"
            );
        }

        // Tạo thông báo cho ứng viên
        if (application.Candidate?.UserId != null)
        {
            var userId = application.Candidate.UserId.Value;
            string title = "Cập nhật trạng thái hồ sơ";
            string message = $"Hồ sơ ứng tuyển vị trí {application.Job.Title} của bạn đã được cập nhật trạng thái mới.";

            switch (newStatus.ToUpper())
            {
                case "HIRED":
                    title = "Chúc mừng bạn trúng tuyển!";
                    message = $"Bạn đã chính thức trúng tuyển vị trí {application.Job.Title}.";
                    break;
                case "REJECTED":
                    title = "Kết quả ứng tuyển";
                    message = $"Cảm ơn bạn đã quan tâm vị trí {application.Job.Title}. Rất tiếc hồ sơ chưa phù hợp ở thời điểm hiện tại.";
                    break;
                case "OFFER_SENT":
                case "PENDING_OFFER":
                    title = "Bạn có Offer mới";
                    message = $"Nhà tuyển dụng vừa gửi Offer cho vị trí {application.Job.Title}. Vui lòng kiểm tra!";
                    break;
                case "OFFER_ACCEPTED":
                    title = "Phản hồi Offer thành công";
                    message = $"Bạn đã đồng ý Offer vị trí {application.Job.Title}. Nhân sự sẽ liên hệ với bạn để trao đổi các bước tiếp theo.";
                    break;
            }

            try
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    title,
                    message,
                    "APPLICATION_UPDATED",
                    application.ApplicationId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo cho Candidate: {UserId}", userId);
            }
        }

        return new UpdateApplicationStatusResponse
        {
            Success = true,
            JobId = application.JobId,
            TotalHired = await _context.Applications.CountAsync(a => a.JobId == application.JobId && a.Status == "HIRED"),
            NumberOfPositions = application.Job.NumberOfPositions,
            IsJobActive = !string.Equals(application.Job.Status, "CLOSED", StringComparison.OrdinalIgnoreCase)
        };
    }

    public async Task<List<MyApplicationDto>> GetMyApplicationsAsync(Guid userId)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (candidate == null)
            return new List<MyApplicationDto>();

        var applications = await _context.Applications
            .Include(a => a.Job)
            .Where(a => a.CandidateId == candidate.CandidateId)
            .Select(a => new MyApplicationDto
            {
                ApplicationId = a.ApplicationId,
                JobTitle = a.Job.Title,
                Status = a.Status,
                AppliedAt = a.AppliedAt
            })
            .ToListAsync();

        return applications;
    }

    public async Task<bool> TrackViewAsync(Guid applicationId, Guid viewerId)
    {
        var application = await _context.Applications.FindAsync(applicationId);
        if (application == null)
            return false;

        // Add tracking logic here
        return true;
    }

    public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
    {
        var query = _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Job)
            .Include(a => a.CurrentStage)
            .Include(a => a.ApplicationAiScores)
            .Where(a => !a.Job.IsDeleted)
            .OrderByDescending(a => a.AppliedAt);

        return await ProjectToApplicationDto(query).ToListAsync();
    }

    public async Task<List<SlaStageConfigDto>> GetSlaStageConfigsAsync()
    {
        var configs = await _context.PipelineStages
            .Select(s => new SlaStageConfigDto
            {
                StageId = s.StageId,
                Code = s.Code,
                Name = s.Name,
                SortOrder = s.SortOrder,
                IsTerminal = s.IsTerminal,
                IsSlaEnabled = (s.SlaMaxDays ?? 0) > 0,
                SlaMaxDays = s.SlaMaxDays,
                SlaWarnBeforeDays = s.SlaWarnBeforeDays
            })
            .ToListAsync();

        return configs;
    }

    public async Task<bool> UpdateSlaStageConfigAsync(Guid stageId, UpdateSlaStageConfigRequest request)
    {
        var stage = await _context.PipelineStages.FindAsync(stageId);
        if (stage == null)
            return false;

        if (request.IsSlaEnabled)
        {
            if (request.SlaMaxDays <= 0)
                throw new ArgumentException("SlaMaxDays phải lớn hơn 0 khi bật SLA.");
            if ((request.SlaWarnBeforeDays ?? 0) < 0)
                throw new ArgumentException("SlaWarnBeforeDays không được âm.");

            stage.SlaMaxDays = request.SlaMaxDays;
            stage.SlaWarnBeforeDays = request.SlaWarnBeforeDays;
        }
        else
        {
            stage.SlaMaxDays = null;
            stage.SlaWarnBeforeDays = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SlaDashboardDto> GetSlaDashboardAsync(Guid? recruiterUserId = null)
    {
        var applications = await _context.Applications
            .Include(a => a.CurrentStage)
            .Include(a => a.Job)
            .Where(a => !a.Job.IsDeleted)
            .ToListAsync();

        var tracked = applications.Where(a => a.CurrentStage?.SlaMaxDays > 0).ToList();
        var overdue = tracked.Where(a => DateTime.UtcNow > a.LastStageChangedAt.AddDays(a.CurrentStage!.SlaMaxDays!.Value)).ToList();
        var warning = tracked.Where(a => !overdue.Contains(a)
            && a.CurrentStage!.SlaWarnBeforeDays.HasValue
            && DateTime.UtcNow > a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays!.Value - a.CurrentStage.SlaWarnBeforeDays.Value)).ToList();
        var onTrack = tracked.Except(overdue).Except(warning).ToList();
        var severe = overdue.Where(a => (DateTime.UtcNow - a.LastStageChangedAt).TotalDays - a.CurrentStage!.SlaMaxDays!.Value >= 7).ToList();

        return new SlaDashboardDto
        {
            TotalTrackedApplications = tracked.Count,
            OnTrackApplications = onTrack.Count,
            OverdueApplications = overdue.Count,
            WarningApplications = warning.Count,
            SevereOverdueApplications = severe.Count,
            ComplianceRate = tracked.Count == 0 ? 100 : Math.Round((double)onTrack.Count * 100 / tracked.Count, 2),
            SlaHealthScore = tracked.Count == 0 ? 100 : Math.Round((double)(onTrack.Count + warning.Count * 0.5) * 100 / tracked.Count, 2),
            Recruiters = new List<SlaRecruiterBottleneckDto>(),
            Stages = new List<SlaStageBottleneckDto>(),
            TopStuckApplications = overdue
                .OrderByDescending(a => (DateTime.UtcNow - a.LastStageChangedAt).TotalDays - a.CurrentStage!.SlaMaxDays!.Value)
                .Take(10)
                .Select(a => new SlaStuckApplicationDto
                {
                    ApplicationId = a.ApplicationId,
                    CandidateName = a.ContactName ?? "Ứng viên",
                    JobTitle = a.Job.Title,
                    StageName = a.CurrentStage!.Name,
                    RecruiterName = string.Empty,
                    EnteredStageAt = a.LastStageChangedAt,
                    DueAt = a.LastStageChangedAt.AddDays(a.CurrentStage.SlaMaxDays!.Value),
                    OverdueDays = Math.Max(0, (int)((DateTime.UtcNow - a.LastStageChangedAt).TotalDays - a.CurrentStage.SlaMaxDays.Value))
                })
                .ToList()
        };
    }
}

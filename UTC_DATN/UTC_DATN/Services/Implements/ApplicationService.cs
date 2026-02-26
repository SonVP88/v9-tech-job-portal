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

    // Các extension được phép
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };
    
    // Kích thước file tối đa: 5MB
    private const long MaxFileSize = 5 * 1024 * 1024;

    public ApplicationService(
        UTC_DATNContext context,
        IWebHostEnvironment environment,
        ILogger<ApplicationService> logger,
        IAiMatchingService aiMatchingService,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _aiMatchingService = aiMatchingService;
        _emailService = emailService;
        _notificationService = notificationService;
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

                // === BƯỚC 3: TÌM/TẠO CANDIDATE (LOGIC MỚI - UserId-based) ===
                Candidate? candidate = null;
                var normalizedEmail = request.Email.Trim().ToUpper();

                // Priority 1: Tìm theo UserId (nếu có)
                if (userId.HasValue)
                {
                    candidate = await _context.Candidates
                        .FirstOrDefaultAsync(c => c.UserId == userId.Value);
                    
                    if (candidate != null)
                    {
                        _logger.LogInformation(" Tìm thấy Candidate qua UserId: {CandidateId}", candidate.CandidateId);
                    }
                }

                // Priority 2: Tìm theo Email (fallback)
                if (candidate == null)
                {
                    candidate = await _context.Candidates
                        .FirstOrDefaultAsync(c => c.NormalizedEmail == normalizedEmail);
                    
                    if (candidate != null)
                    {
                        _logger.LogInformation(" Tìm thấy Candidate qua Email: {CandidateId}", candidate.CandidateId);
                        
                        // Nếu tìm thấy qua Email NHƯNG chưa có UserId -> Link ngay!
                        if (userId.HasValue && candidate.UserId == null)
                        {
                            candidate.UserId = userId.Value;
                            _logger.LogInformation("🔗 Auto-link Candidate {CandidateId} với User {UserId}", 
                                candidate.CandidateId, userId.Value);
                        }
                    }
                }

                // Nếu không tìm thấy -> Tạo mới
                if (candidate == null)
                {
                    candidate = new Candidate
                    {
                        CandidateId = Guid.NewGuid(),
                        Email = request.Email.Trim(),
                        NormalizedEmail = normalizedEmail,
                        FullName = request.FullName,
                        Phone = request.Phone,
                        Summary = request.Introduction,
                        Source = "CAREER_SITE",
                        UserId = userId, // Gán UserId ngay từ đầu!
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _context.Candidates.Add(candidate);
                    _logger.LogInformation("➕ Tạo mới Candidate: {CandidateId} với UserId: {UserId}", 
                        candidate.CandidateId, userId?.ToString() ?? "NULL");
                }
                else
                {
                    // Update thông tin Candidate (nếu đã tồn tại)
                    candidate.FullName = request.FullName;
                    candidate.Phone = request.Phone;
                    candidate.Summary = request.Introduction;
                    candidate.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation(" Cập nhật Candidate: {CandidateId}", candidate.CandidateId);
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
                    // Snapshot thông tin liên lạc tại thời điểm nộp hồ sơ
                    ContactEmail = request.Email?.Trim(),
                    ContactPhone = request.Phone?.Trim()
                };

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã tạo Application: {ApplicationId}", application.ApplicationId);

                // ===== AI SCORING =====
                try
                {
                    _logger.LogInformation("Bắt đầu chấm điểm CV bằng AI cho Application: {ApplicationId}", application.ApplicationId);

                    // Kiểm tra file extension - Chỉ hỗ trợ PDF
                    var cvExt = Path.GetExtension(savedFilePath).ToLowerInvariant();
                    if (cvExt != ".pdf")
                    {
                        _logger.LogWarning("File không phải PDF ({Extension}), bỏ qua AI scoring. Chỉ hỗ trợ file PDF.", cvExt);
                    }
                    else
                    {
                        // 1. Lấy Job Description đầy đủ (gộp Title + Description + Requirements)
                        var jobContext = new System.Text.StringBuilder();
                        jobContext.AppendLine($"Job Title: {job.Title}");
                        jobContext.AppendLine("Job Description:");
                        jobContext.AppendLine(job.Description ?? "");
                        jobContext.AppendLine("Job Requirements:");
                        jobContext.AppendLine(job.Requirements ?? "");

                        var fullJobDescription = jobContext.ToString();
                        
                        if (string.IsNullOrWhiteSpace(fullJobDescription.Replace("Job Title: " + job.Title, "").Trim()))
                        {
                            _logger.LogWarning("Job không có Description/Requirements, bỏ qua AI scoring");
                        }
                        else
                        {
                            // 2. Gọi AI để chấm điểm (Sử dụng Document AI Native với File Path)
                            _logger.LogInformation("Gửi trực tiếp tệp PDF cho AI: {FilePath}", savedFilePath);
                            var aiScore = await _aiMatchingService.ScoreApplicationAsync(savedFilePath, fullJobDescription);
                            
                            // 3. Lưu kết quả vào database
                            var applicationAiScore = new ApplicationAiScore
                            {
                                    AiScoreId = Guid.NewGuid(),
                                    ApplicationId = application.ApplicationId,
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

                                _context.ApplicationAiScores.Add(applicationAiScore);
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation("Đã lưu AI Score: {Score}/100 cho Application: {ApplicationId}", 
                                    aiScore.Score, application.ApplicationId);
                            }
                        }
                    }
                catch (Exception aiEx)
                {
                    _logger.LogWarning(aiEx, "Lỗi khi chấm điểm CV bằng AI, tiếp tục xử lý");
                }
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
                            $"Ứng viên {candidate.FullName} vừa nộp hồ sơ vào vị trí {job.Title}.",
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

                // Commit transaction
                await transaction.CommitAsync();
                _logger.LogInformation("Hoàn thành nộp hồ sơ thành công cho JobId: {JobId}", request.JobId);

                return true;
            }
            catch (Exception ex)
            {
                // Rollback transaction
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
            
            // Xóa file vật lý nếu có lỗi và file đã được lưu
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
        // OPTIMIZED: Use projection instead of Include to load only needed fields
        var applications = await _context.Applications
            .AsNoTracking()
            .Where(a => a.JobId == jobId)
            .Select(a => new
            {
                ApplicationId = a.ApplicationId,
                CandidateId = a.CandidateId,
                CandidateName = a.Candidate.FullName ?? "Unknown",
                Email = a.Candidate.Email ?? "",
                Phone = a.Candidate.Phone ?? "",
                AppliedAt = a.AppliedAt,
                Status = a.Status,
                CvUrl = a.ResumeDocument != null && a.ResumeDocument.File != null 
                    ? a.ResumeDocument.File.Url 
                    : "",
                JobTitle = a.Job.Title,
                JobId = a.JobId,
                // Get latest AI score (avoid loading all scores)
                LatestScore = a.ApplicationAiScores
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new { s.MatchingScore, s.MatchedSkillsJson })
                    .FirstOrDefault()
            })
            .OrderByDescending(a => a.LatestScore != null ? a.LatestScore.MatchingScore : -1)
            .ThenByDescending(a => a.AppliedAt)
            .ToListAsync();

        // Map to DTO
        var result = applications.Select(a =>
        {
            string explanation = null;
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
                    // Ignore JSON parse error
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
                AiExplanation = explanation
            };
        }).ToList();

        return result;
    }

    public async Task<UpdateApplicationStatusResponse?> UpdateStatusAsync(Guid applicationId, string newStatus)
    {
        var application = await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Job)
                .ThenInclude(j => j.CreatedByNavigation)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
            
        if (application == null) return null;

        var oldStatus = application.Status;
        application.Status = newStatus;
        
        var saveResult = await _context.SaveChangesAsync() > 0;

        // Gửi email tự động nếu status là HIRED hoặc REJECTED
        if (saveResult && (newStatus == "HIRED" || newStatus == "REJECTED"))
        {
            try
            {
                _logger.LogInformation("📧 Bắt đầu gửi email thông báo cho ứng viên. Status: {Status}", newStatus);
                
                var candidateName = application.Candidate?.FullName ?? "Ứng viên";
                var jobTitle = application.Job?.Title ?? "Vị trí ứng tuyển";
                var companyName = application.Job?.CreatedByNavigation?.FullName ?? "Công ty";

                // Ưu tiên lấy ContactEmail (snapshot tại thời điểm nộp hồ sơ)
                // Fallback sang Candidate.Email nếu ContactEmail null (hồ sơ cũ)
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
                // Không throw exception nếu gửi email thất bại, chỉ log warning
                // Để không ảnh hưởng đến luồng chính (status đã được cập nhật thành công)
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
                    case "REJECTED":
                        notifTitle = "Thông báo kết quả ứng tuyển";
                        notifMessage = $"Cảm ơn bạn đã quan tâm đến vị trí {application.Job?.Title}. Rất tiếc hiện tại hồ sơ của bạn chưa phù hợp.";
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

        // 9. THÔNG BÁO NGƯỢC LẠI CHO HR khi ứng viên phản hồi Offer (hoặc HR tự update HIRED)
        if (saveResult && (newStatus == "HIRED" || newStatus == "REJECTED"))
        {
            _logger.LogInformation("[DEBUG] Block 9 triggered. oldStatus={Old}, newStatus={New}", oldStatus, newStatus);
            try
            {
                var hrUser = application.Job?.CreatedByNavigation;
                _logger.LogInformation("[DEBUG] hrUser={HR}, hrUserId={Id}",
                    hrUser?.FullName ?? "NULL",
                    hrUser?.UserId.ToString() ?? "NULL");

                if (hrUser != null)
                {
                    var candidateName = application.Candidate?.FullName ?? "Ứng viên";
                    var jobTitle = application.Job?.Title ?? "vị trí ứng tuyển";
                    string hrNotifTitle, hrNotifMessage;

                    if (newStatus == "HIRED")
                    {
                        hrNotifTitle = $"{candidateName} đã đồng ý nhận việc";
                        hrNotifMessage = $"{candidateName} đã CHẤP NHẬN offer cho vị trí \"{jobTitle}\". Vui lòng tiến hành các bước onboarding.";
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
                    _logger.LogInformation("[SUCCESS] Đã gửi thông báo HR UserId: {UserId}", hrUser.UserId);
                }
                else
                {
                    _logger.LogWarning("[WARN] hrUser NULL - Job.CreatedByNavigation không được load hoặc Job null.");
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

        // --- Thông báo cho HR khi đã tuyển đủ vị trí (để HR quyết định có đóng job không) ---
        if (newStatus == "HIRED"
            && application.Job != null
            && application.Job.NumberOfPositions.HasValue
            && totalHired >= application.Job.NumberOfPositions.Value
            && (application.Job.Status == "OPEN" || application.Job.Status == "PUBLISHED"))
        {
            try
            {
                var hrUser = application.Job.CreatedByNavigation;
                if (hrUser?.UserId != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        hrUser.UserId,
                        $"Đã tuyển đủ vị trí cho \"{application.Job.Title}\"",
                        $"Job \"{application.Job.Title}\" đã tuyển đủ {totalHired}/{application.Job.NumberOfPositions.Value} vị trí. Bạn có muốn đóng tin tuyển dụng này không?",
                        "APPLICATION_UPDATE",
                        application.JobId.ToString()
                    );
                    _logger.LogInformation("🔔 Đã thông báo cho HR về job đủ vị trí: {JobId}", application.JobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, " Không thể gửi thông báo đủ vị trí cho HR.");
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
                CvUrl = a.ResumeDocument != null && a.ResumeDocument.File != null 
                    ? a.ResumeDocument.File.Url 
                    : null
            })
            .ToListAsync();

        _logger.LogInformation(" Found {Count} applications for CandidateId: {CandidateId}", 
            applications.Count, candidate.CandidateId);

        return applications;
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
                CandidateName = a.Candidate.FullName ?? "Unknown",
                Email = a.Candidate.Email ?? "",
                Phone = a.Candidate.Phone ?? "",
                AppliedAt = a.AppliedAt,
                Status = a.Status,
                CvUrl = a.ResumeDocument != null && a.ResumeDocument.File != null 
                    ? a.ResumeDocument.File.Url 
                    : "",
                JobTitle = a.Job.Title,
                JobId = a.JobId,
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
                catch { /* Ignore JSON parse error */ }
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
                AiExplanation = explanation
            };
        }).ToList();

        return result;
    }
}

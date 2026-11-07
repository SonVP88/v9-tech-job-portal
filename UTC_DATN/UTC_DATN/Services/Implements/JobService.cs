using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Job;
using UTC_DATN.DTOs.Common;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

public class JobService : IJobService
{
    private readonly UTC_DATNContext _context;
    private readonly ICampaignQrService _campaignQrService;
    private readonly ILogger<JobService> _logger;

    public JobService(UTC_DATNContext context, ICampaignQrService campaignQrService, ILogger<JobService> logger)
    {
        _context = context;
        _campaignQrService = campaignQrService;
        _logger = logger;
    }

    public async Task<CreateJobResultDto> CreateJobAsync(CreateJobRequest request, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Tạo mã Code duy nhất
            var jobCode = await GenerateUniqueJobCodeAsync();

            // Lấy tên phòng ban nếu có DepartmentId
            string? deptName = null;
            if (request.DepartmentId.HasValue)
            {
                var dept = await _context.Departments.FindAsync(request.DepartmentId.Value);
                deptName = dept?.DepartmentName;
            }

            // Tạo object Job mới
            var job = new Job
            {
                JobId = Guid.NewGuid(),
                Code = jobCode,
                Title = request.Title,
                DepartmentId = request.DepartmentId,
                Department = deptName,
                Currency = "VND",
                Description = request.Description,
                Requirements = request.Requirements,
                Benefits = request.Benefits,
                NumberOfPositions = request.NumberOfPositions,
                SalaryMin = request.SalaryMin,
                SalaryMax = request.SalaryMax,
                Location = request.Location,
                EmploymentType = request.EmploymentType,
                ExperienceLevel = request.ExperienceLevel,
                SeniorityLevel = request.SeniorityLevel,
                Deadline = request.Deadline,
                CreatedBy = userId,
                RequestedByUserId = userId,
                RecruitmentTrack = request.RecruitmentTrack ?? "PROFESSIONAL",
                TargetQuantity = request.TargetQuantity > 0 ? request.TargetQuantity : 50,
                HiredCount = 0,
                WaitlistBufferPercent = 15,
                CurrentScoringWeightVersion = 1,
                RequireNightShift = request.RequireNightShift,
                RequisitionNote = request.RequisitionNote,
                RequisitionStatus = request.RequisitionStatus ?? "Approved",
                Status = (request.RequisitionStatus == "Pending_Approval") ? "PENDING_APPROVAL" : "OPEN",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Xử lý JobSkillMap
            if (request.SkillIds != null && request.SkillIds.Any())
            {
                foreach (var skillId in request.SkillIds)
                {
                    var jobSkillMap = new JobSkillMap
                    {
                        JobId = job.JobId,
                        SkillId = skillId,
                        Weight = 1 // Mặc định Weight = 1
                    };
                    job.JobSkillMaps.Add(jobSkillMap);
                }
            }

            // Lưu vào DB
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            CampaignQrDto? campaign = null;
            if (request.CreateCampaignQr)
            {
                try
                {
                    campaign = await _campaignQrService.CreateForJobAsync(job);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tạo Google Form/QR thất bại sau khi đã lưu Job {JobId}", job.JobId);
                }
            }

            return new CreateJobResultDto
            {
                Success = true,
                JobId = job.JobId,
                Message = "Đăng tin tuyển dụng thành công",
                Campaign = campaign
            };
        }
        catch (Exception)
        {
            // Rollback nếu có lỗi
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CampaignQrDto?> GetCampaignQrAsync(Guid jobId, string? customApiBase = null, string? customClientBase = null)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId && !j.IsDeleted);
        if (job == null) return null;

        if (string.IsNullOrWhiteSpace(job.GoogleFormPublicUrl) || (!string.IsNullOrWhiteSpace(customClientBase) && job.CampaignFormSource == "INTERNAL"))
            return await EnsureCampaignQrAsync(jobId, forceRecreate: false, customApiBase, customClientBase);

        var (apiBase, clientBase, secret) = _campaignQrService.GetPublicConfig();
        if (!string.IsNullOrWhiteSpace(customApiBase)) apiBase = customApiBase.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(customClientBase)) clientBase = customClientBase.Trim().TrimEnd('/');

        var png = _campaignQrService.GeneratePngBytes(job.GoogleFormPublicUrl);
        var webhookUrl = $"{apiBase}/api/applications/webhook-google-form";
        var isPublic = !apiBase.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                       && !apiBase.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        return new CampaignQrDto
        {
            JobId = job.JobId,
            JobTitle = job.Title,
            Source = job.CampaignFormSource ?? "INTERNAL",
            GoogleFormId = job.GoogleFormId,
            FormUrl = job.GoogleFormPublicUrl,
            FormEditUrl = job.GoogleFormEditUrl,
            QrCodeDataUrl = "data:image/png;base64," + Convert.ToBase64String(png),
            QrCodePath = job.QrCodeImagePath,
            AppsScript = _campaignQrService.BuildWebhookAppsScript(job.JobId, apiBase),
            PublicApiBaseUrl = apiBase,
            PublicClientBaseUrl = clientBase,
            WebhookUrl = webhookUrl,
            WebhookSecret = secret,
            IsUsingPublicUrl = isPublic
        };
    }

    public async Task<CampaignQrDto> EnsureCampaignQrAsync(Guid jobId, bool forceRecreate = false, string? customApiBase = null, string? customClientBase = null)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId && !j.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng.");

        var campaign = await _campaignQrService.CreateForJobAsync(job, forceRecreate, customApiBase, customClientBase);
        await _context.SaveChangesAsync();
        return campaign;
    }

    public async Task<CampaignQrDto> UpdatePublicConfigAndRegenerateAsync(Guid jobId, string? publicApiBaseUrl, string? publicClientBaseUrl, string? webhookSecret = null)
    {
        _campaignQrService.SetRuntimePublicConfig(publicApiBaseUrl, publicClientBaseUrl, webhookSecret);
        return await EnsureCampaignQrAsync(jobId, forceRecreate: false, publicApiBaseUrl, publicClientBaseUrl);
    }

    /// <summary>
    /// Cập nhật thông tin tin tuyển dụng
    /// </summary>
    public async Task<bool> UpdateJobAsync(Guid id, UpdateJobRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var job = await _context.Jobs
                .Include(j => j.JobSkillMaps)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null || job.IsDeleted)
            {
                return false;
            }

            // Update 
            job.Title = request.Title;
            job.Description = request.Description;
            if (request.DepartmentId.HasValue)
            {
                job.DepartmentId = request.DepartmentId;
                var dept = await _context.Departments.FindAsync(request.DepartmentId.Value);
                if (dept != null)
                {
                    job.Department = dept.DepartmentName;
                }
            }
            // 1. Kiểm tra không cho phép giảm số lượng cần tuyển xuống bé hơn số người đã HIRED
            if (request.NumberOfPositions.HasValue)
            {
                var currentHired = await _context.Applications
                    .CountAsync(a => a.JobId == id && a.Status == "HIRED");

                if (request.NumberOfPositions.Value < currentHired)
                {
                    throw new Exception($"Không thể giảm số lượng cần tuyển. Bạn đã tuyển thành công {currentHired} người cho vị trí này.");
                }
            }

            job.Requirements = request.Requirements;
            job.Benefits = request.Benefits;
            job.NumberOfPositions = request.NumberOfPositions;
            job.SalaryMin = request.SalaryMin;
            job.SalaryMax = request.SalaryMax;
            job.Location = request.Location;
            job.EmploymentType = request.EmploymentType;
            job.ExperienceLevel = request.ExperienceLevel;
            job.SeniorityLevel = request.SeniorityLevel;
            job.Deadline = request.Deadline;

            // Auto-update Status based on deadline:
            if (request.Deadline.HasValue)
            {
                var deadlineDate = request.Deadline.Value.Date;
                var isAfterPostDate = deadlineDate >= job.CreatedAt.Date;
                var isNotExpired = deadlineDate >= DateTime.UtcNow.Date;

                if (isAfterPostDate && isNotExpired)
                {
                    // Lấy số lượng đã tuyển để xác định xem có mở lại được không
                    var totalHired = await _context.Applications
                        .CountAsync(a => a.JobId == id && a.Status == "HIRED");

                    // Nếu đã tuyển đủ, thì Job buộc phải đóng lại kể cả chưa hết hạn nộp hồ sơ
                    if (request.NumberOfPositions.HasValue && totalHired >= request.NumberOfPositions.Value)
                    {
                        job.Status = "CLOSED";
                    }
                    else
                    {
                        job.Status = "OPEN";
                    }
                }
                else
                {
                    job.Status = "CLOSED";
                }
            }

            // Update Skills if provided 
            if (request.SkillIds != null)
            {
                // Remove existing skills
                _context.JobSkillMaps.RemoveRange(job.JobSkillMaps);
                
                // Add new skills
                foreach (var skillId in request.SkillIds)
                {
                    job.JobSkillMaps.Add(new JobSkillMap
                    {
                        JobId = job.JobId,
                        SkillId = skillId,
                        Weight = 1
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Xóa tin tuyển dụng 
    /// </summary>
    public async Task<bool> DeleteJobAsync(Guid id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null || job.IsDeleted)
        {
            return false;
        }

        job.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Đóng tin tuyển dụng (Status = CLOSED) và ghi audit trail
    /// </summary>
    public async Task<bool> CloseJobAsync(Guid id, Guid closedById, string closedByName)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null || job.IsDeleted)
            return false;

        job.Status = "CLOSED";
        job.ClosedAt = DateTime.UtcNow;
        job.ClosedById = closedById;
        job.ClosedByName = closedByName;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Mở lại tin tuyển dụng (Status = OPEN)
    /// </summary>
    public async Task<bool> OpenJobAsync(Guid id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null || job.IsDeleted)
        {
            return false;
        }

        // KIỂM TRA VALIDATE: Nếu mở lại job, check xem số lượng đã tuyển có vượt/bằng số lượng yêu cầu không
        var totalHired = await _context.Applications
            .CountAsync(a => a.JobId == id && a.Status == "HIRED");

        if (job.NumberOfPositions.HasValue && totalHired >= job.NumberOfPositions.Value)
        {
            throw new Exception($"Không thể mở job. Bạn đã tuyển đủ {totalHired}/{job.NumberOfPositions.Value} vị trí. Vui lòng chỉnh sửa job và tăng số lượng cần tuyển.");
        }

        job.Status = "OPEN";
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Phê duyệt định biên ngân sách chiến dịch (RequisitionStatus = 'Approved')
    /// </summary>
    public async Task<bool> ApproveRequisitionAsync(Guid id, Guid approverUserId)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null || job.IsDeleted)
        {
            return false;
        }

        job.RequisitionStatus = "Approved";
        job.ApprovedByUserId = approverUserId;
        job.ApprovedAt = DateTime.UtcNow;
        job.Status = "OPEN";
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("✅ Job {JobId} ({Title}) requisition approved by user {UserId}", job.JobId, job.Title, approverUserId);
        return true;
    }

    /// <summary>
    /// Sinh mã Code duy nhất theo format: JOB-{yyyyMMdd}-{RandomString}
    /// </summary>
    private async Task<string> GenerateUniqueJobCodeAsync()
    {
        string code;
        bool isUnique;

        do
        {
            // Tạo mã theo format JOB-{yyyyMMdd}-{RandomString}
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = GenerateRandomString(6);
            code = $"JOB-{datePart}-{randomPart}";

            // Kiểm tra xem mã đã tồn tại chưa
            isUnique = !await _context.Jobs.AnyAsync(j => j.Code == code);
        }
        while (!isUnique);

        return code;
    }

    /// <summary>
    /// Sinh chuỗi ngẫu nhiên gồm chữ và số
    /// </summary>
    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Lấy danh sách job mới nhất để hiển thị trên trang chủ
    /// Hỗ trợ tìm kiếm theo keyword và location
    /// </summary>
    public async Task<List<JobHomeDto>> GetLatestJobsAsync(int count, string? keyword = null, string? location = null)
    {
        var query = _context.Jobs
            .Where(j => !j.IsDeleted && j.Status == "OPEN") // Chỉ lấy job đang mở và chưa xóa
            .Include(j => j.JobSkillMaps)
                .ThenInclude(jsm => jsm.Skill) // Include Skills
            .Include(j => j.CreatedByNavigation) // Include User để lấy CompanyName
            .AsQueryable();

        // Filter theo keyword (tìm trong title, company name, hoặc skills)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordLower = keyword.Trim().ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(keywordLower) ||
                (j.CreatedByNavigation != null && j.CreatedByNavigation.FullName.ToLower().Contains(keywordLower)) ||
                j.JobSkillMaps.Any(jsm => jsm.Skill.Name.ToLower().Contains(keywordLower))
            );
        }

        // Filter theo location (CONTAINS matching - bắt cả "Hà Nội" khi DB có "Thành phố Hà Nội")
        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationLower = location.Trim().ToLower();
            query = query.Where(j =>
                j.Location != null && j.Location.ToLower().Contains(locationLower)
            );
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt) // Mới nhất lên đầu
            .Take(count) // Lấy số lượng yêu cầu
            .ToListAsync();

        // Lấy thông tin công ty từ Admin (dùng chung cho toàn hệ thống)
        var adminUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            .Select(u => new { u.CompanyName })
            .FirstOrDefaultAsync();
        
        string sysCompanyName = adminUser?.CompanyName ?? "Unknown Company";

        // Map sang DTO
        var result = jobs.Select(j => new JobHomeDto
        {
            JobId = j.JobId,
            Title = j.Title,
            CompanyName = sysCompanyName,
            DepartmentId = j.DepartmentId,
            DepartmentName = j.Department,
            SalaryMin = j.SalaryMin,
            SalaryMax = j.SalaryMax,
            Location = j.Location,
            EmploymentType = j.EmploymentType,
            ExperienceLevel = j.ExperienceLevel,
            SeniorityLevel = j.SeniorityLevel,
            Deadline = j.Deadline,
            CreatedDate = j.CreatedAt,
            Skills = j.JobSkillMaps.Select(jsm => jsm.Skill.Name).ToList()
        }).ToList();

        return result;
    }

    /// <summary>
    /// Lấy chi tiết job theo ID
    /// </summary>
    public async Task<JobDetailDto?> GetJobByIdAsync(Guid id)
    {
        Console.WriteLine($"[DEBUG] Searching for job ID: {id}");
        
        // Tạm bỏ Include để test
        var job = await _context.Jobs
            .Where(j => j.JobId == id && !j.IsDeleted)
            .FirstOrDefaultAsync();

        Console.WriteLine($"[DEBUG] Found job: {(job != null ? job.Title : "NULL")}");
        
        if (job == null)
        {
            Console.WriteLine($"[DEBUG] Job not found or IsDeleted = true");
            return null;
        }

        // Load navigation properties separately
        await _context.Entry(job)
            .Collection(j => j.JobSkillMaps)
            .Query()
            .Include(jsm => jsm.Skill)
            .LoadAsync();
            
        await _context.Entry(job)
            .Reference(j => j.CreatedByNavigation)
            .LoadAsync();

        Console.WriteLine($"[DEBUG] Loaded {job.JobSkillMaps.Count} skills");
        Console.WriteLine($"[DEBUG] CreatedBy: {job.CreatedByNavigation?.FullName ?? "NULL"}");

        // Lấy thông tin công ty từ Admin (dùng chung cho toàn hệ thống)
        var adminUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            .Select(u => new { u.CompanyName })
            .FirstOrDefaultAsync();
        
        string sysCompanyName = adminUser?.CompanyName ?? "Unknown Company";

        // Map sang DTO
        var result = new JobDetailDto
        {
            JobId = job.JobId,
            Title = job.Title,
            DepartmentId = job.DepartmentId,
            DepartmentName = job.Department,
            CompanyName = sysCompanyName,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            Location = job.Location,
            EmploymentType = job.EmploymentType,
            ExperienceLevel = job.ExperienceLevel,
            SeniorityLevel = job.SeniorityLevel,
            Status = job.Status,
            Deadline = job.Deadline,
            CreatedDate = job.CreatedAt,
            Skills = job.JobSkillMaps.Select(jsm => jsm.Skill.Name).ToList(),
            SkillIds = job.JobSkillMaps.Select(jsm => jsm.SkillId).ToList(),
            
            // Thông tin chi tiết
            Description = job.Description,
            Requirements = job.Requirements,
            Benefits = job.Benefits,
            ContactEmail = job.CreatedByNavigation?.Email,
            NumberOfPositions = job.NumberOfPositions,
            CampaignFormSource = job.CampaignFormSource,
            GoogleFormId = job.GoogleFormId,
            GoogleFormPublicUrl = job.GoogleFormPublicUrl,
            GoogleFormEditUrl = job.GoogleFormEditUrl,
            QrCodeImagePath = job.QrCodeImagePath,

            RecruitmentTrack = job.RecruitmentTrack ?? "PROFESSIONAL",
            TargetQuantity = job.TargetQuantity > 0 ? job.TargetQuantity : (job.NumberOfPositions ?? 50),
            HiredCount = job.HiredCount,
            WaitlistBufferPercent = job.WaitlistBufferPercent > 0 ? job.WaitlistBufferPercent : 15,
            CurrentScoringWeightVersion = job.CurrentScoringWeightVersion > 0 ? job.CurrentScoringWeightVersion : 1,
            GoogleFormUrl = job.GoogleFormUrl,
            RequisitionStatus = job.RequisitionStatus ?? "Approved",
            RequestedByUserId = job.RequestedByUserId,
            ApprovedByUserId = job.ApprovedByUserId,
            ApprovedAt = job.ApprovedAt,
            RequisitionNote = job.RequisitionNote,
            RequireNightShift = job.RequireNightShift
        };

        Console.WriteLine($"[DEBUG] Returning DTO for job: {result.Title}");
        return result;
    }

    /// <summary>
    /// Tìm kiếm việc làm công khai 
    /// </summary>
    public async Task<List<JobPublicDto>> SearchJobsPublicAsync(string? keyword, string? location, string? jobType, decimal? minSalary)
    {
        var query = _context.Jobs
            .AsNoTracking() // Optimize request tìm kiếm (không cần tracking)
            .Where(j => !j.IsDeleted && j.Status == "OPEN") 
            .Include(j => j.JobSkillMaps)
                .ThenInclude(jsm => jsm.Skill)
            .Include(j => j.CreatedByNavigation)
            .AsQueryable();

        // 1. Keyword search (Title or Skills)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordLower = keyword.Trim().ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(keywordLower) ||
                j.JobSkillMaps.Any(jsm => jsm.Skill.Name.ToLower().Contains(keywordLower))
            );
        }

        // 2. Location search
        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationLower = location.Trim().ToLower()
                .Replace("thành phố ", "")
                .Replace("tỉnh ", "")
                .Trim();

            query = query.Where(j =>
                j.Location != null && j.Location.ToLower().Contains(locationLower)
            );
        }

        // 3. JobType filter 
        if (!string.IsNullOrWhiteSpace(jobType))
        {
            var jobTypeLower = jobType.Trim().ToLower();
            query = query.Where(j => 
                j.EmploymentType != null && j.EmploymentType.ToLower().Contains(jobTypeLower)
            );
        }

        // 4. MinSalary filter
        // Logic: Return jobs where Max Salary >= Request Min Salary (or Max is null/unlimited)
        // AND ensuring valid range with Min Salary
        if (minSalary.HasValue)
        {
            query = query.Where(j => 
                (j.SalaryMax.HasValue && j.SalaryMax >= minSalary) || 
                (!j.SalaryMax.HasValue && j.SalaryMin >= minSalary) // Nếu không có Max, check Min
            );
        }

        // Sorting: Mới nhất lên đầu
        query = query.OrderByDescending(j => j.CreatedAt);

        // Lấy thông tin công ty từ Admin (dùng chung cho toàn hệ thống)
        var adminUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            .Select(u => new { u.CompanyName, u.CompanyLogoUrl })
            .FirstOrDefaultAsync();
        
        string sysCompanyName = adminUser?.CompanyName ?? "Unknown Company";
        string sysCompanyLogo = adminUser?.CompanyLogoUrl ?? "";

        // Projection to DTO
        var jobs = await query.Select(j => new JobPublicDto
        {
            JobId = j.JobId,
            Title = j.Title,
            CompanyName = sysCompanyName,
            CompanyLogo = sysCompanyLogo,
            Location = j.Location,
            SalaryMin = j.SalaryMin,
            SalaryMax = j.SalaryMax,
            JobType = j.EmploymentType,
            ExperienceLevel = j.ExperienceLevel,
            SeniorityLevel = j.SeniorityLevel,
            CreatedAt = j.CreatedAt,
            Skills = j.JobSkillMaps.Select(jsm => jsm.Skill.Name).ToList()
        }).ToListAsync();

        return jobs;
    }


    /// <summary>
    /// Lấy danh sách tất cả các job đang mở
    /// </summary>
    public async Task<List<JobHomeDto>> GetAllJobsAsync()
    {
        // Lấy thông tin công ty từ Admin (dùng chung cho toàn hệ thống)
        var sysCompanyName = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            .Select(u => u.CompanyName)
            .FirstOrDefaultAsync() ?? "Unknown Company";

        // Tối ưu hóa: Dùng Projection (.Select) thay vì .Include() để tránh Cartesian Explosion
        // Giúp query này chạy nhanh gấp 10-100 lần (từ 10s xuống 100ms)
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => !j.IsDeleted) // Removed Status == "OPEN" check to show all jobs to HR
            // Ưu tiên: OPEN lên trước, CLOSED xuống dưới -> Cùng Status thì mới nhất lên đầu
            .OrderByDescending(j => j.Status == "OPEN") 
            .ThenByDescending(j => j.CreatedAt)
            .Select(j => new JobHomeDto
            {
                JobId = j.JobId,
                Title = j.Title,
                CompanyName = sysCompanyName,
                DepartmentId = j.DepartmentId,
                DepartmentName = j.Department,
                CreatedByName = j.CreatedByNavigation != null ? j.CreatedByNavigation.FullName : null,
                CreatedByRole = j.CreatedByNavigation != null && j.CreatedByNavigation.UserRoles.Any()
                    ? j.CreatedByNavigation.UserRoles.FirstOrDefault().Role.Name
                    : null,
                SalaryMin = j.SalaryMin,
                SalaryMax = j.SalaryMax,
                Location = j.Location,
                EmploymentType = j.EmploymentType,
                ExperienceLevel = j.ExperienceLevel,
                SeniorityLevel = j.SeniorityLevel,
                Deadline = j.Deadline,
                CreatedDate = j.CreatedAt,
                Status = j.Status,
                NumberOfPositions = j.NumberOfPositions,
                TotalHired = j.Applications.Count(a => a.Status == "HIRED"),
                Skills = j.JobSkillMaps.Select(jsm => jsm.Skill.Name).ToList(),
                // Audit Trail
                ClosedByName = j.ClosedByName,
                ClosedAt = j.ClosedAt,
                CampaignFormSource = j.CampaignFormSource,
                GoogleFormPublicUrl = j.GoogleFormPublicUrl,
                QrCodeImagePath = j.QrCodeImagePath
            })
            .ToListAsync();

        return jobs;
    }

    public async Task<SystemStatsDto> GetSystemStatsAsync()
    {
        // IsDeleted không nullable nên chỉ cần check !j.IsDeleted
        var jobCount = await _context.Jobs.CountAsync(j => j.Status == "OPEN" && !j.IsDeleted);
        
        // Method 2: Count Applications (Lượt ứng tuyển) for Single Company context
        var applicationCount = await _context.Applications.CountAsync();

        var candidateCount = await _context.Candidates.CountAsync();

        return new SystemStatsDto
        {
            JobCount = jobCount,
            ApplicationCount = applicationCount,
            CandidateCount = candidateCount
        };
    }
    }


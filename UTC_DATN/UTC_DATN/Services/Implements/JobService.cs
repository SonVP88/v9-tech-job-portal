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

    public JobService(UTC_DATNContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateJobAsync(CreateJobRequest request, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Tạo mã Code duy nhất
            var jobCode = await GenerateUniqueJobCodeAsync();

            // Tạo object Job mới
            var job = new Job
            {
                JobId = Guid.NewGuid(),
                Code = jobCode,
                Title = request.Title,
                Description = request.Description,
                Requirements = request.Requirements,
                Benefits = request.Benefits,
                NumberOfPositions = request.NumberOfPositions,
                SalaryMin = request.SalaryMin,
                SalaryMax = request.SalaryMax,
                Location = request.Location,
                EmploymentType = request.EmploymentType,
                Deadline = request.Deadline,
                CreatedBy = userId,
                Status = "OPEN",
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

            // Commit transaction
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception)
        {
            // Rollback nếu có lỗi
            await transaction.RollbackAsync();
            throw;
        }
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
            job.Requirements = request.Requirements;
            job.Benefits = request.Benefits;
            job.NumberOfPositions = request.NumberOfPositions;
            job.SalaryMin = request.SalaryMin;
            job.SalaryMax = request.SalaryMax;
            job.Location = request.Location;
            job.EmploymentType = request.EmploymentType;
            job.Deadline = request.Deadline;

            // Auto-update Status based on deadline:
            // OPEN  = deadline >= ngày đăng (createdAt) AND deadline >= hôm nay (UtcNow)
            // CLOSED = deadline < ngày đăng OR deadline < hôm nay (hết hạn hoặc sai logic)
            if (request.Deadline.HasValue)
            {
                var deadlineDate = request.Deadline.Value.Date;
                var isAfterPostDate = deadlineDate >= job.CreatedAt.Date;
                var isNotExpired = deadlineDate >= DateTime.UtcNow.Date;

                if (isAfterPostDate && isNotExpired)
                {
                    job.Status = "OPEN";
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
    /// Đóng tin tuyển dụng (Status = CLOSED)
    /// </summary>
    public async Task<bool> CloseJobAsync(Guid id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null || job.IsDeleted)
        {
            return false;
        }

        job.Status = "CLOSED";
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

        job.Status = "OPEN";
        await _context.SaveChangesAsync();
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
            SalaryMin = j.SalaryMin,
            SalaryMax = j.SalaryMax,
            Location = j.Location,
            EmploymentType = j.EmploymentType,
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
            CompanyName = sysCompanyName,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            Location = job.Location,
            EmploymentType = job.EmploymentType,
            Deadline = job.Deadline,
            CreatedDate = job.CreatedAt,
            Skills = job.JobSkillMaps.Select(jsm => jsm.Skill.Name).ToList(),
            SkillIds = job.JobSkillMaps.Select(jsm => jsm.SkillId).ToList(),
            
            // Thông tin chi tiết
            Description = job.Description,
            Requirements = job.Requirements,
            Benefits = job.Benefits,
            ContactEmail = job.CreatedByNavigation?.Email,
            NumberOfPositions = job.NumberOfPositions
        };

        Console.WriteLine($"[DEBUG] Returning DTO for job: {result.Title}");
        return result;
    }

    /// <summary>
    /// Tìm kiếm việc làm công khai (kèm bộ lọc)
    /// </summary>
    public async Task<List<JobPublicDto>> SearchJobsPublicAsync(string? keyword, string? location, string? jobType, decimal? minSalary)
    {
        var query = _context.Jobs
            .AsNoTracking() // Optimize request tìm kiếm (không cần tracking)
            .Where(j => !j.IsDeleted && j.Status == "OPEN") // Chỉ lấy job đang mở
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
        // 2. Location search
        if (!string.IsNullOrWhiteSpace(location))
        {
            // Normalize: Remove 'Thành phố', 'Tỉnh' prefixes to match broader locations like "Hà Nội"
            var locationLower = location.Trim().ToLower()
                .Replace("thành phố ", "")
                .Replace("tỉnh ", "")
                .Trim();

            query = query.Where(j =>
                j.Location != null && j.Location.ToLower().Contains(locationLower)
            );
        }

        // 3. JobType filter (Exact match or Contains)
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
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => !j.IsDeleted) // Removed Status == "OPEN" check to show all jobs to HR
            .Include(j => j.JobSkillMaps)
                .ThenInclude(jsm => jsm.Skill)
            .Include(j => j.CreatedByNavigation)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        // Lấy thông tin công ty từ Admin (dùng chung cho toàn hệ thống)
        var adminUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            .Select(u => new { u.CompanyName })
            .FirstOrDefaultAsync();
        
        string sysCompanyName = adminUser?.CompanyName ?? "Unknown Company";

        return jobs.Select(j => new JobHomeDto
        {
            JobId = j.JobId,
            Title = j.Title,
            CompanyName = sysCompanyName,
            CreatedByName = j.CreatedByNavigation?.FullName,
            CreatedByRole = j.CreatedByNavigation?.UserRoles?.FirstOrDefault()?.Role?.Name,
            SalaryMin = j.SalaryMin,
            SalaryMax = j.SalaryMax,
            Location = j.Location,
            EmploymentType = j.EmploymentType,
            Deadline = j.Deadline,
            CreatedDate = j.CreatedAt,
            Status = j.Status,
            Skills = j.JobSkillMaps.Select(jsm => jsm.Skill.Name).ToList()
        }).ToList();
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


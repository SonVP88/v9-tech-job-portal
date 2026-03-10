using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Recommendation;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements;

public class RecommendationService : IRecommendationService
{
    private readonly UTC_DATNContext _context;

    public RecommendationService(UTC_DATNContext context)
    {
        _context = context;
    }

    public async Task<List<RecommendedJobDto>> GetRecommendedJobsForCandidateAsync(Guid userId, int top = 10)
    {
        // 1. Lấy kỹ năng của ứng viên (cả SkillId lẫn tên)
        var candidateSkillData = await _context.CandidateSkills
            .Where(cs => cs.Candidate.UserId == userId)
            .Select(cs => new { cs.SkillId, cs.Skill.Name })
            .ToListAsync();

        var candidateSkillIds = candidateSkillData.Select(s => s.SkillId).ToList();
        var candidateSkillNames = candidateSkillData.Select(s => s.Name.ToLower()).ToList();

        // Nếu không có kỹ năng nào → không gợi ý
        if (!candidateSkillIds.Any())
        {
            return new List<RecommendedJobDto>();
        }

        // 2. Lấy tất cả Job đang OPEN
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => !j.IsDeleted && j.Status == "OPEN")
            .Select(j => new
            {
                j.JobId,
                j.Title,
                j.Description,
                j.Requirements,
                CompanyName = j.CreatedByNavigation != null ? j.CreatedByNavigation.CompanyName : "Unknown",
                j.Location,
                j.SalaryMin,
                j.SalaryMax,
                j.EmploymentType,
                j.Deadline,
                j.CreatedAt,
                JobSkills = j.JobSkillMaps.Select(m => new { m.SkillId, m.Skill.Name }).ToList()
            })
            .ToListAsync();

        var recommendedJobs = new List<RecommendedJobDto>();

        foreach (var job in jobs)
        {
            double score = 0;
            int matchedCount = 0;
            int totalRequired = job.JobSkills.Count;

            // === TIER 1: SkillId Exact Match (độ chính xác cao) ===
            if (totalRequired > 0)
            {
                matchedCount = job.JobSkills.Count(s => candidateSkillIds.Contains(s.SkillId));
                if (matchedCount > 0)
                {
                    score = (double)matchedCount / totalRequired * 100;
                }
            }

            // === TIER 2: Keyword Match trong Title + Description + Requirements (fallback) ===
            if (score == 0)
            {
                var jobText = $"{job.Title} {job.Description} {job.Requirements}".ToLower();
                var keywordMatches = candidateSkillNames.Count(skillName =>
                    jobText.Contains(skillName)
                );

                if (keywordMatches > 0)
                {
                    // Keyword match → tính điểm dựa trên số từ khóa khớp / tổng kỹ năng ứng viên
                    // Có hệ số 0.7x vì keyword match ít chính xác hơn SkillId match
                    score = Math.Min((double)keywordMatches / candidateSkillNames.Count * 70, 70);
                    matchedCount = keywordMatches;
                    totalRequired = candidateSkillNames.Count;
                }
            }

            // Chỉ thêm vào danh sách khi có ít nhất 1 match
            if (score > 0)
            {
                recommendedJobs.Add(new RecommendedJobDto
                {
                    JobId = job.JobId,
                    Title = job.Title,
                    CompanyName = job.CompanyName ?? "Unknown",
                    Location = job.Location,
                    SalaryMin = job.SalaryMin,
                    SalaryMax = job.SalaryMax,
                    EmploymentType = job.EmploymentType,
                    Deadline = job.Deadline,
                    CreatedDate = job.CreatedAt,
                    Skills = job.JobSkills.Select(s => s.Name).ToList(),
                    MatchScore = Math.Round(score, 1),
                    MatchedSkillsCount = matchedCount,
                    TotalRequiredSkills = totalRequired
                });
            }
        }

        // Nếu vẫn không có kết quả nào (kỹ năng rất hiếm) → return empty, KHÔNG fallback chung chung
        return recommendedJobs
            .OrderByDescending(j => j.MatchScore)
            .ThenByDescending(j => j.CreatedDate)
            .Take(top)
            .ToList();
    }

    public async Task<List<RecommendedCandidateDto>> GetRecommendedCandidatesForJobAsync(Guid jobId, int top = 10)
    {
        // 1. Lấy dữ liệu skill yêu cầu của Job
        var jobSkills = await _context.JobSkillMaps
            .Where(m => m.JobId == jobId)
            .Select(m => m.SkillId)
            .ToListAsync();

        if (!jobSkills.Any())
        {
            return new List<RecommendedCandidateDto>();
        }

        int requiredSkillsCount = jobSkills.Count;

        // 2. Lấy dữ liệu ứng viên (với chế độ public/công khai nếu có rule đó sau này)
        var candidates = await _context.Candidates
            .AsNoTracking()
            .Select(c => new 
            {
                c.CandidateId,
                c.UserId,
                FullName = c.User != null ? c.User.FullName : c.FullName,
                Title = c.Headline,
                AvatarUrl = c.User != null ? c.User.AvatarUrl : c.Avatar,
                Experiences = c.CandidateExperiences.Select(e => new { e.StartDate, e.EndDate }).ToList(),
                CandidateSkills = c.CandidateSkills.Select(cs => new { cs.SkillId, cs.Skill.Name }).ToList()
            })
            .ToListAsync();

        // 3. Chấm điểm & Xếp hạng Ứng viên
        var recommendedCandidates = new List<RecommendedCandidateDto>();
        foreach (var candidate in candidates)
        {
            if (candidate.CandidateSkills.Count == 0) continue;

            int matchedCount = candidate.CandidateSkills.Count(s => jobSkills.Contains(s.SkillId));
            if (matchedCount > 0)
            {
                double score = (double)matchedCount / requiredSkillsCount * 100;
                
                // Tính tạm số năm kinh nghiệm dựa vào Experiences
                int yearsOfExp = 0;
                foreach(var exp in candidate.Experiences) 
                {
                     int startYear = exp.StartDate?.Year ?? DateTime.UtcNow.Year;
                     int endYear = exp.EndDate?.Year ?? DateTime.UtcNow.Year;
                     int diff = endYear - startYear;
                     if (diff > 0) yearsOfExp += diff;
                }

                recommendedCandidates.Add(new RecommendedCandidateDto
                {
                    CandidateId = candidate.CandidateId,
                    UserId = candidate.UserId,
                    FullName = candidate.FullName,
                    Title = candidate.Title,
                    AvatarUrl = candidate.AvatarUrl,
                    YearsOfExperience = yearsOfExp,
                    Skills = candidate.CandidateSkills.Select(s => s.Name).ToList(),
                    MatchScore = Math.Round(score, 1),
                    MatchedSkillsCount = matchedCount,
                    TotalRequiredSkills = requiredSkillsCount
                });
            }
        }

        return recommendedCandidates.OrderByDescending(c => c.MatchScore).Take(top).ToList();
    }
}

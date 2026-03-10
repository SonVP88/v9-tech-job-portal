using UTC_DATN.DTOs.Recommendation;

namespace UTC_DATN.Services.Interfaces;

public interface IRecommendationService
{
    Task<List<RecommendedJobDto>> GetRecommendedJobsForCandidateAsync(Guid userId, int top = 10);
    Task<List<RecommendedCandidateDto>> GetRecommendedCandidatesForJobAsync(Guid jobId, int top = 10);
}

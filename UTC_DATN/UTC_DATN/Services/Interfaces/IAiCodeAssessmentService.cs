using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UTC_DATN.DTOs.AiCodeAssessment;

namespace UTC_DATN.Services.Interfaces
{
    public interface IAiCodeAssessmentService
    {
        Task<CodeChallengeDto> CreateChallengeAsync(CreateCodeChallengeDto request);
        Task<List<CodeChallengeDto>> GetChallengesByJobAsync(Guid jobId);
        Task<CodeChallengeDto?> GetChallengeByIdAsync(Guid challengeId);
        
        Task<CodeSubmissionDto> SubmitCodeAsync(SubmitCodeDto request);
        Task<AiCodeReviewResponseDto> GetOrCreateReviewAsync(Guid submissionId);
        
        // This is the core AI logic method
        Task<AiCodeReviewResponseDto> AnalyzeCodeWithAiAsync(Guid submissionId);
    }
}

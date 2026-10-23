using Microsoft.AspNetCore.Http;
using UTC_DATN.DTOs.Ai;

namespace UTC_DATN.Services.Interfaces;

public interface IAiScreeningService
{
    Task<List<CandidateScreeningResultDto>> ScreenBulkCvsAsync(
        List<IFormFile> files,
        Guid jobId,
        int skillsWeight,
        int experienceWeight,
        int healthShiftWeight);

    Task<List<ActiveCampaignDto>> GetActiveCampaignsAsync();

    Task<bool> ForwardCandidatesToManagerAsync(ForwardToManagerRequestDto request);
}

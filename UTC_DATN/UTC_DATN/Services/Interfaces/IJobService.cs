using UTC_DATN.DTOs.Job;
using UTC_DATN.DTOs.Common;

namespace UTC_DATN.Services.Interfaces;

public interface IJobService
{
    Task<CreateJobResultDto> CreateJobAsync(CreateJobRequest request, Guid userId);
    Task<CampaignQrDto?> GetCampaignQrAsync(Guid jobId, string? customApiBase = null, string? customClientBase = null);
    Task<CampaignQrDto> EnsureCampaignQrAsync(Guid jobId, bool forceRecreate = false, string? customApiBase = null, string? customClientBase = null);
    
    Task<List<JobHomeDto>> GetLatestJobsAsync(int count, string? keyword = null, string? location = null);
    Task<List<JobHomeDto>> GetAllJobsAsync();
    
    Task<bool> UpdateJobAsync(Guid id, UpdateJobRequest request);
    Task<bool> DeleteJobAsync(Guid id);
    Task<bool> CloseJobAsync(Guid id, Guid closedById, string closedByName);
    Task<bool> OpenJobAsync(Guid id);
    Task<bool> ApproveRequisitionAsync(Guid id, Guid approverUserId);

    Task<JobDetailDto?> GetJobByIdAsync(Guid id);

    /// <summary>
    /// Tìm kiếm việc làm công khai (kèm bộ lọc)
    /// </summary>
    Task<List<JobPublicDto>> SearchJobsPublicAsync(string? keyword, string? location, string? jobType, decimal? minSalary);
        
    /// <summary>
    /// Lấy thống kê hệ thống (số lượng việc làm, công ty, ứng viên)
    /// </summary>
    Task<SystemStatsDto> GetSystemStatsAsync();
}

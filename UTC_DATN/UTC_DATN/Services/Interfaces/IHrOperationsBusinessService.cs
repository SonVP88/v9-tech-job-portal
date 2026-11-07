using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UTC_DATN.Entities;

namespace UTC_DATN.Services.Interfaces;

public class ScheduleBatchInterviewRequest
{
    public Guid JobId { get; set; }
    public List<Guid> ApplicationIds { get; set; } = new List<Guid>();
    public DateTime ScheduledDate { get; set; }
    public string ShiftSlot { get; set; } = "MORNING_08_11"; // 'MORNING_08_11' | 'AFTERNOON_13_17'
    public Guid InterviewerUserId { get; set; }
    public int MaxCapacity { get; set; } = 50;
}

public class BatchScheduleResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalCandidates { get; set; }
    public int BatchesCreated { get; set; }
    public List<BatchDetailDto> Batches { get; set; } = new List<BatchDetailDto>();
}

public class BatchDetailDto
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string ShiftSlot { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public int MaxCapacity { get; set; }
}

public class ProbationFeedbackRequest
{
    public string ProbationStatus { get; set; } = "Passed_30Days"; // 'Passed_30Days' | 'Early_Resigned'
    public string? FeedbackNote { get; set; }
}

public interface IHrOperationsBusinessService
{
    Task<(bool IsRehire, string? EmployeeCode, string? Message)> CheckExEmployeeRehireAsync(
        Application application, string? rawPhone, string? rawCitizenId);

    Task<BatchScheduleResultDto> ScheduleBatchInterviewsAsync(
        ScheduleBatchInterviewRequest request, Guid? actorUserId);

    Task<(bool Success, bool IsRejected, int CurrentCount, string Message)> RescheduleInterviewAsync(
        Guid applicationId, DateTime newDate, string newShiftSlot, string? reason, Guid? actorUserId);

    Task<(int ExpiredCount, List<Guid> ExpiredApplicationIds)> CheckAndExpireOffersAsync();

    Task<(bool Success, int MovedToTalentPoolCount, string Message)> AutoCloseCampaignAndMoveToTalentPoolAsync(
        Guid jobId, Guid? actorUserId);

    Task<(bool Success, string Message, decimal? AccuracyRate)> UpdateProbationFeedbackAsync(
        Guid applicationId, ProbationFeedbackRequest request, Guid? actorUserId);

    Task<List<InterviewBatch>> GetActiveInterviewBatchesAsync(Guid? jobId);
}


namespace UTC_DATN.DTOs.Application;

public class UpdateApplicationStatusResponse
{
    public bool Success { get; set; }
    public Guid JobId { get; set; }
    public int TotalHired { get; set; }
    public int? NumberOfPositions { get; set; }
    public bool IsJobActive { get; set; }
}

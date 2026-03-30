namespace UTC_DATN.DTOs.Application;

public class SlaStageConfigDto
{
    public Guid StageId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsTerminal { get; set; }
    public bool IsSlaEnabled { get; set; }
    public int? SlaMaxDays { get; set; }
    public int? SlaWarnBeforeDays { get; set; }
}

public class UpdateSlaStageConfigRequest
{
    public bool IsSlaEnabled { get; set; }
    public int? SlaMaxDays { get; set; }
    public int? SlaWarnBeforeDays { get; set; }
}

namespace UTC_DATN.DTOs.Job;

public class CreateJobResultDto
{
    public bool Success { get; set; }
    public Guid JobId { get; set; }
    public string Message { get; set; } = string.Empty;
    public CampaignQrDto? Campaign { get; set; }
}

public class CampaignQrDto
{
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Source { get; set; } = "INTERNAL";
    public string? GoogleFormId { get; set; }
    public string FormUrl { get; set; } = string.Empty;
    public string? FormEditUrl { get; set; }
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public string? QrCodePath { get; set; }
    public string? Warning { get; set; }
    public string? AppsScript { get; set; }

    // Public / Ngrok URL information
    public string PublicApiBaseUrl { get; set; } = string.Empty;
    public string PublicClientBaseUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsUsingPublicUrl { get; set; }
}

public class PublicCampaignInfoDto
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? Location { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsOpen { get; set; }
}

public class UpdatePublicIntegrationConfigRequest
{
    public string? PublicApiBaseUrl { get; set; }
    public string? PublicClientBaseUrl { get; set; }
    public string? WebhookSecret { get; set; }
}

public class UpdateCustomFormUrlRequest
{
    public string FormUrl { get; set; } = string.Empty;
}


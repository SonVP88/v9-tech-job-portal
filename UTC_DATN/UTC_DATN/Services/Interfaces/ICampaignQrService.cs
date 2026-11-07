using UTC_DATN.DTOs.Job;
using UTC_DATN.Entities;

namespace UTC_DATN.Services.Interfaces;

public interface ICampaignQrService
{
    Task<CampaignQrDto> CreateForJobAsync(Job job, bool forceRecreate = false, string? customApiBase = null, string? customClientBase = null);
    string GeneratePngDataUrl(string content);
    byte[] GeneratePngBytes(string content);
    string BuildWebhookAppsScript(Guid jobId, string? customApiBase = null);
    (string PublicApiBaseUrl, string PublicClientBaseUrl, string WebhookSecret) GetPublicConfig();
    void SetRuntimePublicConfig(string? publicApiBaseUrl, string? publicClientBaseUrl, string? webhookSecret = null);
}

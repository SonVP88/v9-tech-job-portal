namespace UTC_DATN.Services.Models;

public class GeminiKeyStat
{
    public int KeyIndex { get; set; }
    public string KeyPreview { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public bool Disabled { get; set; }
}

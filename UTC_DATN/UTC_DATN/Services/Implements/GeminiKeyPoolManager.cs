using System.Collections.Concurrent;

namespace UTC_DATN.Services.Implements;

// =============================================================================
// Model trạng thái của mỗi API Key trong Pool
// =============================================================================
public sealed class ApiKeyState
{
    public string ApiKey { get; init; } = string.Empty;

    /// Thời điểm được sử dụng gần nhất (LRU sorting)
    public DateTime LastUsedAt { get; set; } = DateTime.MinValue;

    /// Thời điểm hết bị phạt 429/503 (nếu > UtcNow thì key đang trong cooldown)
    public DateTime CooldownUntil { get; set; } = DateTime.MinValue;

    /// Tổng số lần đã gọi API
    public long UsageCount { get; set; }

    /// Kiểm tra key có đang trong cooldown không
    public bool IsOnCooldown => CooldownUntil > DateTime.UtcNow;

    /// Preview che key để log an toàn
    public string Preview => ApiKey.Length <= 8
        ? ApiKey
        : $"{ApiKey[..4]}...{ApiKey[^4..]}";
}

// =============================================================================
// GeminiKeyPoolManager — Singleton, Thread-Safe
// Thuật toán: LRU (Least-Recently-Used) + Cooldown-aware
// =============================================================================
public sealed class GeminiKeyPoolManager
{
    // SemaphoreSlim(1,1) = Async-friendly Mutex để serialize việc chọn key
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<ApiKeyState> _keyStates;
    private readonly ILogger<GeminiKeyPoolManager> _logger;

    // Thời gian phạt mặc định: 62s (vượt qua cửa sổ 60s của Google Free Tier)
    private const int DefaultCooldownSeconds = 62;

    public int TotalKeyCount => _keyStates.Count;

    public GeminiKeyPoolManager(IConfiguration configuration, ILogger<GeminiKeyPoolManager> logger)
    {
        _logger = logger;

        // Đọc danh sách keys từ: "GeminiAI": { "ApiKeys": ["key1", "key2", ...] }
        var keys = configuration.GetSection("GeminiAI:ApiKeys").Get<List<string>>()
                   ?? new List<string>();

        // Fallback sang single key (backward-compat)
        if (keys.Count == 0)
        {
            var singleKey = configuration["GeminiAI:ApiKey"];
            if (!string.IsNullOrWhiteSpace(singleKey))
                keys.Add(singleKey);
        }

        if (keys.Count == 0)
            throw new InvalidOperationException(
                "[GeminiKeyPoolManager] Không có Gemini API Key nào trong appsettings.json!");

        _keyStates = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .Select(k => new ApiKeyState { ApiKey = k.Trim() })
            .ToList();

        _logger.LogInformation(
            "✅ [GeminiKeyPoolManager] Khởi tạo với {Count} API Keys.",
            _keyStates.Count);
    }

    // -------------------------------------------------------------------------
    // AcquireKeyAsync: Lấy key tối ưu theo LRU + Cooldown-aware
    // Nếu tất cả key đang cooldown → tự động delay chờ key hồi phục sớm nhất
    // -------------------------------------------------------------------------
    public async Task<ApiKeyState> AcquireKeyAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        bool semaphoreHeld = true;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var now = DateTime.UtcNow;

                // Lấy key không cooldown với LastUsedAt xa nhất (LRU = Least-Recently-Used)
                var best = _keyStates
                    .Where(k => !k.IsOnCooldown)
                    .OrderBy(k => k.LastUsedAt)
                    .FirstOrDefault();

                if (best != null)
                {
                    best.LastUsedAt = now;
                    best.UsageCount++;

                    _logger.LogDebug(
                        "🔑 [Pool] Cấp Key [{Preview}] (Usage={Count}, LastUsed=now)",
                        best.Preview, best.UsageCount);

                    return best;
                }

                // Tất cả đang cooldown → tính thời gian chờ đến key hồi phục sớm nhất
                var soonestRecovery = _keyStates.Min(k => k.CooldownUntil);
                var waitMs = (int)Math.Ceiling((soonestRecovery - now).TotalMilliseconds) + 150;
                if (waitMs < 150) waitMs = 150;

                _logger.LogWarning(
                    "⏳ [Pool] TẤT CẢ {Count} key đang cooldown. Chờ {WaitMs}ms đến {Recovery:HH:mm:ss.fff} UTC...",
                    _keyStates.Count, waitMs, soonestRecovery);

                // Release semaphore trong khi delay để không chặn ReportRateLimit từ thread khác
                _semaphore.Release();
                semaphoreHeld = false;
                await Task.Delay(waitMs, cancellationToken);
                await _semaphore.WaitAsync(cancellationToken);
                semaphoreHeld = true;
            }
        }
        finally
        {
            if (semaphoreHeld)
                _semaphore.Release();
        }
    }

    // -------------------------------------------------------------------------
    // ReportRateLimit: Thread-safe — Đánh dấu key bị 429/503
    // Gọi hàm này KHÔNG cần await — cập nhật in-place ngay lập tức
    // -------------------------------------------------------------------------
    public void ReportRateLimit(string apiKey, int cooldownSeconds = DefaultCooldownSeconds)
    {
        var state = _keyStates.FirstOrDefault(k => k.ApiKey == apiKey);
        if (state == null) return;

        // Không cần lock vì DateTime write là atomic trên 64-bit OS
        var until = DateTime.UtcNow.AddSeconds(cooldownSeconds);
        state.CooldownUntil = until;

        _logger.LogWarning(
            "🚫 [Pool] Key [{Preview}] bị 429/503 → Cooldown {Seconds}s đến {Until:HH:mm:ss} UTC",
            state.Preview, cooldownSeconds, until);
    }

    // -------------------------------------------------------------------------
    // GetHealthyKeyCount: Số key đang khỏe mạnh (không cooldown)
    // -------------------------------------------------------------------------
    public int GetHealthyKeyCount()
        => _keyStates.Count(k => !k.IsOnCooldown);

    // -------------------------------------------------------------------------
    // GetPoolStats: Diagnostics — thông tin đầy đủ toàn bộ pool
    // -------------------------------------------------------------------------
    public IReadOnlyList<ApiKeyPoolStatDto> GetPoolStats()
    {
        var now = DateTime.UtcNow;
        return _keyStates.Select((k, i) => new ApiKeyPoolStatDto
        {
            Index = i,
            KeyPreview = k.Preview,
            IsOnCooldown = k.IsOnCooldown,
            CooldownRemainingSeconds = k.IsOnCooldown
                ? (int)Math.Ceiling((k.CooldownUntil - now).TotalSeconds)
                : 0,
            LastUsedAt = k.LastUsedAt,
            UsageCount = k.UsageCount
        }).ToList().AsReadOnly();
    }
}

// =============================================================================
// DTO cho diagnostics endpoint /api/gemini/pool-stats
// =============================================================================
public sealed class ApiKeyPoolStatDto
{
    public int Index { get; init; }
    public string KeyPreview { get; init; } = string.Empty;
    public bool IsOnCooldown { get; init; }
    public int CooldownRemainingSeconds { get; init; }
    public DateTime LastUsedAt { get; init; }
    public long UsageCount { get; init; }
}

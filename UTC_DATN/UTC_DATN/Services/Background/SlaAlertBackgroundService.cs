using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Background
{
    /// <summary>
    /// Background service chạy định kỳ để kiểm tra SLA status thay đổi
    /// và gửi alert cho recruiter
    /// </summary>
    public class SlaAlertBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SlaAlertBackgroundService> _logger;
        private readonly int _intervalMinutes = 5; // Chạy mỗi 5 phút

        public SlaAlertBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SlaAlertBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 SLA Alert Background Service khởi động");

            // Delay ban đầu 30 giây để app startup xong rồi mới chạy
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var slaAlertService = scope.ServiceProvider.GetRequiredService<ISlaAlertService>();
                        await slaAlertService.CheckAndSendSlaAlertsAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Lỗi trong SLA Alert Background Service: {ex.Message}");
                }

                // Chờ interval trước khi chạy lần tiếp theo
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }

            _logger.LogInformation("🛑 SLA Alert Background Service dừng lại");
        }
    }
}

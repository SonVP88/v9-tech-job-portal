using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UTC_DATN.Services.Background
{
    public class JobExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Run every 1 minute for testing

        public JobExpirationService(IServiceProvider serviceProvider, ILogger<JobExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobExpirationService is running.");

            // Run immediately on start
            try
            {
                await CheckExpiredJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in JobExpirationService during initial check.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);

                    _logger.LogInformation("JobExpirationService checking for expired jobs...");
                    await CheckExpiredJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in JobExpirationService.");
                }
            }

            _logger.LogInformation("JobExpirationService is stopping.");
        }

        private async Task CheckExpiredJobsAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<UTC_DATNContext>();

                var now = DateTime.UtcNow;

                // Find jobs that are OPEN and past their Deadline
                // Note: Deadline is stored in UTC or user local time? Assuming UTC for now or server time consistent.
                // Assuming Deadline is just date or datetime.
                
                var expiredJobs = await context.Jobs
                    .Where(j => j.Status == "OPEN" && !j.IsDeleted && j.Deadline.HasValue && j.Deadline < now)
                    .ToListAsync(stoppingToken);

                if (expiredJobs.Any())
                {
                    _logger.LogInformation($"Found {expiredJobs.Count} expired jobs. Closing them...");

                    foreach (var job in expiredJobs)
                    {
                        job.Status = "CLOSED";
                        job.ClosedAt = now; // Mark the actual closed time
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Expired jobs closed successfully.");
                }
                else
                {
                    _logger.LogInformation("No expired jobs found.");
                }
            }
        }
    }
}

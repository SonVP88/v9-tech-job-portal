using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace UTC_DATN.Services.Background
{
    public class BackgroundProcessingService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<BackgroundProcessingService> _logger;

        public BackgroundProcessingService(IBackgroundTaskQueue taskQueue, ILogger<BackgroundProcessingService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundProcessingService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    try
                    {
                        await workItem(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing background work item.");
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dequeueing background work item.");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("BackgroundProcessingService stopping.");
        }
    }
}

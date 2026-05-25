using DataIngestorService.Core.Orchestration.Factories;

namespace DataIngestorService.Host.Workers;

public class DataIngestorWorker(
    ILogger<DataIngestorWorker> logger,
    IDataIngestionOrchestratorFactory dataIngestionOrchestratorFactory) : BackgroundService
{
    private const int DefaultDelayMs = 30 * 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            try
            {
                using var scopedClient = dataIngestionOrchestratorFactory.CreateService();
                var success = await scopedClient.Service.IngestCycleAsync(stoppingToken);

                if (success)
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                    logger.LogInformation("{ServiceName} successfully finished data fetching and publishing.", scopedClient.Service.GetType().Name);
                    }
                }
                else
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("{ServiceName} failed to fetch and publish data", scopedClient.Service.GetType().Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ServiceName} failed to read data from WeakApp.", nameof(DataIngestorWorker));
            }
            
            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(DefaultDelayMs, stoppingToken);
            }
        }
    }
}

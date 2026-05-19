using DataIngestorService.Core.Contracts.Mesaging;
using DataIngestorService.Core.Contracts.WeakApp;
using DataIngestorService.Core.Mappings;
using DataIngestorService.Core.Orchestration.Contracts;
using Microsoft.Extensions.Logging;

namespace DataIngestorService.Core.Orchestration;

public class DataIngestionOrchestrator : IDataIngestionOrchestrator
{
    private readonly IWeakAppClient _weakAppClient;
    private readonly IDataPublisher _dataPublisher;
    private readonly ILogger<DataIngestionOrchestrator> _logger;

    public DataIngestionOrchestrator(
        IWeakAppClient weakAppClient,
        IDataPublisher dataPublisher,
        ILogger<DataIngestionOrchestrator> logger)
    {
        _weakAppClient = weakAppClient;
        _dataPublisher = dataPublisher;
        _logger = logger;
    }

    public async Task<bool> IngestCycleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Starting ingestion cycle...");
            }

            var readings = await _weakAppClient.FetchReadingsAsync(cancellationToken);

            if (readings.Count == 0)
            {
                _logger.LogInformation("No readings to process in this cycle.");
                return true;
            }

            var publishedCount = 0;
            foreach (var reading in readings)
            {
                var message = reading!.ToIngestedDataMessage();

                await _dataPublisher.PublishAsync(message, cancellationToken);
                publishedCount++;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Published message {EventId} for reading.",
                        message.EventId);
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Ingestion cycle completed: {PublishedCount}/{TotalCount} messages published",
                    publishedCount, readings.Count);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Ingestion cycle cancelled (shutdown requested).");
            }

            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Exception occurred during ingestion cycle.");
            }

            return false;
        }
    }
}

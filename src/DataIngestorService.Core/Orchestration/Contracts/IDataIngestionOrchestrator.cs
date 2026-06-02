namespace DataIngestorService.Core.Orchestration.Contracts;

public interface IDataIngestionOrchestrator
{
    Task<bool> IngestCycleAsync(CancellationToken cancellationToken = default);
}

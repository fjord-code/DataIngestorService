using DataIngestorService.Core.Contracts.WeakApp.Dto;

namespace DataIngestorService.Core.Contracts.WeakApp;

public interface IWeakAppClient
{
    Task<List<WeakAppReading>> FetchReadingsAsync(CancellationToken cancellationToken = default);
}

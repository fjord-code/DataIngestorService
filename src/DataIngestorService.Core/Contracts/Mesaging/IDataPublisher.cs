namespace DataIngestorService.Core.Contracts.Mesaging;

public interface IDataPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}

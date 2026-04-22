using DataIngestorService.Core.Contracts.Mesaging;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace DataIngestorService.Infrastructure.Messaging;

public class WolverineDataPublisher : IDataPublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<WolverineDataPublisher> _logger;

    public WolverineDataPublisher(
        IMessageBus messageBus,
        ILogger<WolverineDataPublisher> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            await _messageBus.PublishAsync(message);

            _logger.LogDebug("Message of type {MessageType} published successfully",
                typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType}",
                typeof(T).Name);
            throw;
        }
    }
}

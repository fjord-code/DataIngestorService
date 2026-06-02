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
        await _messageBus.PublishAsync(message);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Message of type {MessageType} published successfully", typeof(T).Name);
        }
    }
}

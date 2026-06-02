using DataIngestorService.Core.Contracts.Options;

namespace DataIngestorService.Core.Extensions.Contracts.Options;

public static class RabbitMQOptionsExtensions
{
    public static string GetConnectionString(this RabbitMQOptions options)
    {
        return $"amqp://{Uri.EscapeDataString(options.UserName)}:{Uri.EscapeDataString(options.Password)}@{options.HostName}:{options.Port}";
    }
}
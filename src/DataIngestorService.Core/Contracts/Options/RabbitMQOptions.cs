using System.ComponentModel.DataAnnotations;

namespace DataIngestorService.Core.Contracts.Options;

public class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";

    public required string HostName { get; set; }

    public int? Port { get; set; }

    public required string UserName { get; set; }

    public required string Password { get; set; }

    public required string ExchangeName { get; set; }

    public required string QueueName { get; set; }

    public required string QueueKey { get; set; }
}

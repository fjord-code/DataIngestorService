using System.ComponentModel.DataAnnotations;

namespace DataIngestorService.Core.Contracts.Options;

public class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";

    [Required]
    public string HostName { get; set; }

    public int Port { get; set; }

    [Required]
    public string UserName { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string ExchangeName { get; set; }

    [Required]
    public string QueueName { get; set; }

    [Required]
    public string QueueKey { get; set; }
}

namespace DataIngestorService.Core.Contracts.Options;

public class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ExchangeName { get; set; } = string.Empty;
}

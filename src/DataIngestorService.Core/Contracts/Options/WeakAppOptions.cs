namespace DataIngestorService.Core.Contracts.Options;

public class WeakAppOptions
{
    public const string SectionName = "WeakApp";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 3;
    public int CircuitBreakerFailureCount { get; set; } = 5;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}

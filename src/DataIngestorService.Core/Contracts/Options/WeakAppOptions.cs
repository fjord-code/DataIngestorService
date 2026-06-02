using DataIngestorService.Core.Constants;
using System.ComponentModel.DataAnnotations;

namespace DataIngestorService.Core.Contracts.Options;

public class WeakAppOptions
{
    public const string SectionName = WeakAppConstants.AppName;
    public string? BaseUrl { get; set; }
    public string? MeteringEndpoint { get; set; }
    public double? TimeoutSeconds { get; set; }
    public int? RetryCount { get; set; }
    public int? CircuitBreakerFailureCount { get; set; }
    public int? CircuitBreakerBreakDurationSeconds { get; set; }
    public string? ApiKey { get; set; }
}

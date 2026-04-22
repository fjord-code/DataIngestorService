using DataIngestorService.Core.Constants;
using DataIngestorService.Core.Contracts.Options;
using DataIngestorService.Core.Contracts.WeakApp;
using DataIngestorService.Core.Contracts.WeakApp.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace DataIngestorService.Infrastructure.Api;

public sealed class WeakAppClient : IWeakAppClient
{
    private readonly HttpClient _httpClient;
    private readonly WeakAppOptions _options;
    private readonly ILogger<WeakAppClient> _logger;

    public WeakAppClient(
        HttpClient httpClient,
        IOptions<WeakAppOptions> options,
        ILogger<WeakAppClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<WeakAppReading>> FetchReadingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching data from {AppName}...", WeakAppConstants.AppName);

        var response = await _httpClient.GetAsync(_options.MeteringEndpoint, cancellationToken);

        response.EnsureSuccessStatusCode();

        var readings = await response.Content.ReadFromJsonAsync<List<WeakAppReading>>(cancellationToken: cancellationToken)
            ?? new List<WeakAppReading>();

        _logger.LogInformation("Successfully fetched {Count} readings from {AppName}.", readings.Count, WeakAppConstants.AppName);

        return readings;
    }
}

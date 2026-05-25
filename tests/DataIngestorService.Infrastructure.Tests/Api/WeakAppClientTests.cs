using DataIngestorService.Core.Constants;
using DataIngestorService.Core.Contracts.Options;
using DataIngestorService.Core.Contracts.WeakApp.Dto;
using DataIngestorService.Infrastructure.Api;
using JasperFx.CodeGeneration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DataIngestorService.Infrastructure.Tests.Api;

public class WeakAppClientTests
{
    private readonly Mock<IOptions<WeakAppOptions>> _optionsMock;
    private readonly Mock<ILogger<WeakAppClient>> _loggerMock;
    private readonly WeakAppOptions _options;

    public WeakAppClientTests()
    {
        _optionsMock = new Mock<IOptions<WeakAppOptions>>();
        _loggerMock = new Mock<ILogger<WeakAppClient>>();
        _options = new WeakAppOptions()
        {
            BaseUrl = WeakAppConstants.DefaultBaseUrl,
            MeteringEndpoint = WeakAppConstants.DefaultMeteringEndpoint,
            TimeoutSeconds = 10,
            RetryCount = 3,
            CircuitBreakerFailureCount = 5,
            CircuitBreakerBreakDurationSeconds = 30,
            ApiKey = "api-key-123",
        };

        _optionsMock.Setup(x => x.Value).Returns(_options);
    }

    #region FetchReadingsAsync Tests

    [Fact]
    public async Task FetchReadingsAsync_WithSuccessfulResponse_ReturnsDeserializedReadings()
    {
        // Arrange
        List<WeakAppReading> expectedReadings = CreateExpectedReadings();
        var jsonResponse = JsonSerializer.Serialize(expectedReadings);

        var handlerMock = SetupHttpMessageHandler(
            WeakAppConstants.DefaultMeteringEndpoint,
            HttpStatusCode.OK,
            jsonResponse);

        var httpClient = CreateWeakAppHttpClient(handlerMock);

        var sut = new WeakAppClient(
            httpClient,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        var result = await sut.FetchReadingsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedReadings.Count, result.Count);
    }

    private static List<WeakAppReading> CreateExpectedReadings()
    {
        return new List<WeakAppReading>
        {
            new(
                "motion",
                "Office",
                JsonElement.Parse(@"
                {
                    ""motionDetected"": false
                }")),
            new(
                "energy",
                "Garage",
                JsonElement.Parse(@"
                {
                    ""energy"": 795.61
                }")),
        };
    }

    [Fact]
    public async Task FetchReadingsAsync_WithNullJsonContent_ReturnsEmptyList()
    {
        // Arrange
        var handlerMock = SetupHttpMessageHandler(
            WeakAppConstants.DefaultMeteringEndpoint,
            HttpStatusCode.OK,
            "null");

        var httpClient = CreateWeakAppHttpClient(handlerMock);

        var sut = new WeakAppClient(
            httpClient,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        var result = await sut.FetchReadingsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchReadingsAsync_WithEmptyJsonArray_ReturnsEmptyList()
    {
        // Arrange
        var handlerMock = SetupHttpMessageHandler(
            WeakAppConstants.DefaultMeteringEndpoint,
            HttpStatusCode.OK,
            "[]");

        var httpClient = CreateWeakAppHttpClient(handlerMock);

        var sut = new WeakAppClient(
            httpClient,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        var result = await sut.FetchReadingsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchReadingsAsync_WithHttpErrorStatusCode_ThrowsHttpRequestException()
    {
        // Arrange
        var handlerMock = SetupHttpMessageHandler(
            WeakAppConstants.DefaultMeteringEndpoint,
            HttpStatusCode.InternalServerError,
            string.Empty);

        var httpClient = CreateWeakAppHttpClient(handlerMock);

        var sut = new WeakAppClient(
            httpClient,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.FetchReadingsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task FetchReadingsAsync_RequestsCorrectEndpoint()
    {
        // Arrange
        string? capturedUri = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                capturedUri = req.RequestUri?.ToString())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        var httpClient = CreateWeakAppHttpClient(handlerMock);

        var sut = new WeakAppClient(
            httpClient,
            _optionsMock.Object,
            _loggerMock.Object);

        // Act
        await sut.FetchReadingsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(capturedUri);
        Assert.Contains(WeakAppConstants.DefaultMeteringEndpoint, capturedUri);
    }

    #endregion

    #region Helper Methods

    private static HttpClient CreateWeakAppHttpClient()
    {
        return new HttpClient()
        {
            BaseAddress = new Uri(WeakAppConstants.DefaultBaseUrl),
        };
    }

    private static HttpClient CreateWeakAppHttpClient(Mock<HttpMessageHandler> handlerMock)
    {
        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(WeakAppConstants.DefaultBaseUrl),

        };
    }

    private static Mock<HttpMessageHandler> SetupHttpMessageHandler(
        string expectedEndpoint,
        HttpStatusCode statusCode,
        string responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith(expectedEndpoint) == true),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }

    #endregion
}

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
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptions<WeakAppOptions>> _optionsMock;
    private readonly Mock<ILogger<WeakAppClient>> _loggerMock;
    private readonly WeakAppOptions _options;

    public WeakAppClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _optionsMock = new Mock<IOptions<WeakAppOptions>>();
        _loggerMock = new Mock<ILogger<WeakAppClient>>();
        _options = new WeakAppOptions();

        _optionsMock.Setup(x => x.Value).Returns(_options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        // Arrange
        HttpClient httpClient = CreateWeakAppHttpClient();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient);

        // Act
        var sut = new WeakAppClient(
            _httpClientFactoryMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void Constructor_CreatesHttpClientWithCorrectNamedClient()
    {
        // Arrange
        var httpClient = CreateWeakAppHttpClient();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient)
            .Verifiable();

        // Act
        _ = new WeakAppClient(
            _httpClientFactoryMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Assert
        _httpClientFactoryMock.Verify(
            x => x.CreateClient(WeakAppConstants.AppName),
            Times.Once);
    }

    #endregion

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
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient);

        var sut = new WeakAppClient(
            _httpClientFactoryMock.Object,
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
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient);

        var sut = new WeakAppClient(
            _httpClientFactoryMock.Object,
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
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient);

        var sut = new WeakAppClient(
            _httpClientFactoryMock.Object,
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
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient);

        var sut = new WeakAppClient(
            _httpClientFactoryMock.Object,
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
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(WeakAppConstants.AppName))
            .Returns(httpClient);

        var sut = new WeakAppClient(
            _httpClientFactoryMock.Object,
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
            BaseAddress = new Uri(WeakAppConstants.DefaultBaseUrl)
        };
    }

    private Mock<HttpMessageHandler> SetupHttpMessageHandler(
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

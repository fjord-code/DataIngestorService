using DataIngestorService.Core.Contracts.Mesaging;
using DataIngestorService.Core.Contracts.Mesaging.Dto;
using DataIngestorService.Core.Contracts.WeakApp;
using DataIngestorService.Core.Contracts.WeakApp.Dto;
using DataIngestorService.Core.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace DataIngestorService.Core.Tests.Orchestration;

public class DataIngestionOrchestratorTests
{
    private readonly Mock<IWeakAppClient> _weakAppClientMock;
    private readonly Mock<IDataPublisher> _dataPublisherMock;
    private readonly Mock<ILogger<DataIngestionOrchestrator>> _loggerMock;
    private readonly DataIngestionOrchestrator sut;

    public DataIngestionOrchestratorTests()
    {
        _weakAppClientMock = new Mock<IWeakAppClient>();
        _dataPublisherMock = new Mock<IDataPublisher>();
        _loggerMock = new Mock<ILogger<DataIngestionOrchestrator>>();

        sut = new DataIngestionOrchestrator(
            _weakAppClientMock.Object,
            _dataPublisherMock.Object,
            _loggerMock.Object);
    }

    #region IngestCycleAsync Tests

    [Fact]
    public async Task IngestCycleAsync_WithSuccessfulFetchAndPublish_ReturnsTrue()
    {
        // Arrange
        var expectedReadings = CreateTestReadings();
        _weakAppClientMock
            .Setup(x => x.FetchReadingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReadings);

        _dataPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<IngestedDataMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await sut.IngestCycleAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        _weakAppClientMock.Verify(
            x => x.FetchReadingsAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
        _dataPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<IngestedDataMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(expectedReadings.Count));
    }

    [Fact]
    public async Task IngestCycleAsync_WithEmptyReadings_ReturnsTrueWithoutPublishing()
    {
        // Arrange
        _weakAppClientMock
            .Setup(x => x.FetchReadingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeakAppReading>());

        // Act
        var result = await sut.IngestCycleAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        _weakAppClientMock.Verify(
            x => x.FetchReadingsAsync(It.IsAny<CancellationToken>())
            , Times.Once);
        _dataPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<IngestedDataMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IngestCycleAsync_WithOperationCanceledException_RethrowsException()
    {
        // Arrange
        _weakAppClientMock
            .Setup(x => x.FetchReadingsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.IngestCycleAsync(CancellationToken.None));

        _dataPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<IngestedDataMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IngestCycleAsync_WhenPublishFails_ReturnsFalse()
    {
        // Arrange
        var readings = CreateTestReadings();
        _weakAppClientMock
            .Setup(x => x.FetchReadingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        _dataPublisherMock
            .SetupSequence(x => x.PublishAsync(It.IsAny<IngestedDataMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new InvalidOperationException("Publish failed"));

        // Act
        var result = await sut.IngestCycleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        _dataPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<IngestedDataMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region Helper Methods

    private static List<WeakAppReading> CreateTestReadings()
    {
        return new List<WeakAppReading>
        {
            new(
                "motion",
                "Office",
                JsonElement.Parse(@"{ ""motionDetected"": false }")),
            new(
                "energy",
                "Garage",
                JsonElement.Parse(@"{ ""energy"": 795.61 }")),
        };
    }

    #endregion
}

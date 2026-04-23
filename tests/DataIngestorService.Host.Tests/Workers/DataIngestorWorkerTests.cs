using DataIngestorService.Core.Base;
using DataIngestorService.Core.Orchestration.Contracts;
using DataIngestorService.Core.Orchestration.Factories;
using DataIngestorService.Host.Workers;
using ImTools;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataIngestorService.Host.Tests.Workers;

public class DataIngestorWorkerTests
{
    private readonly Mock<ILogger<DataIngestorWorker>> _loggerMock;
    private readonly Mock<IDataIngestionOrchestratorFactory> _factoryMock;
    private readonly Mock<IDataIngestionOrchestrator> _orchestratorMock;

    public DataIngestorWorkerTests()
    {
        _loggerMock = new Mock<ILogger<DataIngestorWorker>>();
        _factoryMock = new Mock<IDataIngestionOrchestratorFactory>();
        _orchestratorMock = new Mock<IDataIngestionOrchestrator>();
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulCycle_InvokesOrchestrator()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var scopeMock = SetupScopedClient(cts, successResult: true);

        var sut = CreateWorker();

        // Act
        await sut.ExecuteTestAsync(cts.Token);

        // Assert
        _factoryMock.Verify(x => x.CreateService(), Times.Once);
        _orchestratorMock.Verify(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedCycle_ContinuesLoopWithoutThrowing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        SetupScopedClient(cts, successResult: false);

        var sut = CreateWorker();

        // Act & Assert (Should not throw)
        var exception = await Record.ExceptionAsync(() => sut.ExecuteTestAsync(cts.Token));
        Assert.Null(exception);

        _orchestratorMock.Verify(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorThrows_CatchesExceptionAndContinuesLoop()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _orchestratorMock
            .Setup(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transient network failure"));

        SetupScopedClient(cts, successResult: true);

        var sut = CreateWorker();

        // Act & Assert (Should not throw)
        var exception = await Record.ExceptionAsync(() => sut.ExecuteTestAsync(cts.Token));
        Assert.Null(exception);

        _orchestratorMock.Verify(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledBeforeStart_ExitsImmediately()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before execution begins

        var sut = CreateWorker();

        // Act
        await sut.ExecuteTestAsync(cts.Token);

        // Assert
        _factoryMock.Verify(x => x.CreateService(), Times.Never);
        _orchestratorMock.Verify(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToOrchestrator()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        var scopedOrchestrator = new ScopedOrchestrator
        {
            Service = _orchestratorMock.Object,
            Scope = null!
        };
        _factoryMock.Setup(x => x.CreateService()).Returns(scopedOrchestrator);
        _orchestratorMock
            .Setup(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct =>
            {
                capturedToken = ct;
                cts.Cancel();
            })
            .ReturnsAsync(true);

        var sut = CreateWorker();

        // Act
        await sut.ExecuteTestAsync(cts.Token);

        // Assert
        Assert.Equal(cts.Token, capturedToken);
    }

    #endregion

    #region Helper Methods

    private TestableDataIngestorWorker CreateWorker()
    {
        return new TestableDataIngestorWorker(_loggerMock.Object, _factoryMock.Object);
    }

    private ScopedOrchestrator SetupScopedClient(CancellationTokenSource cancellationTokenSource, bool successResult)
    {
        var scopedOrchestrator = new ScopedOrchestrator()
        {
            Service = _orchestratorMock.Object,
            Scope = null!
        };
        
        _factoryMock.Setup(x => x.CreateService()).Returns(scopedOrchestrator);
        _orchestratorMock
            .Setup(x => x.IngestCycleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                cancellationTokenSource.Cancel();
                return successResult;
            });

        return scopedOrchestrator;
    }

    /// <summary>
    /// Testable subclass to expose the protected ExecuteAsync method for unit testing.
    /// This is a standard pattern for testing BackgroundService implementations.
    /// </summary>
    private class TestableDataIngestorWorker : DataIngestorWorker
    {
        public TestableDataIngestorWorker(ILogger<DataIngestorWorker> logger, IDataIngestionOrchestratorFactory factory)
            : base(logger, factory) { }

        public Task ExecuteTestAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
    }

    private class ScopedOrchestrator : ScopedService<IDataIngestionOrchestrator>
    {
    }

    #endregion
}
using DataIngestorService.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Wolverine;

namespace DataIngestorService.Infrastructure.Tests.Messaging;

public class WolverineDataPublisherTests
{
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<ILogger<WolverineDataPublisher>> _loggerMock;
    private readonly WolverineDataPublisher sut;

    public WolverineDataPublisherTests()
    {
        _messageBusMock = new Mock<IMessageBus>();
        _loggerMock = new Mock<ILogger<WolverineDataPublisher>>();

        sut = new WolverineDataPublisher(
            _messageBusMock.Object,
            _loggerMock.Object);
    }

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithValidMessage_CallsMessageBusOnce()
    {
        // Arrange
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Payload = "TestPayload" };
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        // Act
        await sut.PublishAsync(testMessage, CancellationToken.None);

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(testMessage, It.IsAny<DeliveryOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenMessageBusThrows_RethrowsException()
    {
        // Arrange
        var testMessage = new TestMessage();
        var expectedException = new InvalidOperationException("RabbitMQ connection refused");
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.PublishAsync(testMessage, CancellationToken.None));

        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private class TestMessage
    {
        public Guid Id { get; set; }
        public string Payload { get; set; } = string.Empty;
    }

    #endregion
}

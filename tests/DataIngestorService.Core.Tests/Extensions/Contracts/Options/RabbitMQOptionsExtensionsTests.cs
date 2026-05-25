using DataIngestorService.Core.Contracts.Options;
using DataIngestorService.Core.Extensions.Contracts.Options;

namespace DataIngestorService.Core.Tests.Extensions.Contracts.Options;

public class RabbitMQOptionsExtensionsTests
{
    [Fact]
    public void GetConnectionString_WithStandardCredentials_ReturnsExpectedConnectionString()
    {
        // Arrange
        var options = new RabbitMQOptions
        {
            UserName = "guest",
            Password = "guestPassword123",
            HostName = "localhost",
            Port = 5672,
            ExchangeName = "myExchange",
            QueueName = "myQueue",
            QueueKey = "myQueueKey"
        };

        var expectedConnectionString = "amqp://guest:guestPassword123@localhost:5672";

        // Act
        var result = options.GetConnectionString();

        // Assert
        Assert.Equal(expectedConnectionString, result);
    }

    [Fact]
    public void GetConnectionString_WithSpecialCharactersInCredentials_ReturnsEscapedConnectionString()
    {
        // Arrange
        var options = new RabbitMQOptions
        {
            UserName = "user/name@domain",
            Password = "pass#word?",
            HostName = "rabbitmq.local",
            Port = 5672,
            ExchangeName = "myExchange",
            QueueName = "myQueue",
            QueueKey = "myQueueKey"
        };

        var expectedConnectionString = "amqp://user%2Fname%40domain:pass%23word%3F@rabbitmq.local:5672";

        // Act
        var result = options.GetConnectionString();

        // Assert
        Assert.Equal(expectedConnectionString, result);
    }
}


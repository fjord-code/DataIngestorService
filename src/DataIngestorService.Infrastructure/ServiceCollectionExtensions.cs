using DataIngestorService.Core.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Serilog;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace DataIngestorService.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the WeakApp HttpClient with Polly Resilience.
    /// </summary>
    public static IServiceCollection AddWeakAppClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WeakAppOptions>()
            .Bind(configuration.GetSection(WeakAppOptions.SectionName))
            .ValidateDataAnnotations();

        var options = configuration.Get<WeakAppOptions>()!;

        var timeoutStrategyOptions = new TimeoutStrategyOptions()
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        var retryStrategyOptions = new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>(),
            MaxRetryAttempts = options.RetryCount,
            DelayGenerator = (context) =>
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, context.AttemptNumber));
                return new ValueTask<TimeSpan?>(delay);
            }
        };

        var circuitBreakerStrategyOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>()
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<Exception>(),
            FailureRatio = 1.0,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = options.CircuitBreakerFailureCount,
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds)
        };

        services
            .AddHttpClient("WeakApp", client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddResilienceHandler("weakapp-resilience", (resiliencePipelinebuilder) =>
            {
                resiliencePipelinebuilder
                    .AddTimeout(timeoutStrategyOptions)
                    .AddRetry(retryStrategyOptions)
                    .AddCircuitBreaker(circuitBreakerStrategyOptions);
            });

        return services;
    }

    /// <summary>
    /// Configures Wolverine RabbitMQ Messaging.
    /// </summary>
    public static IServiceCollection AddWolverineMessaging(this IServiceCollection services, IHostBuilder hostBuilder, IConfiguration configuration)
    {
        services.AddOptions<RabbitMQOptions>()
            .Bind(configuration.GetSection(RabbitMQOptions.SectionName))
            .ValidateDataAnnotations();

        var options = configuration.Get<RabbitMQOptions>()!;

        hostBuilder.UseWolverine(wolverineOptions =>
        {
            wolverineOptions.UseRabbitMq(c =>
            {
                c.HostName = options.HostName;
                c.Port = options.Port;
                c.UserName = options.UserName;
                c.Password = options.Password;
            })
            .DeclareExchange(options.ExchangeName, exchange =>
            {
                exchange.ExchangeType = ExchangeType.Direct;
            });

            wolverineOptions.OnException<Exception>().MoveToErrorQueue();
        });

        return services;
    }

    public static IHostBuilder UseCustomSerilog(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        hostBuilder.UseSerilog();
        return hostBuilder;
    }
}

using DataIngestorService.Core.Constants;
using DataIngestorService.Core.Contracts.Mesaging;
using DataIngestorService.Core.Contracts.Mesaging.Dto;
using DataIngestorService.Core.Contracts.Options;
using DataIngestorService.Core.Contracts.WeakApp;
using DataIngestorService.Core.Orchestration;
using DataIngestorService.Core.Orchestration.Contracts;
using DataIngestorService.Core.Orchestration.Factories;
using DataIngestorService.Infrastructure.Api;
using DataIngestorService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Serilog;
using System.Data;
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

        var options = configuration
            .GetSection(WeakAppOptions.SectionName)
            .Get<WeakAppOptions>()!;

        var timeoutStrategyOptions = new TimeoutStrategyOptions()
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        var retryStrategyOptions = new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(response => !response.IsSuccessStatusCode),
            MaxRetryAttempts = options.RetryCount,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
        };

        var circuitBreakerStrategyOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>()
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(response => !response.IsSuccessStatusCode),
            FailureRatio = 0.9,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = options.CircuitBreakerFailureCount,
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds)
        };

        services
            .AddHttpClient<WeakAppClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            })
            .AddResilienceHandler(WeakAppConstants.AppName, (resiliencePipelinebuilder) =>
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

        var options = configuration
            .GetSection(RabbitMQOptions.SectionName)
            .Get<RabbitMQOptions>()!;

        hostBuilder.UseWolverine(wolverineOptions =>
        {
            wolverineOptions
                .PublishMessage<IngestedDataMessage>()
                .ToRabbitExchange(
                    options.ExchangeName,
                    exchange =>
                    {
                        exchange.ExchangeType = ExchangeType.Fanout;
                        exchange.BindQueue(options.QueueName, options.QueueKey);
                    });

            wolverineOptions
                .UseRabbitMq(c =>
                {
                    c.HostName = options.HostName;
                    c.Port = options.Port;
                    c.UserName = options.UserName;
                    c.Password = options.Password;
                })
                .AutoProvision();

            wolverineOptions
                .OnException<Exception>()
                .MoveToErrorQueue();
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

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        return services
            .AddScoped<IWeakAppClient>(provider => provider.GetRequiredService<WeakAppClient>())
            .AddScoped<IDataPublisher, WolverineDataPublisher>()
            .AddScoped<IDataIngestionOrchestrator, DataIngestionOrchestrator>()
            .AddSingleton<IDataIngestionOrchestratorFactory, DataIngestionOrchestratorFactory>();
    }
}

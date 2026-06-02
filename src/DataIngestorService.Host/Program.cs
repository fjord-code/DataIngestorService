using DataIngestorService.Core.Contracts.Options;
using DataIngestorService.Core.Extensions.Contracts.Options;
using DataIngestorService.Host.Extensions;
using DataIngestorService.Host.Workers;
using DataIngestorService.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using RabbitMQ.Client;
using System.Diagnostics.CodeAnalysis;

namespace DataIngestorService.Host;

[ExcludeFromCodeCoverage]
public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseCustomSerilog(builder.Configuration);

        builder.Services.AddHostedService<DataIngestorWorker>();
        builder.Services.AddWeakAppClient(builder.Configuration);
        builder.Services.AddWolverineMessaging(builder.Host, builder.Configuration);

        var rabbitMqOptions = builder.Configuration
            .GetSection(RabbitMQOptions.SectionName)
            .Get<RabbitMQOptions>()!;

        builder.Services.AddHealthChecks()
            .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
            .AddRabbitMQ(
                async _ => await new ConnectionFactory
                {
                    Uri = new Uri(rabbitMqOptions.GetConnectionString())
                }.CreateConnectionAsync(),
                name: "rabbitmq",
                tags: ["ready"]);

        builder.AddOtlp();

        builder.Services.AddInfrastructureServices();

        var app = builder.Build();

        app.MapGet("/health/live", () => Results.Ok());
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapPrometheusScrapingEndpoint("/metrics");

        app.Run();
    }
}
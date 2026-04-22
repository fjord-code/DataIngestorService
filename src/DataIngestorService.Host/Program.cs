using DataIngestorService.Host.Workers;
using DataIngestorService.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCustomSerilog(builder.Configuration);

builder.Services.AddHostedService<DataIngestorWorker>();
builder.Services.AddWeakAppClient(builder.Configuration);
builder.Services.AddWolverineMessaging(builder.Host, builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddRabbitMQ();

builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddPrometheusExporter());

builder.Services.AddInfrastructureServices();

var config = builder.Configuration;

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok());
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

using DataIngestorService.Host.Workers;
using DataIngestorService.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder();

builder.Host.UseCustomSerilog(builder.Configuration);

builder.Services.AddHostedService<DataIngestorWorker>();
builder.Services.AddWeakAppClient(builder.Configuration);
builder.Services.AddWolverineMessaging(builder.Host, builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddRabbitMQ()
    .AddUrlGroup(new Uri(builder.Configuration["WeakApp:BaseUrl"]!), "weakapp");

builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddPrometheusExporter());

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok());
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

using System.Text.Json;

namespace DataIngestorService.Core.Contracts.WeakApp.Dto;

public record WeakAppReading(
    string Type,
    string Name,
    JsonElement Payload);

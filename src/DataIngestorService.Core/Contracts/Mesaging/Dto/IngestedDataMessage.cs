using DataIngestorService.Core.Contracts.WeakApp.Dto;
using System.Text.Json;

namespace DataIngestorService.Core.Contracts.Mesaging.Dto;

public record IngestedDataMessage
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = null!;
    public string Name { get; init; } = null!;
    public JsonElement Payload { get; init; } = new JsonElement();
};

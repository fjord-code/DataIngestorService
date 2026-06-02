using DataIngestorService.Core.Contracts.Mesaging.Dto;
using DataIngestorService.Core.Contracts.WeakApp.Dto;

namespace DataIngestorService.Core.Mappings;

public static class MappingExtensions
{
    public static IngestedDataMessage ToIngestedDataMessage(this WeakAppReading reading)
    {
        return new IngestedDataMessage()
        {
            Type = reading.Type,
            Name = reading.Name,
            Payload = reading.Payload,
        };
    }
}

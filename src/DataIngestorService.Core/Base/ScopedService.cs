using Microsoft.Extensions.DependencyInjection;

namespace DataIngestorService.Core.Base;

public class ScopedService<T> : IDisposable
{
    public T Service { get; init; }

    public required IServiceScope Scope { get; init; }

    public void Dispose()
    {
        Scope?.Dispose();
        GC.SuppressFinalize(this);
    }
}
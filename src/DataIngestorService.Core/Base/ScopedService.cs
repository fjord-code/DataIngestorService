using Microsoft.Extensions.DependencyInjection;

namespace DataIngestorService.Core.Base;

public class ScopedService<T> : IDisposable
{
    public T Service { get; init; }

    public IServiceScope Scope { get; init; }

    public void Dispose()
    {
        Scope?.Dispose();
    }
}
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DataIngestorService.Core.Base;

public interface IAbstractFactory<T> where T : class
{
    ScopedService<T> CreateService();
}

[ExcludeFromCodeCoverage]
public abstract class AbstractFactory<T> : IAbstractFactory<T>
    where T : class
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    protected AbstractFactory(
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public ScopedService<T> CreateService()
    {
        var scope = _serviceScopeFactory.CreateScope();
        return new ScopedService<T>
        {
            Service = scope.ServiceProvider.GetRequiredService<T>(),
            Scope = scope,
        };
    }
}
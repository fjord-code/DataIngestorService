using Microsoft.Extensions.DependencyInjection;

namespace DataIngestorService.Core.Base;

public interface IAbstractFactory<T> where T : class
{
    ScopedService<T> CreateService();
}

public abstract class AbstractFactory<T> : IAbstractFactory<T>
    where T : class
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AbstractFactory(
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
using DataIngestorService.Core.Base;
using DataIngestorService.Core.Orchestration.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DataIngestorService.Core.Orchestration.Factories;

public interface IDataIngestionOrchestratorFactory : IAbstractFactory<IDataIngestionOrchestrator>
{ }

public sealed class DataIngestionOrchestratorFactory : AbstractFactory<IDataIngestionOrchestrator>, IDataIngestionOrchestratorFactory
{
    public DataIngestionOrchestratorFactory(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
    {
    }
}

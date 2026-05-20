using DataIngestorService.Core.Base;
using DataIngestorService.Core.Orchestration.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DataIngestorService.Core.Orchestration.Factories;

public interface IDataIngestionOrchestratorFactory : IAbstractFactory<IDataIngestionOrchestrator>
{ }

[ExcludeFromCodeCoverage]
public sealed class DataIngestionOrchestratorFactory : AbstractFactory<IDataIngestionOrchestrator>, IDataIngestionOrchestratorFactory
{
    public DataIngestionOrchestratorFactory(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
    {
    }
}

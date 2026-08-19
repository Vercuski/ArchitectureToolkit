using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Infrastructure.HealthChecks;
using ArchitectureToolkit.Persistence.Contexts;
using ArchitectureToolkit.Presentation.API.Controllers;
using System.Reflection;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

internal static class AssemblyReferences
{
    internal static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    internal static readonly Assembly ApplicationAssembly = typeof(IQueryDbContext).Assembly;
    internal static readonly Assembly InfrastrcutureAssembly = typeof(SimpleHealthCheck).Assembly;
    internal static readonly Assembly PersistenceAssembly = typeof(QueryDbContext).Assembly;
    internal static readonly Assembly PresentationAssembly = typeof(SampleController).Assembly;
    internal static readonly Assembly TestsAssembly = typeof(DomainArchitectureTests).Assembly;
}

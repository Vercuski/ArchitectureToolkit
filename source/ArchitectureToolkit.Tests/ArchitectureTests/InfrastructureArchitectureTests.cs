using NetArchTest.Rules;
using static ArchitectureToolkit.Tests.ArchitectureTests.AssemblyReferences;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

[TestFixture]
public class InfrastructureArchitectureTests
{
    // Infrastructure currently has zero ProjectReferences — it only depends on the ASP.NET Core
    // shared framework and Azure.Identity. This test guards that invariant: nothing in Infrastructure
    // should ever need to see Application, Domain, Persistence, or Presentation types. If it does,
    // that's a sign the new code belongs somewhere else (most likely as a port in Application,
    // implemented in whichever layer actually needs the dependency).
    [Test]
    public void InfrastructureAssembly_ShouldNot_ReferenceApplicationDomainPersistenceOrPresentation()
    {
        var result = Types
            .InAssembly(InfrastrcutureAssembly)
            .ShouldNot()
            .HaveDependencyOnAll([
                "ArchitectureToolkit.Application",
                "ArchitectureToolkit.Domain",
                "ArchitectureToolkit.Persistence",
                "ArchitectureToolkit.Presentation"
            ])
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Infrastructure Types Referencing Other Layers:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }
}

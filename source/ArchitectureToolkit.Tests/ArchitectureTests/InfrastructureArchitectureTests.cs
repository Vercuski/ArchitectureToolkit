using NetArchTest.Rules;
using static ArchitectureToolkit.Tests.ArchitectureTests.AssemblyReferences;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

[TestFixture]
public class InfrastructureArchitectureTests
{
    // Infrastructure currently has zero ProjectReferences to other ArchitectureToolkit
    // assemblies — it only depends on the ASP.NET Core shared framework, EF Core/Npgsql
    // (for ApplicationIdentityDbContext, ADR-0003), and OpenIddict/ASP.NET Core Identity.
    // This test guards that invariant: nothing in Infrastructure should ever need to see
    // Application, Domain, Persistence, or Presentation types. If it does, that's a sign
    // the new code belongs somewhere else (most likely as a port in Application,
    // implemented in whichever layer actually needs the dependency).
    //
    // Uses HaveDependencyOnAny rather than HaveDependencyOnAll: a type referencing even
    // one of these four namespaces is a violation on its own — it doesn't need to
    // reference all four simultaneously to represent a real layering leak. (An earlier
    // version of this test used HaveDependencyOnAll, which only fails when a single type
    // depends on Application *and* Domain *and* Persistence *and* Presentation all at
    // once — considerably weaker than this comment always intended. Confirmed via a full
    // scan of Infrastructure's `using ArchitectureToolkit.*` statements that tightening
    // this doesn't turn up any pre-existing leak the weaker check had been missing.)
    [Test]
    public void InfrastructureAssembly_ShouldNot_ReferenceApplicationDomainPersistenceOrPresentation()
    {
        var result = Types
            .InAssembly(InfrastrcutureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([
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

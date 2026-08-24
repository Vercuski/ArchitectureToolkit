using NetArchTest.Rules;
using static ArchitectureToolkit.Tests.ArchitectureTests.AssemblyReferences;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

/// <summary>
/// ADR-0003 v1.0.2: ApplicationIdentityDbContext and the rest of
/// Infrastructure.Identity persist Identity's/OpenIddict's own stores
/// entirely within Infrastructure. This must never gain a reference to
/// Domain, Application, or Persistence — that would either duplicate
/// ICommandDbContext/IUnitOfWork's job or quietly reintroduce the same
/// layering problem Phase 3's IUserProvisioningService placement fixed.
///
/// Scoped to just the "ArchitectureToolkit.Infrastructure.Identity"
/// namespace (not the whole Infrastructure assembly) and uses
/// HaveDependencyOnAny rather than HaveDependencyOnAll, so a single
/// accidental reference to any one of these three namespaces fails the
/// test — not only a type that somehow references all three at once.
/// (InfrastructureArchitectureTests' existing assembly-wide check uses
/// HaveDependencyOnAll, which is weaker than its own comment implies; that
/// looks worth a second look, but is a separate, pre-existing concern from
/// this new, more narrowly-scoped test.)
/// </summary>
[TestFixture]
public class IdentityArchitectureTests
{
    [Test]
    public void IdentityNamespace_ShouldNot_ReferenceApplicationDomainOrPersistence()
    {
        var result = Types
            .InAssembly(InfrastrcutureAssembly)
            .That()
            .ResideInNamespace("ArchitectureToolkit.Infrastructure.Identity")
            .ShouldNot()
            .HaveDependencyOnAny([
                "ArchitectureToolkit.Application",
                "ArchitectureToolkit.Domain",
                "ArchitectureToolkit.Persistence"
            ])
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Infrastructure.Identity Types Referencing Other Layers:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }
}

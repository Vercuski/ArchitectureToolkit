using ArchitectureToolkit.Domain.Abstractions;
using NetArchTest.Rules;
using static ArchitectureToolkit.Tests.ArchitectureTests.AssemblyReferences;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

[TestFixture]
public class DomainArchitectureTests
{
    [Test]
    public void DomainEntities_Should_InheritFromTheEntityTypeAndBeSealed()
    {
        // ProjectMember is deliberately exempt: it has no synthetic Id in the
        // ERD — its primary key is the (ProjectId, UserId) composite — so it
        // intentionally does not inherit Entity/IEntity (see ProjectMember's
        // own doc comment). Excluding it here documents that as a known,
        // intentional design choice rather than leaving this rule red.
        var result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("ArchitectureToolkit.Domain.Entities")
            .And()
            .DoNotHaveName("ProjectMember")
            .Should()
            .Inherit(typeof(Entity))
            .And()
            .BeSealed()
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Failing Entity Types:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void ProjectMember_Should_NotInheritFromEntity()
    {
        // The positive counterpart to the exemption above: locks in that
        // ProjectMember's lack of an Entity base class is intentional. If
        // someone later "fixes" ProjectMember to inherit Entity, this test
        // fails too, prompting a look at the (ProjectId, UserId) composite
        // key design rather than a silent, unnoticed change.
        var result = Types
            .InAssembly(DomainAssembly)
            .That()
            .HaveName("ProjectMember")
            .ShouldNot()
            .Inherit(typeof(Entity))
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void DomainAssembly_ShouldNot_ReferenceAnyOtherProjects()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAll([
                "Application",
                "Infrastructure",
                "Persistence",
                "Presentation",
                "Tests"
            ])
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Failing Reference Types:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void OptionsEntities_Should_InheritFromTheBaseConfigTypeAndBeSealed()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("ArchitectureToolkit.Domain.Options")
            .Should()
            .ImplementInterface(typeof(IBaseOptionsConfig))
            .And()
            .BeSealed()
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Failing Options Types:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }
}

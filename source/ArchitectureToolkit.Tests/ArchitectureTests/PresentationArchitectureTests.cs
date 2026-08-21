using NetArchTest.Rules;
using static ArchitectureToolkit.Tests.ArchitectureTests.AssemblyReferences;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

[TestFixture]
public class PresentationArchitectureTests
{
    private const string ControllersNamespace = "ArchitectureToolkit.Presentation.API.Controllers";

    // Scoped to the Controllers namespace rather than the whole assembly: Program.cs is the
    // composition root and legitimately calls AddPersistenceRegistrations()/AddEFCorePersistenceRegistrations()
    // to wire up DI, so an assembly-wide ban on referencing Persistence would fail for the wrong
    // reason. Controllers, on the other hand, have no legitimate reason to see Persistence at all —
    // they should only talk to Application via MediatR and get DTOs back.
    [Test]
    public void Controllers_ShouldNot_ReferencePersistenceDirectly()
    {
        var result = Types
            .InAssembly(PresentationAssembly)
            .That()
            .ResideInNamespace(ControllersNamespace)
            .ShouldNot()
            .HaveDependencyOn("ArchitectureToolkit.Persistence")
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Controllers Referencing Persistence:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }
}

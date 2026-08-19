using NetArchTest.Rules;
using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Tests.ArchitectureTests.CustomRules;
using static ArchitectureToolkit.Tests.ArchitectureTests.AssemblyReferences;

namespace ArchitectureToolkit.Tests.ArchitectureTests;

[TestFixture]
public class ApplicationArchitectureTests
{
    [Test]
    public void ApplicationEntityQueryHandlers_Should_HaveAnIQueryDbContextParameterInTheConstructor()
    {
        var customRuleIQueryDbContextMustBeConstructorParameter = new IQueryDbContextMustBeConstructorParameter();

        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceMatching("ArchitectureToolkit.Application.Actions.*.Queries.*")
            .And()
            .ImplementInterface(typeof(IMediatRQueryHandler<,>))
            .Should()
            .MeetCustomRule(customRuleIQueryDbContextMustBeConstructorParameter)
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
    public void ApplicationEntityCommandHandlers_Should_HaveAnICommandDbContextParameterInTheConstructor()
    {
        var customRuleICommandDbContextMustBeConstructorParameter = new ICommandDbContextMustBeConstructorParameter();

        var result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceMatching("ArchitectureToolkit.Application.Actions.*.Commands.*")
            .And()
            .ImplementInterface(typeof(IMediatRCommandHandler<,>))
            .Should()
            .MeetCustomRule(customRuleICommandDbContextMustBeConstructorParameter)
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
    public void ApplicationAssembly_ShouldNot_ReferenceDapper()
    {
        // Application must depend only on abstractions (ICommandDbContext, IQueryDbContext,
        // ISampleEntityDapperQueryRepository, ISampleEntityDapperCommandRepository, etc.) that
        // are implemented in Persistence. This is a whole-assembly check, independent of the
        // constructor-shape rules above, so it also catches Dapper usage introduced outside a
        // MediatR handler (e.g. a helper class, static method, or future feature slice).
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Types Referencing Dapper:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void ApplicationAssembly_ShouldNot_ReferenceEntityFrameworkCore()
    {
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        if (result.FailingTypeNames != null && result.FailingTypeNames.Any())
        {
            Console.WriteLine("Types Referencing Microsoft.EntityFrameworkCore:");
            foreach (var failingType in result.FailingTypeNames)
            {
                Console.WriteLine($"    {failingType}");
            }
        }
        Assert.That(result.IsSuccessful, Is.True);
    }
}

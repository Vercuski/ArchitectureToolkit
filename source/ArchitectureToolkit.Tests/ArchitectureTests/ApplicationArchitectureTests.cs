using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Tests.ArchitectureTests.CustomRules;
using NetArchTest.Rules;
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

using ArchitectureToolkit.Application.Abstractions.Context;
using Mono.Cecil;
using NetArchTest.Rules;

namespace ArchitectureToolkit.Tests.ArchitectureTests.CustomRules;

/// <summary>
/// Requires a command handler's constructor to take a real command-side abstraction —
/// <see cref="ICommandDbContext"/> for the EF Core path.
/// Deliberately does NOT accept a raw connection factory
/// (e.g. IDbWriteConnectionFactory): allowing that would let a handler open an
/// IDbConnection and run ad-hoc SQL directly in Application, which is the violation
/// this rule exists to prevent.
/// </summary>
internal class ICommandDbContextMustBeConstructorParameter : ICustomRule
{
    public bool MeetsRule(TypeDefinition type)
    {
        bool isValid = true;
        foreach (var method in type.Methods.Where(x => x.IsConstructor))
        {
            isValid &= method.Parameters.Any(x => x.ParameterType.Name == typeof(ICommandDbContext).Name);
        }
        return isValid;
    }
}

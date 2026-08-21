using ArchitectureToolkit.Application.Abstractions.Context;
using Mono.Cecil;
using NetArchTest.Rules;

namespace ArchitectureToolkit.Tests.ArchitectureTests.CustomRules;

/// <summary>
/// Requires a query handler's constructor to take a real query-side abstraction —
/// <see cref="IQueryDbContext"/> for the EF Core path. Deliberately does NOT accept a raw connection factory
/// (e.g. IDbReadOnlyConnectionFactory): allowing that would let a handler open an
/// IDbConnection and run ad-hoc SQL directly in Application, which is the violation
/// this rule exists to prevent.
/// </summary>
internal class IQueryDbContextMustBeConstructorParameter : ICustomRule
{
    public bool MeetsRule(TypeDefinition type)
    {
        bool isValid = true;
        foreach (var method in type.Methods.Where(x => x.IsConstructor))
        {
            isValid &= method.Parameters.Any(x => x.ParameterType.Name == typeof(IQueryDbContext).Name);
        }
        return isValid;
    }
}

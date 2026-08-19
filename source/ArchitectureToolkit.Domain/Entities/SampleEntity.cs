using ArchitectureToolkit.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ArchitectureToolkit.Domain.Entities;

[ExcludeFromCodeCoverage]
public sealed class SampleEntityDefinition : Entity
{
    [Key]
    public int SampleId { get; set; }
    [Required]
    public string? SampleString { get; set; }
    public bool SampleBoolean { get; set; }
    public int SampleInt { get; set; }
    public decimal SampleDecimal { get; set; }
}

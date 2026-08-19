using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Persistence.Options;

public sealed record DatabasePlatformOptions : IBaseOptionsConfig
{
    public string QueryDbPlatform { get; set; } = null!;
    public string CommandDbPlatform { get; set; } = null!;
    public string Section => "DatabasePlatform";
}

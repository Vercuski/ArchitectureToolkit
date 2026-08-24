using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Persistence.Options;

public sealed record TemplateLibraryOptions : IBaseOptionsConfig
{
    /// <summary>
    /// Path to the template library's root folder — the one containing the
    /// 12 category subfolders. May be relative (resolved against the
    /// current working directory) or absolute. Local dev defaults to
    /// "../DocumentationTemplates" (source/DocumentationTemplates,
    /// relative to the API project's own directory, which is the working
    /// directory `dotnet run` uses by default); the container overrides
    /// this to the absolute path the Dockerfile copies it to
    /// (see docker-compose.yml).
    /// </summary>
    public string RootPath { get; set; } = null!;

    public string Section => "TemplateLibrary";
}

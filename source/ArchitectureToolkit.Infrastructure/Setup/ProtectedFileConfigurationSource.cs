using Microsoft.Extensions.Configuration;

namespace ArchitectureToolkit.Infrastructure.Setup;

/// <summary>
/// Bridges <see cref="IAppConfigurationStore"/> into
/// <see cref="IConfiguration"/>, so every existing
/// IOptions&lt;ConnectionStringOptions&gt;/AuthenticationConfiguration/
/// SmtpConfiguration/TemplateLibraryOptions binding (Persistence,
/// Presentation.API) keeps working completely unchanged — this changes
/// only *where* those sections' values come from, never how any of their
/// existing consumers read them.
///
/// Added as the very first builder.Configuration.Add(...) call in
/// Program.cs, ahead of AddPersistenceRegistrations/
/// AddInfrastructureRegistration, so those registrations see real values
/// already in place once a deployment is configured, and see nothing (the
/// signal Program.cs uses to enter Setup Mode instead) on a fresh install.
/// </summary>
public sealed class ProtectedFileConfigurationSource(IAppConfigurationStore store) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new ProtectedFileConfigurationProvider(store);
}

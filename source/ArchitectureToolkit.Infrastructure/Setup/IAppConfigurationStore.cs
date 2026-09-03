namespace ArchitectureToolkit.Infrastructure.Setup;

/// <summary>
/// Reads/writes the encrypted app-configuration blob backing
/// <see cref="ProtectedFileConfigurationSource"/>. Kept separate from that
/// IConfigurationSource/Provider pair (which only ever reads, at
/// WebApplicationBuilder time) because Presentation.API's
/// SetupCompletionService also needs to write a fresh
/// <see cref="PersistedAppConfiguration"/> once setup completes, and to
/// clear <see cref="PendingInitialUser"/> once consumed on the following
/// boot — this interface is the one seam both call sites share.
/// </summary>
public interface IAppConfigurationStore
{
    /// <summary>
    /// True once this store's own encrypted file has been written at
    /// least once. Narrower than "is this deployment configured" in
    /// general — appsettings.Testing.json legitimately supplies the same
    /// configuration sections for WebApplicationFactory-based tests
    /// without ever going through Setup, so Program.cs's actual gating
    /// decision is a separate check against the fully-layered
    /// IConfiguration, not this property. This property exists for the
    /// two call sites that specifically care whether *our* file exists:
    /// SetupController's re-submission guard, and the post-migration
    /// PendingInitialUser step.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>Null if <see cref="IsConfigured"/> is false.</summary>
    PersistedAppConfiguration? Load();

    void Save(PersistedAppConfiguration configuration);
}

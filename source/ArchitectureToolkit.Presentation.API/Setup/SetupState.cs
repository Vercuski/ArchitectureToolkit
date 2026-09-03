namespace ArchitectureToolkit.Presentation.API.Setup;

/// <summary>
/// The single "is this deployment configured" signal, computed once in
/// Program.cs from the fully-layered IConfiguration (not merely whether
/// IAppConfigurationStore's own encrypted file has been written —
/// appsettings.Testing.json legitimately supplies
/// ConnectionStrings:CommandDbConnection for WebApplicationFactory-based
/// tests without ever going through Setup) and registered as a singleton
/// so SetupController agrees with the exact same answer the running
/// pipeline itself already committed to when it decided which services
/// to register.
/// </summary>
public sealed record SetupState(bool IsConfigured);

using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace ArchitectureToolkit.Infrastructure.Setup;

/// <summary>
/// Persists <see cref="PersistedAppConfiguration"/> to a single encrypted
/// file, protected by a standalone (non-DI) ASP.NET Core Data Protection
/// key ring rooted in the same storage directory.
///
/// Deliberately standalone rather than resolving IDataProtectionProvider
/// from the app's own DI container: this store has to be usable from
/// <see cref="ProtectedFileConfigurationProvider"/>.Load(), which runs
/// during WebApplicationBuilder.Configuration.Add(...) — before
/// builder.Services is ever built into a service provider at all. Data
/// Protection's key-ring format doesn't care which IDataProtectionProvider
/// instance created it, only that CreateProtector is called with the same
/// purpose string against the same ring directory, so this has no bearing
/// on any other, ordinary AddDataProtection() registration the app may
/// also have for unrelated purposes (antiforgery tokens, etc.).
/// </summary>
public sealed class ProtectedFileAppConfigurationStore : IAppConfigurationStore
{
    private const string ProtectionPurpose = "ArchitectureToolkit.Setup.AppConfiguration.v1";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly string _settingsFilePath;
    private readonly IDataProtector _protector;

    /// <param name="storageDirectory">
    /// Root directory this deployment's encrypted configuration, Data
    /// Protection key ring, and (see Program.cs) OpenIddict certificates
    /// all live under. Must be on storage that survives container
    /// restarts (docker-compose.yml's app-config volume) — this file is
    /// the only thing that makes a restart resume as "configured" rather
    /// than showing Setup again. This is a path, not a secret, so it's
    /// fine for the caller to source it from ordinary configuration
    /// (Setup:StorageDirectory) rather than from this store itself.
    /// </param>
    public ProtectedFileAppConfigurationStore(string storageDirectory)
    {
        Directory.CreateDirectory(storageDirectory);
        var keysDirectory = Path.Combine(storageDirectory, "dataprotection-keys");
        Directory.CreateDirectory(keysDirectory);

        _settingsFilePath = Path.Combine(storageDirectory, "settings.protected");

        var provider = DataProtectionProvider.Create(new DirectoryInfo(keysDirectory));
        _protector = provider.CreateProtector(ProtectionPurpose);
    }

    public bool IsConfigured => File.Exists(_settingsFilePath);

    public PersistedAppConfiguration? Load()
    {
        if (!IsConfigured)
        {
            return null;
        }

        var protectedText = File.ReadAllText(_settingsFilePath);
        var json = _protector.Unprotect(protectedText);
        return JsonSerializer.Deserialize<PersistedAppConfiguration>(json, SerializerOptions);
    }

    public void Save(PersistedAppConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration, SerializerOptions);
        var protectedText = _protector.Protect(json);

        // Write-to-temp-then-move: a crash or power loss mid-write must
        // never leave settings.protected truncated/corrupt — that would
        // strand the deployment (IsConfigured true, but Load() throwing
        // on the next boot), unlike the file simply not existing yet,
        // which is a normal, fully-recoverable state Setup Mode handles
        // by design. File.Move with overwrite is atomic on both Linux
        // (rename(2)) and Windows (MoveFileEx) as long as source and
        // destination are on the same volume, which they always are here.
        var tempPath = _settingsFilePath + ".tmp";
        File.WriteAllText(tempPath, protectedText);
        File.Move(tempPath, _settingsFilePath, overwrite: true);
    }
}

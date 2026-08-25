using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ArchitectureToolkit.Infrastructure.Identity;

/// <summary>
/// Loads a persisted self-signed X.509 certificate from disk, generating and
/// saving one on first use if it doesn't exist yet.
///
/// ADR-0003 follow-up: OpenIddict's <c>AddDevelopmentSigningCertificate()</c>/
/// <c>AddDevelopmentEncryptionCertificate()</c> generate a brand-new ephemeral
/// key pair every process start. That's fine for local development, but in
/// Production it would invalidate every previously-issued token — including
/// refresh tokens — on every restart or redeploy. This provisioner instead
/// creates the signing/encryption certificates once and persists them to a
/// mounted volume (see docker-compose.yml's <c>identity-keys</c> volume), so
/// they survive container restarts.
///
/// This is deliberately simple (self-signed, file-based, single instance) to
/// match ArchitectureToolkit's zero-external-dependency self-hosting default
/// established in ADR-0003 — it does not assume access to a KMS, Key Vault,
/// or HSM. A deployer with those available can still swap them in, since
/// OpenIddict's <c>AddSigningCertificate</c>/<c>AddEncryptionCertificate</c>
/// accept any <see cref="X509Certificate2"/> regardless of where it came
/// from; only the provisioning step here is specific to file-based storage.
/// </summary>
public static class PersistedCertificateProvisioner
{
    /// <summary>
    /// Loads the certificate at <paramref name="filePath"/> if it exists;
    /// otherwise generates a new 2048-bit RSA self-signed certificate valid
    /// for 10 years, persists it to <paramref name="filePath"/> (PFX,
    /// password-protected), and returns it.
    /// </summary>
    public static X509Certificate2 GetOrCreate(string filePath, string password, string subjectName)
    {
        if (File.Exists(filePath))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(
                filePath,
                password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Backdated slightly to tolerate minor clock skew between the
        // generating host and whatever validates the certificate's
        // NotBefore immediately after creation.
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(filePath, pfxBytes);

        // Reload from the exported bytes rather than returning `certificate`
        // directly: the freshly-created instance doesn't carry the
        // PersistKeySet/Exportable flags OpenIddict expects when it later
        // uses the private key, and reloading also proves what was written
        // to disk is actually loadable before the app depends on it.
        return X509CertificateLoader.LoadPkcs12(
            pfxBytes,
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }
}

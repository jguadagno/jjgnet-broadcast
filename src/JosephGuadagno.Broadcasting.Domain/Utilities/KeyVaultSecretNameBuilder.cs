using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JosephGuadagno.Broadcasting.Domain.Utilities;

/// <summary>
/// Builds sanitized Azure Key Vault secret names for collector and publisher settings.
/// </summary>
public static class KeyVaultSecretNameBuilder
{
    private static readonly Regex SecretNameSanitizer = new(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);

    /// <summary>
    /// Builds a Key Vault secret name in the format:
    /// <c>{ownerType}-{sanitizedOwnerOid}-{platform}-{settingName}</c>
    /// or, when a discriminator is provided:
    /// <c>{ownerType}-{sanitizedOwnerOid}-{platform}-{hashedDiscriminator}-{settingName}</c>
    /// </summary>
    /// <param name="ownerType">The category of the secret owner: <see cref="KeyVaultSecretOwnerType.Publisher"/> or <see cref="KeyVaultSecretOwnerType.Collector"/>.</param>
    /// <param name="ownerOid">The owner's Entra (AAD) object ID. Non-alphanumeric/hyphen characters are replaced with hyphens.</param>
    /// <param name="platform">The platform segment — use a <see cref="KeyVaultSecretNames.Platform"/> constant.</param>
    /// <param name="settingName">The setting segment — use a <see cref="KeyVaultSecretNames.SettingName"/> constant.</param>
    /// <param name="discriminator">
    /// An optional discriminator inserted between platform and settingName, e.g. a YouTube channel ID.
    /// The value is SHA-256 hashed (first 8 bytes, lowercase hex) to avoid collisions between
    /// base64url-encoded identifiers that differ only by <c>-</c> vs <c>_</c>.
    /// </param>
    /// <returns>The sanitized Key Vault secret name.</returns>
    public static string Build(KeyVaultSecretOwnerType ownerType, string ownerOid, string platform, string settingName, string? discriminator = null)
    {
        var type = ownerType.ToString().ToLowerInvariant();
        var sanitizedOwner = SanitizeSegment(ownerOid);
        var sanitizedPlatform = SanitizeSegment(platform);
        var sanitizedSettingName = SanitizeSegment(settingName);
        return discriminator is null
            ? $"{type}-{sanitizedOwner}-{sanitizedPlatform}-{sanitizedSettingName}"
            : $"{type}-{sanitizedOwner}-{sanitizedPlatform}-{HashDiscriminator(discriminator)}-{sanitizedSettingName}";
    }

    /// <summary>
    /// Returns the first 8 bytes of the SHA-256 hash of <paramref name="discriminator"/> as
    /// a 16-character lowercase hex string. This guarantees that any Unicode value (including
    /// base64url IDs that contain both <c>-</c> and <c>_</c>) maps to a unique, Key-Vault-safe
    /// segment without silent collisions.
    /// </summary>
    private static string HashDiscriminator(string discriminator)
    {
        if (string.IsNullOrEmpty(discriminator))
            return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(discriminator));
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }

    /// <summary>
    /// Replaces any character that is not a letter, digit, or hyphen with a hyphen.
    /// Azure Key Vault secret names only permit <c>[a-zA-Z0-9-]</c>.
    /// </summary>
    private static string SanitizeSegment(string segment) =>
        SecretNameSanitizer.Replace(segment, "-");
}

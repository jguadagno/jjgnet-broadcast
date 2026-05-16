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
    /// <c>{ownerType}-{sanitizedOwnerOid}-{platform}-{discriminator}-{settingName}</c>
    /// </summary>
    /// <param name="ownerType">The category of the secret owner: <see cref="KeyVaultSecretOwnerType.Publisher"/> or <see cref="KeyVaultSecretOwnerType.Collector"/>.</param>
    /// <param name="ownerOid">The owner's Entra (AAD) object ID. Non-alphanumeric/hyphen characters are replaced with hyphens.</param>
    /// <param name="platform">The platform segment — use a <see cref="KeyVaultSecretNames.Platform"/> constant.</param>
    /// <param name="settingName">The setting segment — use a <see cref="KeyVaultSecretNames.SettingName"/> constant.</param>
    /// <param name="discriminator">An optional discriminator inserted between platform and settingName, e.g. a YouTube channel ID.</param>
    /// <returns>The sanitized Key Vault secret name.</returns>
    public static string Build(KeyVaultSecretOwnerType ownerType, string ownerOid, string platform, string settingName, string? discriminator = null)
    {
        var type = ownerType.ToString().ToLowerInvariant();
        var sanitizedOwner = SanitizeSegment(ownerOid);
        var sanitizedPlatform = SanitizeSegment(platform);
        var sanitizedSettingName = SanitizeSegment(settingName);
        return discriminator is null
            ? $"{type}-{sanitizedOwner}-{sanitizedPlatform}-{sanitizedSettingName}"
            : $"{type}-{sanitizedOwner}-{sanitizedPlatform}-{SanitizeSegment(discriminator)}-{sanitizedSettingName}";
    }

    /// <summary>
    /// Replaces any character that is not a letter, digit, or hyphen with a hyphen.
    /// Azure Key Vault secret names only permit <c>[a-zA-Z0-9-]</c>.
    /// </summary>
    private static string SanitizeSegment(string segment) =>
        SecretNameSanitizer.Replace(segment, "-");
}

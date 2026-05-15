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
    /// <c>{type}-{sanitizedOwnerOid}-{platform}-{settingName}</c>
    /// or, when a discriminator is provided:
    /// <c>{type}-{sanitizedOwnerOid}-{platform}-{discriminator}-{settingName}</c>
    /// </summary>
    /// <param name="type">The category of the secret, e.g. "collector" or "publisher".</param>
    /// <param name="ownerOid">The owner's Entra (AAD) object ID. Non-alphanumeric/hyphen characters are replaced with hyphens.</param>
    /// <param name="platform">The platform name, e.g. "bluesky", "youtube-channel".</param>
    /// <param name="settingName">The setting name, e.g. "app-password", "api-key".</param>
    /// <param name="discriminator">An optional discriminator inserted between platform and settingName, e.g. a YouTube channel ID.</param>
    /// <returns>The sanitized Key Vault secret name.</returns>
    public static string Build(string type, string ownerOid, string platform, string settingName, string? discriminator = null)
    {
        var sanitizedOwner = SecretNameSanitizer.Replace(ownerOid, "-");
        return discriminator is null
            ? $"{type}-{sanitizedOwner}-{platform}-{settingName}"
            : $"{type}-{sanitizedOwner}-{platform}-{discriminator}-{settingName}";
    }
}

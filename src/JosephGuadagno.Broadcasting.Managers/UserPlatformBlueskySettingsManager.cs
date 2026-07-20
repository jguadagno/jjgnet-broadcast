using System;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for per-user Bluesky publisher settings
/// </summary>
public class UserPlatformBlueskySettingsManager(
	IUserPlatformBlueskySettingsDataStore UserPlatformBlueskySettingsDataStore,
	IKeyVault keyVault,
	ILogger<UserPlatformBlueskySettingsManager> logger)
	: IUserPlatformBlueskySettingsManager
{
	/// <inheritdoc />
    public Task<UserPlatformBlueskySettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return UserPlatformBlueskySettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserPlatformBlueskySettings?> SaveAsync(UserPlatformBlueskySettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        return UserPlatformBlueskySettingsDataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var existing = await UserPlatformBlueskySettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        return await UserPlatformBlueskySettingsDataStore.DeleteAsync(existing.Id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAppPasswordAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Bluesky, KeyVaultSecretNames.SettingName.AppPassword);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Bluesky app password from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreAppPasswordAsync(string ownerOid, string appPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(appPassword);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Bluesky, KeyVaultSecretNames.SettingName.AppPassword);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, appPassword, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Bluesky app password in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

}


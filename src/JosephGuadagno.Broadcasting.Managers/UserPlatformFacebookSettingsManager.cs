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
/// Manager for per-user Facebook publisher settings
/// </summary>
public class UserPlatformFacebookSettingsManager(
	IUserPlatformFacebookSettingsDataStore UserPlatformFacebookSettingsDataStore,
	IKeyVault keyVault,
	ILogger<UserPlatformFacebookSettingsManager> logger)
	: IUserPlatformFacebookSettingsManager
{
	/// <inheritdoc />
    public Task<UserPlatformFacebookSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return UserPlatformFacebookSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserPlatformFacebookSettings?> SaveAsync(UserPlatformFacebookSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        return UserPlatformFacebookSettingsDataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var existing = await UserPlatformFacebookSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        return await UserPlatformFacebookSettingsDataStore.DeleteAsync(existing.Id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetPageAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.PageAccessToken);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Facebook page access token from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StorePageAccessTokenAsync(string ownerOid, string pageAccessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageAccessToken);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.PageAccessToken);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, pageAccessToken, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Facebook page access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAppSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.AppSecret);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Facebook app secret from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreAppSecretAsync(string ownerOid, string appSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(appSecret);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.AppSecret);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, appSecret, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Facebook app secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetClientTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.ClientToken);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Facebook client token from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreClientTokenAsync(string ownerOid, string clientToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientToken);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.ClientToken);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, clientToken, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Facebook client token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetShortLivedAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.ShortLivedAccessToken);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Facebook short-lived access token from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreShortLivedAccessTokenAsync(string ownerOid, string shortLivedAccessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortLivedAccessToken);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.ShortLivedAccessToken);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, shortLivedAccessToken, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Facebook short-lived access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetLongLivedAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.LongLivedAccessToken);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Facebook long-lived access token from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreLongLivedAccessTokenAsync(string ownerOid, string longLivedAccessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(longLivedAccessToken);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.LongLivedAccessToken);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, longLivedAccessToken, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Facebook long-lived access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }
}


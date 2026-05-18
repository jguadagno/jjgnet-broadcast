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
/// Manager for per-user Twitter publisher settings
/// </summary>
public class UserPublisherTwitterSettingsManager(
	IUserPublisherTwitterSettingsDataStore userPublisherTwitterSettingsDataStore,
	IKeyVault keyVault,
	ILogger<UserPublisherTwitterSettingsManager> logger)
	: IUserPublisherTwitterSettingsManager
{
	/// <inheritdoc />
    public Task<UserPublisherTwitterSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return userPublisherTwitterSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserPublisherTwitterSettings?> SaveAsync(UserPublisherTwitterSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        return userPublisherTwitterSettingsDataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var existing = await userPublisherTwitterSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        return await userPublisherTwitterSettingsDataStore.DeleteAsync(existing.Id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetConsumerKeyAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.ConsumerKey);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Twitter consumer key from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreConsumerKeyAsync(string ownerOid, string consumerKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerKey);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.ConsumerKey);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, consumerKey, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Twitter consumer key in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetConsumerSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.ConsumerSecret);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Twitter consumer secret from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreConsumerSecretAsync(string ownerOid, string consumerSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerSecret);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.ConsumerSecret);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, consumerSecret, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Twitter consumer secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.AccessToken);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Twitter access token from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreAccessTokenAsync(string ownerOid, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.AccessToken);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, accessToken, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Twitter access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.AccessTokenSecret);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve Twitter access token secret from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreAccessTokenSecretAsync(string ownerOid, string accessTokenSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenSecret);
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.AccessTokenSecret);
        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, accessTokenSecret, DateTime.UtcNow.AddYears(10));
        logger.LogInformation(
            "Stored Twitter access token secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }
}

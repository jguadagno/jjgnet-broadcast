using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for per-user YouTube channel collector configurations
/// </summary>
public class UserCollectorYouTubeChannelManager(
	IUserCollectorYouTubeChannelDataStore dataStore,
	IKeyVault keyVault,
	ILogger<UserCollectorYouTubeChannelManager> logger)
	: IUserCollectorYouTubeChannelManager
{
	/// <inheritdoc />
    public Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetByUserAsync(ownerOid, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserCollectorYouTubeChannel?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        var config = await dataStore.GetByIdAsync(id, cancellationToken);
        if (config is not null)
        {
            await SetHasApiKeyAsync(config, cancellationToken);
        }
        return config;
    }

    /// <inheritdoc />
    public Task<List<UserCollectorYouTubeChannel>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return dataStore.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserCollectorYouTubeChannel?> SaveAsync(
        UserCollectorYouTubeChannel config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.ChannelId);

        var saved = await dataStore.SaveAsync(config, cancellationToken);
        if (saved is not null)
        {
            await SetHasApiKeyAsync(saved, cancellationToken);
        }
        return saved;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        int id, string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.DeleteAsync(id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<UserCollectorYouTubeChannel>> GetAllAsync(
        string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> StoreApiKeyToKeyVaultAsync(
        string ownerOid, string youTubeChannelId, string rawApiKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawApiKey);

        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, ownerOid, KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, youTubeChannelId);

        await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, rawApiKey, DateTime.UtcNow.AddYears(10));

        logger.LogInformation(
            "Stored YouTube API key in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));

        return secretName;
    }

    /// <inheritdoc />
    public async Task<string?> GetApiKeyAsync(
        string ownerOid, int id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var config = await dataStore.GetByIdAsync(id, cancellationToken);
        if (config is null)
        {
            return null;
        }
        var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, ownerOid, KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, config.ChannelId);
        try
        {
            var secret = await keyVault.GetSecretAsync(secretName);
            return secret.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to retrieve YouTube API key from Key Vault for secret '{SecretName}', owner '{OwnerOid}', config {Id}",
                secretName,
                LogSanitizer.Sanitize(ownerOid),
                id);
            return null;
        }
    }

    private async Task SetHasApiKeyAsync(UserCollectorYouTubeChannel config, CancellationToken cancellationToken = default)
    {
        if (config is null) return;
        try
        {
            var secretName = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, config.CreatedByEntraOid, KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, config.ChannelId);
            var secret = await keyVault.GetSecretAsync(secretName);
            config.HasApiKey = !string.IsNullOrWhiteSpace(secret?.Value);
        }
        catch
        {
            config.HasApiKey = false;
        }
    }
}

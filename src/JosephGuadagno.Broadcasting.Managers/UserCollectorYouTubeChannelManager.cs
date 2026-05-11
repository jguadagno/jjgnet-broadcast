using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
public class UserCollectorYouTubeChannelManager : IUserCollectorYouTubeChannelManager
{
    private readonly IUserCollectorYouTubeChannelDataStore _dataStore;
    private readonly IKeyVault _keyVault;
    private readonly ILogger<UserCollectorYouTubeChannelManager> _logger;

    private static readonly Regex SecretNameSanitizer = new(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);

    public UserCollectorYouTubeChannelManager(
        IUserCollectorYouTubeChannelDataStore dataStore,
        IKeyVault keyVault,
        ILogger<UserCollectorYouTubeChannelManager> logger)
    {
        _dataStore = dataStore;
        _keyVault = keyVault;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return _dataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserCollectorYouTubeChannel?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return _dataStore.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<UserCollectorYouTubeChannel>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserCollectorYouTubeChannel?> SaveAsync(
        UserCollectorYouTubeChannel config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.ChannelId);

        return _dataStore.SaveAsync(config, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        int id, string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return _dataStore.DeleteAsync(id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<UserCollectorYouTubeChannel>> GetAllAsync(
        string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return _dataStore.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> StoreApiKeyToKeyVaultAsync(
        string ownerOid, string youTubeChannelId, string rawApiKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawApiKey);

        var secretName = BuildSecretName(ownerOid, youTubeChannelId);

        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, rawApiKey, DateTime.UtcNow.AddYears(10));

        _logger.LogInformation(
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

        var config = await _dataStore.GetByIdAsync(id, cancellationToken);
        if (config is null || string.IsNullOrWhiteSpace(config.ApiKeySecretName))
        {
            return null;
        }

        try
        {
            var secret = await _keyVault.GetSecretAsync(config.ApiKeySecretName);
            return secret.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve YouTube API key from Key Vault for secret '{SecretName}', owner '{OwnerOid}', config {Id}",
                config.ApiKeySecretName,
                LogSanitizer.Sanitize(ownerOid),
                id);
            return null;
        }
    }

    private static string BuildSecretName(string ownerOid, string youTubeChannelId)
    {
        var sanitizedOwner = SecretNameSanitizer.Replace(ownerOid, "-");
        var sanitizedChannel = SecretNameSanitizer.Replace(youTubeChannelId, "-");
        return $"youtube-channel-apikey-{sanitizedOwner}-{sanitizedChannel}";
    }
}

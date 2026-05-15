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
public class UserPublisherTwitterSettingsManager : IUserPublisherTwitterSettingsManager
{
    private readonly IUserPublisherTwitterSettingsDataStore _userPublisherTwitterSettingsDataStore;
    private readonly IKeyVault _keyVault;
    private readonly ILogger<UserPublisherTwitterSettingsManager> _logger;

    public UserPublisherTwitterSettingsManager(
        IUserPublisherTwitterSettingsDataStore userPublisherTwitterSettingsDataStore,
        IKeyVault keyVault,
        ILogger<UserPublisherTwitterSettingsManager> logger)
    {
        _userPublisherTwitterSettingsDataStore = userPublisherTwitterSettingsDataStore;
        _keyVault = keyVault;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<UserPublisherTwitterSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return _userPublisherTwitterSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserPublisherTwitterSettings?> SaveAsync(UserPublisherTwitterSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        return _userPublisherTwitterSettingsDataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var existing = await _userPublisherTwitterSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        return await _userPublisherTwitterSettingsDataStore.DeleteAsync(existing.Id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetConsumerKeyAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "consumer-key");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "consumer-key");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, consumerKey, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Twitter consumer key in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetConsumerSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "consumer-secret");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "consumer-secret");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, consumerSecret, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Twitter consumer secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "access-token");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "access-token");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, accessToken, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Twitter access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "access-token-secret");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "twitter", "access-token-secret");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, accessTokenSecret, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Twitter access token secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }
}

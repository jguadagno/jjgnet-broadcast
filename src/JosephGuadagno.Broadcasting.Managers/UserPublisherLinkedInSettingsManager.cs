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
/// Manager for per-user LinkedIn publisher settings
/// </summary>
public class UserPublisherLinkedInSettingsManager : IUserPublisherLinkedInSettingsManager
{
    private readonly IUserPublisherLinkedInSettingsDataStore _userPublisherLinkedInSettingsDataStore;
    private readonly IKeyVault _keyVault;
    private readonly ILogger<UserPublisherLinkedInSettingsManager> _logger;

    public UserPublisherLinkedInSettingsManager(
        IUserPublisherLinkedInSettingsDataStore userPublisherLinkedInSettingsDataStore,
        IKeyVault keyVault,
        ILogger<UserPublisherLinkedInSettingsManager> logger)
    {
        _userPublisherLinkedInSettingsDataStore = userPublisherLinkedInSettingsDataStore;
        _keyVault = keyVault;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<UserPublisherLinkedInSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return _userPublisherLinkedInSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserPublisherLinkedInSettings?> SaveAsync(UserPublisherLinkedInSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        return _userPublisherLinkedInSettingsDataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var existing = await _userPublisherLinkedInSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        return await _userPublisherLinkedInSettingsDataStore.DeleteAsync(existing.Id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetClientSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "linkedin", "client-secret");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve LinkedIn client secret from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
                secretName,
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task StoreClientSecretAsync(string ownerOid, string clientSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "linkedin", "client-secret");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, clientSecret, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored LinkedIn client secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "linkedin", "access-token");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve LinkedIn access token from Key Vault for secret '{SecretName}', owner '{OwnerOid}'",
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
        var secretName = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "linkedin", "access-token");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, accessToken, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored LinkedIn access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }
}

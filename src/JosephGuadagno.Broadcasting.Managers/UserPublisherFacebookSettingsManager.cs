using System;
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
/// Manager for per-user Facebook publisher settings
/// </summary>
public class UserPublisherFacebookSettingsManager : IUserPublisherFacebookSettingsManager
{
    private readonly IUserPublisherFacebookSettingsDataStore _userPublisherFacebookSettingsDataStore;
    private readonly IKeyVault _keyVault;
    private readonly ILogger<UserPublisherFacebookSettingsManager> _logger;

    private static readonly Regex SecretNameSanitizer = new(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);

    public UserPublisherFacebookSettingsManager(
        IUserPublisherFacebookSettingsDataStore userPublisherFacebookSettingsDataStore,
        IKeyVault keyVault,
        ILogger<UserPublisherFacebookSettingsManager> logger)
    {
        _userPublisherFacebookSettingsDataStore = userPublisherFacebookSettingsDataStore;
        _keyVault = keyVault;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<UserPublisherFacebookSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return _userPublisherFacebookSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserPublisherFacebookSettings?> SaveAsync(UserPublisherFacebookSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        return _userPublisherFacebookSettingsDataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var existing = await _userPublisherFacebookSettingsDataStore.GetByUserAsync(ownerOid, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        return await _userPublisherFacebookSettingsDataStore.DeleteAsync(existing.Id, ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetPageAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = BuildSecretName(ownerOid, "page-access-token");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = BuildSecretName(ownerOid, "page-access-token");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, pageAccessToken, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Facebook page access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetAppSecretAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = BuildSecretName(ownerOid, "app-secret");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = BuildSecretName(ownerOid, "app-secret");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, appSecret, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Facebook app secret in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetClientTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = BuildSecretName(ownerOid, "client-token");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = BuildSecretName(ownerOid, "client-token");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, clientToken, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Facebook client token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetShortLivedAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = BuildSecretName(ownerOid, "short-lived-access-token");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = BuildSecretName(ownerOid, "short-lived-access-token");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, shortLivedAccessToken, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Facebook short-lived access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    /// <inheritdoc />
    public async Task<string?> GetLongLivedAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        var secretName = BuildSecretName(ownerOid, "long-lived-access-token");
        try
        {
            var secret = await _keyVault.GetSecretAsync(secretName);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
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
        var secretName = BuildSecretName(ownerOid, "long-lived-access-token");
        await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, longLivedAccessToken, DateTime.UtcNow.AddYears(10));
        _logger.LogInformation(
            "Stored Facebook long-lived access token in Key Vault as secret '{SecretName}' for owner '{OwnerOid}'",
            secretName,
            LogSanitizer.Sanitize(ownerOid));
    }

    private static string BuildSecretName(string ownerOid, string settingName)
    {
        var sanitizedOwner = SecretNameSanitizer.Replace(ownerOid, "-");
        return $"publisher-{sanitizedOwner}-facebook-{settingName}";
    }
}

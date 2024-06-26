﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Facebook;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Constants = JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class RefreshTokens
{
    private readonly IFacebookManager _facebookManager;
    private readonly IFacebookApplicationSettings _facebookApplicationSettings;
    private readonly TokenRefreshRepository _refreshTokenRepository;
    private readonly ISettings _settings;
    private readonly ILogger<RefreshTokens> _logger;
    private readonly TelemetryClient _telemetryClient;
    

    public RefreshTokens(IFacebookManager facebookManager, IFacebookApplicationSettings facebookApplicationSettings, TokenRefreshRepository tokenRefreshRepository, ISettings settings, ILoggerFactory loggerFactory, TelemetryClient telemetryClient)
    {
        _facebookManager = facebookManager;
        _facebookApplicationSettings = facebookApplicationSettings;
        _refreshTokenRepository = tokenRefreshRepository;
        _settings = settings;
        _logger = loggerFactory.CreateLogger<RefreshTokens>();
        _telemetryClient = telemetryClient;
    }

    [Function("facebook_refresh_tokens")]
    public async Task Run([TimerTrigger("%facebook_refresh_tokens_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.CollectorsFeedLoadNewPosts, startedAt);

        await RefreshToken(Managers.Facebook.Constants.TokenTypes.LongLived, _facebookApplicationSettings.LongLivedAccessToken);
        await RefreshToken(Managers.Facebook.Constants.TokenTypes.Page, _facebookApplicationSettings.PageAccessToken);
    }
    
    private async Task RefreshToken(Managers.Facebook.Constants.TokenTypes tokenType, string token)
    {
        if (token == null || string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("Token is null or empty. Cannot refresh the token");
            return;
        }
        
        const string azureKeyVaultSecretName = "jjg-net-facebook-{token-name}-access-token";
        
        // Check the Long Lived Token - Refresh it if needed
        var tokenInfo = await _refreshTokenRepository.GetAsync(Constants.Tables.TokenRefresh,
                                    tokenType.DisplayName()) ??
                                new TokenRefreshInfo(tokenType.DisplayName())
                                {
                                    LastRefreshed = DateTime.MinValue,
                                    LastChecked = DateTime.MinValue,
                                    Expires = DateTime.MinValue
                                };
        
        if (tokenInfo.Expires < DateTime.UtcNow || tokenInfo.Expires.AddDays(-5) < DateTime.UtcNow)
        {
            // Refresh the token
            _logger.LogDebug("{DisplayName} is expired or will expire soon. Refreshing the token", tokenType.DisplayName());
            try
            {
                var newToken = await _facebookManager.RefreshToken(token);
                
                // Save the token to Key Vault
                // NOTE: Locally, you will need to set the environment variables for the Azure Key Vault
                var client = new SecretClient(new Uri(_settings.AzureKeyVaultUrl), new DefaultAzureCredential());
                var secretName = azureKeyVaultSecretName.Replace("{token-name}", tokenType.DisplayName());
                
                await UpdateSecretValueAndProperties(client, secretName, newToken.AccessToken, newToken.ExpiresOn);
                
                // Save the token refresh info to the database
                tokenInfo.LastRefreshed = tokenInfo.LastChecked = DateTime.UtcNow;
                tokenInfo.Expires = newToken.ExpiresOn;
                await _refreshTokenRepository.SaveAsync(tokenInfo);
                
                // Update logs and telemetry
                _logger.LogInformation("{DisplayName} refreshed successfully. Expires on {Expires:f}",
                    tokenType.DisplayName(), tokenInfo.Expires);
                var eventName = "{DisplayName}TokenRefreshed".Replace("{DisplayName}", tokenType.DisplayName());
                _telemetryClient.TrackEvent(eventName, new Dictionary<string, string>
                {
                    {"Expires", tokenInfo.Expires.ToString("O")},
                    {"TokenType", tokenType.DisplayName()}
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error refreshing the {DisplayName} Token", tokenType.DisplayName());
            }
        }
        else
        {
            _logger.LogDebug("{DisplayName} is still valid until {Expires:f}", tokenType.DisplayName(), tokenInfo.Expires);
        }
    }
    
    /// <summary>
    /// Updates the secret value and expiration date
    /// </summary>
    /// <param name="client">A SecretClient for the connection to Azure Key Vault</param>
    /// <param name="secretName">The name of the secret to update</param>
    /// <param name="secretValue">The value to update the secret to</param>
    /// <param name="expiresOn">The UTC datetime that this secret expires</param>
    /// <exception cref="ApplicationException">Thrown is any of the operations fail</exception>
    /// <remarks>
    /// This method will update the secret value and expiration date. It will also disable the old secret.
    /// </remarks>
    public async Task UpdateSecretValueAndProperties(SecretClient client, string secretName, string secretValue, DateTime expiresOn)
    {
        var originalSecretResponse = await client.GetSecretAsync(secretName);
        if (originalSecretResponse is null)
        {
            throw new ApplicationException($"Failed to get the secret '{secretName}' from the Key Vault");
        }
        var originalSecret = originalSecretResponse.Value;
        
        // Set the old secret to disabled
        originalSecret.Properties.Enabled = false;
        var updatePropertiesResponse = await client.UpdateSecretPropertiesAsync(originalSecret.Properties);
        if (updatePropertiesResponse is null)
        {
            throw new ApplicationException($"Failed to update the original version secret properties for '{secretName}'");
        }
        
        // Update secret value (create a new version)
        var newSecretVersionResponse = await client.SetSecretAsync(secretName, secretValue);
        if (newSecretVersionResponse is null)
        {
            throw new ApplicationException($"Failed to update the secret value for '{secretName}'");
        }
        var newKeyVaultSecretVersion = newSecretVersionResponse.Value;
        
        // Update the expiration date
        newKeyVaultSecretVersion.Properties.ExpiresOn = expiresOn;
        updatePropertiesResponse = await client.UpdateSecretPropertiesAsync(newKeyVaultSecretVersion.Properties);
        if (updatePropertiesResponse is null)
        {
            throw new ApplicationException($"Failed to update the new version secret properties for '{secretName}'");
        }
    }
}
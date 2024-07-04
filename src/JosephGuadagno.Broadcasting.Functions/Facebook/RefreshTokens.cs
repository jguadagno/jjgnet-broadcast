using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Repositories;
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
    private readonly IKeyVault _keyVault;
    private readonly ILogger<RefreshTokens> _logger;
    private readonly TelemetryClient _telemetryClient;

    public RefreshTokens(IFacebookManager facebookManager, IFacebookApplicationSettings facebookApplicationSettings, TokenRefreshRepository tokenRefreshRepository, IKeyVault keyVault, ILoggerFactory loggerFactory, TelemetryClient telemetryClient)
    {
        _facebookManager = facebookManager;
        _facebookApplicationSettings = facebookApplicationSettings;
        _refreshTokenRepository = tokenRefreshRepository;
        _keyVault = keyVault;
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
                var secretName = azureKeyVaultSecretName.Replace("{token-name}", tokenType.DisplayName());
                
                await _keyVault.UpdateSecretValueAndPropertiesAsync(secretName, newToken.AccessToken, newToken.ExpiresOn);
                
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
}
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class RefreshTokens(
    IFacebookManager facebookManager,
    IFacebookApplicationSettings facebookApplicationSettings,
    ITokenRefreshManager tokenRefreshManager,
    IKeyVault keyVault,
    ILoggerFactory loggerFactory,
    TelemetryClient telemetryClient)
{
    private readonly ILogger<RefreshTokens> _logger = loggerFactory.CreateLogger<RefreshTokens>();

    [Function(ConfigurationFunctionNames.FacebookTokenRefresh)]
    public async Task Run([TimerTrigger("%facebook_refresh_tokens_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookTokenRefresh, startedAt);

        await RefreshToken(Managers.Facebook.Constants.TokenTypes.LongLived, facebookApplicationSettings.LongLivedAccessToken);
        await RefreshToken(Managers.Facebook.Constants.TokenTypes.Page, facebookApplicationSettings.PageAccessToken);
    }
    
    private async Task RefreshToken(Managers.Facebook.Constants.TokenTypes tokenType, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("Token is null or empty. Cannot refresh the token");
            return;
        }
        
        const string azureKeyVaultSecretName = "jjg-net-facebook-{token-name}-access-token";
        
        // Check the Long Lived Token - Refresh it if needed
        var tokenInfo = await tokenRefreshManager.GetByNameAsync(tokenType.DisplayName()) ??
                                new TokenRefresh
                                {
                                    Name = tokenType.DisplayName(),
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
                var newToken = await facebookManager.RefreshToken(token);
                
                // Save the token to Key Vault
                // NOTE: Locally, you will need to set the environment variables for the Azure Key Vault
                var secretName = azureKeyVaultSecretName.Replace("{token-name}", tokenType.DisplayName());
                
                await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, newToken.AccessToken, newToken.ExpiresOn);
                
                // Save the token refresh info to the database
                tokenInfo.LastRefreshed = tokenInfo.LastChecked = DateTime.UtcNow;
                tokenInfo.Expires = newToken.ExpiresOn;
                await tokenRefreshManager.SaveAsync(tokenInfo);
                
                // Update logs and telemetry
                _logger.LogInformation("{DisplayName} refreshed successfully. Expires on {Expires:f}",
                    tokenType.DisplayName(), tokenInfo.Expires);
                var eventName = "{DisplayName}TokenRefreshed".Replace("{DisplayName}", tokenType.DisplayName());
                telemetryClient.TrackEvent(eventName, new Dictionary<string, string>
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
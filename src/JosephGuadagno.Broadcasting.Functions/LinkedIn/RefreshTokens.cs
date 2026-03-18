using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class RefreshTokens(
    ILinkedInManager linkedInManager,
    ILinkedInApplicationSettings linkedInApplicationSettings,
    ITokenRefreshManager tokenRefreshManager,
    IKeyVault keyVault,
    ILogger<RefreshTokens> logger)
{
    private const string AccessTokenSecretName = "jjg-net-linkedin-access-token";
    private const string RefreshTokenSecretName = "jjg-net-linkedin-refresh-token";
    private const string TokenName = "LinkedIn";

    [Function(ConfigurationFunctionNames.LinkedInTokenRefresh)]
    public async Task Run([TimerTrigger("%linkedin_refresh_tokens_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInTokenRefresh, startedAt);

        await RefreshToken();
    }

    private async Task RefreshToken()
    {
        // Retrieve the stored refresh token from Key Vault
        string refreshToken;
        try
        {
            var refreshTokenSecret = await keyVault.GetSecretAsync(RefreshTokenSecretName);
            refreshToken = refreshTokenSecret.Value;
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Failed to retrieve LinkedIn refresh token from Key Vault secret '{SecretName}'. " +
                "Manual re-authorization via the Web UI LinkedIn controller is required.",
                RefreshTokenSecretName);
            return;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogError(
                "LinkedIn refresh token is empty. Manual re-authorization via the Web UI LinkedIn controller is required.");
            return;
        }

        // Load (or initialize) the token refresh tracking record
        var tokenInfo = await tokenRefreshManager.GetByNameAsync(TokenName) ??
                        new TokenRefresh
                        {
                            Name = TokenName,
                            LastRefreshed = DateTime.MinValue,
                            LastChecked = DateTime.MinValue,
                            Expires = DateTime.MinValue
                        };

        if (tokenInfo.Expires < DateTime.UtcNow || tokenInfo.Expires.AddDays(-5) < DateTime.UtcNow)
        {
            logger.LogDebug("LinkedIn access token is expired or will expire soon (expires {Expires:f}). Refreshing.",
                tokenInfo.Expires);
            try
            {
                var newTokenInfo = await linkedInManager.RefreshTokenAsync(
                    linkedInApplicationSettings.ClientId,
                    linkedInApplicationSettings.ClientSecret,
                    refreshToken,
                    linkedInApplicationSettings.AccessTokenUrl);

                // Persist the new access token
                await keyVault.UpdateSecretValueAndPropertiesAsync(
                    AccessTokenSecretName, newTokenInfo.AccessToken, newTokenInfo.ExpiresOn);

                // Persist the new refresh token if LinkedIn issued a replacement
                if (!string.IsNullOrEmpty(newTokenInfo.RefreshToken) &&
                    newTokenInfo.RefreshTokenExpiresOn.HasValue)
                {
                    await keyVault.UpdateSecretValueAndPropertiesAsync(
                        RefreshTokenSecretName, newTokenInfo.RefreshToken, newTokenInfo.RefreshTokenExpiresOn.Value);
                    logger.LogInformation("LinkedIn refresh token rotated and saved to Key Vault.");
                }

                // Update the token refresh tracking record
                tokenInfo.LastRefreshed = tokenInfo.LastChecked = DateTime.UtcNow;
                tokenInfo.Expires = newTokenInfo.ExpiresOn;
                await tokenRefreshManager.SaveAsync(tokenInfo);

                logger.LogInformation("LinkedIn access token refreshed successfully. Expires on {Expires:f}",
                    tokenInfo.Expires);

                var properties = new Dictionary<string, string>
                {
                    { "Expires", tokenInfo.Expires.ToString("O") },
                    { "TokenType", TokenName }
                };
                logger.LogCustomEvent("LinkedInTokenRefreshed", properties);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing the LinkedIn access token");
            }
        }
        else
        {
            logger.LogDebug("LinkedIn access token is still valid until {Expires:f}", tokenInfo.Expires);
        }
    }
}

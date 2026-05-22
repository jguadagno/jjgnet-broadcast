using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class RefreshTokens(
    IFacebookManager facebookManager,
    IUserOAuthTokenManager userOAuthTokenManager,
    ILogger<RefreshTokens> logger)
{
    [Function(ConfigurationFunctionNames.FacebookTokenRefresh)]
    public async Task Run([TimerTrigger("%facebook_refresh_tokens_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookTokenRefresh, startedAt);

        var from = DateTimeOffset.UtcNow.AddYears(-10);
        var to = DateTimeOffset.UtcNow.AddDays(5);

        var expiringTokens = await userOAuthTokenManager.GetExpiringWindowAsync(from, to);
        var facebookTokens = expiringTokens
            .Where(t => t.SocialMediaPlatformId == SocialMediaPlatformIds.Facebook)
            .ToList();

        if (facebookTokens.Count == 0)
        {
            logger.LogDebug("No Facebook tokens are expired or expiring within 5 days");
            return;
        }

        logger.LogInformation("Refreshing {Count} Facebook token(s)", facebookTokens.Count);

        foreach (var token in facebookTokens)
        {
            try
            {
                var newToken = await facebookManager.RefreshToken(token.AccessToken);
                var expiresAt = new DateTimeOffset(DateTime.SpecifyKind(newToken.ExpiresOn, DateTimeKind.Utc));

                await userOAuthTokenManager.StoreOAuthCallbackTokenAsync(
                    token.CreatedByEntraOid,
                    SocialMediaPlatformIds.Facebook,
                    newToken.AccessToken,
                    null,
                    expiresAt,
                    null);

                logger.LogInformation(
                    "Facebook token refreshed for owner '{OwnerOid}'. Expires on {Expires:f}",
                    LogSanitizer.Sanitize(token.CreatedByEntraOid), expiresAt);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to refresh Facebook token for owner '{OwnerOid}'",
                    LogSanitizer.Sanitize(token.CreatedByEntraOid));
            }
        }
    }
}
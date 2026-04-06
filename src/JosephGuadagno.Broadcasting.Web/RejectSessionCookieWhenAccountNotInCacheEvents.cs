using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web;

internal class RejectSessionCookieWhenAccountNotInCacheEvents : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        try
        {
            var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                scopes: new[] { "profile" },
                user: context.Principal);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex) when (AccountDoesNotExitInTokenCache(ex))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning("Token cache issue detected during cookie validation: {ErrorCode}. Rejecting principal to force re-authentication.", 
                ex.InnerException is MsalUiRequiredException msalEx ? msalEx.ErrorCode : "unknown");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
        catch (MsalServiceException ex) when (IsTokenCacheCollision(ex))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning("Multiple tokens detected in cache after app recycle: {ErrorCode}. Rejecting principal to force re-authentication.", ex.ErrorCode);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
        catch (MsalClientException ex) when (IsTokenCacheCollision(ex))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning("Token cache collision detected: {Message}. Rejecting principal to force re-authentication.", ex.Message);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }
    
    /// <summary>
    /// Is the exception thrown because there is no account in the token cache?
    /// </summary>
    /// <param name="ex">Exception thrown by <see cref="ITokenAcquisition"/>.GetTokenForXX methods.</param>
    /// <returns>A boolean telling if the exception was about not having an account in the cache</returns>
    private static bool AccountDoesNotExitInTokenCache(MicrosoftIdentityWebChallengeUserException ex)
    {
        return ex.InnerException is MsalUiRequiredException { ErrorCode: "user_null" };
    }
    
    /// <summary>
    /// Detects token cache collision errors (multiple matching tokens in cache)
    /// </summary>
    /// <param name="ex">MSAL exception</param>
    /// <returns>True if this is a cache collision error</returns>
    private static bool IsTokenCacheCollision(MsalException ex)
    {
        return ex.ErrorCode == "multiple_matching_tokens_detected" ||
               ex.Message.Contains("multiple tokens", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("cache contains multiple", StringComparison.OrdinalIgnoreCase);
    }
}
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
        // Fix 1: Extract logger once at method start to avoid multiple service resolutions per request (performance)
        var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
        
        try
        {
            var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                scopes: new[] { "profile" },
                user: context.Principal);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex) when (AccountDoesNotExitInTokenCache(ex))
        {
            // Fix 2: SignOutAsync added to clear stale session cookies before forcing re-authentication.
            // When token cache is empty but cookie still exists (common after app recycle), the stale cookie
            // would cause repeated validation failures. SignOutAsync ensures clean state for re-login flow.
            // This extends the original fix from PR #555 (reverted in #572) to handle issue #81's cache collision scenario.
            logger?.LogWarning("Token cache issue detected during cookie validation: {ErrorCode}. Rejecting principal to force re-authentication.", 
                ex.InnerException is MsalUiRequiredException msalEx ? msalEx.ErrorCode : "unknown");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
        catch (MsalServiceException ex) when (IsTokenCacheCollision(ex))
        {
            logger?.LogWarning("Multiple tokens detected in cache after app recycle: {ErrorCode}. Rejecting principal to force re-authentication.", ex.ErrorCode);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
        catch (MsalClientException ex) when (IsTokenCacheCollision(ex))
        {
            logger?.LogWarning("Token cache collision detected: {ErrorCode}. Rejecting principal to force re-authentication.", ex.ErrorCode);
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
        // Fix 3: Use ErrorCode constants instead of fragile Message string matching
        // MSAL error codes are stable across versions; error messages can change
        return ex.ErrorCode == MsalError.MultipleTokensMatchedError ||
               ex.ErrorCode == MsalError.NoTokensFoundError;
    }
}
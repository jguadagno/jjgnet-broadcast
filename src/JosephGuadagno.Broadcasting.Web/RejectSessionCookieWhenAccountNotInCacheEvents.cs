using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web;

internal class RejectSessionCookieWhenAccountNotInCacheEvents : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // Guard: If the principal is null or unauthenticated (e.g. after SignOut clears the session cookie),
        // skip all token cache operations. Without this guard, calling GetAccessTokenForUserAsync(user: null)
        // throws MsalUiRequiredException with ErrorCode "user_null", which matches AccountDoesNotExitInTokenCache,
        // which calls SignOutAsync again — creating an infinite redirect loop.
        if (context.Principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

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
            logger?.LogWarning("Token cache issue detected during cookie validation: {ErrorCode}. Rejecting principal to force re-authentication.", 
                ex.InnerException is MsalUiRequiredException msalEx ? msalEx.ErrorCode : "unknown");
            context.RejectPrincipal();
        }
        catch (MsalServiceException ex) when (IsTokenCacheCollision(ex))
        {
            logger?.LogWarning("Multiple tokens detected in cache after app recycle: {ErrorCode}. Rejecting principal to force re-authentication.", ex.ErrorCode);
            context.RejectPrincipal();
        }
        catch (MsalClientException ex) when (IsTokenCacheCollision(ex))
        {
            logger?.LogWarning("Token cache collision detected: {ErrorCode}. Rejecting principal to force re-authentication.", ex.ErrorCode);
            context.RejectPrincipal();
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
    
    private static bool IsTokenCacheCollision(MsalException ex)
    {
        return ex.ErrorCode == MsalError.MultipleTokensMatchedError ||
               ex.ErrorCode == MsalError.NoTokensFoundError;
    }
}
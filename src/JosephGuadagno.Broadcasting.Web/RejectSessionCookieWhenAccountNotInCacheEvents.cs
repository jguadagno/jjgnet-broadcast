using Microsoft.AspNetCore.Authentication.Cookies;
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
            context.RejectPrincipal();
        }
        catch (MsalClientException msalEx) when (msalEx.ErrorCode == "multiple_matching_tokens_detected")
        {
            var userName = context.Principal?.Identity?.Name ?? "(unknown)";
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning(msalEx,
                "Token cache collision (multiple_matching_tokens_detected) detected for user {User}. " +
                "Rejecting principal to force re-authentication and a clean cache entry.",
                userName);
            // ITokenAcquisition does not expose a public cache-clear API without acquiring an
            // IConfidentialClientApplication and calling RemoveAsync, which requires the IAccount
            // object — unavailable here since the cache is in a collision state. Rejecting the
            // principal invalidates the session cookie; the next sign-in creates a clean cache entry.
            // The global MsalExceptionMiddleware (Issue #546) provides a second-layer fallback if
            // this ValidatePrincipal path is bypassed.
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
}
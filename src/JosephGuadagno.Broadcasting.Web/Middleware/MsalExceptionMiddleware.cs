using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Middleware;

public class MsalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MsalExceptionMiddleware> _logger;

    public MsalExceptionMiddleware(RequestDelegate next, ILogger<MsalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            // Fallback handler: [AuthorizeForScopes] missed this interaction-required exception.
            _logger.LogWarning(ex,
                "MicrosoftIdentityWebChallengeUserException — interaction required for scopes {Scopes}. Redirecting to re-auth.",
                ex.Scopes);
            context.Response.Redirect("/Account/SignIn?reauth=true");
        }
        catch (MsalServiceException ex)
        {
            // Azure AD service-side error (5xx from AAD). Treat as transient; show friendly error page.
            _logger.LogError(ex,
                "MsalServiceException — Azure AD service error. ErrorCode={ErrorCode} StatusCode={StatusCode}",
                ex.ErrorCode, ex.StatusCode);
            context.Response.Redirect(
                "/Home/AuthError?message=Authentication+service+is+temporarily+unavailable.+Please+try+again+later.");
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "multiple_matching_tokens_detected")
        {
            // Token cache collision (Issue #83). Sign the user out so a fresh cache entry is created.
            var userIdentity = context.User.Identity?.Name ?? "(anonymous)";
            _logger.LogWarning(ex,
                "MsalClientException — multiple_matching_tokens_detected for user {User}. Redirecting to sign-out to clear cache.",
                userIdentity);
            context.Response.Redirect("/Account/SignOut?reason=cache_error");
        }
        catch (MsalClientException ex)
        {
            // Other client-side MSAL errors. Re-auth is the safest recovery.
            _logger.LogWarning(ex,
                "MsalClientException — client error. ErrorCode={ErrorCode}. Redirecting to re-auth.",
                ex.ErrorCode);
            context.Response.Redirect("/Account/SignIn?reauth=true");
        }
    }
}

public static class MsalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseMsalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<MsalExceptionMiddleware>();
}

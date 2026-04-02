using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Web;

/// <summary>
/// Middleware that gates access based on user approval status.
/// Redirects pending users to a pending approval page and rejected users to a rejection page.
/// </summary>
public class UserApprovalMiddleware(RequestDelegate next, ILogger<UserApprovalMiddleware> logger)
{
    private const string ApprovalStatusClaimType = "approval_status";
    private const string PendingApprovalPath = "/Account/PendingApproval";
    private const string RejectedPath = "/Account/Rejected";

    public async Task InvokeAsync(HttpContext context)
    {
        // Pass through if user is not authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var requestPath = context.Request.Path.Value ?? string.Empty;

        // Pass through if already on the approval pages (avoid redirect loops)
        if (requestPath.Equals(PendingApprovalPath, StringComparison.OrdinalIgnoreCase) ||
            requestPath.Equals(RejectedPath, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Pass through for static files and well-known endpoints
        if (IsStaticOrWellKnownPath(requestPath))
        {
            await next(context);
            return;
        }

        // Pass through for identity endpoints (sign-in, sign-out, etc.)
        if (requestPath.StartsWith("/MicrosoftIdentity", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Extract approval status claim
        var approvalStatusClaim = context.User.FindFirst(ApprovalStatusClaimType);
        if (approvalStatusClaim is null)
        {
            // No approval status claim yet - might be during initial login
            // Let the request through and EntraClaimsTransformation will add it on next request
            await next(context);
            return;
        }

        var approvalStatus = approvalStatusClaim.Value;

        // Redirect based on approval status
        if (approvalStatus.Equals(ApprovalStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "User {IdentityName} with approval status {ApprovalStatus} redirected to pending approval page",
                context.User.Identity?.Name ?? "unknown", approvalStatus);
            
            context.Response.Redirect(PendingApprovalPath);
            return;
        }

        if (approvalStatus.Equals(ApprovalStatus.Rejected.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "User {IdentityName} with approval status {ApprovalStatus} redirected to rejected page",
                context.User.Identity?.Name ?? "unknown", approvalStatus);
            
            context.Response.Redirect(RejectedPath);
            return;
        }

        // Approved users or any other status - pass through
        await next(context);
    }

    private static bool IsStaticOrWellKnownPath(string path)
    {
        return path.StartsWith("/.well-known", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension methods for registering UserApprovalMiddleware
/// </summary>
public static class UserApprovalMiddlewareExtensions
{
    /// <summary>
    /// Adds the user approval gate middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseUserApprovalGate(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserApprovalMiddleware>();
    }
}

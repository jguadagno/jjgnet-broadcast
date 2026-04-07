using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for the Facebook Graph API integration.
/// Validates that the minimum required credentials (AppId and PageAccessToken) are configured.
/// An unconfigured page access token means all Facebook publishing operations will fail at runtime.
/// </summary>
internal sealed class FacebookHealthCheck : IHealthCheck
{
    private readonly IFacebookApplicationSettings _settings;

    public FacebookHealthCheck(IFacebookApplicationSettings settings)
    {
        _settings = settings;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.AppId))
            missing.Add(nameof(_settings.AppId));

        if (string.IsNullOrWhiteSpace(_settings.PageId))
            missing.Add(nameof(_settings.PageId));

        if (string.IsNullOrWhiteSpace(_settings.PageAccessToken))
            missing.Add(nameof(_settings.PageAccessToken));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Facebook application configuration is incomplete. Missing: {string.Join(", ", missing)}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Facebook application configuration is valid."));
    }
}

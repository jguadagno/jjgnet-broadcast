using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for the LinkedIn API integration.
/// Validates that the required OAuth credentials and author identity are configured.
/// A missing access token or author ID means all LinkedIn publishing operations will fail at runtime.
/// </summary>
internal sealed class LinkedInHealthCheck(ILinkedInApplicationSettings settings) : IHealthCheck
{
	public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.ClientId))
            missing.Add(nameof(settings.ClientId));

        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            missing.Add(nameof(settings.AccessToken));

        if (string.IsNullOrWhiteSpace(settings.AuthorId))
            missing.Add(nameof(settings.AuthorId));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"LinkedIn application configuration is incomplete. Missing: {string.Join(", ", missing)}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("LinkedIn application configuration is valid."));
    }
}

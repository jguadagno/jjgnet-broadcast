using JosephGuadagno.Utilities.Web.Shortener.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for the Bitly URL shortener API.
/// Validates that the Bitly API token and root URI are configured.
/// A missing or empty token means URL shortening will fail silently at runtime.
/// Returns <see cref="HealthCheckResult.Degraded"/> (not Unhealthy) when configuration is missing,
/// because Bitly is an optional/non-critical service — the app continues to publish content
/// with unshortened URLs and should not trigger a load-balancer failover.
/// </summary>
internal sealed class BitlyHealthCheck(IBitlyConfiguration bitlyConfiguration) : IHealthCheck
{
	public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(bitlyConfiguration.Token))
            missing.Add(nameof(bitlyConfiguration.Token));

        if (string.IsNullOrWhiteSpace(bitlyConfiguration.ApiRootUri))
            missing.Add(nameof(bitlyConfiguration.ApiRootUri));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Bitly configuration is incomplete. Missing: {string.Join(", ", missing)}. URL shortening will be skipped; content publishing is unaffected."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Bitly API configuration is valid."));
    }
}

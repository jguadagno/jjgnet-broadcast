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
internal sealed class BitlyHealthCheck : IHealthCheck
{
    private readonly IBitlyConfiguration _bitlyConfiguration;

    public BitlyHealthCheck(IBitlyConfiguration bitlyConfiguration)
    {
        _bitlyConfiguration = bitlyConfiguration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(_bitlyConfiguration.Token))
            missing.Add(nameof(_bitlyConfiguration.Token));

        if (string.IsNullOrWhiteSpace(_bitlyConfiguration.ApiRootUri))
            missing.Add(nameof(_bitlyConfiguration.ApiRootUri));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Bitly configuration is incomplete. Missing: {string.Join(", ", missing)}. URL shortening will be skipped; content publishing is unaffected."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Bitly API configuration is valid."));
    }
}

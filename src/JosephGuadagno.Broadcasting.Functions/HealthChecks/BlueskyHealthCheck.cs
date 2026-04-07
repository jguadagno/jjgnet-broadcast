using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for the Bluesky (AT Protocol) API integration.
/// Validates that the Bluesky account credentials are configured.
/// Missing credentials mean all Bluesky posting operations will fail at runtime.
/// </summary>
internal sealed class BlueskyHealthCheck : IHealthCheck
{
    private readonly IBlueskySettings _settings;

    public BlueskyHealthCheck(IBlueskySettings settings)
    {
        _settings = settings;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.BlueskyUserName))
            missing.Add(nameof(_settings.BlueskyUserName));

        if (string.IsNullOrWhiteSpace(_settings.BlueskyPassword))
            missing.Add(nameof(_settings.BlueskyPassword));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Bluesky configuration is incomplete. Missing: {string.Join(", ", missing)}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Bluesky configuration is valid."));
    }
}

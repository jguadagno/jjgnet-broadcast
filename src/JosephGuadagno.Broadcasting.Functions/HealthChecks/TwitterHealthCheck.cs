using LinqToTwitter.OAuth;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for the Twitter/X API integration.
/// Validates that all required OAuth credentials are configured.
/// An unconfigured credential store means all tweet operations will fail at runtime.
/// </summary>
internal sealed class TwitterHealthCheck(InMemoryCredentialStore credentialStore) : IHealthCheck
{
	public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(credentialStore.ConsumerKey))
            missing.Add(nameof(credentialStore.ConsumerKey));

        if (string.IsNullOrWhiteSpace(credentialStore.ConsumerSecret))
            missing.Add(nameof(credentialStore.ConsumerSecret));

        if (string.IsNullOrWhiteSpace(credentialStore.OAuthToken))
            missing.Add(nameof(credentialStore.OAuthToken));

        if (string.IsNullOrWhiteSpace(credentialStore.OAuthTokenSecret))
            missing.Add(nameof(credentialStore.OAuthTokenSecret));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Twitter/X OAuth configuration is incomplete. Missing: {string.Join(", ", missing)}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Twitter/X OAuth configuration is valid."));
    }
}

using LinqToTwitter.OAuth;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for the Twitter/X API integration.
/// Validates that all required OAuth credentials are configured.
/// An unconfigured credential store means all tweet operations will fail at runtime.
/// </summary>
internal sealed class TwitterHealthCheck : IHealthCheck
{
    private readonly InMemoryCredentialStore _credentialStore;

    public TwitterHealthCheck(InMemoryCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(_credentialStore.ConsumerKey))
            missing.Add(nameof(_credentialStore.ConsumerKey));

        if (string.IsNullOrWhiteSpace(_credentialStore.ConsumerSecret))
            missing.Add(nameof(_credentialStore.ConsumerSecret));

        if (string.IsNullOrWhiteSpace(_credentialStore.OAuthToken))
            missing.Add(nameof(_credentialStore.OAuthToken));

        if (string.IsNullOrWhiteSpace(_credentialStore.OAuthTokenSecret))
            missing.Add(nameof(_credentialStore.OAuthTokenSecret));

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Twitter/X OAuth configuration is incomplete. Missing: {string.Join(", ", missing)}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Twitter/X OAuth configuration is valid."));
    }
}

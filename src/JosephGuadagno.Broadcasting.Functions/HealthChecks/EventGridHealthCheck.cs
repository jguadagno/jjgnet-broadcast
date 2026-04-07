using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Functions.HealthChecks;

/// <summary>
/// Health check for Azure EventGrid topic endpoints.
/// Validates that at least one topic endpoint is configured with a non-empty Endpoint URL and Key.
/// An empty or missing topic configuration means event publishing will fail silently at runtime.
/// </summary>
internal sealed class EventGridHealthCheck : IHealthCheck
{
    private readonly IEventPublisherSettings _settings;

    public EventGridHealthCheck(IEventPublisherSettings settings)
    {
        _settings = settings;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_settings.TopicEndpointSettings is null || _settings.TopicEndpointSettings.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "No EventGrid topic endpoints are configured."));
        }

        var misconfigured = _settings.TopicEndpointSettings
            .Where(t => string.IsNullOrWhiteSpace(t.Endpoint) || string.IsNullOrWhiteSpace(t.Key))
            .Select(t => t.TopicName ?? "(unnamed)")
            .ToList();

        if (misconfigured.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"EventGrid topic(s) have missing Endpoint or Key: {string.Join(", ", misconfigured)}."));
        }

        var topicCount = _settings.TopicEndpointSettings.Count;
        return Task.FromResult(HealthCheckResult.Healthy(
            $"EventGrid configuration is valid. {topicCount} topic endpoint(s) configured."));
    }
}

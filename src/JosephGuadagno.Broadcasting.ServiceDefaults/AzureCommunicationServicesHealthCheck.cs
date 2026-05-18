using Azure.Communication.Email;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.ServiceDefaults;

/// <summary>
/// Health check for Azure Communication Services.
/// Validates that the connection string is well-formed and the SDK client can be instantiated.
/// Returns Degraded (not Unhealthy) because email is a non-critical path.
/// </summary>
internal sealed class AzureCommunicationServicesHealthCheck(string connectionString) : IHealthCheck
{
	public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded("Azure Communication Services connection string is not configured."));
            }

            // Validate the connection string is well-formed by attempting to instantiate the client.
            // This does NOT send any email — it only parses the connection string and creates the client object.
            _ = new EmailClient(connectionString);

            return Task.FromResult(HealthCheckResult.Healthy("Azure Communication Services connection string is valid."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded(
                    "Azure Communication Services connection string is missing or malformed.",
                    exception: ex));
        }
    }
}

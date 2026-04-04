using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JosephGuadagno.Broadcasting.Web.HealthChecks;

/// <summary>
/// Health check for Azure Key Vault.
/// Probes connectivity by listing secret properties (does not fetch secret values).
/// </summary>
internal sealed class AzureKeyVaultHealthCheck : IHealthCheck
{
    private readonly SecretClient _secretClient;

    public AzureKeyVaultHealthCheck(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Enumerate the first page of secret properties only — does NOT fetch secret values.
            await foreach (var _ in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken)
                               .AsPages(pageSizeHint: 1)
                               .WithCancellation(cancellationToken))
            {
                // One page is enough to confirm connectivity.
                break;
            }

            return HealthCheckResult.Healthy("Azure Key Vault is reachable.");
        }
        catch (RequestFailedException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Azure Key Vault health check failed: {ex.Message}",
                exception: ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Key Vault health check encountered an unexpected error.",
                exception: ex);
        }
    }
}

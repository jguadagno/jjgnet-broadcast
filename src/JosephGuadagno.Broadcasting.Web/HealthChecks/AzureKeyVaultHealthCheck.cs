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
            var secret = await _secretClient.GetSecretAsync("AzureKeyVaultSecretsHealthCheck", null, cancellationToken);
            return secret is null
                ? HealthCheckResult.Unhealthy("Azure Key Vault health check failed: Secret not found.")
                : HealthCheckResult.Healthy("Azure Key Vault is reachable.");
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
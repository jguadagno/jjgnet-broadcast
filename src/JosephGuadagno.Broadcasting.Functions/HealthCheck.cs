using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions;

/// <summary>
/// Lightweight health endpoint for App Service Health Check probing and uptime monitoring.
/// Route: GET /api/health
/// Returns HTTP 200 with JSON body on success, HTTP 503 on any connectivity or configuration failure.
/// Infrastructure checks (queue/table storage) run inline.
/// External-dependency readiness checks (Bitly, Twitter, Facebook, LinkedIn, Bluesky, EventGrid)
/// are delegated to registered <see cref="IHealthCheck"/> implementations via <see cref="HealthCheckService"/>.
/// </summary>
public class HealthCheck(
    QueueServiceClient queueServiceClient,
    TableServiceClient tableServiceClient,
    HealthCheckService healthCheckService,
    ILogger<HealthCheck> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    [Function("HealthCheck")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Health check requested");

        var checks = new List<(string Name, bool Healthy, string? Message)>();

        // Check queue storage connectivity
        checks.Add(await CheckQueueStorageAsync(cancellationToken));

        // Check table storage connectivity (Serilog logging sink)
        checks.Add(await CheckTableStorageAsync(cancellationToken));

        // Run all registered IHealthCheck implementations tagged "ready"
        // (Bitly, Twitter, Facebook, LinkedIn, Bluesky, EventGrid)
        var healthReport = await healthCheckService.CheckHealthAsync(
            r => r.Tags.Contains("ready"), cancellationToken);

        foreach (var entry in healthReport.Entries)
        {
            checks.Add((
                entry.Key,
                entry.Value.Status == HealthStatus.Healthy,
                entry.Value.Status == HealthStatus.Healthy ? null : entry.Value.Description));
        }

        var allHealthy = checks.All(c => c.Healthy);
        var timestamp = DateTimeOffset.UtcNow;

        var responseBody = new
        {
            status = allHealthy ? "Healthy" : "Unhealthy",
            timestamp = timestamp.ToString("O"),
            checks = checks.Select(c => new { name = c.Name, healthy = c.Healthy, message = c.Message })
        };

        var json = JsonSerializer.Serialize(responseBody, JsonOptions);

        if (allHealthy)
        {
            logger.LogDebug("Health check passed at {Timestamp}", timestamp);
            return new ContentResult
            {
                Content = json,
                ContentType = "application/json",
                StatusCode = StatusCodes.Status200OK
            };
        }

        var failedChecks = checks.Where(c => !c.Healthy).Select(c => c.Name);
        logger.LogWarning("Health check failed at {Timestamp}. Failed checks: {FailedChecks}",
            timestamp, string.Join(", ", failedChecks));

        return new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }

    private async Task<(string Name, bool Healthy, string? Message)> CheckQueueStorageAsync(
        CancellationToken cancellationToken)
    {
        const string name = "queue-storage";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            // List one queue to verify connectivity -- does not modify any data.
            await foreach (var _ in queueServiceClient.GetQueuesAsync(cancellationToken: cts.Token)
                               .AsPages(pageSizeHint: 1).WithCancellation(cts.Token))
            {
                break;
            }

            return (name, true, null);
        }
        catch (OperationCanceledException)
        {
            return (name, false, "Queue storage probe timed out.");
        }
        catch (Exception ex)
        {
            return (name, false, $"Queue storage unreachable: {ex.Message}");
        }
    }

    private async Task<(string Name, bool Healthy, string? Message)> CheckTableStorageAsync(
        CancellationToken cancellationToken)
    {
        const string name = "table-storage";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            // List one table to verify connectivity -- does not modify any data.
            await foreach (var _ in tableServiceClient.QueryAsync(cancellationToken: cts.Token)
                               .AsPages(pageSizeHint: 1).WithCancellation(cts.Token))
            {
                break;
            }

            return (name, true, null);
        }
        catch (OperationCanceledException)
        {
            return (name, false, "Table storage probe timed out.");
        }
        catch (Exception ex)
        {
            return (name, false, $"Table storage unreachable: {ex.Message}");
        }
    }
}

using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions;

/// <summary>
/// Lightweight health endpoint for App Service Health Check probing and uptime monitoring.
/// Route: GET /api/health
/// Returns HTTP 200 with JSON body on success, HTTP 503 on any storage connectivity failure.
/// </summary>
public class HealthCheck(IConfiguration configuration, ILogger<HealthCheck> logger)
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
        var queueConnStr = configuration.GetConnectionString("QueueStorage");
        checks.Add(await CheckQueueStorageAsync(queueConnStr, cancellationToken));

        // Check table storage connectivity (Serilog logging sink)
        var tableConnStr = configuration["Settings:LoggingStorageAccount"];
        checks.Add(await CheckTableStorageAsync(tableConnStr, cancellationToken));

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

    private static async Task<(string Name, bool Healthy, string? Message)> CheckQueueStorageAsync(
        string? connectionString,
        CancellationToken cancellationToken)
    {
        const string name = "queue-storage";
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (name, false, "Connection string 'QueueStorage' is not configured.");
        }

        try
        {
            var serviceClient = new QueueServiceClient(connectionString);
            // List one queue to verify connectivity — does not modify any data.
            await foreach (var _ in serviceClient.GetQueuesAsync(cancellationToken: cancellationToken)
                               .AsPages(pageSizeHint: 1).WithCancellation(cancellationToken))
            {
                break;
            }

            return (name, true, null);
        }
        catch (Exception ex)
        {
            return (name, false, $"Queue storage unreachable: {ex.Message}");
        }
    }

    private static async Task<(string Name, bool Healthy, string? Message)> CheckTableStorageAsync(
        string? connectionString,
        CancellationToken cancellationToken)
    {
        const string name = "table-storage";
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (name, false, "Configuration key 'Settings:LoggingStorageAccount' is not configured.");
        }

        try
        {
            var serviceClient = new TableServiceClient(connectionString);
            // List one table to verify connectivity — does not modify any data.
            await foreach (var _ in serviceClient.QueryAsync(cancellationToken: cancellationToken)
                               .AsPages(pageSizeHint: 1).WithCancellation(cancellationToken))
            {
                break;
            }

            return (name, true, null);
        }
        catch (Exception ex)
        {
            return (name, false, $"Table storage unreachable: {ex.Message}");
        }
    }
}

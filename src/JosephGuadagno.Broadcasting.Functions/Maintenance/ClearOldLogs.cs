using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using JosephGuadagno.Broadcasting.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Maintenance;

public class ClearOldLogs
{
    
    private readonly ILogger<ClearOldLogs> _logger;

    public ClearOldLogs(ILogger<ClearOldLogs> logger)
    {
        _logger = logger;
    }
        
    [Function("maintenance_clear_old_logs")]
    public async Task RunAsync(
        [TimerTrigger("%maintenance_clear_old_logs_cron_settings%")] TimerInfo myTimer,
        [TableInput(Constants.Tables.Logging)] TableClient tableClient)
    {
        // 0 */2 * * * * - Every 2 minutes
        // 0 23 * * 0 - Run at 11pm on Sunday
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.MaintenanceClearOldLogs, startedAt);
        var numberOfItemsDeleted = 0;
        var numberOfItemsDeletedFailed = 0;

        // Get all the log messages older than a week
        #if DEBUG
        var dateTimeAsString = DateTime.UtcNow.AddMinutes(-2).ToString("s");
        #else
        var dateTimeAsString = DateTime.UtcNow.AddDays(-7).ToString("s");
        #endif
        var filter = $"Timestamp le datetime'{dateTimeAsString}'";
        AsyncPageable<TableEntity> queryResults = tableClient.QueryAsync<TableEntity>(filter: filter);
        
        // Delete them
        await foreach (var tableEntity in queryResults)
        {
            
            var response = await tableClient.DeleteEntityAsync(tableEntity.PartitionKey, tableEntity.RowKey, tableEntity.ETag);
            if (response.Status == (int)HttpStatusCode.NoContent)
            {
                numberOfItemsDeleted++;    
            }
            else
            {
                Console.WriteLine($"Failed: Primary Key: {tableEntity.PartitionKey}, Row Key: {tableEntity.RowKey}");
                numberOfItemsDeletedFailed++;
            }
            
        }
        
        // Return
        _logger.LogDebug("Delete {NumberOfItemsDeleted} log messages, failed to delete {NumberOfItemsDeletedFailed} log messages",
            numberOfItemsDeleted, numberOfItemsDeletedFailed);
    }
}
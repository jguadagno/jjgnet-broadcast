using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using JosephGuadagno.Broadcasting.Domain;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Maintenance;

public class ClearOldLogs
{
    
    private readonly ILogger<ClearOldLogs> _logger;

    public ClearOldLogs(ILogger<ClearOldLogs> logger)
    {
        _logger = logger;
    }
        
    [FunctionName("maintenance_clear_old_logs")]
    public async Task RunAsync(
        [TimerTrigger("0 22 * * * 1")] TimerInfo myTimer,
        [Table(Constants.Tables.Logging)] TableClient tableClient)
    {
        // 0 */2 * * * *
        // 0 23 * * 0 - Run at 11pm on Sunday
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.MaintenanceClearOldLogs, startedAt);
        var numberOfItemsDeleted = 0;

        // Get all the log messages older than a week
        var dateTimeAsString = DateTime.UtcNow.AddDays(-7).ToString("s");
        var filter = $"Timestamp le datetime'{dateTimeAsString}'";
        AsyncPageable<TableEntity> queryResults = tableClient.QueryAsync<TableEntity>(filter: filter);
        
        // Delete them
        await foreach (var tableEntity in queryResults)
        {
            await tableClient.DeleteEntityAsync(tableEntity.PartitionKey, tableEntity.RowKey);
            numberOfItemsDeleted++;
        }
        
        // Return
        _logger.LogDebug("Cleaned up {numberOfItemsDeleted} log messages");
    }
}
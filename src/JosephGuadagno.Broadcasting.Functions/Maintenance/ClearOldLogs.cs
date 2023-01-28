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
        [TimerTrigger("0 23 * * 0")] TimerInfo myTimer,
        [Table(Constants.Tables.Logging)] TableClient tableClient)
    {
        // 0 */2 * * * *
        // 0 23 * * 0 - Run at 11pm on Sunday
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.MaintenanceClearOldLogs, startedAt);

        // Get all the log messages older than a week
        AsyncPageable<TableEntity> queryResults = tableClient.QueryAsync<TableEntity>(filter: $"Timestamp le datetime'{DateTime.UtcNow.AddDays(-7)}'");

        // Delete them
        await queryResults.AsPages().ForEachAwaitAsync(async page => {
            // Since we don't know how many rows the table has and the results are ordered by PartitionKey+RowKey
            // we'll delete each page immediately and not cache the whole table in memory
            await BatchManipulateEntities(tableClient, page.Values, TableTransactionActionType.Delete).ConfigureAwait(false);
        });
        
        // Return
        _logger.LogDebug("Cleaned up old log messages");
    }
    
    /// <summary>
    /// Groups entities by PartitionKey into batches of max 100 for valid transactions
    /// </summary>
    /// <returns>List of Azure Responses for Transactions</returns>
    private  async Task BatchManipulateEntities<T>(TableClient tableClient, IEnumerable<T> entities,
        TableTransactionActionType tableTransactionActionType) where T : class, ITableEntity, new()
    {
        var groups = entities.GroupBy(x => x.PartitionKey);
        var responses = new List<Response<IReadOnlyList<Response>>>();
        foreach (var group in groups)
        {
            var items = group.AsEnumerable();
            while (items.Any())
            {
                var batch = items.Take(100);
                items = items.Skip(100);

                var actions = new List<TableTransactionAction>();
                actions.AddRange(batch.Select(e => new TableTransactionAction(tableTransactionActionType, e)));
                var response = await tableClient.SubmitTransactionAsync(actions).ConfigureAwait(false);
                responses.Add(response);
            }
        }
    }
}
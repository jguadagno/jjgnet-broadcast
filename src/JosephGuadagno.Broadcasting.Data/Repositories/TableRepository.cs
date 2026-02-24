using JosephGuadagno.AzureHelpers.Cosmos;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class TableRepository<T> where T : TableEntity, new()
{
    private readonly Table _table;

    protected TableRepository(string connectionString, string tableName)
    {
        _table = GetTable(connectionString, tableName);
    }

    protected virtual Table GetTable(string connectionString, string tableName)
    {
        return new Table(connectionString, tableName);
    }

    public virtual async Task<T> GetAsync(string partitionKey, string rowKey)
    {
        return await GetTableEntityAsync<T>(partitionKey, rowKey);
    }
        
    public virtual async Task<bool> SaveAsync(T entity)
    {
        var tableResult = await InsertOrReplaceEntityAsync(entity);
        return tableResult.WasSuccessful;
    }

    public virtual async Task<bool> AddAllAsync(List<T> entities)
    {
        if (entities == null)
        {
            return false;
        }

        var allSuccessful = true;
        foreach (var entity in entities)
        {
            var wasSuccessful = await SaveAsync(entity);
            if (wasSuccessful == false)
            {
                allSuccessful = false;
            }
        }

        return allSuccessful;
    }

    public virtual async Task<List<T>> GetAllAsync(string partitionKey)
    {
        return await GetPartitionAsync<T>(partitionKey);
    }

    protected virtual async Task<T> GetTableEntityAsync<T>(string partitionKey, string rowKey) where T : TableEntity, new()
    {
        return await _table.GetTableEntityAsync<T>(partitionKey, rowKey);
    }

    protected virtual async Task<TableOperationResult> InsertOrReplaceEntityAsync(T entity)
    {
        return await _table.InsertOrReplaceEntityAsync(entity);
    }

    protected virtual async Task<List<T>> GetPartitionAsync<T>(string partitionKey) where T : TableEntity, new()
    {
        return await _table.GetPartitionAsync<T>(partitionKey);
    }
}
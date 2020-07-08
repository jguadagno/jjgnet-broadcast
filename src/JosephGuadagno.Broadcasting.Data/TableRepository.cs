using System;
using System.Threading.Tasks;
using JosephGuadagno.AzureHelpers.Cosmos;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Data
{
    public class TableRepository<T> where T : TableEntity
    {
        private readonly Table _table;
        
        public TableRepository(string connectionString, string tableName)
        {
            _table = new Table(connectionString, tableName);
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            return await _table.GetTableEntityAsync<T>(partitionKey, rowKey);
        }
        
        public async Task<bool> SaveAsync(T entity)
        {
            var tableResult = await _table.InsertOrReplaceEntityAsync(entity);
            return tableResult.WasSuccessful;
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.AzureHelpers.Cosmos;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Data.Repositories
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

        public async Task<bool> AddAllAsync(List<T> entities)
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
    }
}
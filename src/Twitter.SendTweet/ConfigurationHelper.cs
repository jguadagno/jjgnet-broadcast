using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Twitter
{
    public class ConfigurationHelper
    {
        private CloudTable _cloudTable;
        
        public ConfigurationHelper(string storageConnectionString, string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var cloudTableClient = storageAccount.CreateCloudTableClient();
            _cloudTable = cloudTableClient.GetTableReference(tableName);
        }

        public async Task<Configuration> GetConfigurationAsync()
        {
            var configuration = new Configuration();
            var retrieveTableOperation =
                TableOperation.Retrieve<Configuration>(configuration.PartitionKey, configuration.RowKey);
            var result = await _cloudTable.ExecuteAsync(retrieveTableOperation);
            return result?.Result as Configuration;
        }

        public async Task<bool> SaveConfigurationAsync(Configuration configuration)
        {
            var insertOrReplaceOperation = TableOperation.InsertOrReplace(configuration);
            var tableResult = await _cloudTable.ExecuteAsync(insertOrReplaceOperation);

            return tableResult.HttpStatusCode == (int) HttpStatusCode.NoContent;
        }
    }
}
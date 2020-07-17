using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Domain.Models
{
    /// <summary>
    /// This is the configuration that is saved to and from Table Storage for each function
    /// </summary>
    public class ConfigurationBase: TableEntity
    {
        public ConfigurationBase(string functionName)
        {
            PartitionKey = functionName;
            RowKey = Constants.Tables.Configuration;
        }
    }
}
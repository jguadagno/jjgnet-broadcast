using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data
{
    public class ConfigurationRepository: TableRepository<CollectorConfiguration>
    {
        public ConfigurationRepository(string connectionString) : base(connectionString, Domain.Constants.Tables.Configuration)
        {
        }
    }
}
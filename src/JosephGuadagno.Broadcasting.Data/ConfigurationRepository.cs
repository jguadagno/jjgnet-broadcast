using JosephGuadagno.Broadcasting.Domain;

namespace JosephGuadagno.Broadcasting.Data
{
    public class ConfigurationRepository: TableRepository<NewPostCheckerConfiguration>
    {
        public ConfigurationRepository(string connectionString) : base(connectionString, "Configuration")
        {
        }
    }
}
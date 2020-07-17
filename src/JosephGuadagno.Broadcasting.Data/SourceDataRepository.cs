using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data
{
    public class SourceDataRepository: TableRepository<SourceData>
    {
        public SourceDataRepository(string connectionString) : base(connectionString, Domain.Constants.Tables.SourceData)
        {
            
        }
    }
}
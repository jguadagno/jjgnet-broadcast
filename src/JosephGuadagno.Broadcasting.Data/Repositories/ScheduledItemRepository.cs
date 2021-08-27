using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories
{
    public class ScheduledItemRepository: TableRepository<ScheduledItem>
    {
        public ScheduledItemRepository(string connectionString) : base(connectionString,
            Domain.Constants.Tables.ScheduledItems)
        {
            
        }
    }
}
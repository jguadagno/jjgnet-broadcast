using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IScheduledItemRepository : IDataRepository<ScheduledItem>
{
    public Task<List<ScheduledItem>> GetUpcomingScheduledItemsAsync(DateTimeOffset lastChecked);
    public Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn);
}
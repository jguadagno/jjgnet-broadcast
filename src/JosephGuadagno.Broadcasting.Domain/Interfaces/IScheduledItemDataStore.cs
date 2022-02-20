using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IScheduledItemDataStore : IDataRepository<ScheduledItem>
{
    public Task<List<ScheduledItem>> GetUpcomingScheduledItemsAsync();
    public Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn);
}
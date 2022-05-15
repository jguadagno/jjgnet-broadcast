using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IScheduledItemManager : IManager<ScheduledItem>
{
    public Task<List<ScheduledItem>> GetScheduledItemsToSendAsync();
    public Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync();
    public Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month);
    public Task<bool> SentScheduledItemAsync(int primaryKey);
    public Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn);
}
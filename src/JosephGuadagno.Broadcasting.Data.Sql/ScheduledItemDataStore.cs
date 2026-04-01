using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class ScheduledItemDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IScheduledItemDataStore
{
    public async Task<Domain.Models.ScheduledItem> GetAsync(int primaryKey)
    {
        var dbScheduledItem = await broadcastingContext.ScheduledItems.FindAsync(primaryKey);
        return mapper.Map<Domain.Models.ScheduledItem>(dbScheduledItem);
    }

    public async Task<Domain.Models.ScheduledItem> SaveAsync(Domain.Models.ScheduledItem scheduledItem)
    {
        var dbScheduledItem = mapper.Map<Models.ScheduledItem>(scheduledItem);
        broadcastingContext.Entry(dbScheduledItem).State =
            dbScheduledItem.Id == 0 ? EntityState.Added : EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.ScheduledItem>(dbScheduledItem);
        }

        throw new ApplicationException("Failed to save scheduled item");
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetAllAsync()
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems.ToListAsync();
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<bool> DeleteAsync(Domain.Models.ScheduledItem scheduledItem)
    {
        return await DeleteAsync(scheduledItem.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbScheduledItem = await broadcastingContext.ScheduledItems.FindAsync(primaryKey);
        if (dbScheduledItem is null)
        {
            return false;
        }
        broadcastingContext.ScheduledItems.Remove(dbScheduledItem);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetScheduledItemsToSendAsync()
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false && si.SendOnDateTime <= DateTimeOffset.Now)
            .ToListAsync();
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetUnsentScheduledItemsAsync()
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false)
            .ToListAsync();
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 11, 59, 59);
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.SendOnDateTime >= startDate && si.SendOnDateTime <= endDate)
            .ToListAsync();
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }
    
    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems.FindAsync(primaryKey);
        if (dbScheduledItems is null)
        {
            return false;
        }

        dbScheduledItems.MessageSentOn = sentOn;
        dbScheduledItems.MessageSent = true;
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<IEnumerable<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync()
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(s =>
                (s.ItemTableName == ScheduledItemType.Engagements.ToString() &&
                 !broadcastingContext.Engagements.Any(e => e.Id == s.ItemPrimaryKey)) ||
                (s.ItemTableName == ScheduledItemType.Talks.ToString() &&
                 !broadcastingContext.Talks.Any(t => t.Id == s.ItemPrimaryKey)) ||
                (s.ItemTableName == ScheduledItemType.SyndicationFeedSources.ToString() &&
                 !broadcastingContext.SyndicationFeedSources.Any(sf => sf.Id == s.ItemPrimaryKey)) ||
                (s.ItemTableName == ScheduledItemType.YouTubeSources.ToString() &&
                 !broadcastingContext.YouTubeSources.Any(y => y.Id == s.ItemPrimaryKey))
            )
            .ToListAsync();

        return mapper.Map<IEnumerable<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetAllAsync(int page, int pageSize)
    {
        var totalCount = await broadcastingContext.ScheduledItems.CountAsync();
        var dbItems = await broadcastingContext.ScheduledItems
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetUnsentScheduledItemsAsync(int page, int pageSize)
    {
        var query = broadcastingContext.ScheduledItems.Where(si => !si.MessageSent);
        var totalCount = await query.CountAsync();
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetScheduledItemsToSendAsync(int page, int pageSize)
    {
        var query = broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false && si.SendOnDateTime <= DateTimeOffset.Now);
        var totalCount = await query.CountAsync();
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int page, int pageSize)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 11, 59, 59);
        var query = broadcastingContext.ScheduledItems
            .Where(si => si.SendOnDateTime >= startDate && si.SendOnDateTime <= endDate);
        var totalCount = await query.CountAsync();
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync(int page, int pageSize)
    {
        var query = broadcastingContext.ScheduledItems
            .Where(s =>
                (s.ItemTableName == ScheduledItemType.Engagements.ToString() &&
                 !broadcastingContext.Engagements.Any(e => e.Id == s.ItemPrimaryKey)) ||
                (s.ItemTableName == ScheduledItemType.Talks.ToString() &&
                 !broadcastingContext.Talks.Any(t => t.Id == s.ItemPrimaryKey)) ||
                (s.ItemTableName == ScheduledItemType.SyndicationFeedSources.ToString() &&
                 !broadcastingContext.SyndicationFeedSources.Any(sf => sf.Id == s.ItemPrimaryKey)) ||
                (s.ItemTableName == ScheduledItemType.YouTubeSources.ToString() &&
                 !broadcastingContext.YouTubeSources.Any(y => y.Id == s.ItemPrimaryKey))
            );
        var totalCount = await query.CountAsync();
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }
}
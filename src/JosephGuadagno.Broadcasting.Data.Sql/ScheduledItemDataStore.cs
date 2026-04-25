using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class ScheduledItemDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<ScheduledItemDataStore> logger) : IScheduledItemDataStore
{
    public async Task<Domain.Models.ScheduledItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbScheduledItem = await broadcastingContext.ScheduledItems.FindAsync(new object[] { primaryKey }, cancellationToken);
        return mapper.Map<Domain.Models.ScheduledItem>(dbScheduledItem);
    }

    public async Task<OperationResult<Domain.Models.ScheduledItem>> SaveAsync(Domain.Models.ScheduledItem scheduledItem, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbScheduledItem = mapper.Map<Models.ScheduledItem>(scheduledItem);
            broadcastingContext.Entry(dbScheduledItem).State =
                dbScheduledItem.Id == 0 ? EntityState.Added : EntityState.Modified;

            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result) return OperationResult<Domain.Models.ScheduledItem>.Success(mapper.Map<Domain.Models.ScheduledItem>(dbScheduledItem));
            return OperationResult<Domain.Models.ScheduledItem>.Failure("Failed to save scheduled item");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save scheduled item {ScheduledItemId}", scheduledItem.Id);
            return OperationResult<Domain.Models.ScheduledItem>.Failure("An error occurred while saving the scheduled item", ex);
        }
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.ScheduledItem scheduledItem, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(scheduledItem.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbScheduledItem = await broadcastingContext.ScheduledItems.FindAsync(new object[] { primaryKey }, cancellationToken);
            if (dbScheduledItem is null) return OperationResult<bool>.Failure("Scheduled item not found");
            broadcastingContext.ScheduledItems.Remove(dbScheduledItem);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete scheduled item {ScheduledItemId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the scheduled item", ex);
        }
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetScheduledItemsToSendAsync(CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false && si.SendOnDateTime <= DateTimeOffset.Now)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetUnsentScheduledItemsAsync(CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetUnsentScheduledItemsAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.CreatedByEntraOid == ownerEntraOid && si.MessageSent == false)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 11, 59, 59);
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.SendOnDateTime >= startDate && si.SendOnDateTime <= endDate)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(string ownerEntraOid, int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 11, 59, 59);
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(si => si.CreatedByEntraOid == ownerEntraOid && si.SendOnDateTime >= startDate && si.SendOnDateTime <= endDate)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }
    
    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn, CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems.FindAsync(new object[] { primaryKey }, cancellationToken);
        if (dbScheduledItems is null) return false;

        dbScheduledItems.MessageSentOn = sentOn;
        dbScheduledItems.MessageSent = true;
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
    }

    public async Task<IEnumerable<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync(CancellationToken cancellationToken = default)
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
            .ToListAsync(cancellationToken);

        return mapper.Map<IEnumerable<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<IEnumerable<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbScheduledItems = await broadcastingContext.ScheduledItems
            .Where(s => s.CreatedByEntraOid == ownerEntraOid)
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
            .ToListAsync(cancellationToken);

        return mapper.Map<IEnumerable<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await broadcastingContext.ScheduledItems.CountAsync(cancellationToken);
        var dbItems = await broadcastingContext.ScheduledItems
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.ScheduledItems.Where(si => si.CreatedByEntraOid == ownerEntraOid);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetUnsentScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.ScheduledItems.Where(si => !si.MessageSent);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetUnsentScheduledItemsAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.ScheduledItems.Where(si => si.CreatedByEntraOid == ownerEntraOid && !si.MessageSent);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetScheduledItemsToSendAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false && si.SendOnDateTime <= DateTimeOffset.Now);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 11, 59, 59);
        var query = broadcastingContext.ScheduledItems
            .Where(si => si.SendOnDateTime >= startDate && si.SendOnDateTime <= endDate);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(string ownerEntraOid, int year, int month, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0);
        var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 11, 59, 59);
        var query = broadcastingContext.ScheduledItems
            .Where(si => si.CreatedByEntraOid == ownerEntraOid && si.SendOnDateTime >= startDate && si.SendOnDateTime <= endDate);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
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
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.ScheduledItems
            .Where(s => s.CreatedByEntraOid == ownerEntraOid)
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
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(si => si.SendOnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetAllAsync(int page, int pageSize, string sortBy = "sendondate", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.ScheduledItem> query = broadcastingContext.ScheduledItems;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(si => si.Message.ToLower().Contains(lowerFilter));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "message" => sortDescending ? query.OrderByDescending(si => si.Message) : query.OrderBy(si => si.Message),
            "messagesent" => sortDescending ? query.OrderByDescending(si => si.MessageSent) : query.OrderBy(si => si.MessageSent),
            _ => sortDescending ? query.OrderByDescending(si => si.SendOnDateTime) : query.OrderBy(si => si.SendOnDateTime),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.ScheduledItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "sendondate", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.ScheduledItem> query = broadcastingContext.ScheduledItems
            .Where(si => si.CreatedByEntraOid == ownerEntraOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(si => si.Message.ToLower().Contains(lowerFilter));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "message" => sortDescending ? query.OrderByDescending(si => si.Message) : query.OrderBy(si => si.Message),
            "messagesent" => sortDescending ? query.OrderByDescending(si => si.MessageSent) : query.OrderBy(si => si.MessageSent),
            _ => sortDescending ? query.OrderByDescending(si => si.SendOnDateTime) : query.OrderBy(si => si.SendOnDateTime),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.ScheduledItem>
        {
            Items = mapper.Map<List<Domain.Models.ScheduledItem>>(dbItems),
            TotalCount = totalCount
        };
    }
}
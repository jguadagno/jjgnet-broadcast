using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class MessageTemplateManager(IMessageTemplateDataStore messageTemplateDataStore, IMemoryCache cache)
	: IMessageTemplateManager
{
	private const string CacheKeyAll = "MessageTemplate_All";
    private static string CacheKeyItem(int platformId, string messageType) => $"MessageTemplate_{platformId}_{messageType}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public async Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyItem(socialMediaPlatformId, messageType);
        if (cache.TryGetValue(cacheKey, out MessageTemplate? cached) && cached is not null)
        {
            return cached;
        }

        var result = await messageTemplateDataStore.GetAsync(socialMediaPlatformId, messageType, cancellationToken);
        if (result is not null)
        {
            cache.Set(cacheKey, result, CacheOptions);
        }
        return result;
    }

    public async Task<List<MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKeyAll, out List<MessageTemplate>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await messageTemplateDataStore.GetAllAsync(cancellationToken);
        cache.Set(CacheKeyAll, result, CacheOptions);
        return result;
    }

    public async Task<PagedResult<MessageTemplate>> GetAllAsync(int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return ApplyFilterSortPage(all, ownerOid: null, page, pageSize, sortBy, sortDescending, filter);
    }

    public async Task<PagedResult<MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return ApplyFilterSortPage(all, ownerOid: ownerEntraOid, page, pageSize, sortBy, sortDescending, filter);
    }

    public async Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate, CancellationToken cancellationToken = default)
    {
        var result = await messageTemplateDataStore.UpdateAsync(messageTemplate, cancellationToken);
        if (result is not null)
        {
            InvalidateListCaches();
            cache.Remove(CacheKeyItem(messageTemplate.SocialMediaPlatformId, messageTemplate.MessageType));
        }
        return result;
    }

    private void InvalidateListCaches()
    {
        cache.Remove(CacheKeyAll);
    }

    private static PagedResult<MessageTemplate> ApplyFilterSortPage(
        List<MessageTemplate> source,
        string? ownerOid,
        int page,
        int pageSize,
        string sortBy,
        bool sortDescending,
        string? filter)
    {
        IEnumerable<MessageTemplate> query = source;

        if (ownerOid is not null)
        {
            query = query.Where(t => t.CreatedByEntraOid == ownerOid);
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(t =>
                t.MessageType.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (t.Template != null && t.Template.Contains(filter, StringComparison.OrdinalIgnoreCase)));
        }

        query = sortBy.ToLowerInvariant() switch
        {
            "messagetype" => sortDescending
                ? query.OrderByDescending(t => t.MessageType)
                : query.OrderBy(t => t.MessageType),
            _ => sortDescending
                ? query.OrderByDescending(t => t.MessageType)
                : query.OrderBy(t => t.MessageType)
        };

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<MessageTemplate> { Items = items, TotalCount = totalCount };
    }
}

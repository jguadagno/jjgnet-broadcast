using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public class MessageTemplateManager(
    IMessageTemplateDataStore messageTemplateDataStore,
    IMemoryCache cache,
    ISocialMediaPlatformManager socialMediaPlatformManager,
    ILogger<MessageTemplateManager> logger)
	: IMessageTemplateManager
{
	private const string CacheKeyAll = "MessageTemplate_All";
    private const string CacheKeyDefaults = "MessageTemplate_Defaults";
    private static string CacheKeyItem(int platformId, string messageType, string ownerOid)
        => $"MessageTemplate_{platformId}_{messageType}_{ownerOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    /// <inheritdoc />
    public Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default)
        => GetAsync(socialMediaPlatformId, messageType, MessageTemplates.SystemOwnerEntraOid, cancellationToken);

    /// <inheritdoc />
    public async Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyItem(socialMediaPlatformId, messageType, ownerEntraOid);
        if (cache.TryGetValue(cacheKey, out MessageTemplate? cached) && cached is not null)
        {
            return cached;
        }

        var result = await messageTemplateDataStore.GetAsync(socialMediaPlatformId, messageType, ownerEntraOid, cancellationToken);
        if (result is not null)
        {
            cache.Set(cacheKey, result, CacheOptions);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<MessageTemplate?> GetAsync(string platformName, string messageType, string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var platform = await socialMediaPlatformManager.GetByNameAsync(platformName, cancellationToken);
        if (platform is null)
        {
            logger.LogWarning(
                "MessageTemplateManager: platform '{PlatformName}' not found — skipping template lookup",
                LogSanitizer.Sanitize(platformName));
            return null;
        }

        return await GetAsync(platform.Id, messageType, ownerEntraOid, cancellationToken);
    }

    public async Task<List<MessageTemplate>> GetAllDefaultsAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKeyDefaults, out List<MessageTemplate>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await messageTemplateDataStore.GetAllDefaultsAsync(cancellationToken);
        cache.Set(CacheKeyDefaults, result, CacheOptions);
        return result;
    }

    public async Task<MessageTemplate?> CreateAsync(MessageTemplate messageTemplate, CancellationToken cancellationToken = default)
    {
        var result = await messageTemplateDataStore.CreateAsync(messageTemplate, cancellationToken);
        if (result is not null)
        {
            InvalidateListCaches();
            cache.Set(
                CacheKeyItem(messageTemplate.SocialMediaPlatformId, messageTemplate.MessageType, messageTemplate.CreatedByEntraOid),
                result, CacheOptions);
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

    public Task<PagedResult<MessageTemplate>> GetAllAsync(int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        // Delegate to the data store so the ApplicationUsers batch lookup runs and
        // OwnerDisplayName is populated on each result.
        return messageTemplateDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public Task<PagedResult<MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return messageTemplateDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate, CancellationToken cancellationToken = default)
    {
        var result = await messageTemplateDataStore.UpdateAsync(messageTemplate, cancellationToken);
        if (result is not null)
        {
            InvalidateListCaches();
            cache.Remove(CacheKeyItem(messageTemplate.SocialMediaPlatformId, messageTemplate.MessageType, messageTemplate.CreatedByEntraOid));
        }
        return result;
    }

    private void InvalidateListCaches()
    {
        cache.Remove(CacheKeyAll);
        cache.Remove(CacheKeyDefaults);
    }
}

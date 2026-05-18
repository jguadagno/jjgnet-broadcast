using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace JosephGuadagno.Broadcasting.Managers;

public class EngagementManager(
	IEngagementDataStore engagementDataStore,
	ILogger<EngagementManager> logger,
	IMemoryCache cache)
	: IEngagementManager
{
	private static string CacheKeyAllByOwner(string ownerEntraOid) => $"Engagements_All_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public async Task<Engagement> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<Engagement>> SaveAsync(Engagement entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity.Id == 0)
            {
                var existingEngagement = await engagementDataStore.GetByNameAndUrlAndYearAsync(entity.Name, entity.Url, entity.StartDateTime.Year, cancellationToken);
                if (existingEngagement != null)
                {
                    entity.Id = existingEngagement.Id;
                }
            }

            entity.StartDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.StartDateTime); 
            entity.EndDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.EndDateTime); 
            return await engagementDataStore.SaveAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save engagement {EngagementId} with name '{EngagementName}'", entity.Id, entity.Name);
            return OperationResult<Engagement>.Failure("An error occurred while saving the engagement", ex);
        }
        finally
        {
            InvalidateUserCaches(entity.CreatedByEntraOid);
        }
    }
    
    public async Task<List<Engagement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<List<Engagement>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyAllByOwner(ownerEntraOid);
        if (cache.TryGetValue(cacheKey, out List<Engagement>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await engagementDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(Engagement entity, CancellationToken cancellationToken = default)
    {
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return await engagementDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var entity = await engagementDataStore.GetAsync(primaryKey, cancellationToken);
        if (entity is not null)
        {
            InvalidateUserCaches(entity.CreatedByEntraOid);
        }
        return await engagementDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<List<Talk>> GetTalksForEngagementAsync(int engagementId, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetTalksForEngagementAsync(engagementId, cancellationToken);
    }

    public async Task<OperationResult<Talk>> SaveTalkAsync(Talk talk, CancellationToken cancellationToken = default)
    {
        try
        {
            var engagement = await engagementDataStore.GetAsync(talk.EngagementId, cancellationToken);
            if (engagement is null)
            {
                return OperationResult<Talk>.Failure($"Could not find an engagement of id '{talk.EngagementId}'");
            }

            talk.StartDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.StartDateTime);
            talk.EndDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.EndDateTime);
            return await engagementDataStore.SaveTalkAsync(talk, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save talk {TalkId} for engagement {EngagementId}", talk.Id, talk.EngagementId);
            return OperationResult<Talk>.Failure("An error occurred while saving the talk", ex);
        }
    }

    public async Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(int talkId, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.RemoveTalkFromEngagementAsync(talkId, cancellationToken);
    }

    public async Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(Talk talk, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.RemoveTalkFromEngagementAsync(talk, cancellationToken);
    }

    public async Task<Talk> GetTalkAsync(int talkId, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetTalkAsync(talkId, cancellationToken)
            ?? throw new ApplicationException($"Talk with id '{talkId}' not found.");
    }
    
    public DateTimeOffset UpdateDateTimeOffsetWithTimeZone(string timeZoneId, DateTimeOffset dateTimeOffset)
    {
        var eventTimeZone = DateTimeZoneProviders.Tzdb[timeZoneId];

        LocalDateTime localDateTime = new LocalDateTime(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day,
            dateTimeOffset.Hour, dateTimeOffset.Minute);
        var zonedDateTime = localDateTime.InZoneLeniently(eventTimeZone);
        return zonedDateTime.ToDateTimeOffset();
    }

    public async Task<Engagement?> GetByNameAndUrlAndYearAsync(string name, string url, int year, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetByNameAndUrlAndYearAsync(name, url, year, cancellationToken);
    }

    public async Task<bool> IsEngagementUniqueToUser(string name, string url, int year, string ownerOid, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.IsEngagementUniqueToUser(name, url, year, ownerOid, cancellationToken);
    }
    
    public async Task<PagedResult<Engagement>> GetAllAsync(int page, int pageSize, string sortBy = "startdate", bool sortDescending = true, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<Engagement>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "startdate", bool sortDescending = true, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }
    
    public async Task<PagedResult<Talk>> GetTalksForEngagementAsync(int engagementId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await engagementDataStore.GetTalksForEngagementAsync(engagementId, page, pageSize, cancellationToken);
    }

    private void InvalidateUserCaches(string? ownerEntraOid)
    {
        if (!string.IsNullOrEmpty(ownerEntraOid))
        {
            cache.Remove(CacheKeyAllByOwner(ownerEntraOid));
        }
    }
}

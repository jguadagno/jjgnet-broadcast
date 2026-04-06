using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using NodaTime;

namespace JosephGuadagno.Broadcasting.Managers;

public class EngagementManager: IEngagementManager
{
    private readonly IEngagementDataStore _engagementDataStore;

    public EngagementManager(IEngagementDataStore engagementDataStore)
    {
        _engagementDataStore = engagementDataStore;
    }
    
    public async Task<Engagement> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<Engagement>> SaveAsync(Engagement entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity.Id == 0)
            {
                var existingEngagement = await _engagementDataStore.GetByNameAndUrlAndYearAsync(entity.Name, entity.Url, entity.StartDateTime.Year, cancellationToken);
                if (existingEngagement != null)
                {
                    entity.Id = existingEngagement.Id;
                }
            }

            entity.StartDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.StartDateTime); 
            entity.EndDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.EndDateTime); 
            return await _engagementDataStore.SaveAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            return OperationResult<Engagement>.Failure("An error occurred while saving the engagement", ex);
        }
    }
    
    public async Task<List<Engagement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Engagement entity, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<List<Talk>> GetTalksForEngagementAsync(int engagementId, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.GetTalksForEngagementAsync(engagementId, cancellationToken);
    }

    public async Task<OperationResult<Talk>> SaveTalkAsync(Talk talk, CancellationToken cancellationToken = default)
    {
        try
        {
            var engagement = await _engagementDataStore.GetAsync(talk.EngagementId, cancellationToken);
            if (engagement is null)
            {
                return OperationResult<Talk>.Failure($"Could not find an engagement of id '{talk.EngagementId}'");
            }

            talk.StartDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.StartDateTime);
            talk.EndDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.EndDateTime);
            return await _engagementDataStore.SaveTalkAsync(talk, cancellationToken);
        }
        catch (Exception ex)
        {
            return OperationResult<Talk>.Failure("An error occurred while saving the talk", ex);
        }
    }

    public async Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(int talkId, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.RemoveTalkFromEngagementAsync(talkId, cancellationToken);
    }

    public async Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(Talk talk, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.RemoveTalkFromEngagementAsync(talk, cancellationToken);
    }

    public async Task<Talk> GetTalkAsync(int talkId, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.GetTalkAsync(talkId, cancellationToken)
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
        return await _engagementDataStore.GetByNameAndUrlAndYearAsync(name, url, year, cancellationToken);
    }
    
    public async Task<PagedResult<Engagement>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.GetAllAsync(page, pageSize, cancellationToken);
    }
    
    public async Task<PagedResult<Talk>> GetTalksForEngagementAsync(int engagementId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _engagementDataStore.GetTalksForEngagementAsync(engagementId, page, pageSize, cancellationToken);
    }
}
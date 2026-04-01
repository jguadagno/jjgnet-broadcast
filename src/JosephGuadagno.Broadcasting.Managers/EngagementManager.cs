using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    
    public async Task<Engagement> GetAsync(int primaryKey)
    {
        return await _engagementDataStore.GetAsync(primaryKey);
    }

    public async Task<Engagement> SaveAsync(Engagement entity)
    {

        if (entity.Id == 0)
        {
            // We need to see if there is an existing record since there is no "id" from the SpeakingEngagementsReaders
            // We will assume that if the following fields match, we will update the record.
            //  - Name, Url, StartDateTime (Year)
            var existingEngagement = await _engagementDataStore.GetByNameAndUrlAndYearAsync(entity.Name, entity.Url, entity.StartDateTime.Year);
            if (existingEngagement != null)
            {
                entity.Id = existingEngagement.Id;
            }
        }

        // Apply the time zone offset to the hours
        entity.StartDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.StartDateTime); 
        entity.EndDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.EndDateTime); 
        return await _engagementDataStore.SaveAsync(entity);
    }
    
    public async Task<List<Engagement>> GetAllAsync()
    {
        return await _engagementDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(Engagement entity)
    {
        return await _engagementDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _engagementDataStore.DeleteAsync(primaryKey);
    }

    public async Task<List<Talk>> GetTalksForEngagementAsync(int engagementId)
    {
        return await _engagementDataStore.GetTalksForEngagementAsync(engagementId);
    }

    public async Task<Talk> SaveTalkAsync(Talk talk)
    {
        var engagement = await _engagementDataStore.GetAsync(talk.EngagementId);
        if (engagement is null)
        {
            throw new ApplicationException(
                $"Failed to save the talk. Could not find an engagement of id '{talk.EngagementId}");
        }

        talk.StartDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.StartDateTime);
        talk.EndDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.EndDateTime);
        return await _engagementDataStore.SaveTalkAsync(talk);
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(int talkId)
    {
        return await _engagementDataStore.RemoveTalkFromEngagementAsync(talkId);
    }
    public async Task<bool> RemoveTalkFromEngagementAsync(Talk talk)
    {
        return await _engagementDataStore.RemoveTalkFromEngagementAsync(talk);
    }

    public async Task<Talk> GetTalkAsync(int talkId)
    {
        return await _engagementDataStore.GetTalkAsync(talkId)
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

    public async Task<Engagement?> GetByNameAndUrlAndYearAsync(string name, string url, int year)
    {
        return await _engagementDataStore.GetByNameAndUrlAndYearAsync(name, url, year);
    }
    
    public async Task<PagedResult<Engagement>> GetAllAsync(int page, int pageSize)
    {
        return await _engagementDataStore.GetAllAsync(page, pageSize);
    }
    
    public async Task<PagedResult<Talk>> GetTalksForEngagementAsync(int engagementId, int page, int pageSize)
    {
        return await _engagementDataStore.GetTalksForEngagementAsync(engagementId, page, pageSize);
    }
}
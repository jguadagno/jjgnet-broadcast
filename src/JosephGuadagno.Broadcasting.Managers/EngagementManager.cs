using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using NodaTime;

namespace JosephGuadagno.Broadcasting.Managers;

public class EngagementManager: IEngagementManager
{
    private readonly IEngagementRepository _engagementRepository;

    public EngagementManager(IEngagementRepository engagementRepository)
    {
        _engagementRepository = engagementRepository;
    }
    
    public async Task<Engagement> GetAsync(int primaryKey)
    {
        return await _engagementRepository.GetAsync(primaryKey);
    }

    public async Task<Engagement> SaveAsync(Engagement entity)
    {

        if (entity.Id == 0)
        {
            // We need to see if there is an existing record since there is no "id" from the SpeakingEngagementsReaders
            // We will assume that if the following fields match, we will update the record.
            //  - Name, Url, StartDateTime (Year)
            var existingEngagement = await _engagementRepository.GetByNameAndUrlAndYearAsync(entity.Name, entity.Url, entity.StartDateTime.Year);
            if (existingEngagement != null)
            {
                entity.Id = existingEngagement.Id;
            }
        }

        // Apply the time zone offset to the hours
        entity.StartDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.StartDateTime); 
        entity.EndDateTime = UpdateDateTimeOffsetWithTimeZone(entity.TimeZoneId, entity.EndDateTime); 
        return await _engagementRepository.SaveAsync(entity);
    }
    
    public async Task<List<Engagement>> GetAllAsync()
    {
        return await _engagementRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(Engagement entity)
    {
        return await _engagementRepository.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _engagementRepository.DeleteAsync(primaryKey);
    }

    public async Task<List<Talk>> GetTalksForEngagementAsync(int engagementId)
    {
        return await _engagementRepository.GetTalksForEngagementAsync(engagementId);
    }

    public async Task<Talk> SaveTalkAsync(Talk talk)
    {
        var engagement = await _engagementRepository.GetAsync(talk.EngagementId);
        if (engagement is null)
        {
            throw new ApplicationException(
                $"Failed to save the talk. Could not find an engagement of id '{talk.EngagementId}");
        }

        talk.StartDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.StartDateTime);
        talk.EndDateTime = UpdateDateTimeOffsetWithTimeZone(engagement.TimeZoneId, talk.EndDateTime);
        return await _engagementRepository.SaveTalkAsync(talk);
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(int talkId)
    {
        return await _engagementRepository.RemoveTalkFromEngagementAsync(talkId);
    }
    public async Task<bool> RemoveTalkFromEngagementAsync(Talk talk)
    {
        return await _engagementRepository.RemoveTalkFromEngagementAsync(talk);
    }

    public async Task<Talk> GetTalkAsync(int talkId)
    {
        return await _engagementRepository.GetTalkAsync(talkId);
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
        return await _engagementRepository.GetByNameAndUrlAndYearAsync(name, url, year);
    }
}
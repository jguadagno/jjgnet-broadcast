using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EngagementDataStore: IEngagementDataStore
{
    private readonly BroadcastingContext _broadcastingContext;
    private readonly Mapper _mapper;

    public EngagementDataStore(ISettings settings)
    {
        _broadcastingContext = new BroadcastingContext(settings);
        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        });
        _mapper = new Mapper(mapperConfiguration);
    }
        
    public async Task<Domain.Models.Engagement> GetAsync(int primaryKey)
    {
        var dbEngagement =
            await _broadcastingContext.Engagements.Include(e => e.Talks)
                .FirstOrDefaultAsync(e => e.Id == primaryKey);
        return _mapper.Map<Domain.Models.Engagement>(dbEngagement);
    }

    public async Task<bool> SaveAsync(Domain.Models.Engagement engagement)
    {
        var dbEngagement = _mapper.Map<Models.Engagement>(engagement);
        _broadcastingContext.Entry(dbEngagement).State =
            dbEngagement.Id == 0 ? EntityState.Added : EntityState.Modified;

        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> SaveAllAsync(List<Domain.Models.Engagement> engagements)
    {
        foreach (var engagement in engagements)
        {
            var wasSaved = await SaveAsync(engagement);
            if (wasSaved == false)
            {
                return false;
            }
        }
        return true;
    }

    public async Task<List<Domain.Models.Engagement>> GetAllAsync()
    {
        var dbEngagements = await _broadcastingContext.Engagements.ToListAsync();
        return _mapper.Map<List<Domain.Models.Engagement>>(dbEngagements);
    }

    public async Task<bool> DeleteAsync(Domain.Models.Engagement engagement)
    {
        return await DeleteAsync(engagement.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbEngagement = await _broadcastingContext.Engagements
            .Include(e => e.Talks)
            .FirstOrDefaultAsync(e => e.Id == primaryKey);

        if (dbEngagement == null)
        {
            return true;
        }

        foreach (var talk in dbEngagement.Talks)
        {
            _broadcastingContext.Talks.Remove(talk);
        }
        _broadcastingContext.Engagements.Remove(dbEngagement);

        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> AddTalkToEngagementAsync(Domain.Models.Engagement engagement, Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(engagement);
        ArgumentNullException.ThrowIfNull(talk);

        Models.Engagement? dbEngagement;
        if (engagement.Id == 0)
        {
            dbEngagement = _mapper.Map<Models.Engagement>(engagement);
            _broadcastingContext.Engagements.Add(dbEngagement);
        }
        else
        {
            dbEngagement = await _broadcastingContext.Engagements.FindAsync(engagement.Id);
            if (dbEngagement is null)
            {
                return false;
            }
        }
        
        // Save Talk
        var dbTalk = _mapper.Map<Models.Talk>(talk);
        dbEngagement.Talks.Add(dbTalk);
        
        // Save
        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> AddTalkToEngagementAsync(int engagementId, Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(talk);
        if (engagementId <= 0)
        {
            throw new ApplicationException("EngagementId can not <= 0");
        }
        
        var dbEngagement = await _broadcastingContext.Engagements.FindAsync(engagementId);
        if (dbEngagement is null)
        {
            return false;
        }
        
        // Save Talk
        var dbTalk = _mapper.Map<Models.Talk>(talk);
        dbEngagement.Talks.Add(dbTalk);
        
        // Save
        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> SaveTalkAsync(Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(talk);
        var dbTalk = _mapper.Map<Models.Talk>(talk);
        _broadcastingContext.Talks.Update(dbTalk);
        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(int talkId)
    {
        if (talkId <= 0)
        {
            throw new ApplicationException("The TalkId can not be <=0");
        }

        var dbTalk = await _broadcastingContext.Talks.FindAsync(talkId);
        if (dbTalk is null)
        {
            return true;
        }

        _broadcastingContext.Talks.Remove(dbTalk);

        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(talk);

        var dbTalk = _mapper.Map<Models.Talk>(talk);
        _broadcastingContext.Talks.Remove(dbTalk);
        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Talk?> GetTalkAsync(int talkId)
    {
        if (talkId <= 0)
        {
            throw new ApplicationException("The TalkId can not be <=0");
        }

        var dbTalk = await _broadcastingContext.Talks.FindAsync(talkId);
        return dbTalk is null ? null : _mapper.Map<Domain.Models.Talk>(dbTalk);
    }
}
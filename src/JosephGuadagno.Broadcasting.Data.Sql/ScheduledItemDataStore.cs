using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class ScheduledItemDataStore: IScheduledItemDataStore
{

    private readonly BroadcastingContext _broadcastingContext;
    private readonly Mapper _mapper;

    public ScheduledItemDataStore(ISettings settings)
    {
        _broadcastingContext = new BroadcastingContext(settings);
        var mappingConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        });
        _mapper = new Mapper(mappingConfiguration);
    }
        
    public async Task<Domain.Models.ScheduledItem> GetAsync(int primaryKey)
    {
        var dbScheduledItem = await _broadcastingContext.ScheduledItems.FindAsync(primaryKey);
        return _mapper.Map<Domain.Models.ScheduledItem>(dbScheduledItem);
    }

    public async Task<bool> SaveAsync(Domain.Models.ScheduledItem talk)
    {
        var dbScheduledItem = _mapper.Map<Sql.Models.ScheduledItem>(talk);
        _broadcastingContext.Entry(dbScheduledItem).State = talk.Id == 0 ? EntityState.Added : EntityState.Modified;

        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> SaveAllAsync(List<Domain.Models.ScheduledItem> talks)
    {
        foreach (var scheduledItem in talks)
        {
            var wasSaved = await SaveAsync(scheduledItem);
            if (wasSaved == false)
            {
                return false;
            }
        }
        return true;
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetAllAsync()
    {
        var dbScheduledItems = await _broadcastingContext.ScheduledItems.ToListAsync();
        return _mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<bool> DeleteAsync(Domain.Models.ScheduledItem scheduledItem)
    {
        return await DeleteAsync(scheduledItem.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbScheduledItem = await _broadcastingContext.ScheduledItems.FindAsync(primaryKey);
        if (dbScheduledItem is null)
        {
            return false;
        }
        _broadcastingContext.ScheduledItems.Remove(dbScheduledItem);
        return await _broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<List<Domain.Models.ScheduledItem>> GetUpcomingScheduledItemsAsync()
    {
        var dbScheduledItems = await _broadcastingContext.ScheduledItems
            .Where(si => si.MessageSent == false && si.SendOnDateTime <= DateTimeOffset.Now)
            .ToListAsync();
        return _mapper.Map<List<Domain.Models.ScheduledItem>>(dbScheduledItems);
    }

    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn)
    {
        var dbScheduledItems = await _broadcastingContext.ScheduledItems.FindAsync(primaryKey);
        if (dbScheduledItems is null)
        {
            return false;
        }

        dbScheduledItems.MessageSentOn = sentOn;
        dbScheduledItems.MessageSent = true;
        return await _broadcastingContext.SaveChangesAsync() != 0;
    }
}
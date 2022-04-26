using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Azure.Documents;

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

    public async Task<bool> AddTalkToEngagementAsync(Engagement engagement, Talk talk)
    {
        return await _engagementRepository.AddTalkToEngagementAsync(engagement, talk);
    }

    public async Task<List<Talk>> GetTalksForEngagementAsync(int engagementId)
    {
        return await _engagementRepository.GetTalksForEngagementAsync(engagementId);
    }
    
    public async Task<bool> AddTalkToEngagementAsync(int engagementId, Talk talk)
    {
        return await _engagementRepository.AddTalkToEngagementAsync(engagementId, talk);
    }

    public async Task<bool> SaveTalkAsync(Talk talk)
    {
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
}
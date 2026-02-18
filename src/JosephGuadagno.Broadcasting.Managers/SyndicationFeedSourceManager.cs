using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class SyndicationFeedSourceManager : ISyndicationFeedSourceManager
{
    private readonly ISyndicationFeedSourceRepository _syndicationFeedSourceRepository;

    public SyndicationFeedSourceManager(ISyndicationFeedSourceRepository syndicationFeedSourceRepository)
    {
        _syndicationFeedSourceRepository = syndicationFeedSourceRepository;
    }

    public async Task<SyndicationFeedSource> GetAsync(int primaryKey)
    {
        return await _syndicationFeedSourceRepository.GetAsync(primaryKey);
    }

    public async Task<SyndicationFeedSource> SaveAsync(SyndicationFeedSource entity)
    {
        return await _syndicationFeedSourceRepository.SaveAsync(entity);
    }

    public async Task<List<SyndicationFeedSource>> GetAllAsync()
    {
        return await _syndicationFeedSourceRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(SyndicationFeedSource entity)
    {
        return await _syndicationFeedSourceRepository.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _syndicationFeedSourceRepository.DeleteAsync(primaryKey);
    }

    public async Task<SyndicationFeedSource?> GetByUrlAsync(string url)
    {
        return await _syndicationFeedSourceRepository.GetByUrlAsync(url);
    }

    public async Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories)
    {
        return await _syndicationFeedSourceRepository.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);
    }
}
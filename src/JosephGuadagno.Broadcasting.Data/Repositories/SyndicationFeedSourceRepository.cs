using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class SyndicationFeedSourceRepository : ISyndicationFeedSourceRepository
{
    private readonly ISyndicationFeedSourceDataStore _syndicationFeedSourceDataStore;

    public SyndicationFeedSourceRepository(ISyndicationFeedSourceDataStore syndicationFeedSourceDataStore)
    {
        _syndicationFeedSourceDataStore = syndicationFeedSourceDataStore;
    }

    public async Task<SyndicationFeedSource> GetAsync(int primaryKey)
    {
        return await _syndicationFeedSourceDataStore.GetAsync(primaryKey);
    }

    public async Task<SyndicationFeedSource> SaveAsync(SyndicationFeedSource entity)
    {
        return await _syndicationFeedSourceDataStore.SaveAsync(entity);
    }

    public async Task<List<SyndicationFeedSource>> GetAllAsync()
    {
        return await _syndicationFeedSourceDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(SyndicationFeedSource entity)
    {
        return await _syndicationFeedSourceDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _syndicationFeedSourceDataStore.DeleteAsync(primaryKey);
    }

    public async Task<SyndicationFeedSource?> GetByUrlAsync(string url)
    {
        return await _syndicationFeedSourceDataStore.GetByUrlAsync(url);
    }

    public async Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories)
    {
        return await _syndicationFeedSourceDataStore.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);
    }
}
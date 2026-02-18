using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class FeedCheckRepository : IFeedCheckRepository
{
    private readonly IFeedCheckDataStore _feedCheckDataStore;

    public FeedCheckRepository(IFeedCheckDataStore feedCheckDataStore)
    {
        _feedCheckDataStore = feedCheckDataStore;
    }

    public async Task<FeedCheck> GetAsync(int primaryKey)
    {
        return await _feedCheckDataStore.GetAsync(primaryKey);
    }

    public async Task<FeedCheck> SaveAsync(FeedCheck entity)
    {
        return await _feedCheckDataStore.SaveAsync(entity);
    }

    public async Task<List<FeedCheck>> GetAllAsync()
    {
        return await _feedCheckDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(FeedCheck entity)
    {
        return await _feedCheckDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _feedCheckDataStore.DeleteAsync(primaryKey);
    }

    public async Task<FeedCheck?> GetByNameAsync(string name)
    {
        return await _feedCheckDataStore.GetByNameAsync(name);
    }
}
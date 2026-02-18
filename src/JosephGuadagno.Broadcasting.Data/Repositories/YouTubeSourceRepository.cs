using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class YouTubeSourceRepository : IYouTubeSourceRepository
{
    private readonly IYouTubeSourceDataStore _youTubeSourceDataStore;

    public YouTubeSourceRepository(IYouTubeSourceDataStore youTubeSourceDataStore)
    {
        _youTubeSourceDataStore = youTubeSourceDataStore;
    }

    public async Task<YouTubeSource> GetAsync(int primaryKey)
    {
        return await _youTubeSourceDataStore.GetAsync(primaryKey);
    }

    public async Task<YouTubeSource> SaveAsync(YouTubeSource entity)
    {
        return await _youTubeSourceDataStore.SaveAsync(entity);
    }

    public async Task<List<YouTubeSource>> GetAllAsync()
    {
        return await _youTubeSourceDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(YouTubeSource entity)
    {
        return await _youTubeSourceDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _youTubeSourceDataStore.DeleteAsync(primaryKey);
    }

    public async Task<YouTubeSource?> GetByUrlAsync(string url)
    {
        return await _youTubeSourceDataStore.GetByUrlAsync(url);
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class YouTubeSourceManager : IYouTubeSourceManager
{
    private readonly IYouTubeSourceDataStore _youTubeSourceDataStore;

    public YouTubeSourceManager(IYouTubeSourceDataStore youTubeSourceDataStore)
    {
        _youTubeSourceDataStore = youTubeSourceDataStore;
    }

    public async Task<YouTubeSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<YouTubeSource> SaveAsync(YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.SaveAsync(entity, cancellationToken);
    }

    public async Task<List<YouTubeSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetByUrlAsync(url, cancellationToken);
    }

    public async Task<YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetByVideoIdAsync(videoId, cancellationToken);
    }
}
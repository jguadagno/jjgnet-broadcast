using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class YouTubeSourceManager : IYouTubeSourceManager
{
    private readonly IYouTubeSourceRepository _youTubeSourceRepository;

    public YouTubeSourceManager(IYouTubeSourceRepository youTubeSourceRepository)
    {
        _youTubeSourceRepository = youTubeSourceRepository;
    }

    public async Task<YouTubeSource> GetAsync(int primaryKey)
    {
        return await _youTubeSourceRepository.GetAsync(primaryKey);
    }

    public async Task<YouTubeSource> SaveAsync(YouTubeSource entity)
    {
        return await _youTubeSourceRepository.SaveAsync(entity);
    }

    public async Task<List<YouTubeSource>> GetAllAsync()
    {
        return await _youTubeSourceRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(YouTubeSource entity)
    {
        return await _youTubeSourceRepository.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _youTubeSourceRepository.DeleteAsync(primaryKey);
    }

    public async Task<YouTubeSource?> GetByUrlAsync(string url)
    {
        return await _youTubeSourceRepository.GetByUrlAsync(url);
    }
}
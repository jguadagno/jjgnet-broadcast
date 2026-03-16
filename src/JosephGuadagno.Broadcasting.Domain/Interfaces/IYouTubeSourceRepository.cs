using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeSourceRepository : IDataRepository<YouTubeSource>
{
    public Task<YouTubeSource?> GetByUrlAsync(string url);
    Task<YouTubeSource?> GetByVideoIdAsync(string videoId);
}

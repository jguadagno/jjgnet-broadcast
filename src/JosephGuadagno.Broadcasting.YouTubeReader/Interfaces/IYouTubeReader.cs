using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

public interface IYouTubeReader
{
    public List<YouTubeSource> GetSinceDate(string ownerOid, DateTimeOffset sinceWhen);
    public Task<List<YouTubeSource>> GetAsync(string ownerOid, DateTimeOffset sinceWhen);
}

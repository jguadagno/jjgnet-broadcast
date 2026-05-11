using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

public interface IYouTubeReader
{
    public List<YouTubeItem> GetSinceDate(string ownerOid, DateTimeOffset sinceWhen);
    public Task<List<YouTubeItem>> GetAsync(string ownerOid, DateTimeOffset sinceWhen);
}

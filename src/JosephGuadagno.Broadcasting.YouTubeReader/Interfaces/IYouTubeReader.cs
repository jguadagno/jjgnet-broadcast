using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

public interface IYouTubeReader
{
    public List<YouTubeItem> GetSinceDate(string ownerOid, DateTimeOffset sinceWhen);
    public Task<List<YouTubeItem>> GetAsync(string ownerOid, DateTimeOffset sinceWhen);
    /// <summary>
    /// Retrieves YouTube videos published after <paramref name="sinceWhen"/> using per-user settings
    /// (channel ID, playlist ID, and API key) rather than the globally configured credentials.
    /// </summary>
    public Task<List<YouTubeItem>> GetAsync(string ownerOid, DateTimeOffset sinceWhen, IYouTubeSettings settings);
}

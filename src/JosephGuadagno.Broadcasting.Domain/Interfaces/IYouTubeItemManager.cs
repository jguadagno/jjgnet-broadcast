using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeItemManager : IManager<YouTubeItem>
{
    public Task<YouTubeItem?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<YouTubeItem?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default);
    Task<List<YouTubeItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<PagedResult<YouTubeItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<PagedResult<YouTubeItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeSourceManager : IManager<YouTubeSource>
{
    public Task<YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default);
    Task<List<YouTubeSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<PagedResult<YouTubeSource>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<PagedResult<YouTubeSource>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

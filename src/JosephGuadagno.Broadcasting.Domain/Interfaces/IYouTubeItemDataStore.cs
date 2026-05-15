namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeItemDataStore : IDataStore<Models.YouTubeItem>
{
    public Task<Models.YouTubeItem?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Models.YouTubeItem?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default);
    Task<List<Models.YouTubeItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<Models.PagedResult<Models.YouTubeItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<Models.PagedResult<Models.YouTubeItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeSourceDataStore : IDataStore<Models.YouTubeSource>
{
    public Task<Models.YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Models.YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default);
    Task<List<Models.YouTubeSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<Models.PagedResult<Models.YouTubeSource>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<Models.PagedResult<Models.YouTubeSource>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

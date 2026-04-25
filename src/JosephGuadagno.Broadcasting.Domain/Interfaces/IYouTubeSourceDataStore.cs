namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeSourceDataStore : IDataStore<Domain.Models.YouTubeSource>
{
    public Task<Domain.Models.YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Domain.Models.YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);

    Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default);
    Task<List<Domain.Models.YouTubeSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<Domain.Models.PagedResult<Domain.Models.YouTubeSource>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<Domain.Models.PagedResult<Domain.Models.YouTubeSource>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

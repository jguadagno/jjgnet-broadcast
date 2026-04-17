namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeSourceDataStore : IDataStore<Domain.Models.YouTubeSource>
{
    public Task<Domain.Models.YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Domain.Models.YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    
    Task<List<Domain.Models.YouTubeSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
}

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ISyndicationFeedSourceDataStore : IDataStore<SyndicationFeedSource>
{
    public Task<SyndicationFeedSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<SyndicationFeedSource?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default);
    Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default);
    
    Task<List<SyndicationFeedSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(string ownerEntraOid, DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default);
}
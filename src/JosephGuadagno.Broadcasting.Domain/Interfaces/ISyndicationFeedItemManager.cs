using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ISyndicationFeedItemManager : IManager<SyndicationFeedItem>
{
    Task<SyndicationFeedItem?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default);
    Task<bool> IsFeedItemUniqueToUser(string feedIdentifier, string ownerOid, CancellationToken cancellationToken = default);
    Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default);
    Task<List<SyndicationFeedItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<SyndicationFeedItem?> GetRandomSyndicationDataAsync(string ownerEntraOid, DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default);
    Task<PagedResult<SyndicationFeedItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<PagedResult<SyndicationFeedItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

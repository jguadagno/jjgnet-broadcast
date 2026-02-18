using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ISyndicationFeedSourceRepository : IDataRepository<SyndicationFeedSource>
{
    public Task<SyndicationFeedSource?> GetByUrlAsync(string url);
    Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories);
}
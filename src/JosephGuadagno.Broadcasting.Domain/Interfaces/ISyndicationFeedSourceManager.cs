using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ISyndicationFeedSourceManager : IManager<SyndicationFeedSource>
{
    public Task<SyndicationFeedSource?> GetByUrlAsync(string url);
    Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories);
}
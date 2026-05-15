using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

public interface ISyndicationFeedReader
{
    public List<SyndicationFeedItem> GetSinceDate(string ownerOid, DateTimeOffset sinceWhen);
    public Task<List<SyndicationFeedItem>> GetAsync(string ownerOid, DateTimeOffset sinceWhen);
    public List<SyndicationFeedItem> GetSyndicationItems(string ownerOid, DateTimeOffset sinceWhen, List<string>? excludeCategories = null);
    public SyndicationFeedItem? GetRandomSyndicationItem(string ownerOid, DateTimeOffset sinceWhen, List<string>? excludeCategories = null);

    /// <summary>Gets all syndication feed items from a specific feed URL for the given owner since a date</summary>
    /// <param name="feedUrl">The URL of the feed to read</param>
    /// <param name="ownerOid">The Entra Object ID of the owner</param>
    /// <param name="sinceWhen">Only return items published or updated after this date</param>
    /// <returns>A list of syndication feed sources</returns>
    public Task<List<SyndicationFeedItem>> GetAsync(string feedUrl, string ownerOid, DateTimeOffset sinceWhen);
}

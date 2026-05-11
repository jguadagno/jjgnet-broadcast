using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

public interface ISyndicationFeedReader
{
    public List<SyndicationFeedSource> GetSinceDate(string ownerOid, DateTimeOffset sinceWhen);
    public Task<List<SyndicationFeedSource>> GetAsync(string ownerOid, DateTimeOffset sinceWhen);
    public List<SyndicationFeedSource> GetSyndicationItems(string ownerOid, DateTimeOffset sinceWhen, List<string>? excludeCategories = null);
    public SyndicationFeedSource? GetRandomSyndicationItem(string ownerOid, DateTimeOffset sinceWhen, List<string>? excludeCategories = null);

    /// <summary>Gets all syndication feed items from a specific feed URL for the given owner since a date</summary>
    /// <param name="feedUrl">The URL of the feed to read</param>
    /// <param name="ownerOid">The Entra Object ID of the owner</param>
    /// <param name="sinceWhen">Only return items published or updated after this date</param>
    /// <returns>A list of syndication feed sources</returns>
    public Task<List<SyndicationFeedSource>> GetAsync(string feedUrl, string ownerOid, DateTimeOffset sinceWhen);
}

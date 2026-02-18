using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

public interface ISyndicationFeedReader
{
    public List<SyndicationFeedSource> GetSinceDate(DateTimeOffset sinceWhen);
    public Task<List<SyndicationFeedSource>> GetAsync(DateTimeOffset sinceWhen);
    public List<SyndicationFeedSource> GetSyndicationItems(DateTimeOffset sinceWhen, List<string> excludeCategories = null);
    public SyndicationFeedSource GetRandomSyndicationItem(DateTimeOffset sinceWhen, List<string> excludeCategories = null);
}
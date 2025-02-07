using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

public interface ISyndicationFeedReader : ISourceReader
{
    public List<SyndicationItem> GetSyndicationItems(DateTime sinceWhen, List<string> excludeCategories = null);
}
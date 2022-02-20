using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;

public class SyndicationFeedReaderSettings: ISyndicationFeedReaderSettings
{
    public string FeedUrl { get; set; }
}
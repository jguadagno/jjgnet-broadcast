namespace JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;

public interface IJsonFeedReaderSettings
{
    /// <summary>
    /// The Url to the Jsonfeed.org Feed
    /// </summary>
    public string FeedUrl { get; set; }
}
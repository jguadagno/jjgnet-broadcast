using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;

namespace JosephGuadagno.Broadcasting.JsonFeedReader.Models
{
    public class JsonFeedReaderSettings: IJsonFeedReaderSettings
    {
        public string FeedUrl { get; set; }
    }
}
#nullable disable

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class SyndicationFeedSource
{
    public int Id { get; set; }
    public string FeedIdentifier { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }
    public string ShortenedUrl { get; set; }
    public string Tags { get; set; }
    public string Url { get; set; }
    public DateTimeOffset PublicationDate { get; set; }
    public DateTimeOffset AddedOn { get; set; }
    public DateTimeOffset? ItemLastUpdatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
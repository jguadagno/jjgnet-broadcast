namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a syndication feed source.
/// </summary>
public class SyndicationFeedSourceResponse
{
    /// <summary>
    /// The unique identifier for the syndication feed source record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The unique identifier for the feed item within the syndication feed (e.g., GUID or permalink).
    /// </summary>
    public string FeedIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The author or creator of the feed item.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The title of the feed item.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL of the feed item.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The original publication date of the feed item.
    /// </summary>
    public DateTimeOffset PublicationDate { get; set; }

    /// <summary>
    /// An optional shortened URL for the feed item, used when posting to social media.
    /// </summary>
    public string? ShortenedUrl { get; set; }

    /// <summary>
    /// The list of tags or categories associated with the feed item.
    /// </summary>
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// The date and time when this record was added to the system.
    /// </summary>
    public DateTimeOffset AddedOn { get; set; }

    /// <summary>
    /// The date and time when this record was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who created this record.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
}

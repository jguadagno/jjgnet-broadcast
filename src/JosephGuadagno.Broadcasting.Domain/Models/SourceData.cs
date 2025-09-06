using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class SourceData : TableEntity, ISourceData
{
    public SourceData()
    {
    }

    public SourceData(string sourceSystem) : base(sourceSystem, Guid.NewGuid().ToString())
    {

    }

    public SourceData(string sourceSystem, string id)
    {
        if (string.IsNullOrEmpty(sourceSystem))
        {
            throw new ArgumentNullException(nameof(sourceSystem), "The source system cannot be null or empty.");
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id), "The id cannot be null or empty.");
        }
        PartitionKey = sourceSystem;
        RowKey = EncodeUrlInKey(id);
    }

    /// <summary>
    /// The type of source data.
    /// </summary>
    /// <remarks>This list can be found at <see cref="Domain.SourceSystems"/></remarks>
    public string SourceSystem => PartitionKey;

    /// <summary>
    /// The Id of the item
    /// </summary>
    public string Id => RowKey;

    /// <summary>
    /// Indicates when the item was added
    /// </summary>
    /// <remarks>The date time is in UTC</remarks>
    public DateTime AddedOn { get; set; }
        
    /// <summary>
    /// The Author of the Source
    /// </summary>
    /// <remarks>For blog posts or videos, this is the author of the blog post or video.</remarks>
    public string Author { get; set; }
        
    public string Title { get; set; }
        
    // Text was commented out due to a size limitation of Azure Table Storage
    //     this will be revisited if we determine text is needed.
    //public string Text { get; set; }
        
    /// <summary>
    /// The Url of the Source
    /// </summary>
    public string Url { get; set; }
        
    /// <summary>
    /// The shortened version of the Url
    /// </summary>
    public string ShortenedUrl { get; set; }
        
    /// <summary>
    /// When the item was published at the source
    /// </summary>
    /// <remarks>
    /// The date time is in UTC. If the publication date is not available from the source, the value will be the same as the <see cref="AddedOn"/> property.
    /// </remarks>
    public DateTime PublicationDate { get; set; }
        
    /// <summary>
    /// When the item was updated at the source
    /// </summary>
    /// <remarks>
    /// The date time is in UTC. If the last modified date is not available from the source, the value will be the same as the <see cref="PublicationDate"/> property.
    /// </remarks>
    public DateTime? UpdatedOnDate { get; set; }
        
    /// <summary>
    /// Indicates when we should stop sending out social posts on this item
    /// </summary>
    /// <remarks>The date time is in UTC</remarks>
    public DateTime? EndAfter { get; set; }
        
    /// <summary>
    /// A comma separated list of tags or categories
    /// </summary>
    public string Tags { get; set; }
        
    /// <summary>
    /// Returns certain properties in a Dictionary
    /// </summary>
    /// <returns>
    /// This is used for logging.  Current properties returned are <see cref="SourceSystem"/>, <see cref="Id"/>, <see cref="Title"/>, and <see cref="Url"/>.
    /// </returns>
    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            {"SourceSystem", SourceSystem},
            {"Id", Id},
            {"Title", Title},
            {"Url", Url}
        };
    }
    
    /// <summary>
    /// Converts the Tags to a HashTag string
    /// </summary>
    /// <returns>A string with HashTags</returns>
    /// <remarks>The tag of Article is excluded.</remarks>
    public string TagsToHashTags()
    {
        // Articles,.NET,dotnet,SQL Server
        if (string.IsNullOrEmpty(Tags))
        {
            return "#dotnet #csharp #dotnetcore";
        }
        
        var hashTagsArray = Tags.Split(',');

        return hashTagsArray.Where(hashTag => !hashTag.Equals("Articles", StringComparison.OrdinalIgnoreCase))
            .Aggregate(string.Empty, (current, hashTag) => current + (" #" + hashTag.Replace(" ", "").Replace(".", "")));
    }
    
    public static string EncodeUrlInKey(string url)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
        var base64 = Convert.ToBase64String(keyBytes);
        return base64.Replace('/','_').Replace('+','-');
    }
    
    public static string DecodeUrlInKey(string encodedKey)
    {
        var base64 = encodedKey.Replace('-','+').Replace('_', '/');
        var bytes = Convert.FromBase64String(base64);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
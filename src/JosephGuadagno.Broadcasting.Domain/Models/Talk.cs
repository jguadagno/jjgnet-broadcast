using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// A talk that is given at an <see cref="Engagement"/>
/// </summary>
public class Talk
{
        
    /// <summary>
    /// The identifier for the talk
    /// </summary>
    public int Id { get; set; }
        
    /// <summary>
    /// The name of the talk
    /// </summary>
    [Required]
    public string Name { get; set; }
        
    /// <summary>
    /// A Url for the talk on the conference website
    /// </summary>
    [Required]
    [Url]
    public string UrlForConferenceTalk { get; set; }
        
    /// <summary>
    /// The Url for the talk on the talk deliverers website
    /// </summary>
    [Required]
    [Url]
    public string UrlForTalk { get; set; }
        
    /// <summary>
    /// The start date and time of the talk
    /// </summary>
    [Required]
    public DateTimeOffset StartDateTime { get; set; }
        
    /// <summary>
    /// The end date and time of the talk
    /// </summary>
    [Required]
    public DateTimeOffset EndDateTime { get; set; }
        
    /// <summary>
    /// The room/channel/url for the talk
    /// </summary>
    public string TalkLocation { get; set; }
        
    /// <summary>
    /// Comments for the talk
    /// </summary>
    public string Comments { get; set; }
    
    /// <summary>
    /// The identifier of the engagement
    /// </summary>
    public int EngagementId { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// A speaking engagement
/// </summary>
/// <remarks>This can be a conference or webinar or event that holds one or more <see cref="Talk"/></remarks>s
public class Engagement
{
        
    /// <summary>
    /// The Id of the item
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The name of the engagement
    /// </summary>
    [Required]
    public string Name { get; set; }
        
    /// <summary>
    /// The Url for the engagement
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; }
        
    /// <summary>
    /// The date and time the engagement starts
    /// </summary>
    [Required]
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The date and time the engagement ends
    /// </summary>
    [Required]
    public DateTimeOffset EndDateTime { get; set; }
    
    /// <summary>
    /// The IANA Time Zone Identifier for the engagement
    /// </summary>
    [Required]
    public string TimeZoneId { get; set; }

    /// <summary>
    /// Comments for the engagement
    /// </summary>
    /// <remarks>Could be a discount code for the engagement</remarks>
    public string? Comments { get; set; }

    /// <summary>
    /// A list of all of the talks that are being delivered
    /// </summary>
    public List<Talk>? Talks { get; set; }
}
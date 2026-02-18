using System.ComponentModel.DataAnnotations;
using NodaTime.TimeZones;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// A speaking engagement
/// </summary>
/// <remarks>This can be a conference or webinar or event that holds one or more <see cref="TalkViewModel"/></remarks>s
public class EngagementViewModel
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
    /// The IANI of the time zone for the engagement
    /// </summary>
    [Required]
    public string TimeZoneId { get; set; }
    
    /// <summary>
    /// Comments for the engagement
    /// </summary>
    /// <remarks>Could be a discount code for the engagement</remarks>
    public string? Comments { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }

    /// <summary>
    /// A list of all the talks that are being delivered
    /// </summary>
    public List<TalkViewModel>? Talks { get; set; }

    /// <summary>
    /// Returns a list of TimeZones
    /// </summary>
    public List<string> TimeZones
    {
        get
        {
            var timeZones = TzdbDateTimeZoneSource.Default.ZoneLocations;
            return timeZones is null
                ? new List<string>()
                : timeZones.OrderBy(tz => tz.ZoneId).Select(tz => tz.ZoneId).ToList();
        }
    }
}
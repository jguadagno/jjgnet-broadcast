#nullable disable

using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class Talk
{
    public int Id { get; set; }
    public int? EngagementId { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    [Url]
    public string UrlForConferenceTalk { get; set; }
    [Required]
    [Url]
    public string UrlForTalk { get; set; }
    [Required]
    public DateTimeOffset StartDateTime { get; set; }
    [Required]
    public DateTimeOffset EndDateTime { get; set; }
    public string TalkLocation { get; set; }
    public string Comments { get; set; }

    public virtual Engagement Engagement { get; set; }
}
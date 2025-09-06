#nullable disable

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class Talk
{
    public int Id { get; set; }
    public int? EngagementId { get; set; }
    public string Name { get; set; }
    public string UrlForConferenceTalk { get; set; }
    public string UrlForTalk { get; set; }
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public string TalkLocation { get; set; }
    public string Comments { get; set; }

    public virtual Engagement Engagement { get; set; }
}
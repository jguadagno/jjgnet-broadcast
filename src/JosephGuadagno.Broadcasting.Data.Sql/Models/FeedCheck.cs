#nullable disable

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class FeedCheck
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset LastCheckedFeed { get; set; }
    public DateTimeOffset LastItemAddedOrUpdated { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
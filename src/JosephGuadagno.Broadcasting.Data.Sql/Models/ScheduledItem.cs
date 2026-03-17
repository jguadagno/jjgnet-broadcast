#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

[ExcludeFromCodeCoverage]
public partial class ScheduledItem
{
    public int Id { get; set; }
    public string ItemTableName { get; set; }
    public int ItemPrimaryKey { get; set; }

    public string Message { get; set; }
    public string MessageTemplate { get; set; }
    public string ImageUrl { get; set; }
    public DateTimeOffset SendOnDateTime { get; set; }
    public bool MessageSent { get; set; }
    public DateTimeOffset? MessageSentOn { get; set; }
}
using System;
using System.Collections.Generic;

#nullable disable

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class ScheduledItem
{
    public int Id { get; set; }
    public string ItemTable { get; set; }
    public string ItemPrimaryKey { get; set; }
    public string ItemSecondaryKey { get; set; }
    public string Message { get; set; }
    public DateTimeOffset SendOnDateTime { get; set; }
    public bool MessageSent { get; set; }
    public DateTimeOffset? MessageSentOn { get; set; }
}
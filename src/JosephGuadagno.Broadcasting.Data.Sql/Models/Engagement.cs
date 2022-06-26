using System;
using System.Collections.Generic;

#nullable disable

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class Engagement
{
    public Engagement()
    {
        Talks = new HashSet<Talk>();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public string TimeZoneId { get; set; }
    public string Comments { get; set; }

    public virtual ICollection<Talk> Talks { get; set; }
}
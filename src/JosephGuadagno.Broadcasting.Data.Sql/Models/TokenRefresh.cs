#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

[ExcludeFromCodeCoverage]
public partial class TokenRefresh
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset Expires { get; set; }
    public DateTimeOffset LastChecked { get; set; }
    public DateTimeOffset LastRefreshed { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

#nullable disable

namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public partial class TokenRefresh
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset Expires { get; set; }
    public DateTimeOffset LastChecked { get; set; }
    public DateTimeOffset LastRefreshed { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

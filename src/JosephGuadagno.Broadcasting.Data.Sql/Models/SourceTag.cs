namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

public class SourceTag
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

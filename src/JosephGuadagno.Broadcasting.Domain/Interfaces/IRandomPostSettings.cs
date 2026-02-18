namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IRandomPostSettings
{
    public List<string> ExcludedCategories { get; set; }
    public DateTimeOffset CutoffDate { get; set; }
}
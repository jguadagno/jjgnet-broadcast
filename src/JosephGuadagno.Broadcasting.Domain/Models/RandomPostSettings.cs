using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class RandomPostSettings: IRandomPostSettings
{
    public required List<string> ExcludedCategories { get; set; }
    public DateTimeOffset CutoffDate { get; set; }
}
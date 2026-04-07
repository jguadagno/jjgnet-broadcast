namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Result of looking up a scheduled item's source
/// </summary>
public class ScheduledItemLookupResult
{
    /// <summary>
    /// Whether the source item was found
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The title/name of the source item if found
    /// </summary>
    public string? ItemTitle { get; set; }

    /// <summary>
    /// Error message if lookup failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional item details (type-specific)
    /// </summary>
    public string? ItemDetails { get; set; }
}

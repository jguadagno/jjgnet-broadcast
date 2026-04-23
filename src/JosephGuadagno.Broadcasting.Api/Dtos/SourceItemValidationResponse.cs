namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response for the source item validation endpoint
/// </summary>
public class SourceItemValidationResponse
{
    /// <summary>
    /// Whether the source item exists and is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The title or name of the source item, if found
    /// </summary>
    public string? ItemTitle { get; set; }

    /// <summary>
    /// Additional item details (type-specific), if found
    /// </summary>
    public string? ItemDetails { get; set; }

    /// <summary>
    /// Error message when the item is not found or a lookup error occurred
    /// </summary>
    public string? ErrorMessage { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a message template body. The target platform and
/// message type are provided via route parameters, not in this payload.
/// </summary>
public class MessageTemplateRequest
{
    /// <summary>
    /// The message template body. May contain placeholders for dynamic substitution at broadcast time.
    /// <remarks>This field is required.</remarks>
    /// </summary>
    [Required]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// An optional human-readable description of the template's purpose or expected usage context.
    /// </summary>
    public string? Description { get; set; }
}

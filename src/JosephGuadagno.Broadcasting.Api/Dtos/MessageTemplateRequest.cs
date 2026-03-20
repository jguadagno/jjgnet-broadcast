using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for updating a message template.
/// Platform and MessageType are provided via route parameters.
/// </summary>
public class MessageTemplateRequest
{
    [Required]
    public string Template { get; set; } = string.Empty;

    public string? Description { get; set; }
}

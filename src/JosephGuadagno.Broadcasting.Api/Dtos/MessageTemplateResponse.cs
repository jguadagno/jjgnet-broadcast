namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a message template.
/// </summary>
public class MessageTemplateResponse
{
    public string Platform { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string? Description { get; set; }
}

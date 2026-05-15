namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a message template, returned by the message template endpoints.
/// Each template is uniquely identified by the combination of platform and message type.
/// </summary>
public class MessageTemplateResponse
{
    /// <summary>
    /// The name of the social media platform this template targets (e.g., <c>"Twitter"</c>, <c>"LinkedIn"</c>).
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// The category or type of message this template is used for (e.g., <c>"NewPost"</c>, <c>"Reminder"</c>).
    /// Together with <see cref="Platform"/>, this uniquely identifies a template.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The message template body, optionally containing placeholders for dynamic content substituted at broadcast time.
    /// </summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// An optional human-readable description of the template's purpose or expected usage context.
    /// </summary>
    public string? Description { get; set; }
}

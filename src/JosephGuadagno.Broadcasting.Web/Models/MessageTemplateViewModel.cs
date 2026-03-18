using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// A Scriban template used to render broadcast messages for a specific platform and message type.
/// </summary>
public class MessageTemplateViewModel
{
    /// <summary>
    /// The social platform name, e.g. Twitter, Facebook, LinkedIn, Bluesky.
    /// </summary>
    [Required]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// The message category, e.g. RandomPost, NewPost.
    /// </summary>
    [Required]
    [Display(Name = "Message Type")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The Scriban template string used to render the broadcast message.
    /// </summary>
    [Required]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what the template is for.
    /// </summary>
    public string? Description { get; set; }
}

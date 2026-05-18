namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Identifies a (platform × message-type) combination that is required for the current user
/// but does not yet have a message template.
/// </summary>
public sealed record MissingTemplateKey(string Platform, string MessageType);

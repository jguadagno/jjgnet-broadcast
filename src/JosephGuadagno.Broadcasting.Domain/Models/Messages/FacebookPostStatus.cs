namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

public class FacebookPostStatus
{
    public required string StatusText { get; set; }
    public required string LinkUri { get; set; }
    /// <summary>
    /// An optional URL for an image to use as the link preview thumbnail (Facebook Graph API <c>picture</c> parameter).
    /// </summary>
    public string? ImageUrl { get; set; }
    /// <summary>The Entra OID of the user who owns the content. Used to resolve per-user credentials at send time.</summary>
    public string? CreatedByEntraOid { get; set; }
}
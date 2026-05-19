namespace JosephGuadagno.Broadcasting.Composers;

/// <summary>
/// Renders a Scriban message template using content fields from a <see cref="JosephGuadagno.Broadcasting.Domain.Models.SocialMediaPublishRequest"/>.
/// </summary>
public interface IPostComposer
{
    /// <summary>
    /// Renders the provided Scriban <paramref name="templateContent"/> using content fields
    /// from <paramref name="request"/> as template variables.
    /// </summary>
    /// <param name="request">The publish request providing template variables (Title, LinkUrl, ShortenedUrl, Description, Hashtags, ImageUrl).</param>
    /// <param name="templateContent">The Scriban template string to render.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The rendered string if rendering succeeds and the result is non-empty; otherwise <c>null</c>.
    /// </returns>
    Task<string?> ComposeAsync(
        Domain.Models.SocialMediaPublishRequest request,
        string templateContent,
        CancellationToken cancellationToken = default);
}

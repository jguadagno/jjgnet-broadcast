using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Composers;

/// <summary>
/// Renders Scriban message templates using content fields from a <see cref="SocialMediaPublishRequest"/>.
/// Centralizes the template-rendering logic previously duplicated across all four publisher managers.
/// </summary>
public class PostComposer(ILogger<PostComposer> logger) : IPostComposer
{
    /// <inheritdoc />
    public async Task<string?> ComposeAsync(
        SocialMediaPublishRequest request,
        string templateContent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            return null;
        }

        try
        {
            var url = request.ShortenedUrl ?? request.LinkUrl ?? string.Empty;
            var tags = request.Hashtags is { Count: > 0 }
                ? string.Join(" ", request.Hashtags.Select(t => t.StartsWith('#') ? t : $"#{t}"))
                : string.Empty;

            var template = Template.Parse(templateContent);
            var scriptObject = new ScriptObject();
            scriptObject.Import(new
            {
                title = request.Title ?? string.Empty,
                url,
                description = request.Description ?? string.Empty,
                tags,
                image_url = request.ImageUrl ?? string.Empty
            });
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            var rendered = await template.RenderAsync(context);

            return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Scriban template rendering failed for {Title}", request.Title);
            return null;
        }
    }
}

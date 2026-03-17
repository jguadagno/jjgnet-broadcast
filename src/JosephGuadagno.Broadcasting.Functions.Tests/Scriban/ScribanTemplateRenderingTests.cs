using System;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Scriban;

/// <summary>
/// Tests the core Scriban rendering pattern used by all 4 publish functions.
/// Exercises the exact same Template.Parse → ScriptObject.Import → TemplateContext → RenderAsync
/// pipeline to verify field mapping and edge-case behaviour in isolation.
/// </summary>
public class ScribanTemplateRenderingTests
{
    /// <summary>
    /// Replicates the TryRenderTemplateAsync pattern used in every platform function.
    /// </summary>
    private static async Task<string?> RenderAsync(
        string templateContent,
        string title,
        string url,
        string description = "",
        string tags = "",
        string? imageUrl = null)
    {
        try
        {
            var template = Template.Parse(templateContent);
            var scriptObject = new ScriptObject();
            scriptObject.Import(new { title, url, description, tags, image_url = imageUrl });
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            var rendered = await template.RenderAsync(context);
            return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
        }
        catch
        {
            return null;
        }
    }

    [Fact]
    public async Task RenderAsync_WithTitleAndUrl_RendersBothFields()
    {
        var result = await RenderAsync("{{ title }} - {{ url }}", "My Blog Post", "https://example.com/post");

        Assert.Equal("My Blog Post - https://example.com/post", result);
    }

    [Fact]
    public async Task RenderAsync_WithDescription_RendersDescriptionField()
    {
        var result = await RenderAsync(
            "{{ title }}\n\n{{ description }}\n\n{{ url }}",
            "My Talk",
            "https://example.com/talk",
            "Come see my session!");

        Assert.Equal("My Talk\n\nCome see my session!\n\nhttps://example.com/talk", result);
    }

    [Fact]
    public async Task RenderAsync_WithTags_RendersTagsField()
    {
        var result = await RenderAsync(
            "{{ title }} {{ tags }}",
            "Post Title",
            "https://example.com",
            tags: "#dotnet #azure");

        Assert.Equal("Post Title #dotnet #azure", result);
    }

    [Fact]
    public async Task RenderAsync_WhenImageUrlIsSet_ExposesImageUrlInRenderedOutput()
    {
        var result = await RenderAsync(
            "{{ title }} {{ image_url }}",
            "Post Title",
            "https://example.com",
            imageUrl: "https://cdn.example.com/image.jpg");

        Assert.Equal("Post Title https://cdn.example.com/image.jpg", result);
    }

    [Fact]
    public async Task RenderAsync_WhenImageUrlIsNull_ImageUrlRendersAsEmpty()
    {
        // Scriban renders null model values as empty strings
        var result = await RenderAsync(
            "{{ title }}|{{ image_url }}",
            "Post Title",
            "https://example.com",
            imageUrl: null);

        Assert.Equal("Post Title|", result);
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateProducesOnlyWhitespace_ReturnsNull()
    {
        var result = await RenderAsync("   ", "Title", "https://example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateIsEmpty_ReturnsNull()
    {
        var result = await RenderAsync("", "Title", "https://example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateHasNoVariables_ReturnsLiteralText()
    {
        var result = await RenderAsync("Hello World", "Title", "https://example.com");

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task RenderAsync_RenderedOutputIsTrimmed()
    {
        var result = await RenderAsync("  {{ title }}  ", "My Title", "https://example.com");

        Assert.Equal("My Title", result);
    }

    [Fact]
    public async Task RenderAsync_AllFiveFields_AreAvailableInTemplate()
    {
        // Validates the full field set that every platform function exposes
        var result = await RenderAsync(
            "{{ title }}|{{ url }}|{{ description }}|{{ tags }}|{{ image_url }}",
            "Title",
            "https://example.com",
            description: "Desc",
            tags: "#tag",
            imageUrl: "https://img.example.com/img.jpg");

        Assert.Equal("Title|https://example.com|Desc|#tag|https://img.example.com/img.jpg", result);
    }
}

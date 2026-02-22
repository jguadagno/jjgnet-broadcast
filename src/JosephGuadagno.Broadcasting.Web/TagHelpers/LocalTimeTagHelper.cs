using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JosephGuadagno.Broadcasting.Web.TagHelpers;

/// <summary>
/// Renders a <c>&lt;time&gt;</c> element with the ISO 8601 datetime attribute so that
/// client-side JavaScript can convert the value to the user's local time.
/// </summary>
[HtmlTargetElement("local-time")]
public class LocalTimeTagHelper : TagHelper
{
    /// <summary>
    /// The <see cref="DateTimeOffset"/> value to display.
    /// </summary>
    public DateTimeOffset? Value { get; set; }

    /// <summary>
    /// When <c>true</c>, only the date portion is shown (no time).  Defaults to <c>false</c>.
    /// </summary>
    public bool DateOnly { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "time";
        output.TagMode = TagMode.StartTagAndEndTag;

        if (!Value.HasValue)
        {
            output.SuppressOutput();
            return;
        }

        var value = Value.Value;
        output.Attributes.SetAttribute("datetime", value.ToString("o"));
        output.Attributes.SetAttribute("data-local-time", DateOnly ? "date" : "datetime");
        // "d" = short date pattern (e.g. 1/22/2026), "f" = full date/time pattern (e.g. Friday, January 22, 2026 3:00 PM).
        // These serve as server-side fallback text; JavaScript replaces them with the browser's local time.
        output.Content.SetContent(DateOnly
            ? value.ToString("d")
            : value.ToString("f"));
    }
}

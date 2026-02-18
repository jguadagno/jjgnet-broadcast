namespace JosephGuadagno.Broadcasting.Functions;

/// <summary>
/// Helper class to build a list of hashtags
/// </summary>
public static class HashTagLists
{
    /// <summary>
    /// Builds a list of hashtags from a comma-separated list of tags
    /// </summary>
    /// <param name="tags"></param>
    /// <returns>A comma-separated list of hashtags</returns>
    /// <remarks>If the <param name="tags">is null or empty, it returns a default set of hashtags.</param> Removes any hashtags that are articles.</remarks>
    public static string BuildHashTagList(string? tags)
    {
        if (string.IsNullOrEmpty(tags))
        {
            return "#dotnet #csharp #dotnetcore";
        }

        var tagList = tags.Split(',');
        var hashTagCategories = tagList.Where(tag => !tag.Contains("Article"));

        return hashTagCategories.Aggregate("",
            (current, tag) => current + $" #{tag.Replace(" ", "").Replace(".", "")}");
    }
}
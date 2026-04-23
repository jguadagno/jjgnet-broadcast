using System.Net;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the YouTube Sources API
/// </summary>
public class YouTubeSourceService(IDownstreamApi apiClient): IYouTubeSourceService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/YouTubeSources";

    /// <summary>
    /// Gets all YouTube sources
    /// </summary>
    /// <returns>A List&lt;<see cref="YouTubeSource"/>&gt;s</returns>
    public async Task<List<YouTubeSource>> GetAllAsync()
    {
        var sources = await apiClient.GetForUserAsync<List<YouTubeSource>>(ApiServiceName, options =>
        {
            options.RelativePath = BaseUrl;
        });
        return sources ?? [];
    }

    /// <summary>
    /// Gets a YouTube source by ID
    /// </summary>
    /// <param name="id">The identifier of the YouTube source to get</param>
    /// <returns>A <see cref="YouTubeSource"/></returns>
    public async Task<YouTubeSource?> GetAsync(int id)
    {
        var source = await apiClient.GetForUserAsync<YouTubeSource>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });
        return source;
    }

    /// <summary>
    /// Saves a YouTube source
    /// </summary>
    /// <param name="source">The YouTube source to save.</param>
    /// <returns>The saved YouTube source</returns>
    public async Task<YouTubeSource?> SaveAsync(YouTubeSource source)
    {
        var savedSource = await apiClient.PostForUserAsync<YouTubeSource, YouTubeSource>(ApiServiceName, source, options =>
        {
            options.RelativePath = BaseUrl;
        });
        return savedSource;
    }

    /// <summary>
    /// Deletes a YouTube source
    /// </summary>
    /// <param name="id">The identifier of the YouTube source to delete</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> DeleteAsync(int id)
    {
        var response = await apiClient.CallApiForUserAsync<HttpResponseMessage>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
            options.HttpMethod = HttpMethod.Delete.Method;
        });

        return response is { StatusCode: HttpStatusCode.NoContent };
    }
}

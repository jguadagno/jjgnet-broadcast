using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
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
    /// Gets a paged list of YouTube sources
    /// </summary>
    public async Task<PagedResult<YouTubeSource>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<YouTubeSource>>(ApiServiceName, options =>
        {
            var queryParams = $"page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDescending={sortDescending}";
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams += $"&filter={Uri.EscapeDataString(filter)}";
            }
            options.RelativePath = $"{BaseUrl}?{queryParams}";
        });

        if (pagedResponse is null) return new PagedResult<YouTubeSource>();
        return new PagedResult<YouTubeSource> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
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

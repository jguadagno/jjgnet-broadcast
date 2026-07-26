using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the YouTube Sources API
/// </summary>
public class YouTubeItemService(IDownstreamApi apiClient, ILogger<YouTubeItemService> logger) : IYouTubeItemService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/YouTubeItems";

    /// <summary>
    /// Gets a paged list of YouTube sources
    /// </summary>
    public async Task<PagedResult<YouTubeItem>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<YouTubeItem>>(ApiServiceName, options =>
        {
            var queryParams = $"page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDescending={sortDescending}";
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams += $"&filter={Uri.EscapeDataString(filter)}";
            }
            options.RelativePath = $"{BaseUrl}?{queryParams}";
        });

        if (pagedResponse is null)
        {
            logger.LogWarning("GetAllAsync downstream returned null (page={Page}, pageSize={PageSize})", page, pageSize);
            return new PagedResult<YouTubeItem>();
        }
        return new PagedResult<YouTubeItem> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
    }

    /// <summary>
    /// Gets a YouTube source by ID
    /// </summary>
    /// <param name="id">The identifier of the YouTube source to get</param>
    /// <returns>A <see cref="YouTubeItem"/></returns>
    public async Task<YouTubeItem?> GetAsync(int id)
    {
        var source = await apiClient.GetOptionalForUserAsync<YouTubeItem>(ApiServiceName, options =>
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
    public async Task<YouTubeItem?> SaveAsync(YouTubeItem source)
    {
        var savedSource = await apiClient.PostForUserAsync<YouTubeItem, YouTubeItem>(ApiServiceName, source, options =>
        {
            options.RelativePath = BaseUrl;
        });

        if (savedSource is null)
        {
            logger.LogWarning("SaveAsync downstream returned null for YouTube item {ItemId}", source.Id);
        }

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

        if (response is { StatusCode: HttpStatusCode.NoContent })
        {
            return true;
        }

        logger.LogWarning("DeleteAsync unexpected status {StatusCode} for YouTube item {ItemId}", response?.StatusCode, id);
        return false;
    }
}

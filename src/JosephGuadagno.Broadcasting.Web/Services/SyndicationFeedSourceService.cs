using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the Syndication Feed Sources API
/// </summary>
public class SyndicationFeedSourceService(IDownstreamApi apiClient): ISyndicationFeedSourceService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/SyndicationFeedSources";

    /// <summary>
    /// Gets a paged list of syndication feed sources
    /// </summary>
    public async Task<PagedResult<SyndicationFeedSource>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<SyndicationFeedSource>>(ApiServiceName, options =>
        {
            var queryParams = $"page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDescending={sortDescending}";
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams += $"&filter={Uri.EscapeDataString(filter)}";
            }
            options.RelativePath = $"{BaseUrl}?{queryParams}";
        });

        if (pagedResponse is null) return new PagedResult<SyndicationFeedSource>();
        return new PagedResult<SyndicationFeedSource> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
    }

    /// <summary>
    /// Gets a syndication feed source by ID
    /// </summary>
    /// <param name="id">The identifier of the syndication feed source to get</param>
    /// <returns>A <see cref="SyndicationFeedSource"/></returns>
    public async Task<SyndicationFeedSource?> GetAsync(int id)
    {
        var source = await apiClient.GetForUserAsync<SyndicationFeedSource>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });
        return source;
    }

    /// <summary>
    /// Saves a syndication feed source
    /// </summary>
    /// <param name="source">The syndication feed source to save.</param>
    /// <returns>The saved syndication feed source</returns>
    public async Task<SyndicationFeedSource?> SaveAsync(SyndicationFeedSource source)
    {
        var savedSource = await apiClient.PostForUserAsync<SyndicationFeedSource, SyndicationFeedSource>(ApiServiceName, source, options =>
        {
            options.RelativePath = BaseUrl;
        });
        return savedSource;
    }

    /// <summary>
    /// Deletes a syndication feed source
    /// </summary>
    /// <param name="id">The identifier of the syndication feed source to delete</param>
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

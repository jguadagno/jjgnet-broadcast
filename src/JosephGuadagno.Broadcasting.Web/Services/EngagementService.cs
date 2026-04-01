using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;

using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the Engagement Api
/// </summary>
public class EngagementService(IDownstreamApi apiClient): IEngagementService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string EngagementBaseUrl = "/engagements";

    /// <summary>
    /// Gets all the engagements
    /// </summary>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="Engagement"/>&gt;s</returns>
    public async Task<List<Engagement>> GetEngagementsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        var pagedResponse  = await apiClient.GetForUserAsync<PagedResponse<Engagement>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}?page={page}&pageSize={pageSize}";
        });

        return pagedResponse is null ? [] : pagedResponse.Items.ToList();
    }
    
    /// <summary>
    /// Gets an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to get</param>
    /// <returns>An <see cref="Engagement"/></returns>
    public async Task<Engagement?> GetEngagementAsync(int engagementId)
    {
        var engagement = await apiClient.GetForUserAsync<Engagement>(ApiServiceName, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}/{engagementId}";
        });
        return engagement;
    }
    
    /// <summary>
    /// Saves an engagement
    /// </summary>
    /// <param name="engagement">The engagement to save.</param>
    /// <returns>The engagement</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<Engagement?> SaveEngagementAsync(Engagement engagement)
    {
        var savedEngagement = await apiClient.PostForUserAsync<Engagement, Engagement>(ApiServiceName, engagement, options =>
        {
            options.RelativePath = EngagementBaseUrl;
        });
        return savedEngagement;
    }
    
    /// <summary>
    /// Deletes an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to delete</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> DeleteEngagementAsync(int engagementId)
    {
        var response = await apiClient.CallApiForUserAsync<HttpResponseMessage>(ApiServiceName, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}/{engagementId}";
            options.HttpMethod = HttpMethod.Delete.Method;
        });

        return response is { StatusCode: HttpStatusCode.NoContent };
    }

    /// <summary>
    /// Gets all the talks for a given engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to get the talks of</param>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="Talk"/>&gt;s</returns>
    public async Task<List<Talk>> GetEngagementTalksAsync(int engagementId, int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<Talk>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}/{engagementId}/talks?page={page}&pageSize={pageSize}";
        });

        return pagedResponse is null ? [] : pagedResponse.Items.ToList();
    }
    
    /// <summary>
    /// Saves a talk for an engagement
    /// </summary>
    /// <param name="talk">The <see cref="Talk"/> to save</param>
    /// <returns>The talk</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<Talk?> SaveEngagementTalkAsync(Talk talk)
    {
        var savedTalk = await apiClient.PostForUserAsync<Talk, Talk>(ApiServiceName, talk, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}/{talk.EngagementId}/talks";
        });

        return savedTalk;
    }
    
    /// <summary>
    /// Gets a talk for an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk</param>
    /// <returns>A <see cref="Talk"/></returns>
    public async Task<Talk?> GetEngagementTalkAsync(int engagementId, int talkId)
    {
        var talk = await apiClient.GetForUserAsync<Talk>(ApiServiceName, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}/{engagementId}/talks/{talkId}";
        });
        return talk;
    }
    
    /// <summary>
    /// Delete a talk from an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId)
    {
        var response = await apiClient.CallApiForUserAsync<HttpResponseMessage>(ApiServiceName, options =>
        {
            options.RelativePath = $"{EngagementBaseUrl}/{engagementId}/talks/{talkId}";
            options.HttpMethod = HttpMethod.Delete.Method;
        });

        return response is { StatusCode: HttpStatusCode.NoContent };
    }
}
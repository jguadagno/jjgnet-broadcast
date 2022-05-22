using System.Net;
using System.Text;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the Engagement Api
/// </summary>
public class EngagementService: ServiceBase, IEngagementService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<EngagementService> _logger;
    private readonly string _engagementBaseUrl;

    /// <summary>
    /// Initializes the service
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="tokenAcquisition">The token acquisition client</param>
    /// <param name="settings">Application <see cref="Settings"/> to use</param>
    /// <param name="telemetryClient">The telemetry client</param>
    /// <param name="logger">The logger</param>
    public EngagementService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, ISettings settings, TelemetryClient telemetryClient,
        ILogger<EngagementService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;

        HttpClient = httpClient;
        TokenAcquisition = tokenAcquisition;
        ApiScopeUrl = settings.ApiScopeUri;
        _engagementBaseUrl = settings.ApiRootUri + "/engagements";
    }
    
    /// <summary>
    /// Gets all of the engagements
    /// </summary>
    /// <returns>A List&lt;<see cref="Engagement"/>&gt;s</returns>
    public async Task<List<Engagement>?> GetEngagementsAsync()
    {
        await SetRequestHeader(Domain.Scopes.Engagements.List);
        return await ExecuteGetAsync<List<Engagement>>(_engagementBaseUrl);
    }
    
    /// <summary>
    /// Gets an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to get</param>
    /// <returns>An <see cref="Engagement"/></returns>
    public async Task<Engagement?> GetEngagementAsync(int engagementId)
    {
        await SetRequestHeader(Domain.Scopes.Engagements.View);
        var url = $"{_engagementBaseUrl}/{engagementId}";
        return await ExecuteGetAsync<Engagement>(url);
    }
    
    /// <summary>
    /// Saves an engagement
    /// </summary>
    /// <param name="engagement">The engagement to save.</param>
    /// <returns>The engagement</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<Engagement?> SaveEngagementAsync(Engagement engagement)
    {
        await SetRequestHeader(Domain.Scopes.Engagements.Modify);
        var jsonRequest = JsonSerializer.Serialize(engagement);
        var jsonContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(_engagementBaseUrl, jsonContent);

        if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
            
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var savedEngagement = JsonSerializer.Deserialize<Engagement>(content, options);
        return savedEngagement;
    }
    
    /// <summary>
    /// Deletes an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to delete</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> DeleteEngagementAsync(int engagementId)
    {
        await SetRequestHeader(Domain.Scopes.Engagements.Delete);
        var url = $"{_engagementBaseUrl}/{engagementId}";
        var response = await HttpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    /// <summary>
    /// Gets all the talks for a given engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to get the talks of</param>
    /// <returns>A List&lt;<see cref="Talk"/>&gt;s</returns>
    public async Task<List<Talk>?> GetEngagementTalksAsync(int engagementId)
    {
        await SetRequestHeader(Domain.Scopes.Talks.List);
        var url = $"{_engagementBaseUrl}/{engagementId}/talks";
        return await ExecuteGetAsync<List<Talk>>(url);
    }
    
    /// <summary>
    /// Saves a talk for an engagement
    /// </summary>
    /// <param name="talk">The <see cref="Talk"/> to save</param>
    /// <returns>The talk</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<Talk?> SaveEngagementTalkAsync(Talk talk)
    {
        await SetRequestHeader(Domain.Scopes.Talks.Modify);
        var url = $"{_engagementBaseUrl}/{talk.EngagementId}/talks";
        var jsonRequest = JsonSerializer.Serialize(talk);
        var jsonContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, jsonContent);

        if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
            
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var savedTalk = JsonSerializer.Deserialize<Talk>(content, options);
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
        await SetRequestHeader(Domain.Scopes.Talks.View);
        var url = $"{_engagementBaseUrl}/{engagementId}/talks/{talkId}";
        return await ExecuteGetAsync<Talk>(url);
    }
    
    /// <summary>
    /// Delete a talk from an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId)
    {
        await SetRequestHeader(Domain.Scopes.Engagements.Delete);
        var url = $"{_engagementBaseUrl}/{engagementId}/talks/{talkId}";
        var response = await HttpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }

}
using System.Net;
using System.Text;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.ApplicationInsights;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the Engagement Api
/// </summary>
public class EngagementService: ServiceBase, IEngagementService
{
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<EngagementService> _logger;
    private readonly string _engagementBaseUrl;

    /// <summary>
    /// Initializes the service
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="settings">Application <see cref="Settings"/> to use</param>
    /// <param name="telemetryClient">The telemetry client</param>
    /// <param name="logger">The logger</param>
    public EngagementService(HttpClient httpClient, ISettings settings, TelemetryClient telemetryClient,
        ILogger<EngagementService> logger)
    {
        _httpClient = HttpClient = httpClient;
        _telemetryClient = telemetryClient;
        _logger = logger;

        _engagementBaseUrl = settings.ApiRootUri + "/engagements";
    }
    
    /// <summary>
    /// Gets all of the engagements
    /// </summary>
    /// <returns>A List&lt;<see cref="Engagement"/>&gt;s</returns>
    public async Task<List<Engagement>?> GetEngagementsAsync()
    {
        return await ExecuteGetAsync<List<Engagement>>(_engagementBaseUrl);
    }
    
    /// <summary>
    /// Gets an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to get</param>
    /// <returns>An <see cref="Engagement"/></returns>
    public async Task<Engagement?> GetEngagementAsync(int engagementId)
    {
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
        var jsonRequest = JsonSerializer.Serialize(engagement);
        var jsonContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_engagementBaseUrl, jsonContent);

        if (response.StatusCode != HttpStatusCode.Created)
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
        var url = $"{_engagementBaseUrl}{engagementId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    /// <summary>
    /// Gets all the talks for a given engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to get the talks of</param>
    /// <returns>A List&lt;<see cref="Talk"/>&gt;s</returns>
    public async Task<List<Talk>?> GetEngagementTalksAsync(int engagementId)
    {
        var url = $"{_engagementBaseUrl}/{engagementId}/talks";
        return await ExecuteGetAsync<List<Talk>>(url);
    }
    
    /// <summary>
    /// Saves a talk for an engagement
    /// </summary>
    /// <param name="talk">The <see cref="Talk"/> to save</param>
    /// <returns>The talk</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<Talk?> SaveEngagementTalkAsync(Talk? talk)
    {
        var url = $"{_engagementBaseUrl}/talks";
        var jsonRequest = JsonSerializer.Serialize(talk);
        var jsonContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, jsonContent);

        if (response.StatusCode != HttpStatusCode.Created)
            throw new HttpRequestException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
            
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        talk = JsonSerializer.Deserialize<Talk>(content, options);
        return talk;
    }
    
    /// <summary>
    /// Gets a talk for an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk</param>
    /// <returns>A <see cref="Talk"/></returns>
    public async Task<Talk?> GetEngagementTalkAsync(int engagementId, int talkId)
    {
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
        var url = $"{_engagementBaseUrl}/{engagementId}/talks/{talkId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
}
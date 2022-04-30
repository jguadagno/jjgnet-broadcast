using System.Net;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.ApplicationInsights;

namespace JosephGuadagno.Broadcasting.Web.Services;

public class EngagementService: IEngagementService
{
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ISettings _settings;
    private readonly ILogger<EngagementService> _logger;

    public EngagementService(HttpClient httpClient, ISettings settings, TelemetryClient telemetryClient,
        ILogger<EngagementService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    // GetAll
    public async Task<List<Engagement>?> GetEngagementsAsync()
    {
        var url = $"{_settings.ApiRootUri}engagements/";
        return await ExecuteGetAsync<List<Engagement>>(url);
    }
    
    // Get (id)
    public async Task<Engagement?> GetEngagementAsync(int engagementId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}";
        return await ExecuteGetAsync<Engagement>(url);
    }
    
    // TODO: Save (Engagement)
    public async Task<Engagement?> SaveEngagementAsync(Engagement engagement)
    {
        return new Engagement();
    }
    
    // Delete (id)
    public async Task<bool> DeleteEngagementAsync(int engagementId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    // Get Talks (EngagementId)
    public async Task<List<Engagement>?> GetEngagementTalksAsync(int engagementId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}/talks";
        return await ExecuteGetAsync<List<Engagement>>(url);
    }
    
    // TODO: Save Talk (Engagement)
    public async Task<Talk?> SaveEngagementTalkAsync(Talk talk)
    {
        return new Talk();
    }
    
    // Get Talk (EngagementId, TalkId)
    public async Task<Engagement?> GetEngagementTalkAsync(int engagementId, int talkId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}/talks/{talkId}";
        return await ExecuteGetAsync<Engagement>(url);
    }
    
    // Delete Talk (EngagementId, TalkId)
    public async Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}/talks/{talkId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    private async Task<T?> ExecuteGetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
            
        // Parse the Results
        var content = await response.Content.ReadAsStringAsync();
                
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var results = JsonSerializer.Deserialize<T>(content, options);

        return results;
    }
}
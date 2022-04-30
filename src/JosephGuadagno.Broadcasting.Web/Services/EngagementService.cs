using System.Net;
using System.Text;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.ApplicationInsights;

namespace JosephGuadagno.Broadcasting.Web.Services;

public class EngagementService: ServiceBase, IEngagementService
{
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ISettings _settings;
    private readonly ILogger<EngagementService> _logger;

    public EngagementService(HttpClient httpClient, ISettings settings, TelemetryClient telemetryClient,
        ILogger<EngagementService> logger)
    {
        _httpClient = HttpClient = httpClient;
        _settings = settings;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    // GetAll
    public async Task<List<Engagement>?> GetEngagementsAsync()
    {
        var url = $"{_settings.ApiRootUri}/engagements/";
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
        var url = $"{_settings.ApiRootUri}/engagements/";
        var jsonRequest = JsonSerializer.Serialize(engagement);
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
        var savedEngagement = JsonSerializer.Deserialize<Engagement>(content, options);
        return savedEngagement;
    }
    
    // Delete (id)
    public async Task<bool> DeleteEngagementAsync(int engagementId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    // Get Talks (EngagementId)
    public async Task<List<Talk>?> GetEngagementTalksAsync(int engagementId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}/talks";
        return await ExecuteGetAsync<List<Talk>>(url);
    }
    
    // TODO: Save Talk (Engagement)
    public async Task<Talk?> SaveEngagementTalkAsync(Talk? talk)
    {
        var url = $"{_settings.ApiRootUri}/engagements/talks";
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
    
    // Get Talk (EngagementId, TalkId)
    public async Task<Talk?> GetEngagementTalkAsync(int engagementId, int talkId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}/talks/{talkId}";
        return await ExecuteGetAsync<Talk>(url);
    }
    
    // Delete Talk (EngagementId, TalkId)
    public async Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId)
    {
        var url = $"{_settings.ApiRootUri}/engagements/{engagementId}/talks/{talkId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
}
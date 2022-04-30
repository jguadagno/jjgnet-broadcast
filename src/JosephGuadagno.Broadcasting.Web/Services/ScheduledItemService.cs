using System.Net;
using System.Text;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.ApplicationInsights;

namespace JosephGuadagno.Broadcasting.Web.Services;

public class ScheduledItemService: ServiceBase, IScheduledItemService
{
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ISettings _settings;
    private readonly ILogger<ScheduledItemService> _logger;

    public ScheduledItemService(HttpClient httpClient, ISettings settings, TelemetryClient telemetryClient,
        ILogger<ScheduledItemService> logger)
    {
        _httpClient = HttpClient = httpClient;
        _settings = settings;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    // GetAll
    public async Task<List<ScheduledItem>?> GetScheduledItemsAsync()
    {
        var url = $"{_settings.ApiRootUri}/schedules/";
        return await ExecuteGetAsync<List<ScheduledItem>>(url);
    }
    
    // Get (ScheduledItemId)
    public async Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId)
    {
        var url = $"{_settings.ApiRootUri}/schedules/{scheduledItemId}";
        return await ExecuteGetAsync<ScheduledItem>(url);
    }
    
    // Save (ScheduledItem)
    public async Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem)
    {
        var url = $"{_settings.ApiRootUri}/schedules/";
        var jsonRequest = JsonSerializer.Serialize(scheduledItem);
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
        var savedScheduledItem = JsonSerializer.Deserialize<ScheduledItem>(content, options);
        return savedScheduledItem;
    }
    
    // Delete
    public async Task<bool> DeleteScheduledItemAsync(int scheduledItemId)
    {
        var url = $"{_settings.ApiRootUri}/schedules/{scheduledItemId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    // Upcoming
    public async Task<List<ScheduledItem>?> GetUpcomingScheduledItems()
    {
        var url = $"{_settings.ApiRootUri}/schedules/upcoming";
        return await ExecuteGetAsync<List<ScheduledItem>>(url);
    }
}
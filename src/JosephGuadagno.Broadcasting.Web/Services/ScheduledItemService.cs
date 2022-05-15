using System.Net;
using System.Text;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.ApplicationInsights;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the schedule Api
/// </summary>
public class ScheduledItemService: ServiceBase, IScheduledItemService
{
    private readonly HttpClient _httpClient;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ScheduledItemService> _logger;
    private readonly string _scheduleBaseUrl;

    /// <summary>
    /// Initializes the service
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="settings">Application <see cref="Settings"/> to use</param>
    /// <param name="telemetryClient">The telemetry client</param>
    /// <param name="logger">The logger</param>
    public ScheduledItemService(HttpClient httpClient, ISettings settings, TelemetryClient telemetryClient,
        ILogger<ScheduledItemService> logger)
    {
        _httpClient = HttpClient = httpClient;
        _telemetryClient = telemetryClient;
        _logger = logger;

        _scheduleBaseUrl = settings.ApiRootUri + "/schedules";
    }
    
    /// <summary>
    /// Gets all of the scheduled items
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<List<ScheduledItem>?> GetScheduledItemsAsync()
    {
        return await ExecuteGetAsync<List<ScheduledItem>>(_scheduleBaseUrl);
    }
    
    /// <summary>
    /// Gets a scheduled items
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the <see cref="ScheduledItem"/></param>
    /// <returns>A <see cref="ScheduledItem"/></returns>
    public async Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId)
    {
        var url = $"{_scheduleBaseUrl}/{scheduledItemId}";
        return await ExecuteGetAsync<ScheduledItem>(url);
    }
    
    /// <summary>
    /// Saves a scheduled item
    /// </summary>
    /// <param name="scheduledItem">The <see cref="ScheduledItem"/> to save</param>
    /// <returns>A scheduled item</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem)
    {
        var jsonRequest = JsonSerializer.Serialize(scheduledItem);
        var jsonContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_scheduleBaseUrl, jsonContent);

        if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
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
    
    /// <summary>
    /// Deletes a scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the scheduled item to delete</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> DeleteScheduledItemAsync(int scheduledItemId)
    {
        var url = $"{_scheduleBaseUrl}/{scheduledItemId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
    
    /// <summary>
    /// Returns a list of any scheduled items that have not been sent
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<List<ScheduledItem>?> GetUnsentScheduledItemsAsync()
    {
        var url = $"{_scheduleBaseUrl}/unsent";
        return await ExecuteGetAsync<List<ScheduledItem>>(url);
    }
    
    /// <summary>
    /// Returns a list of any scheduled items that have not been sent that should have been sent
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<List<ScheduledItem>?> GetScheduledItemsToSendAsync()
    {
        var url = $"{_scheduleBaseUrl}/upcoming";
        return await ExecuteGetAsync<List<ScheduledItem>>(url);
    }

    /// <summary>
    /// Gets scheduled items for the given calendar month and year
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month</param>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt; that are for the month.  If there are no scheduled items, null will be returned</returns>
    public async Task<List<ScheduledItem>?> GetScheduledItemsByCalendarMonthAsync(int year, int month)
    {
        var url = $"{_scheduleBaseUrl}/calendar/{year}/{month}";
        return await ExecuteGetAsync<List<ScheduledItem>?>(url);
    }
}
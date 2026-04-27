using System.Net;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;

using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the schedule Api
/// </summary>
public class ScheduledItemService (IDownstreamApi apiClient, ILogger<ScheduledItemService> logger): IScheduledItemService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string ScheduledItemBaseUrl = "/Schedules";

    /// <summary>
    /// Gets all the scheduled items
    /// </summary>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize, string sortBy = "sendondatetime", bool sortDescending = true, string? filter = null)
    {
        var url = $"{ScheduledItemBaseUrl}?page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDescending={sortDescending}";
        if (!string.IsNullOrWhiteSpace(filter))
            url += $"&filter={Uri.EscapeDataString(filter)}";
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<ScheduledItem>>(ApiServiceName, options =>
        {
            options.RelativePath = url;
        });
        if (pagedResponse is null) return new PagedResult<ScheduledItem>();
        return new PagedResult<ScheduledItem> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
    }
    
    /// <summary>
    /// Gets a scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the <see cref="ScheduledItem"/></param>
    /// <returns>A <see cref="ScheduledItem"/></returns>
    public async Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId)
    {
        var scheduledItem = await apiClient.GetForUserAsync<ScheduledItem>(ApiServiceName, options =>
        {
            options.RelativePath = $"{ScheduledItemBaseUrl}/{scheduledItemId}";
        });
        return scheduledItem;
    }
    
    /// <summary>
    /// Saves a scheduled item
    /// </summary>
    /// <param name="scheduledItem">The <see cref="ScheduledItem"/> to save</param>
    /// <returns>A scheduled item</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await apiClient.PostForUserAsync<ScheduledItem, ScheduledItem>(ApiServiceName, scheduledItem, options =>
        {
            options.RelativePath = ScheduledItemBaseUrl;
        });
        return savedScheduledItem;
    }
    
    /// <summary>
    /// Deletes a scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the scheduled item to delete</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> DeleteScheduledItemAsync(int scheduledItemId)
    {
        var response = await apiClient.CallApiForUserAsync<HttpResponseMessage>(ApiServiceName, options =>
        {
            options.RelativePath = $"{ScheduledItemBaseUrl}/{scheduledItemId}";
            options.HttpMethod = HttpMethod.Delete.Method;
        });

        return response is { StatusCode: HttpStatusCode.NoContent };
    }

    /// <summary>
    /// Returns a list of any scheduled items that have not been sent
    /// </summary>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        try
        {
            var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<ScheduledItem>>(ApiServiceName, options =>
            {
                options.RelativePath = $"{ScheduledItemBaseUrl}/unsent?page={page}&pageSize={pageSize}";
            });
            if (pagedResponse is null) return new PagedResult<ScheduledItem>();
            return new PagedResult<ScheduledItem> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return new PagedResult<ScheduledItem>();
            }

            logger.LogError(exception, "Error getting unsent scheduled items");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error getting unsent scheduled items");
            throw;
        }
    }

    /// <summary>
    /// Returns a list of any scheduled items that have not been sent that should have been sent
    /// </summary>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsToSendAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        try
        {
            var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<ScheduledItem>>(ApiServiceName, options =>
            {
                options.RelativePath = $"{ScheduledItemBaseUrl}/upcoming?page={page}&pageSize={pageSize}";
            });
            if (pagedResponse is null) return new PagedResult<ScheduledItem>();
            return new PagedResult<ScheduledItem> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return new PagedResult<ScheduledItem>();
            }

            logger.LogError(exception, "Error getting scheduled items to send");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error getting scheduled items to send");
            throw;
        }
    }

    /// <summary>
    /// Gets scheduled items for the given calendar month and year
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month</param>
    /// /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt; that are for the month.  If there are no scheduled items, null will be returned</returns>
    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        try
        {
            var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<ScheduledItem>>(ApiServiceName, options =>
            {
                options.RelativePath =
                    $"{ScheduledItemBaseUrl}/calendar/{year}/{month}?page={page}&pageSize={pageSize}";
            });
            if (pagedResponse is null) return new PagedResult<ScheduledItem>();
            return new PagedResult<ScheduledItem> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return new PagedResult<ScheduledItem>();
            }

            logger.LogError(exception, "Error getting scheduled items by calendar month");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error getting scheduled items by calendar month");
            throw;
        }
    }

    /// <summary>
    /// Returns a list of orphaned scheduled items (items whose source records no longer exist)
    /// </summary>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    public async Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        try
        {
            var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<ScheduledItem>>(ApiServiceName, options =>
            {
                options.RelativePath = $"{ScheduledItemBaseUrl}/orphaned/?page={page}&pageSize={pageSize}";
            });
            if (pagedResponse is null) return new PagedResult<ScheduledItem>();
            return new PagedResult<ScheduledItem> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return new PagedResult<ScheduledItem>();
            }
            logger.LogError(exception, "Error getting orphaned scheduled items");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error getting orphaned scheduled items");
            throw;
        }
    }
}
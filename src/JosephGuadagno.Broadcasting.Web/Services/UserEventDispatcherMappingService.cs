using System.Net;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls the event dispatcher mappings API on behalf of the current user.
/// </summary>
public class UserEventDispatcherMappingService(
    IDownstreamApi apiClient,
    ILogger<UserEventDispatcherMappingService> logger) : IUserEventDispatcherMappingService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/Dispatchers/EventDispatcherMappings";

    /// <inheritdoc />
    public async Task<List<UserEventDispatcherMapping>> GetAllAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserEventDispatcherMapping>>(ApiServiceName, options =>
        {
            options.RelativePath = BaseUrl;
        });

        if (response is null)
        {
            logger.LogWarning("GetAllAsync downstream returned null for event dispatcher mappings");
        }

        return response ?? [];
    }

    /// <inheritdoc />
    public async Task<UserEventDispatcherMapping?> GetAsync(int id)
    {
        return await apiClient.GetOptionalForUserAsync<UserEventDispatcherMapping>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });
    }

    /// <inheritdoc />
    public async Task<UserEventDispatcherMapping?> AddAsync(UserEventDispatcherMapping mapping)
    {
        var request = MapRequest(mapping);
        var response = await apiClient.PostForUserAsync<EventDispatcherMappingApiRequest, UserEventDispatcherMapping>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = BaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "API returned null for {Operation} with mappingId {MappingId}, platformId {PlatformId}, and eventType '{EventType}'",
                nameof(AddAsync),
                mapping.Id,
                mapping.SocialMediaPlatformId,
                LogSanitizer.Sanitize(mapping.EventType));
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<UserEventDispatcherMapping?> UpdateAsync(UserEventDispatcherMapping mapping)
    {
        var request = MapRequest(mapping);
        var response = await apiClient.PutForUserAsync<EventDispatcherMappingApiRequest, UserEventDispatcherMapping>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = $"{BaseUrl}/{mapping.Id}";
            });

        if (response is null)
        {
            logger.LogWarning(
                "API returned null for {Operation} with mappingId {MappingId}, platformId {PlatformId}, and eventType '{EventType}'",
                nameof(UpdateAsync),
                mapping.Id,
                mapping.SocialMediaPlatformId,
                LogSanitizer.Sanitize(mapping.EventType));
        }

        return response;
    }

    /// <inheritdoc />
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

        logger.LogWarning(
            "API returned unexpected status for {Operation} with mappingId {MappingId}: {StatusCode}",
            nameof(DeleteAsync),
            id,
            response?.StatusCode);
        return false;
    }

    private static EventDispatcherMappingApiRequest MapRequest(UserEventDispatcherMapping mapping) =>
        new()
        {
            EventType = mapping.EventType,
            SocialMediaPlatformId = mapping.SocialMediaPlatformId,
            IsActive = mapping.IsActive
        };

    private sealed class EventDispatcherMappingApiRequest
    {
        public string EventType { get; set; } = string.Empty;

        public int SocialMediaPlatformId { get; set; }

        public bool IsActive { get; set; }
    }
}

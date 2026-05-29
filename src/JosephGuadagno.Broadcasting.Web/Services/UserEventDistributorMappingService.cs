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
public class UserEventDistributorMappingService(
    IDownstreamApi apiClient,
    ILogger<UserEventDistributorMappingService> logger) : IUserEventDistributorMappingService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/Distributors/EventDistributorMappings";

    /// <inheritdoc />
    public async Task<List<UserEventDistributorMapping>> GetAllAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserEventDistributorMapping>>(ApiServiceName, options =>
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
    public async Task<UserEventDistributorMapping?> GetAsync(int id)
    {
        return await apiClient.GetOptionalForUserAsync<UserEventDistributorMapping>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });
    }

    /// <inheritdoc />
    public async Task<UserEventDistributorMapping?> AddAsync(UserEventDistributorMapping mapping)
    {
        var request = MapRequest(mapping);
        var response = await apiClient.PostForUserAsync<EventDispatcherMappingApiRequest, UserEventDistributorMapping>(
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
    public async Task<UserEventDistributorMapping?> UpdateAsync(UserEventDistributorMapping mapping)
    {
        var request = MapRequest(mapping);
        var response = await apiClient.PutForUserAsync<EventDispatcherMappingApiRequest, UserEventDistributorMapping>(
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

    private static EventDispatcherMappingApiRequest MapRequest(UserEventDistributorMapping mapping) =>
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

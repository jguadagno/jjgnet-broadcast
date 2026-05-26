using System.Net;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls the event publisher mappings API on behalf of the current user.
/// </summary>
public class UserEventPublisherMappingService(
    IDownstreamApi apiClient,
    ILogger<UserEventPublisherMappingService> logger) : IUserEventPublisherMappingService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/Publishers/EventPublisherMappings";

    /// <inheritdoc />
    public async Task<List<UserEventPublisherMapping>> GetAllAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserEventPublisherMapping>>(ApiServiceName, options =>
        {
            options.RelativePath = BaseUrl;
        });

        return response ?? [];
    }

    /// <inheritdoc />
    public async Task<UserEventPublisherMapping?> GetAsync(int id)
    {
        return await apiClient.GetOptionalForUserAsync<UserEventPublisherMapping>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });
    }

    /// <inheritdoc />
    public async Task<UserEventPublisherMapping?> AddAsync(UserEventPublisherMapping mapping)
    {
        var request = MapRequest(mapping);
        var response = await apiClient.PostForUserAsync<EventPublisherMappingApiRequest, UserEventPublisherMapping>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = BaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning("Event publisher mapping create returned no content for platform {PlatformId}", mapping.SocialMediaPlatformId);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<UserEventPublisherMapping?> UpdateAsync(UserEventPublisherMapping mapping)
    {
        var request = MapRequest(mapping);
        var response = await apiClient.PutForUserAsync<EventPublisherMappingApiRequest, UserEventPublisherMapping>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = $"{BaseUrl}/{mapping.Id}";
            });

        if (response is null)
        {
            logger.LogWarning("Event publisher mapping update returned no content for id {Id}", mapping.Id);
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

        logger.LogWarning("Unexpected status {StatusCode} deleting event publisher mapping {Id}", response?.StatusCode, id);
        return false;
    }

    private static EventPublisherMappingApiRequest MapRequest(UserEventPublisherMapping mapping) =>
        new()
        {
            EventType = mapping.EventType,
            SocialMediaPlatformId = mapping.SocialMediaPlatformId,
            IsActive = mapping.IsActive
        };

    private sealed class EventPublisherMappingApiRequest
    {
        public string EventType { get; set; } = string.Empty;

        public int SocialMediaPlatformId { get; set; }

        public bool IsActive { get; set; }
    }
}

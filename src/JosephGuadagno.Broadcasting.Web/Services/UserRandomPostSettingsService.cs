using System.Net;
using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls the random post settings API on behalf of the current user.
/// </summary>
public class UserRandomPostSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserRandomPostSettingsService> logger,
    IMapper mapper) : IUserRandomPostSettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/Publishers/RandomPostSettings";

    /// <inheritdoc />
    public async Task<List<UserRandomPostSettings>> GetAllAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserRandomPostSettings>>(ApiServiceName, options =>
        {
            options.RelativePath = BaseUrl;
        });

        if (response is null)
        {
            logger.LogWarning("GetAllAsync downstream returned null for random post settings");
        }

        return response ?? [];
    }

    /// <inheritdoc />
    public async Task<UserRandomPostSettings?> GetAsync(int id)
    {
        return await apiClient.GetOptionalForUserAsync<UserRandomPostSettings>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });
    }

    /// <inheritdoc />
    public async Task<UserRandomPostSettings?> AddAsync(UserRandomPostSettings settings)
    {
        var request = mapper.Map<RandomPostSettingsApiRequest>(settings);
        var response = await apiClient.PostForUserAsync<RandomPostSettingsApiRequest, UserRandomPostSettings>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = BaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "API returned null for {Operation} with randomPostSettingsId {SettingsId} and platformId {PlatformId}",
                nameof(AddAsync),
                settings.Id,
                settings.SocialMediaPlatformId);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<UserRandomPostSettings?> UpdateAsync(UserRandomPostSettings settings)
    {
        var request = mapper.Map<RandomPostSettingsApiRequest>(settings);
        var response = await apiClient.PutForUserAsync<RandomPostSettingsApiRequest, UserRandomPostSettings>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = $"{BaseUrl}/{settings.Id}";
            });

        if (response is null)
        {
            logger.LogWarning(
                "API returned null for {Operation} with randomPostSettingsId {SettingsId} and platformId {PlatformId}",
                nameof(UpdateAsync),
                settings.Id,
                settings.SocialMediaPlatformId);
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
            "API returned unexpected status for {Operation} with randomPostSettingsId {SettingsId}: {StatusCode}",
            nameof(DeleteAsync),
            id,
            response?.StatusCode);
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> ToggleActiveAsync(int id)
    {
        var item = await GetAsync(id);
        if (item is null) return false;
        item.IsActive = !item.IsActive;
        var updated = await UpdateAsync(item);
        return updated is not null;
    }

}

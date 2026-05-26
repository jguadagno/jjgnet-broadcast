using System.Net;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls the random post settings API on behalf of the current user.
/// </summary>
public class UserRandomPostSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserRandomPostSettingsService> logger) : IUserRandomPostSettingsService
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
        var request = MapRequest(settings);
        var response = await apiClient.PostForUserAsync<RandomPostSettingsApiRequest, UserRandomPostSettings>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = BaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning("Random post settings create returned no content for platform {PlatformId}", settings.SocialMediaPlatformId);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<UserRandomPostSettings?> UpdateAsync(UserRandomPostSettings settings)
    {
        var request = MapRequest(settings);
        var response = await apiClient.PutForUserAsync<RandomPostSettingsApiRequest, UserRandomPostSettings>(
            ApiServiceName,
            request,
            options =>
            {
                options.RelativePath = $"{BaseUrl}/{settings.Id}";
            });

        if (response is null)
        {
            logger.LogWarning("Random post settings update returned no content for id {Id}", settings.Id);
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

        logger.LogWarning("Unexpected status {StatusCode} deleting random post settings {Id}", response?.StatusCode, id);
        return false;
    }

    private static RandomPostSettingsApiRequest MapRequest(UserRandomPostSettings settings) =>
        new()
        {
            SocialMediaPlatformId = settings.SocialMediaPlatformId,
            CronExpression = settings.CronExpression,
            CutoffDate = settings.CutoffDate,
            ExcludedCategories = settings.ExcludedCategories,
            IsActive = settings.IsActive
        };

    private sealed class RandomPostSettingsApiRequest
    {
        public int SocialMediaPlatformId { get; set; }

        public string CronExpression { get; set; } = string.Empty;

        public DateTimeOffset? CutoffDate { get; set; }

        public List<string> ExcludedCategories { get; set; } = [];

        public bool IsActive { get; set; }
    }
}

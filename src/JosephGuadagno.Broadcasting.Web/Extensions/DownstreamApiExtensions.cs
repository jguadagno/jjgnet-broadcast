using System.Net;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IDownstreamApi"/> that add resilient, null-safe helpers.
/// </summary>
internal static class DownstreamApiExtensions
{
    /// <summary>
    /// Calls a downstream API endpoint for the current user and returns the deserialized response,
    /// or <see langword="null"/> when the server responds with 404 Not Found (resource not found)
    /// or 204 No Content (resource not yet configured). All other non-success status codes are
    /// re-thrown as <see cref="HttpRequestException"/>.
    /// </summary>
    public static async Task<TResult?> GetOptionalForUserAsync<TResult>(
        this IDownstreamApi downstreamApi,
        string serviceName,
        Action<DownstreamApiOptions> optionsOverride,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        try
        {
            return await downstreamApi.GetForUserAsync<TResult>(
                serviceName,
                optionsOverride,
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}

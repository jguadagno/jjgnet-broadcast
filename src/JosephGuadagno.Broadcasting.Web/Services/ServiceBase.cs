using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Web.Exceptions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// The base class for calling Apis
/// </summary>
public abstract class ServiceBase
{
    internal HttpClient HttpClient { get; init; }
    internal string ApiScopeUrl { get; init; }
    internal ITokenAcquisition TokenAcquisition { get; init; }
    
    internal async Task<T?> ExecuteGetAsync<T>(string url)
    {
        var response = await HttpClient.GetAsync(url);
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                return default;
            case HttpStatusCode.OK:
                break;
            default:
                throw new HttpRequestException(
                    $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");    
        }

        // Parse the Results
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var results = JsonSerializer.Deserialize<T>(content, options);

        return results;
    }
    
    internal async Task SetRequestHeader(string scope, string mediaType = "application/json")
    {
        var fullScopeName = ApiScopeUrl + scope;
        var accessToken = await TokenAcquisition.GetAccessTokenForUserAsync(new[] {fullScopeName});
        
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
    }

}
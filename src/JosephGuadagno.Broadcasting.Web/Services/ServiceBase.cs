using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// The base class for calling Apis
/// </summary>
public abstract class ServiceBase
{
    /// <summary>
    /// Base constructor for the service
    /// </summary>
    /// <param name="httpClient">The HTTP Client to use</param>
    /// <param name="tokenAcquisition">The Token Acquisition Service</param>
    /// <param name="apiScopeUrl">The API scope url base</param>
    protected ServiceBase(HttpClient httpClient, ITokenAcquisition tokenAcquisition, string apiScopeUrl )
    {
        HttpClient = httpClient;
        ApiScopeUrl = apiScopeUrl;
        TokenAcquisition = tokenAcquisition;
    }

    internal HttpClient HttpClient { get; init; }
    private string ApiScopeUrl { get; init; }
    private ITokenAcquisition TokenAcquisition { get; init; }
    
    /// <summary>
    /// Executes a Http Get request and returns the response
    /// </summary>
    /// <param name="url">The Url to request</param>
    /// <typeparam name="T">The object to serialize the result to</typeparam>
    /// <returns>A deserialized response.</returns>
    /// <exception cref="HttpRequestException"></exception>
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

    /// <summary>
    /// Sets the authorization header for the request
    /// </summary>
    /// <param name="scope">The scope to add to the header</param>
    /// <param name="mediaType">The media type of the request.  If one is not specified, "application/json" is used.</param>
    internal async Task SetRequestHeader(string scope, string mediaType = "application/json")
    {
        var fullScopeName = ApiScopeUrl + scope;
        var accessToken = await TokenAcquisition.GetAccessTokenForUserAsync(new[] {fullScopeName});
        
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
    }
}
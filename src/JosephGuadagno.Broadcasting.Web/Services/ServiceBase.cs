using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Web.Exceptions;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// The base class for calling Apis
/// </summary>
public abstract class ServiceBase
{
    internal HttpClient HttpClient { get; init; }
    internal string ApiScopeUrl { get; init; }
    internal string RedirectUrl { get; init; }
    internal string AdminConsentUrl { get; init; }
    internal ITokenAcquisition TokenAcquisition { get; init; }
    
    internal async Task<T?> ExecuteGetAsync<T>(string url)
    {
        var response = await HttpClient.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            HandleChallengeFromWebApi(response);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
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

        try
        {
            string fullScopeName = ApiScopeUrl + scope;
            string accessToken = await TokenAcquisition.GetAccessTokenForUserAsync(new[] {fullScopeName});
            
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        }
        catch (MicrosoftIdentityWebChallengeUserException e)
        {
            Console.WriteLine(e);
            throw;
        }

        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
    }
    
    internal void HandleChallengeFromWebApi(HttpResponseMessage response)
    {
        //proposedAction="consent"
        List<string> result = new List<string>();
        AuthenticationHeaderValue bearer = response.Headers.WwwAuthenticate.First(v => v.Scheme == "Bearer");
        IEnumerable<string> parameters = bearer.Parameter.Split(',').Select(v => v.Trim()).ToList();
        string proposedAction = GetParameter(parameters, "proposedAction");

        if (proposedAction == "consent")
        {
            string consentUri = GetParameter(parameters, "consentUri");

            var uri = new Uri(consentUri);

            var queryString = System.Web.HttpUtility.ParseQueryString(uri.Query);
            queryString.Set("redirect_uri", AdminConsentUrl);
            queryString.Add("prompt", "consent");
            queryString.Add("state", RedirectUrl);

            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Query = queryString.ToString();
            var updateConsentUri = uriBuilder.Uri.ToString();
            result.Add("consentUri");
            result.Add(updateConsentUri);

            throw new WebApiMsalUiRequiredException(updateConsentUri);
        }
    }
    private static string? GetParameter(IEnumerable<string> parameters, string parameterName)
    {
        var offset = parameterName.Length + 1;
        return parameters.FirstOrDefault(p => p.StartsWith($"{parameterName}="))?.Substring(offset)?.Trim('"');
    }
}
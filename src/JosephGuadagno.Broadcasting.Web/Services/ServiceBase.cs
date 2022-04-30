using System.Net;
using System.Text.Json;

namespace JosephGuadagno.Broadcasting.Web.Services;

public abstract class ServiceBase
{
    internal HttpClient HttpClient { get; init; }
    
    internal async Task<T?> ExecuteGetAsync<T>(string url)
    {
        var response = await HttpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
            
        // Parse the Results
        var content = await response.Content.ReadAsStringAsync();
                
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var results = JsonSerializer.Deserialize<T>(content, options);

        return results;
    }
}
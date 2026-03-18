using System.Net;
using System.Text;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the MessageTemplates API
/// </summary>
public class MessageTemplateService : ServiceBase, IMessageTemplateService
{
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes the service
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="tokenAcquisition">The token acquisition client</param>
    /// <param name="settings">Application settings</param>
    public MessageTemplateService(HttpClient httpClient, ITokenAcquisition tokenAcquisition, ISettings settings)
        : base(httpClient, tokenAcquisition, settings.ApiScopeUrl)
    {
        _baseUrl = settings.ApiRootUrl + "/messagetemplates";
    }

    /// <summary>
    /// Gets all message templates
    /// </summary>
    public async Task<List<MessageTemplate>?> GetAllAsync()
    {
        await SetRequestHeader(Domain.Scopes.MessageTemplates.All);
        return await ExecuteGetAsync<List<MessageTemplate>>(_baseUrl);
    }

    /// <summary>
    /// Gets a message template by platform and message type
    /// </summary>
    public async Task<MessageTemplate?> GetAsync(string platform, string messageType)
    {
        await SetRequestHeader(Domain.Scopes.MessageTemplates.All);
        return await ExecuteGetAsync<MessageTemplate>($"{_baseUrl}/{platform}/{messageType}");
    }

    /// <summary>
    /// Updates a message template
    /// </summary>
    public async Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate)
    {
        await SetRequestHeader(Domain.Scopes.MessageTemplates.All);
        var json = JsonSerializer.Serialize(messageTemplate);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await HttpClient.PutAsync(
            $"{_baseUrl}/{messageTemplate.Platform}/{messageTemplate.MessageType}", content);

        if (response.StatusCode != HttpStatusCode.OK) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MessageTemplate>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

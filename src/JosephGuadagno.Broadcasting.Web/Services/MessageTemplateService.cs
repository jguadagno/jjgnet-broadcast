using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;

using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the MessageTemplates API
/// </summary>
public class MessageTemplateService(IDownstreamApi apiClient) : IMessageTemplateService

{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string MessageTemplateBaseUrl = "/messagetemplates";

    /// <summary>
    /// Gets all message templates
    /// </summary>
    /// <param name="page">The page number to get</param>
    /// <param name="pageSize">The number of items to return per page</param>
    public async Task<PagedResult<MessageTemplate>?> GetAllAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<MessageTemplate>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{MessageTemplateBaseUrl}?page={page}&pageSize={pageSize}";
        });
        if (pagedResponse is null) return null;
        return new PagedResult<MessageTemplate> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
    }

    /// <summary>
    /// Gets a message template by platform and message type
    /// </summary>
    public async Task<MessageTemplate?> GetAsync(string platform, string messageType)
    {
        var messageTemplate = await apiClient.GetForUserAsync<MessageTemplate>(ApiServiceName, options =>
        {
            options.RelativePath = $"{MessageTemplateBaseUrl}/{platform}/{messageType}";
        });
        return messageTemplate;
    }

    /// <summary>
    /// Updates a message template
    /// </summary>
    public async Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate)
    {
        var savedMessageTemplate = await apiClient.PutForUserAsync<MessageTemplate, MessageTemplate>(ApiServiceName, messageTemplate, options =>
        {
            options.RelativePath = $"{MessageTemplateBaseUrl}/{messageTemplate.Platform}/{messageTemplate.MessageType}";
        });

        return savedMessageTemplate;
    }
}
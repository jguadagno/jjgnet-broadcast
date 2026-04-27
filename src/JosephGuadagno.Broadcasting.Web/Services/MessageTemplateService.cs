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
    /// <param name="sortBy">The field to sort by</param>
    /// <param name="sortDescending">Whether to sort descending</param>
    /// <param name="filter">An optional filter string</param>
    public async Task<PagedResult<MessageTemplate>?> GetAllAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<MessageTemplate>>(ApiServiceName, options =>
        {
            var url = $"{MessageTemplateBaseUrl}?page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDescending={sortDescending}";
            if (!string.IsNullOrEmpty(filter))
            {
                url += $"&filter={filter}";
            }
            options.RelativePath = url;
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
    public async Task<MessageTemplate?> UpdateAsync(string platform, MessageTemplate messageTemplate)
    {
        var savedMessageTemplate = await apiClient.PutForUserAsync<MessageTemplate, MessageTemplate>(ApiServiceName, messageTemplate, options =>
        {
            options.RelativePath = $"{MessageTemplateBaseUrl}/{platform}/{messageTemplate.MessageType}";
        });

        return savedMessageTemplate;
    }
}
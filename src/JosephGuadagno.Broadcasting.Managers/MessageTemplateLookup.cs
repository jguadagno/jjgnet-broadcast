using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Resolves user-scoped message templates via a two-step lookup:
/// platform name → platform ID, then platform ID + message type → template.
/// </summary>
/// <remarks>
/// Centralizes the lookup logic previously inlined in each publisher manager's
/// <c>ComposeMessageAsync()</c>. Phase 2 of the publisher architecture refactor (#980).
/// </remarks>
public class MessageTemplateLookup(
    ISocialMediaPlatformManager platformManager,
    IMessageTemplateDataStore messageTemplateDataStore,
    ILogger<MessageTemplateLookup> logger) : IMessageTemplateLookup
{
    /// <inheritdoc />
    public async Task<MessageTemplate?> GetAsync(
        string platformName,
        string messageType,
        string ownerEntraOid,
        CancellationToken cancellationToken = default)
    {
        var platform = await platformManager.GetByNameAsync(platformName, cancellationToken);
        if (platform is null)
        {
            logger.LogWarning(
                "MessageTemplateLookup: platform '{PlatformName}' not found — skipping template lookup.",
                platformName);
            return null;
        }

        var template = await messageTemplateDataStore.GetAsync(platform.Id, messageType, ownerEntraOid, cancellationToken);
        if (template is null)
        {
            logger.LogWarning(
                "MessageTemplateLookup: no template found for platform '{PlatformName}' (id={PlatformId}), messageType='{MessageType}', owner='{OwnerEntraOid}'.",
                platformName,
                platform.Id,
                messageType,
                ownerEntraOid);
            return null;
        }

        return template;
    }
}

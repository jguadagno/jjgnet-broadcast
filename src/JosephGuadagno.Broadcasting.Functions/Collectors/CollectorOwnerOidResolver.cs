using JosephGuadagno.Broadcasting.Domain.Interfaces;

using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors;

internal static class CollectorOwnerOidResolver
{
    internal static async Task<string?> ResolveAsync(
        ISyndicationFeedSourceManager syndicationFeedSourceManager,
        ILogger logger,
        string functionName,
        CancellationToken cancellationToken = default)
    {
        var ownerOid = await syndicationFeedSourceManager.GetCollectorOwnerOidAsync(cancellationToken);
        return LogMissingOwner(ownerOid, logger, functionName, "syndication feed source");
    }

    internal static async Task<string?> ResolveAsync(
        IYouTubeSourceManager youTubeSourceManager,
        ILogger logger,
        string functionName,
        CancellationToken cancellationToken = default)
    {
        var ownerOid = await youTubeSourceManager.GetCollectorOwnerOidAsync(cancellationToken);
        return LogMissingOwner(ownerOid, logger, functionName, "YouTube source");
    }

    private static string? LogMissingOwner(string? ownerOid, ILogger logger, string functionName, string sourceType)
    {
        if (!string.IsNullOrWhiteSpace(ownerOid))
        {
            return ownerOid;
        }

        logger.LogError(
            "{FunctionName} could not resolve a collector owner OID from existing {SourceType} records.",
            functionName,
            sourceType);

        return null;
    }
}

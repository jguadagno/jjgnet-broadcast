using JosephGuadagno.Broadcasting.Functions.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.Models;

public class Settings : ISettings
{
    /// <summary>
    /// The shortened domain to use for the site.
    /// </summary>
    public required string ShortenedDomainToUse { get; set; }

    /// <summary>
    /// The Entra Object ID to use when persisting collected items.
    /// </summary>
    /// <remarks>
    /// TODO: #731 — This is a temporary system-level scaffold. Replace with per-collector owner OID
    /// loaded from the collector record so each collector carries its own owner identity.
    /// </remarks>
    public required string OwnerEntraOid { get; set; }
}

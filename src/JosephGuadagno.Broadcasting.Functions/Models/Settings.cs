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
    public required string OwnerEntraOid { get; set; }
}

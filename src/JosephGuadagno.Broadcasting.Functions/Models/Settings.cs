using JosephGuadagno.Broadcasting.Functions.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.Models;

public class Settings : ISettings
{
    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public required string LoggingStorageAccount { get; set; }

    /// <summary>
    /// The shortened domain to use for the site.
    /// </summary>
    public required string ShortenedDomainToUse { get; set; }
}
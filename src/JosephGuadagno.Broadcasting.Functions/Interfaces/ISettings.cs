namespace JosephGuadagno.Broadcasting.Functions.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public string LoggingStorageAccount { get; set; }

    /// <summary>
    /// The shortened domain to use for the site.
    /// </summary>
    public string ShortenedDomainToUse { get; set; }
}
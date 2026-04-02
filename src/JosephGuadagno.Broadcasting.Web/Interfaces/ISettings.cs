using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISettings : IEmailSettings
{
    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public string LoggingStorageAccount { get; set; }

    /// <summary>
    /// The root URL for serving static content in the Web application.
    /// </summary>
    public string StaticContentRootUrl { get; set; }
}
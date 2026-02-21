namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The root Uri for the JosephGuadagno.NET Broadcasting Api
    /// </summary>
    public string ApiRootUrl { get; set; }

    /// <summary>
    /// The Uri to get a list of scopes for Api permissions
    /// </summary>
    public string ApiScopeUrl { get; set; }

    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public string LoggingStorageAccount { get; set; }

    /// <summary>
    /// The root URL for serving static content in the Web application.
    /// </summary>
    public string StaticContentRootUrl { get; set; }

}
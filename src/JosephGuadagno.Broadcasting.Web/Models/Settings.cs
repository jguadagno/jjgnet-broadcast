using JosephGuadagno.Broadcasting.Web.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Models;

public class Settings : ISettings
{
    /// <summary>
    /// The key for Application Insights
    /// </summary>
    public string AppInsightsKey { get; set; }

    /// <summary>
    /// The root Uri for the JosephGuadagno.NET Broadcasting Api
    /// </summary>
    public string ApiRootUrl { get; set; }

    /// <summary>
    /// The Uri to get a list of scopes for Api permissions
    /// </summary>
    public string ApiScopeUrl { get; set; }

    /// <summary>
    /// The Azure Storage account to use
    /// </summary>
    public string StorageAccount { get; set; }

    /// <summary>
    /// The database connection string
    /// </summary>
    public string JJGNetDatabaseSqlServer { get; set; }
}
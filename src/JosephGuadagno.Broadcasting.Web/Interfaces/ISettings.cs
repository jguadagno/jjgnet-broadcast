namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The key for Application Insights
    /// </summary>
    public string AppInsightsKey { get; set; }
    
    /// <summary>
    /// The root Uri for the JosephGuadagno.NET broadcasting Api
    /// </summary>
    public string ApiRootUri { get; set; }
    
    /// <summary>
    /// The Uri to get a list of scopes for Api permissions
    /// </summary>
    public string ApiScopeUri { get; set; }
    
    /// <summary>
    /// The Azure Storage account to use
    /// </summary>
    public string StorageAccount { get; set; }
    
    public string JJGNetDatabaseSqlServer { get; set; }
    
    public string AdminConsentRedirectApi { get; set; }
    
    public string RedirectUri { get; set; }
}

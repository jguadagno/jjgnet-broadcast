namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISettings
{
    
    /// <summary>
    /// The root Uri for the JosephGuadagno.NET broadcasting Api
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
    
    /// <summary>
    /// The Azure Key Vault to use
    /// </summary>
    public string AzureKeyVaultUrl { get; set; }
    
    /// <summary>
    /// The static content root url
    /// </summary>
    /// <remarks>This will return where to get images, scripts, etc. from. Could be local or CDN.</remarks>
    public string StaticContentRootUrl { get; set; }
}

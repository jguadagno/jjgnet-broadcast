using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// The application settings for the Web application
/// </summary>
public class Settings : ISettings
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
    /// The Azure Storage account to use
    /// </summary>
    public string StorageAccount { get; set; }

    /// <inheritdoc />
    public string StaticContentRootUrl { get; set; }

    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public AutoMapperSettings AutoMapper { get; set; }
}
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

public class Settings: ISettings
{
    /// <summary>
    /// The URL of the API scope used for authentication and authorization purposes.
    /// </summary>
    public required string ApiScopeUrl { get; set; }

    /// <summary>
    /// The Client Id for the Azure AD Scalar App.
    /// </summary>
    public required string ScalarClientId { get; set; }

    /// <summary>
    /// The storage account connection string.
    /// </summary>
    /// <remarks>This is used for the Azure Table Storage.</remarks>
    public required string StorageAccount { get; set; }
}
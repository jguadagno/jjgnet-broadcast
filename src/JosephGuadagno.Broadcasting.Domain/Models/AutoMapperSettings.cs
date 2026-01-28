using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Settings for AutoMapper.
/// </summary>
public class AutoMapperSettings: IAutoMapperSettings
{
    /// <summary>
    /// The license key for AutoMapper.
    /// </summary>
    public string LicenseKey { get; init; } = string.Empty;
}
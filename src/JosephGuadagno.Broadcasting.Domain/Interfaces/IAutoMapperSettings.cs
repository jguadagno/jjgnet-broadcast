namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// The AutoMapper settings.
/// </summary>
public interface IAutoMapperSettings
{
    /// <summary>
    /// The license key for AutoMapper.
    /// </summary>
    public string LicenseKey { get; init; }
}
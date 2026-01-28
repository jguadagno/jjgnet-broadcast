using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Api.Interfaces;

public interface ISettings
{
    public string StorageAccount { get; set; }
    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public AutoMapperSettings AutoMapper { get; init; }
}
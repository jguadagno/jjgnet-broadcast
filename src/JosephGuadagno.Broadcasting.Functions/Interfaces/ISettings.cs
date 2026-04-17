namespace JosephGuadagno.Broadcasting.Functions.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The shortened domain to use for the site.
    /// </summary>
    public string ShortenedDomainToUse { get; set; }

    /// <summary>
    /// The Entra Object ID to use when persisting collected items.
    /// </summary>
    public string OwnerEntraOid { get; set; }
}

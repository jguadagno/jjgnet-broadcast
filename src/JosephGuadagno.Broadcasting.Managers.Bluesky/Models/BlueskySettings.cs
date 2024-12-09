using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Models;

public class BlueskySettings:IBlueskySettings
{
    /// <summary>
    /// The user name for the Bluesky account
    /// </summary>
    public string? BlueskyUserName { get; set; }
    /// <summary>
    /// The password for the Bluesky account
    /// </summary>
    public string? BlueskyPassword { get; set; }
}
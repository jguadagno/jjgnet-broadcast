namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;

public interface IBlueskySettings
{
    /// <summary>
    /// The user name for the Bluesky account
    /// </summary>
    public string BlueskyUserName {get; set;}
    /// <summary>
    /// The password for the Bluesky account
    /// </summary>
    public string BlueskyPassword {get; set;}
}
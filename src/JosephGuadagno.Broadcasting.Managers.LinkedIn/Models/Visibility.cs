using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class Visibility
{
    private const string VisibilityAnyone = "PUBLIC";
    private const string VisibilityConnectionsOnly = "CONNECTIONS";
    
    [JsonPropertyName("com.linkedin.ugc.MemberNetworkVisibility")]
    public string com_linkedin_ugc_MemberNetworkVisibility => VisibilityEnum switch
    {
        VisibilityEnum.Anyone => VisibilityAnyone,
        VisibilityEnum.ConnectionsOnly => VisibilityConnectionsOnly,
        _ => VisibilityAnyone
    };
    
    /// <summary>
    /// Defines any visibility restrictions for the share
    /// </summary>
    [JsonIgnore]
    public VisibilityEnum VisibilityEnum { get; set; }
}
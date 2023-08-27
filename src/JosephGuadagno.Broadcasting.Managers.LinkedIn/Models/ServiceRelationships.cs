using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class ServiceRelationships
{
    [JsonPropertyName("relationshipType")]
    public string RelationshipType => "OWNER";
    
    [JsonPropertyName("identifier")]
    public string Identifier => "urn:li:userGeneratedContent";
}
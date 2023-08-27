using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class LinkedInUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("profilePicture")]
    public ProfilePicture ProfilePicture { get; set; }
    
    [JsonPropertyName("vanityName")]
    public string VanityName { get; set; }
    
    [JsonPropertyName("localizedFirstName")]
    public string FirstName { get; set; }
    
    [JsonPropertyName("localizedLastName")]
    public string LastName { get; set; }
    
    [JsonPropertyName("localizedHeadline")]
    public string Headline { get; set; }
    
    [JsonPropertyName("firstName")]
    public LocalizedInformation LocalizedFirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public LocalizedInformation LocalizedLastName { get; set; }
    
    [JsonPropertyName("headline")]
    public LocalizedInformation LocalizedHeadline { get; set; }
    

}
using System;
using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Models;

public class Presentation
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; }
    [JsonPropertyName("startDate")] public DateTime? PresentationStartDateTime { get; set; }
    [JsonPropertyName("endDate")] public DateTime? PresentationEndDateTime { get; set; }
    [JsonPropertyName("room")] public string Room { get; set; }
    [JsonPropertyName("comments")] public string Comments { get; set; }
    [JsonPropertyName("isCanceled")] public bool IsCanceled { get; set; }
    [JsonPropertyName("isWorkshop")] public bool IsWorkshop { get; set; }
    [JsonPropertyName("isKeynote")] public bool IsKeynote { get; set; }
    [JsonPropertyName("isPanel")] public bool IsPanel { get; set; }
}
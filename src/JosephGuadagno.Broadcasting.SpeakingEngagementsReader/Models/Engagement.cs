using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Models;

public class Engagement
{
    [JsonPropertyName("eventName")] public string EventName { get; set; }
    [JsonPropertyName("eventUrl")] public string EventUrl { get; set; }
    [JsonPropertyName("location")] public string Location { get; set; }
    [JsonPropertyName("eventStart")] public DateTime EventStart { get; set; }
    [JsonPropertyName("eventEnd")] public DateTime EventEnd { get; set; }
    [JsonPropertyName("comments")] public string Comments { get; set; }
    [JsonPropertyName("isCanceled")] public bool IsCanceled { get; set; }
    [JsonPropertyName("isCurrent")] public bool IsCurrent { get; set; }
    [JsonPropertyName("presentation")] public List<Presentation> Presentations { get; set; } = [];
    [JsonPropertyName("timezone")] public string Timezone { get; set; }
    [JsonPropertyName("inPerson")] public string InPerson { get; set; }
    [JsonPropertyName("createdOrUpdatedOn")] public DateTime CreatedOrUpdatedOn { get; set; }
}
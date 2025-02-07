using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prolizy.API;

[Serializable]
public class CourseDescription
{
    [JsonPropertyName("professor")] public List<string> Professors { get; set; } = [];
    [JsonPropertyName("room")] public List<string> Rooms { get; set; } = [];
    [JsonPropertyName("subject")] public List<string> Subjects { get; set; } = [];
    [JsonPropertyName("group")] public List<string> Groups { get; set; } = [];
    [JsonPropertyName("event_type")] public List<string> EventTypes { get; set; } = [];
    [JsonPropertyName("note")] public List<string> Notes { get; set; } = [];
    [JsonPropertyName("hour")] public List<string> Hours { get; set; } = [];
    
    public string EventType => EventTypes.Count > 0 ? EventTypes[0] : "None";
    
} 
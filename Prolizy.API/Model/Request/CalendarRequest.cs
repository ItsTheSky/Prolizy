using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prolizy.API;

/// <summary>
/// A request to the calendar endpoint.
/// </summary>
[Serializable]
public class CalendarRequest
{
    
    [JsonPropertyName("colourScheme")] public string ColourScheme { get; set; } = "3";
    [JsonPropertyName("start")] public DateOnly StartDate { get; set; }
    [JsonPropertyName("end")] public DateOnly EndDate { get; set; }
    // todo: find out what this is
    [JsonPropertyName("resType")] public string ResourceType { get; set; } = "103";
    
    [JsonPropertyName("calView")] 
    public CalendarViewType ViewType { get; set; } = CalendarViewType.AgendaWeek;
    
    // todo: support multiple federation IDs
    [JsonPropertyName("federationIds[]")] public string FederationId { get; set; } = "INF1-B";

    public enum CalendarViewType
    {
        AgendaDay,
        AgendaWeek,
        ListWeek
    }

}
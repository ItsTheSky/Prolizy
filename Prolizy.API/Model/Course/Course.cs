using System.Drawing;
using System.Text.Json.Serialization;

namespace Prolizy.API;

[Serializable]
public class Course
{

    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("start")] public DateTime Start { get; set; }
    [JsonPropertyName("end")] public DateTime End { get; set; }
    [JsonPropertyName("allDay")] public bool AllDay { get; set; }
    
    [JsonPropertyName("description")] public string RawDescription { get; set; }
    
    [JsonPropertyName("backgroundColor")] public string BackgroundColor { get; set; }
    [JsonPropertyName("textColor")] public string TextColor { get; set; }
    [JsonPropertyName("department")] public string Department { get; set; }
    [JsonPropertyName("faculty")] public string Faculty { get; set; }
    [JsonPropertyName("eventCategory")] public string CourseType { get; set; }
    [JsonPropertyName("sites")] public List<string> Sites { get; set; }
    [JsonPropertyName("modules")] public List<string> Modules { get; set; }
    [JsonPropertyName("registerStatus")] public int RegisterStatus { get; set; }
    [JsonPropertyName("studentMark")] public int StudentMark { get; set; }
    
    [JsonPropertyName("custom1")] public string? Custom1 { get; set; }
    [JsonPropertyName("custom2")] public string? Custom2 { get; set; }
    [JsonPropertyName("custom3")] public string? Custom3 { get; set; }
}
using System.Text.Json.Serialization;

namespace Prolizy.API.Model.Sacoche;

[Serializable]
public class SkillNote
{
    
    [JsonPropertyName("sigle")] public string Sigle { get; set; }
    [JsonPropertyName("image")] public string Image { get; set; }
    [JsonPropertyName("texte")] public string Tooltip { get; set; }
    
}
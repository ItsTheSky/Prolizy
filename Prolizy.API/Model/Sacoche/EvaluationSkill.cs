using System.Text.Json.Serialization;

namespace Prolizy.API.Model.Sacoche;

[Serializable]
public class EvaluationSkill
{
    
    [JsonPropertyName("note")] public string Note { get; set; }
    [JsonPropertyName("reference")] public string Reference { get; set; }
    [JsonPropertyName("nom")] public string Name { get; set; }
    [JsonPropertyName("information")] public string Information { get; set; }
    [JsonPropertyName("lien")] public string Link { get; set; }
    
}
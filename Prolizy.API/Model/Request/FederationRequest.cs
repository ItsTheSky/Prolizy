using System.Text.Json.Serialization;

namespace Prolizy.API;

[Serializable]
public class FederationRequest
{
    
    [JsonPropertyName("searchTerm")] public string SearchTerm { get; set; }
    [JsonPropertyName("pageSize")] public int PageSize { get; set; } = 10;
    [JsonPropertyName("pageNumber")] public int Page { get; set; } = 1;
    [JsonPropertyName("resType")] public string ResourceType { get; set; } = "103";
    [JsonPropertyName("myResources")] public bool MyResources { get; set; } = false;
    
}
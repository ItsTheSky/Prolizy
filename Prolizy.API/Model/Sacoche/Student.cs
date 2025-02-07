using System.Text.Json.Serialization;

namespace Prolizy.API;

[Serializable]
public class Student
{
        
    [JsonPropertyName("nom")] public string LastName { get; set; }
    [JsonPropertyName("prenom")] public string FirstName { get; set; }
    [JsonPropertyName("profil")] public string Profile { get; set; }
    [JsonPropertyName("structure")] public string Structure { get; set; }
    
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prolizy.API;

[Serializable]
public class EvaluationReference
{
    
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("date"), JsonConverter(typeof(DateOnlyConverter))] public DateOnly Date { get; set; }
    [JsonPropertyName("professeur")] public string Teacher { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("rempli")] public string Done { get; set; }
    [JsonPropertyName("enonce")] public string Input { get; set; }
    [JsonPropertyName("corrige")] public string Correction { get; set; }
    
    public class DateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // fomat is 'JJ/MM/AAAA'
            var date = reader.GetString();
            var split = date.Split('/');
            return new DateOnly(int.Parse(split[2]), int.Parse(split[1]), int.Parse(split[0]));
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
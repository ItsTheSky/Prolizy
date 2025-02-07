using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prolizy.API;

[Serializable]
public static class JsonUtils
{

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new JsonColorConverter()
        }
    };
    
    public static string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, Options);
    }
    
    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }
    
    public static Dictionary<string, string> ToDictionary(object obj)
    {
        var json = Serialize(obj);
        
        var parameters = new Dictionary<string, string>();
        foreach (var property in JsonSerializer.Deserialize<JsonElement>(json, Options).EnumerateObject())
            parameters.Add(property.Name, property.Value.ToString());
        return parameters;
    }

    #region Custom Converters

    private class JsonColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var hex = reader.GetString();
            return hex == null ? Color.Empty : ColorTranslator.FromHtml(hex);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(ColorTranslator.ToHtml(value));
        }
    }

    #endregion
}
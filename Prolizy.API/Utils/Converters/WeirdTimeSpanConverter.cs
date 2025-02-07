using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prolizy.API.Converters;

/// <summary>
/// Converter for <see cref="TimeSpan"/>.
/// It takes a double/float value (e.g. '8.75') and converts it to a <see cref="TimeSpan"/> (e.g. '8:45').
/// </summary>
public class WeirdTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDouble();
        var hours = (int) value;
        var minutes = (int) ((value - hours) * 60);
        
        return new TimeSpan(hours, minutes, 0);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.TotalHours);
    }
}
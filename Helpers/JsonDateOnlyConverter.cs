using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasketballOlympics.Helpers;

public class JsonDateOnlyConverter : JsonConverter<DateOnly>
{
    private const string _dateFormat = "dd/MM/yy";

    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return DateOnly.ParseExact(reader.GetString()!, _dateFormat);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

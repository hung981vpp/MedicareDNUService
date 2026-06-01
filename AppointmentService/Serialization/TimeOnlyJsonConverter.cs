using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentService.Serialization;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string TimeFormat = "HH:mm";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (TimeOnly.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
        {
            return time;
        }

        throw new JsonException($"Time value must use {TimeFormat} format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(TimeFormat, CultureInfo.InvariantCulture));
    }
}

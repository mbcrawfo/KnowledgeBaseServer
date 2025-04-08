using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeBaseServer;

public sealed class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    /// <inheritdoc />
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTimeOffset));
        return DateTimeOffset.Parse(reader.GetString() ?? "", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var stringValue =
            value.Offset == TimeSpan.Zero
                ? value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)
                : value.ToString("o", CultureInfo.InvariantCulture);

        writer.WriteStringValue(stringValue);
    }
}

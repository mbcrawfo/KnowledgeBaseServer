using System.Text.Json;

namespace KnowledgeBaseServer;

public static class AppJsonSerializer
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new IsoDateTimeOffsetConverter() },
    };

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions)
            ?? throw new JsonException("Failed to deserialize JSON.");
    }

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _jsonSerializerOptions);
    }
}

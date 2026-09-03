using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoD2026_Snowflake
{
    public static class JsonExtensions
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public static string ToJson<T>(this T value) => JsonSerializer.Serialize(value, Options);

        public static T FromJsonStringAs<T>(this string json) =>
            JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidOperationException($"JSON konnte nicht als {typeof(T).Name} gelesen werden.");
    }
}

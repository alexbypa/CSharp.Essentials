using System.Text.Json;

namespace CSharpEssentials.SerializerHelper;
public static class jsonHelper {
    public static T Deserialize<T>(this string jsonContent, Action<string> ActionOnError) {
        T response = default(T);
        try {
            response = JsonSerializer.Deserialize<T>(jsonContent);
        } catch {
            ActionOnError(jsonContent);
        }
        return response;
    }
    public static int GetInt(this JsonElement element, string path, int valueOnError) {
        try {
            return (int)GetPropertyValue(element, path);
        } catch {
            return valueOnError;
        }
    }
    public static long GetLong(this JsonElement element, string path, long valueOnError) {
        try {
            var res = GetPropertyValue(element, path);
            Console.Write("asd");
            return long.Parse(res.ToString());
            //return (long)GetPropertyValue(element, path);
        } catch {
            return valueOnError;
        }
    }
    public static string GetString(this JsonElement element, string path, string valueOnError) {
        try {
            return (string)GetPropertyValue(element, path);
        } catch {
            return valueOnError;
        }
    }
    private static object GetPropertyValue(JsonElement element, string path) {
        string[] properties = path.Split('.');

        if (properties.Length == 1) {
            if (element.TryGetProperty(properties[0], out JsonElement property)) {
                return property.ValueKind switch {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetInt32(), // Puoi adattare il tipo numerico
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => property.EnumerateArray(), // Restituisce un IEnumerable
                    JsonValueKind.Object => property, // Restituisce un JsonElement
                    _ => throw new InvalidOperationException($"Tipo di dato non supportato: {property.ValueKind}")
                };
            } else {
                return null; // Proprietà non trovata
            }
        }
        if (element.TryGetProperty(properties[0], out JsonElement nextElement)) {
            return GetPropertyValue(nextElement, string.Join(".", properties.Skip(1)));
        }
        return null;
    }
}

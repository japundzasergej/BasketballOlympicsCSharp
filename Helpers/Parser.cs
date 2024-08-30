using System.Globalization;
using System.Text.Json;

namespace BasketballOlympics.Helpers;

public static class Parser
{
    public static Dictionary<char, IEnumerable<Country>> GroupParser(string path)
    {
        using StreamReader r = new(path);
        var json = r.ReadToEnd();
        return JsonSerializer.Deserialize<Dictionary<char, IEnumerable<Country>>>(json)
            ?? throw new InvalidDataException("Object doesn't exist");
    }

    public static Dictionary<string, IEnumerable<Game>> GamesParser(string path)
    {
        using StreamReader r = new(path);
        var json = r.ReadToEnd();
        JsonSerializerOptions options = new();
        options.Converters.Add(new JsonDateOnlyConverter());
        return JsonSerializer.Deserialize<Dictionary<string, IEnumerable<Game>>>(json, options)
            ?? throw new InvalidDataException("Object doesn't exist");
    }
}

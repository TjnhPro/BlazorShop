using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorShop.AI.StorefrontReverseEngineering.Contracts;

public static class VisualJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

namespace BlazorShop.AI.StorefrontReverseEngineering.Domain;

public sealed record ViewportDefinition(
    string Id,
    int Width,
    int Height,
    decimal DeviceScaleFactor,
    bool IsMobile)
{
    public static IReadOnlyList<ViewportDefinition> Defaults { get; } =
    [
        new("desktop-1440", 1440, 1000, 1, false),
        new("tablet-768", 768, 1000, 1, false),
        new("mobile-390", 390, 900, 2, true)
    ];
}

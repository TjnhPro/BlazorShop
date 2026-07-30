namespace BlazorShop.AI.StorefrontReverseEngineering.Domain;

public sealed record ReferenceUrl(string Value)
{
    public static ReferenceUrl Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Reference URL must be an absolute URL.", nameof(value));
        }

        if (uri.Scheme is not ("http" or "https" or "file"))
        {
            throw new ArgumentException("Reference URL must use http, https, or a local file fixture URL.", nameof(value));
        }

        return new ReferenceUrl(uri.ToString());
    }
}

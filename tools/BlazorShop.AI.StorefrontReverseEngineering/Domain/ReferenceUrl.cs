namespace BlazorShop.AI.StorefrontReverseEngineering.Domain;

public sealed record ReferenceUrl(string Value)
{
    public static ReferenceUrl Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"[SRE-URL-001] Reference URL is invalid. Problem: '{value}' is not an absolute URL. Cause: reverse-engineering capture needs a deterministic source location. Fix: pass an absolute http, https, or local file fixture URL.", nameof(value));
        }

        if (uri.Scheme is not ("http" or "https" or "file"))
        {
            throw new ArgumentException($"[SRE-URL-002] Reference URL scheme is unsupported. Problem: '{value}' uses '{uri.Scheme}'. Cause: Phase 3A only permits http, https, and local fixture file URLs. Fix: use an approved reference URL scheme.", nameof(value));
        }

        return new ReferenceUrl(uri.ToString());
    }
}

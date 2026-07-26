namespace BlazorShop.Storefront.Presentation.PagePatterns;

public static class StorefrontPageResultMapper
{
    public static StorefrontPageState.Ready<TContext> Ready<TContext>(
        StorefrontPageKind kind,
        TContext context,
        StorefrontPageDocument document,
        int? httpStatusCode = null,
        bool retryable = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateDocument(document);
        return new StorefrontPageState.Ready<TContext>(kind, context, document, httpStatusCode, retryable);
    }

    public static StorefrontPageState.NotFoundState NotFound(StorefrontPageKind kind, string? message = null)
    {
        return new StorefrontPageState.NotFoundState(kind, message);
    }

    public static StorefrontPageState.ServiceUnavailableState ServiceUnavailable(StorefrontPageKind kind, string? message = null, bool retryable = true)
    {
        return new StorefrontPageState.ServiceUnavailableState(kind, message, retryable);
    }

    private static void ValidateDocument(StorefrontPageDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Title) && string.IsNullOrWhiteSpace(document.Description))
        {
            throw new InvalidOperationException("A ready storefront page must include a SEO document.");
        }
    }
}

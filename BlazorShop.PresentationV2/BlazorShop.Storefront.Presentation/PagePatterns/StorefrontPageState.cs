namespace BlazorShop.Storefront.Presentation.PagePatterns;

public abstract record StorefrontPageState(StorefrontPageStatus Status)
{
    public sealed record LoadingState() : StorefrontPageState(StorefrontPageStatus.Loading);

    public sealed record Ready<TContext>(
        StorefrontPageKind Kind,
        TContext Context,
        StorefrontPageDocument Document,
        int? HttpStatusCode = null,
        bool Retryable = false) : StorefrontPageState(StorefrontPageStatus.Ready);

    public sealed record EmptyState(string Message) : StorefrontPageState(StorefrontPageStatus.Empty);

    public sealed record NotFoundState(StorefrontPageKind Kind, string? Message = null) : StorefrontPageState(StorefrontPageStatus.NotFound);

    public sealed record ServiceUnavailableState(StorefrontPageKind Kind, string? Message = null, bool Retryable = true) : StorefrontPageState(StorefrontPageStatus.ServiceUnavailable);

    public sealed record UnauthorizedState(StorefrontPageKind Kind, string? Message = null) : StorefrontPageState(StorefrontPageStatus.Unauthorized);

    public sealed record MaintenanceState(string? Message = null) : StorefrontPageState(StorefrontPageStatus.Maintenance);

    public sealed record ErrorState(string Message, StorefrontPageProblem? Problem = null) : StorefrontPageState(StorefrontPageStatus.Error);
}

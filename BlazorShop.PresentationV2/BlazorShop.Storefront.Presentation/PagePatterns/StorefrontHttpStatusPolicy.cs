namespace BlazorShop.Storefront.Presentation.PagePatterns;

using Microsoft.AspNetCore.Http;

public static class StorefrontHttpStatusPolicy
{
    public const string NoIndexNoFollow = "noindex, nofollow";
    public const string PrivateCacheControl = "no-store, no-cache, max-age=0";

    public static int ResolveStatusCode(StorefrontPageState state)
    {
        if (TryGetReadyState(state, out var readyStatusCode, out _)
            && readyStatusCode.HasValue)
        {
            return readyStatusCode.Value;
        }

        return state switch
        {
            StorefrontPageState.NotFoundState => StatusCodes.Status404NotFound,
            StorefrontPageState.ServiceUnavailableState => StatusCodes.Status503ServiceUnavailable,
            StorefrontPageState.UnauthorizedState => StatusCodes.Status401Unauthorized,
            StorefrontPageState.MaintenanceState => StatusCodes.Status503ServiceUnavailable,
            StorefrontPageState.ErrorState => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status200OK,
        };
    }

    public static bool IsPrivate(StorefrontPageState state)
    {
        if (TryGetReadyState(state, out _, out var document))
        {
            return document is null || !document.RobotsIndex || !document.RobotsFollow;
        }

        return state is StorefrontPageState.NotFoundState
            || state is StorefrontPageState.ServiceUnavailableState
            || state is StorefrontPageState.UnauthorizedState
            || state is StorefrontPageState.MaintenanceState
            || state is StorefrontPageState.ErrorState;
    }

    private static bool TryGetReadyState(
        StorefrontPageState state,
        out int? httpStatusCode,
        out StorefrontPageDocument? document)
    {
        if (state.GetType().IsGenericType
            && state.GetType().GetGenericTypeDefinition() == typeof(StorefrontPageState.Ready<>))
        {
            httpStatusCode = state.GetType().GetProperty(nameof(StorefrontPageState.Ready<object>.HttpStatusCode))?.GetValue(state) is int statusCode
                ? statusCode
                : null;
            document = state.GetType().GetProperty(nameof(StorefrontPageState.Ready<object>.Document))?.GetValue(state) as StorefrontPageDocument;
            return true;
        }

        httpStatusCode = null;
        document = null;
        return false;
    }
}

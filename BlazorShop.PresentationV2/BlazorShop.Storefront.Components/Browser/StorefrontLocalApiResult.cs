using System.Net;

namespace BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontLocalApiResult<T>(
    bool Success,
    HttpStatusCode StatusCode,
    T? Data,
    string Message)
{
    public static StorefrontLocalApiResult<T> Succeeded(HttpStatusCode statusCode, T? data)
    {
        return new StorefrontLocalApiResult<T>(true, statusCode, data, string.Empty);
    }

    public static StorefrontLocalApiResult<T> Failed(HttpStatusCode statusCode, string message)
    {
        return new StorefrontLocalApiResult<T>(false, statusCode, default, message);
    }
}

public sealed record StorefrontLocalApiErrorResponse(string? Message);

namespace BlazorShop.Application.Common.Results
{
    public enum ApplicationErrorKind
    {
        Validation,
        NotFound,
        Conflict,
        Unauthorized,
        Forbidden,
        RemoteFailure,
        Failure
    }
}

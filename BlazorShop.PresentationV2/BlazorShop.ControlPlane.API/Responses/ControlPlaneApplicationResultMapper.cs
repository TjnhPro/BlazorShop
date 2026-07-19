namespace BlazorShop.ControlPlane.API.Responses
{
    using BlazorShop.Application.Common.Results;

    using Microsoft.AspNetCore.Mvc;

    public static class ControlPlaneApplicationResultMapper
    {
        public static ObjectResult ToControlPlaneActionResult<TData>(
            this ApplicationResult<TData> result,
            int successStatusCode = StatusCodes.Status200OK)
        {
            ArgumentNullException.ThrowIfNull(result);

            return result.Success
                ? ControlPlaneApiResponseWriter.Success(successStatusCode, result.Value, result.Message)
                : ControlPlaneApiResponseWriter.Failure<TData>(
                    ToStatusCode(result.Error?.Kind),
                    result.Message ?? result.Error?.Message,
                    result.Value);
        }

        public static int ToStatusCode(ApplicationErrorKind? kind)
        {
            return kind switch
            {
                ApplicationErrorKind.Validation => StatusCodes.Status400BadRequest,
                ApplicationErrorKind.NotFound => StatusCodes.Status404NotFound,
                ApplicationErrorKind.Conflict => StatusCodes.Status409Conflict,
                ApplicationErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
                ApplicationErrorKind.Forbidden => StatusCodes.Status403Forbidden,
                ApplicationErrorKind.RemoteFailure => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status400BadRequest,
            };
        }
    }
}

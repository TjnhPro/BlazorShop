namespace BlazorShop.CommerceNode.API.Responses
{
    using BlazorShop.Application.Common.Results;

    using Microsoft.AspNetCore.Mvc;

    public static class CommerceNodeApplicationResultMapper
    {
        public static ObjectResult ToCommerceNodeActionResult<TData>(
            this ApplicationResult<TData> result,
            int successStatusCode = StatusCodes.Status200OK)
        {
            ArgumentNullException.ThrowIfNull(result);

            var response = result.Success
                ? CommerceNodeApiResponse<TData>.Succeeded(result.Value, NormalizeMessage(result.Message))
                : CommerceNodeApiResponse<TData>.Failed(
                    NormalizeMessage(result.Message ?? result.Error?.Message),
                    result.Value);

            return new ObjectResult(response)
            {
                StatusCode = result.Success ? successStatusCode : ToStatusCode(result.Error?.Kind),
            };
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
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Commerce Node request could not be completed."
                : message;
        }
    }
}

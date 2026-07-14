namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    public abstract class StorefrontApiControllerBase : ControllerBase
    {
        protected IActionResult Success<TData>(TData? data, string message)
        {
            return this.Ok(CommerceNodeApiResponse<TData>.Succeeded(data, NormalizeMessage(message)));
        }

        protected IActionResult FromServiceResponse(ServiceResponse response)
        {
            if (response.Success)
            {
                return this.Ok(CommerceNodeApiResponse.Succeeded(NormalizeMessage(response.Message)));
            }

            return this.Error(
                StatusCodes.Status400BadRequest,
                "validation_error",
                NormalizeMessage(response.Message));
        }

        protected IActionResult FromServiceResponse<TData>(ServiceResponse response, Func<object?, TData?> mapPayload)
        {
            if (response.Success)
            {
                return this.Ok(CommerceNodeApiResponse<TData>.Succeeded(
                    mapPayload(response.Payload),
                    NormalizeMessage(response.Message)));
            }

            return this.Error(
                StatusCodes.Status400BadRequest,
                "validation_error",
                NormalizeMessage(response.Message));
        }

        protected IActionResult FromServiceResponse<TData>(ServiceResponse<TData> response)
        {
            if (response.Success)
            {
                return this.Ok(CommerceNodeApiResponse<TData>.Succeeded(response.Payload, NormalizeMessage(response.Message)));
            }

            return this.Error(
                ToStatusCode(response.ResponseType),
                ToErrorCode(response.ResponseType),
                NormalizeMessage(response.Message));
        }

        protected IActionResult FromServiceResponse<TSource, TData>(
            ServiceResponse<TSource> response,
            Func<TSource?, TData?> mapPayload)
        {
            if (response.Success)
            {
                return this.Ok(CommerceNodeApiResponse<TData>.Succeeded(
                    mapPayload(response.Payload),
                    NormalizeMessage(response.Message)));
            }

            return this.Error(
                ToStatusCode(response.ResponseType),
                ToErrorCode(response.ResponseType),
                NormalizeMessage(response.Message));
        }

        protected IActionResult Failure<TData>(ServiceResponseType responseType, string message, TData? data = default)
        {
            return this.Error(ToStatusCode(responseType), ToErrorCode(responseType), message);
        }

        protected IActionResult Error(int statusCode, string code, string message)
        {
            return this.StatusCode(
                statusCode,
                new CommerceNodeApiErrorResponse(
                    false,
                    code,
                    NormalizeMessage(message),
                    this.HttpContext.TraceIdentifier));
        }

        private static int ToStatusCode(ServiceResponseType responseType)
        {
            return responseType switch
            {
                ServiceResponseType.ValidationError => StatusCodes.Status400BadRequest,
                ServiceResponseType.NotFound => StatusCodes.Status404NotFound,
                ServiceResponseType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string ToErrorCode(ServiceResponseType responseType)
        {
            return responseType switch
            {
                ServiceResponseType.ValidationError => "validation_error",
                ServiceResponseType.NotFound => "not_found",
                ServiceResponseType.Conflict => "conflict",
                _ => "internal_error",
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

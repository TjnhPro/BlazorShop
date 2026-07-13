namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    public abstract class CommerceAdminControllerBase : ControllerBase
    {
        protected IActionResult Success<TData>(TData? data, string message)
        {
            return this.Ok(CommerceNodeApiResponse<TData>.Succeeded(data, message));
        }

        protected IActionResult FromServiceResponse(ServiceResponse response)
        {
            if (response.Success)
            {
                return this.Ok(CommerceNodeApiResponse<object>.Succeeded(response.Payload ?? new { response.Id }, NormalizeMessage(response.Message)));
            }

            return this.BadRequest(CommerceNodeApiResponse<object>.Failed(NormalizeMessage(response.Message)));
        }

        protected IActionResult FromServiceResponse<TData>(ServiceResponse<TData> response)
        {
            if (response.Success)
            {
                return this.Ok(CommerceNodeApiResponse<TData>.Succeeded(response.Payload, NormalizeMessage(response.Message)));
            }

            return this.StatusCode(ToStatusCode(response.ResponseType), CommerceNodeApiResponse<TData>.Failed(NormalizeMessage(response.Message), response.Payload));
        }

        protected IActionResult Failure<TData>(ServiceResponseType responseType, string message, TData? data = default)
        {
            return this.StatusCode(ToStatusCode(responseType), CommerceNodeApiResponse<TData>.Failed(message, data));
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

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Commerce Node request could not be completed."
                : message;
        }
    }
}

namespace BlazorShop.Application.Common.Results
{
    using BlazorShop.Application.DTOs;

    public static class ServiceResponseApplicationResultExtensions
    {
        private const string DefaultCodePrefix = "service_response";

        public static ApplicationResult<TPayload> ToApplicationResult<TPayload>(
            this ServiceResponse<TPayload> response,
            string errorCodePrefix = DefaultCodePrefix)
        {
            ArgumentNullException.ThrowIfNull(response);

            if (response.Success)
            {
                return new ApplicationResult<TPayload>(
                    Success: true,
                    Message: response.Message,
                    Payload: response.Payload);
            }

            return ApplicationResult<TPayload>.Failed(
                ToApplicationError(response.ResponseType, errorCodePrefix, response.Message),
                response.Payload);
        }

        public static ServiceResponse<TPayload> ToServiceResponse<TPayload>(
            this ApplicationResult<TPayload> result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return new ServiceResponse<TPayload>(result.Success, result.Message)
            {
                Payload = result.Value,
                ResponseType = result.Success
                    ? ServiceResponseType.Success
                    : ToServiceResponseType(result.Error?.Kind),
            };
        }

        public static ApplicationErrorKind ToApplicationErrorKind(ServiceResponseType responseType)
        {
            return responseType switch
            {
                ServiceResponseType.ValidationError => ApplicationErrorKind.Validation,
                ServiceResponseType.NotFound => ApplicationErrorKind.NotFound,
                ServiceResponseType.Conflict => ApplicationErrorKind.Conflict,
                _ => ApplicationErrorKind.Failure,
            };
        }

        public static ServiceResponseType ToServiceResponseType(ApplicationErrorKind? kind)
        {
            return kind switch
            {
                ApplicationErrorKind.Validation => ServiceResponseType.ValidationError,
                ApplicationErrorKind.NotFound => ServiceResponseType.NotFound,
                ApplicationErrorKind.Conflict => ServiceResponseType.Conflict,
                _ => ServiceResponseType.Failure,
            };
        }

        private static ApplicationError ToApplicationError(
            ServiceResponseType responseType,
            string errorCodePrefix,
            string? message)
        {
            var prefix = NormalizeCodePrefix(errorCodePrefix);
            var safeMessage = string.IsNullOrWhiteSpace(message)
                ? "The service request could not be completed."
                : message;

            return responseType switch
            {
                ServiceResponseType.ValidationError => ApplicationError.Validation($"{prefix}.validation", safeMessage),
                ServiceResponseType.NotFound => ApplicationError.NotFound($"{prefix}.not_found", safeMessage),
                ServiceResponseType.Conflict => ApplicationError.Conflict($"{prefix}.conflict", safeMessage),
                _ => ApplicationError.Failure($"{prefix}.failure", safeMessage),
            };
        }

        private static string NormalizeCodePrefix(string? errorCodePrefix)
        {
            return string.IsNullOrWhiteSpace(errorCodePrefix)
                ? DefaultCodePrefix
                : errorCodePrefix.Trim();
        }
    }
}

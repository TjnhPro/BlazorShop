namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.Common.Results;

    public static class CommerceNodeAdminGatewayApplicationResultMapper
    {
        private const string UpstreamStatusCodeMetadataKey = "upstreamStatusCode";

        public static ApplicationResult<TPayload> ToApplicationResult<TPayload>(
            CommerceNodeAdminGatewayResult<TPayload> result)
        {
            if (result.Success)
            {
                return new ApplicationResult<TPayload>(
                    Success: true,
                    Message: result.Message,
                    Payload: result.Payload);
            }

            return ApplicationResult<TPayload>.Failed(
                ToApplicationError(result.Failure, result.Message, result.HttpStatusCode),
                result.Payload);
        }

        public static ApplicationResult<ApplicationMediaContent> ToApplicationMediaResult(
            CommerceNodeAdminMediaGatewayResult result,
            string? fileName = null)
        {
            if (result.Success)
            {
                return new ApplicationResult<ApplicationMediaContent>(
                    Success: true,
                    Message: result.Message,
                    Payload: new ApplicationMediaContent(
                        result.Content ?? [],
                        string.IsNullOrWhiteSpace(result.ContentType)
                            ? "application/octet-stream"
                            : result.ContentType,
                        fileName,
                        BuildMetadata(result.HttpStatusCode)));
            }

            return ApplicationResult<ApplicationMediaContent>.Failed(
                ToApplicationError(result.Failure, result.Message, result.HttpStatusCode));
        }

        private static ApplicationError ToApplicationError(
            CommerceNodeAdminGatewayFailure? failure,
            string? message,
            int? httpStatusCode)
        {
            var safeMessage = string.IsNullOrWhiteSpace(message)
                ? "Commerce Node gateway request failed."
                : message.Trim();
            var metadata = BuildMetadata(httpStatusCode);

            return failure switch
            {
                CommerceNodeAdminGatewayFailure.Validation => ApplicationError.Validation(
                    "commerce_node.validation",
                    safeMessage,
                    metadata),
                CommerceNodeAdminGatewayFailure.NotFound => ApplicationError.NotFound(
                    "commerce_node.not_found",
                    safeMessage,
                    metadata),
                CommerceNodeAdminGatewayFailure.RemoteFailure => ApplicationError.RemoteFailure(
                    "commerce_node.remote_failure",
                    safeMessage,
                    metadata),
                _ => ApplicationError.RemoteFailure(
                    "commerce_node.remote_failure",
                    safeMessage,
                    metadata),
            };
        }

        private static IReadOnlyDictionary<string, string>? BuildMetadata(int? httpStatusCode)
        {
            return httpStatusCode is null
                ? null
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [UpstreamStatusCodeMetadataKey] = httpStatusCode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                };
        }
    }
}

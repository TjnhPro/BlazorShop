namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;

    public sealed class CurrencyExchangeRateUpdateTaskHandler : ICommerceTaskHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IStoreCurrencyExchangeRateProviderService exchangeRateProviderService;
        private readonly IAdminAuditService auditService;

        public CurrencyExchangeRateUpdateTaskHandler(
            IStoreCurrencyExchangeRateProviderService exchangeRateProviderService,
            IAdminAuditService auditService)
        {
            this.exchangeRateProviderService = exchangeRateProviderService;
            this.auditService = auditService;
        }

        public string TaskType => CurrencyExchangeRateTaskTypes.Update;

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            StoreCurrencyExchangeRateUpdateTaskPayload payload;
            try
            {
                payload = JsonSerializer.Deserialize<StoreCurrencyExchangeRateUpdateTaskPayload>(
                    context.PayloadJson,
                    SerializerOptions) ?? throw new JsonException("Empty payload.");
            }
            catch (JsonException)
            {
                return CommerceTaskHandlerResult.Failed(
                    "Exchange-rate update payload is invalid.",
                    "invalid_currency_rate_update_payload",
                    retryable: false);
            }

            var validationError = ValidatePayload(payload);
            if (validationError is not null)
            {
                await this.LogFailureAsync(context, payload, validationError, ServiceResponseType.ValidationError, cancellationToken);
                return CommerceTaskHandlerResult.Failed(
                    validationError,
                    "invalid_currency_rate_update_payload",
                    retryable: false);
            }

            if (await context.IsCancellationRequestedAsync(cancellationToken))
            {
                await this.LogFailureAsync(context, payload, "Exchange-rate update task was cancelled.", ServiceResponseType.Conflict, cancellationToken);
                return CommerceTaskHandlerResult.Failed(
                    "Exchange-rate update task was cancelled.",
                    "task_cancelled",
                    retryable: false);
            }

            var result = await this.exchangeRateProviderService.FetchForStoreAsync(
                payload.StoreId,
                new FetchStoreCurrencyExchangeRatesRequest(
                    payload.ProviderKey,
                    payload.TargetCurrencyCodes,
                    payload.IsEnabled),
                cancellationToken);

            if (!result.Success || result.Payload is null)
            {
                await this.LogFailureAsync(
                    context,
                    payload,
                    result.Message ?? "Exchange-rate provider update failed.",
                    result.ResponseType,
                    cancellationToken);

                return CommerceTaskHandlerResult.Failed(
                    result.Message ?? "Exchange-rate provider update failed.",
                    ToErrorCode(result.ResponseType),
                    retryable: IsRetryable(result.ResponseType));
            }

            var resultJson = JsonSerializer.Serialize(result.Payload, SerializerOptions);
            return CommerceTaskHandlerResult.Succeeded("Exchange-rate update completed.", resultJson);
        }

        private async Task LogFailureAsync(
            CommerceTaskHandlerContext context,
            StoreCurrencyExchangeRateUpdateTaskPayload payload,
            string message,
            ServiceResponseType responseType,
            CancellationToken cancellationToken)
        {
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "StoreCurrencyExchangeRate.ProviderFetchFailed",
                EntityType = "StoreCurrencyExchangeRate",
                Summary = $"Exchange-rate provider update failed for '{payload.ProviderKey}'.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    payload.StoreId,
                    payload.ProviderKey,
                    payload.TargetCurrencyCodes,
                    payload.IsEnabled,
                    TaskPublicId = context.PublicId,
                    context.AttemptNumber,
                    ResponseType = responseType.ToString(),
                    Message = message,
                }, SerializerOptions),
            });
        }

        private static string? ValidatePayload(StoreCurrencyExchangeRateUpdateTaskPayload payload)
        {
            if (!string.Equals(payload.SchemaVersion, "v1", StringComparison.OrdinalIgnoreCase))
            {
                return "Payload schemaVersion must be v1.";
            }

            if (payload.StoreId == Guid.Empty)
            {
                return "Store id is required.";
            }

            if (string.IsNullOrWhiteSpace(payload.ProviderKey))
            {
                return "Provider key is required.";
            }

            if (payload.TargetCurrencyCodes.Count == 0)
            {
                return "At least one target currency code is required.";
            }

            return null;
        }

        private static bool IsRetryable(ServiceResponseType responseType)
        {
            return responseType is ServiceResponseType.Failure or ServiceResponseType.None;
        }

        private static string ToErrorCode(ServiceResponseType responseType)
        {
            return responseType switch
            {
                ServiceResponseType.ValidationError => "currency_rate_update_validation_failed",
                ServiceResponseType.NotFound => "currency_rate_update_not_found",
                ServiceResponseType.Conflict => "currency_rate_update_conflict",
                _ => "currency_rate_update_failed",
            };
        }
    }
}

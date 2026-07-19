namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.Extensions.Options;

    public sealed class QueuedAccountEmailDispatcher : IAccountEmailDispatcher
    {
        private readonly ICommerceStoreContext storeContext;
        private readonly IMessageQueueService messageQueueService;
        private readonly ClientAppOptions clientAppOptions;

        public QueuedAccountEmailDispatcher(
            ICommerceStoreContext storeContext,
            IMessageQueueService messageQueueService,
            IOptions<ClientAppOptions> clientAppOptions)
        {
            this.storeContext = storeContext;
            this.messageQueueService = messageQueueService;
            this.clientAppOptions = clientAppOptions.Value;
        }

        public Task<AccountEmailDispatchResult> SendActivationAsync(
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.QueueAsync(
                TransactionalMessageTemplateSystemNames.AccountActivation,
                "Account.ActivationUrl",
                "account.activation",
                request,
                cancellationToken);
        }

        public Task<AccountEmailDispatchResult> SendPasswordRecoveryAsync(
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.QueueAsync(
                TransactionalMessageTemplateSystemNames.PasswordRecovery,
                "Account.PasswordResetUrl",
                "account.password_recovery",
                request,
                cancellationToken);
        }

        private async Task<AccountEmailDispatchResult> QueueAsync(
            string templateSystemName,
            string actionUrlToken,
            string eventName,
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return AccountEmailDispatchResult.Failed("account_email.email_required", "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ActionUrl))
            {
                return AccountEmailDispatchResult.Failed("account_email.action_url_required", "Action URL is required.");
            }

            var currentStoreResult = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!currentStoreResult.Success || currentStoreResult.Payload is null)
            {
                return AccountEmailDispatchResult.Failed(
                    "account_email.store_unavailable",
                    currentStoreResult.Message ?? "Store could not be resolved.");
            }

            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return AccountEmailDispatchResult.Failed(
                    "account_email.store_unavailable",
                    storeIdResult.Message ?? "Store could not be resolved.");
            }

            var store = currentStoreResult.Payload;
            var tokens = CreateTokens(store, request, actionUrlToken);
            var queueResult = await this.messageQueueService.QueueAsync(
                new QueueTransactionalMessageRequest(
                    storeIdResult.Payload,
                    templateSystemName,
                    request.Email.Trim(),
                    NormalizeOptional(request.FullName),
                    store.DefaultCulture,
                    tokens,
                    IdempotencyKey: null,
                    CorrelationId: CreateCorrelationId(eventName, request.UserId),
                    RelatedEntityType: string.IsNullOrWhiteSpace(request.UserId) ? null : "identity.user",
                    RelatedEntityId: NormalizeOptional(request.UserId)),
                cancellationToken);

            return queueResult.Success
                ? AccountEmailDispatchResult.Succeeded()
                : AccountEmailDispatchResult.Failed(
                    queueResult.ErrorCode ?? "account_email.queue_failed",
                    queueResult.Message ?? "Account email could not be queued.");
        }

        private IReadOnlyDictionary<string, string?> CreateTokens(
            CommerceCurrentStore store,
            AccountEmailDispatchRequest request,
            string actionUrlToken)
        {
            var (firstName, lastName) = SplitName(request.FullName);
            return new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Store.Name"] = store.Name,
                ["Store.Url"] = NormalizeOptional(store.BaseUrl) ?? this.clientAppOptions.BaseUrl,
                ["Store.SupportEmail"] = NormalizeOptional(store.SupportEmail ?? store.CompanyEmail),
                ["Store.SupportPhone"] = NormalizeOptional(store.SupportPhone ?? store.CompanyPhone),
                ["Customer.Email"] = request.Email.Trim(),
                ["Customer.FullName"] = NormalizeOptional(request.FullName) ?? request.Email.Trim(),
                ["Customer.FirstName"] = firstName,
                ["Customer.LastName"] = lastName,
                [actionUrlToken] = request.ActionUrl,
            };
        }

        private static string? CreateCorrelationId(string eventName, string? userId)
        {
            var normalizedUserId = NormalizeOptional(userId);
            return normalizedUserId is null ? null : $"{eventName}:{normalizedUserId}";
        }

        private static (string? FirstName, string? LastName) SplitName(string? fullName)
        {
            var normalized = NormalizeOptional(fullName);
            if (normalized is null)
            {
                return (null, null);
            }

            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => (null, null),
                1 => (parts[0], null),
                _ => (parts[0], string.Join(' ', parts.Skip(1))),
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

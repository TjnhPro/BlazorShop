namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    public sealed class StorefrontContactMessageService : IStorefrontContactMessageService
    {
        private readonly ICommerceStoreContext storeContext;
        private readonly IMessageQueueService messageQueueService;

        public StorefrontContactMessageService(
            ICommerceStoreContext storeContext,
            IMessageQueueService messageQueueService)
        {
            this.storeContext = storeContext;
            this.messageQueueService = messageQueueService;
        }

        public async Task<ServiceResponse<StorefrontContactMessageResult>> SendAsync(
            StorefrontContactMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = Validate(request);
            if (validation is not null)
            {
                return Failure(ServiceResponseType.ValidationError, validation);
            }

            var currentStoreResult = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!currentStoreResult.Success || currentStoreResult.Payload is null)
            {
                return Failure(ServiceResponseType.NotFound, "Storefront store could not be resolved.");
            }

            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success || storeIdResult.Payload == Guid.Empty)
            {
                return Failure(ServiceResponseType.NotFound, "Storefront store could not be resolved.");
            }

            var store = currentStoreResult.Payload;
            var recipientEmail = NormalizeOptional(store.SupportEmail ?? store.CompanyEmail);
            if (recipientEmail is null)
            {
                return Failure(ServiceResponseType.Conflict, "Store contact recipient is not configured.");
            }

            var queueResult = await this.messageQueueService.QueueAsync(
                new QueueTransactionalMessageRequest(
                    storeIdResult.Payload,
                    TransactionalMessageTemplateSystemNames.StorefrontContactForm,
                    recipientEmail,
                    store.Name,
                    store.DefaultCulture,
                    CreateTokens(store, request),
                    IdempotencyKey: null,
                    CorrelationId: $"contact:{Guid.NewGuid():N}",
                    RelatedEntityType: "storefront.contact",
                    RelatedEntityId: null),
                cancellationToken);
            if (!queueResult.Success)
            {
                return Failure(ServiceResponseType.Failure, "Contact request could not be accepted.");
            }

            return new ServiceResponse<StorefrontContactMessageResult>(
                true,
                "Contact request accepted.")
            {
                Payload = new StorefrontContactMessageResult(true, "Contact request accepted."),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static IReadOnlyDictionary<string, string?> CreateTokens(
            CommerceCurrentStore store,
            StorefrontContactMessageRequest request)
        {
            return new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Store.Name"] = store.Name,
                ["Store.Url"] = NormalizeOptional(store.BaseUrl),
                ["Store.SupportEmail"] = NormalizeOptional(store.SupportEmail ?? store.CompanyEmail),
                ["Store.SupportPhone"] = NormalizeOptional(store.SupportPhone ?? store.CompanyPhone),
                ["Contact.Name"] = request.Name.Trim(),
                ["Contact.Email"] = request.Email.Trim(),
                ["Contact.Subject"] = request.Subject.Trim(),
                ["Contact.Message"] = request.Message.Trim(),
            };
        }

        private static string? Validate(StorefrontContactMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (request.Name.Trim().Length > 160)
            {
                return "Name must be 160 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email is required.";
            }

            if (request.Email.Trim().Length > 254 || !request.Email.Contains('@', StringComparison.Ordinal))
            {
                return "Email is invalid.";
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return "Subject is required.";
            }

            if (request.Subject.Trim().Length > 200)
            {
                return "Subject must be 200 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return "Message is required.";
            }

            if (request.Message.Trim().Length > 4000)
            {
                return "Message must be 4,000 characters or fewer.";
            }

            return null;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<StorefrontContactMessageResult> Failure(
            ServiceResponseType responseType,
            string message)
        {
            return new ServiceResponse<StorefrontContactMessageResult>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}

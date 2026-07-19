namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Options;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Services;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    using Stripe.Checkout;

    public sealed class StripeStorefrontPaymentProvider : IStorefrontPaymentProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IStripeCheckoutSessionService checkoutSessionService;
        private readonly IPaymentMinorUnitConverter minorUnitConverter;
        private readonly ClientAppOptions clientAppOptions;
        private readonly IConfiguration configuration;

        public StripeStorefrontPaymentProvider(
            IStripeCheckoutSessionService checkoutSessionService,
            IPaymentMinorUnitConverter minorUnitConverter,
            IOptions<ClientAppOptions> clientAppOptions,
            IConfiguration configuration)
        {
            this.checkoutSessionService = checkoutSessionService;
            this.minorUnitConverter = minorUnitConverter;
            this.clientAppOptions = clientAppOptions.Value;
            this.configuration = configuration;
        }

        public string ProviderKey => PaymentMethodKeys.Stripe;

        public PaymentProviderDescriptor Descriptor { get; } = new(
            PaymentMethodKeys.Stripe,
            "Stripe",
            "Hosted card payments through Stripe Checkout.",
            IconUrl: null,
            DefaultDisplayOrder: 20,
            SupportedCurrencyCodes: [],
            SupportedCountryCodes: [],
            MinOrderTotal: null,
            MaxOrderTotal: null,
            PaymentProviderMethodTypes.Redirect,
            RecurringCapable: false,
            SupportsAuthorize: false,
            SupportsCapture: true,
            SupportsVoid: false,
            SupportsRefund: false,
            SupportsPartialRefund: false,
            RequiresWebhookSignature: true,
            ActiveByDefault: true);

        public async Task<ServiceResponse<PaymentProviderOperationResult>> CreatePaymentSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.CreateHostedSessionAsync(request, cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return PaymentProviderOperationResult.Failed(
                    result.ResponseType is ServiceResponseType.Success ? ServiceResponseType.Conflict : result.ResponseType,
                    result.Message ?? "Stripe checkout session could not be created.",
                    "provider_session_failed");
            }

            return PaymentProviderOperationResult.Succeeded(
                result.Message ?? "Stripe checkout session created.",
                PaymentProviderActionTypes.Redirect,
                result.Payload.NextActionUrl,
                result.Payload.ProviderSessionId,
                result.Payload.ProviderReference,
                result.Payload.MetadataJson,
                PaymentAttemptStates.RequiresAction);
        }

        public async Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(this.configuration["Stripe:SecretKey"]))
            {
                return Failed("Stripe checkout is not configured.");
            }

            if (string.IsNullOrWhiteSpace(this.clientAppOptions.BaseUrl))
            {
                return Failed("Storefront return URL is not configured.");
            }

            if (request.Amount <= 0m || request.Lines.Count == 0)
            {
                return Failed("Payment amount is invalid.");
            }

            var successUrl = this.BuildClientUrl($"payment-success?paymentAttemptId={request.PaymentAttemptId}&provider=stripe");
            var cancelUrl = this.BuildClientUrl($"payment-cancel?paymentAttemptId={request.PaymentAttemptId}&provider=stripe");
            var options = new SessionCreateOptions
            {
                ClientReferenceId = request.PaymentAttemptId.ToString(),
                Mode = "payment",
                PaymentMethodTypes = ["card"],
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["storeId"] = request.StoreId.ToString(),
                    ["checkoutSessionId"] = request.CheckoutSessionId.ToString(),
                    ["paymentAttemptId"] = request.PaymentAttemptId.ToString(),
                    ["idempotencyKey"] = request.IdempotencyKey,
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["paymentAttemptId"] = request.PaymentAttemptId.ToString(),
                        ["idempotencyKey"] = request.IdempotencyKey,
                    },
                },
                LineItems = request.Lines.Select(line => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.ToLowerInvariant(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = string.IsNullOrWhiteSpace(line.Name) ? "Product" : line.Name,
                        },
                        UnitAmount = this.minorUnitConverter.ToMinorUnits(line.UnitAmount, request.CurrencyCode),
                    },
                    Quantity = line.Quantity,
                }).ToList(),
            };

            Session session;
            try
            {
                session = await this.checkoutSessionService.CreateAsync(options, cancellationToken);
            }
            catch (Exception)
            {
                return Failed("Stripe checkout session could not be created.");
            }

            if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
            {
                return Failed("Stripe checkout session could not be created.");
            }

            var metadata = JsonSerializer.Serialize(new
            {
                provider = this.ProviderKey,
                sessionId = session.Id,
                paymentAttemptId = request.PaymentAttemptId,
                createdAt = DateTimeOffset.UtcNow,
            }, JsonOptions);

            return new ServiceResponse<PaymentProviderSessionResult>(true, "Stripe checkout session created.")
            {
                Payload = new PaymentProviderSessionResult(
                    session.Id,
                    session.PaymentIntentId,
                    "redirect",
                    session.Url,
                    metadata),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private string BuildClientUrl(string path)
        {
            return $"{this.clientAppOptions.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private static ServiceResponse<PaymentProviderSessionResult> Failed(string message)
        {
            return new ServiceResponse<PaymentProviderSessionResult>(false, message)
            {
                ResponseType = ServiceResponseType.Conflict,
            };
        }
    }

    public sealed class StorefrontPaymentProviderResolver : IStorefrontPaymentProviderResolver
    {
        private readonly IReadOnlyDictionary<string, IStorefrontPaymentProvider> providers;

        public StorefrontPaymentProviderResolver(IEnumerable<IStorefrontPaymentProvider> providers)
        {
            this.providers = providers.ToDictionary(provider => provider.ProviderKey, StringComparer.OrdinalIgnoreCase);
        }

        public IStorefrontPaymentProvider Resolve(string providerKey)
        {
            if (this.providers.TryGetValue(providerKey, out var provider))
            {
                return provider;
            }

            throw new InvalidOperationException($"Payment provider '{providerKey}' is not supported.");
        }
    }

}

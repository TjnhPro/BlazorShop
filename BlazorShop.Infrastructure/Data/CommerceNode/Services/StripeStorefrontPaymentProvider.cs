namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Options;
    using BlazorShop.Domain.Constants;
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

    public sealed class PaymentProviderCapabilityRegistry : IPaymentProviderCapabilityRegistry
    {
        private readonly IReadOnlySet<string> installedProviders;

        public PaymentProviderCapabilityRegistry(IEnumerable<IStorefrontPaymentProvider> providers)
        {
            this.installedProviders = providers
                .Select(provider => provider.ProviderKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<PaymentProviderCapabilityDto> List()
        {
            return
            [
                CreateCod(),
                CreateStripe(this.installedProviders.Contains(PaymentMethodKeys.Stripe)),
                CreatePayPalSkeleton(),
            ];
        }

        public ServiceResponse<PaymentProviderCapabilityDto> Get(string systemName)
        {
            var normalized = Normalize(systemName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Failure("Payment provider is required.");
            }

            var capability = this.List()
                .FirstOrDefault(candidate => string.Equals(candidate.SystemName, normalized, StringComparison.OrdinalIgnoreCase));
            return capability is null
                ? Failure("Payment provider is not supported.")
                : new ServiceResponse<PaymentProviderCapabilityDto>(true, "Payment provider capability loaded.")
                {
                    Payload = capability,
                    ResponseType = ServiceResponseType.Success,
                };
        }

        private static PaymentProviderCapabilityDto CreateCod()
        {
            return new PaymentProviderCapabilityDto(
                PaymentMethodKeys.Cod,
                Installed: true,
                Active: true,
                DisplayName: "Cash on Delivery",
                Description: "Offline payment collected when the order is delivered.",
                IconUrl: null,
                DefaultDisplayOrder: 10,
                SupportedStoreIds: [],
                SupportedCurrencyCodes: [],
                SupportedCountryCodes: [],
                MinOrderTotal: null,
                MaxOrderTotal: null,
                MethodType: PaymentProviderMethodTypes.Offline,
                RecurringCapable: false,
                SupportsAuthorize: false,
                SupportsCapture: true,
                SupportsVoid: false,
                SupportsRefund: false,
                SupportsPartialRefund: false,
                RequiresWebhookSignature: false);
        }

        private static PaymentProviderCapabilityDto CreateStripe(bool installed)
        {
            return new PaymentProviderCapabilityDto(
                PaymentMethodKeys.Stripe,
                Installed: installed,
                Active: installed,
                DisplayName: "Stripe",
                Description: "Hosted card payments through Stripe Checkout.",
                IconUrl: null,
                DefaultDisplayOrder: 20,
                SupportedStoreIds: [],
                SupportedCurrencyCodes: [],
                SupportedCountryCodes: [],
                MinOrderTotal: null,
                MaxOrderTotal: null,
                MethodType: PaymentProviderMethodTypes.Redirect,
                RecurringCapable: false,
                SupportsAuthorize: false,
                SupportsCapture: true,
                SupportsVoid: false,
                SupportsRefund: false,
                SupportsPartialRefund: false,
                RequiresWebhookSignature: true);
        }

        private static PaymentProviderCapabilityDto CreatePayPalSkeleton()
        {
            return new PaymentProviderCapabilityDto(
                PaymentMethodKeys.PayPal,
                Installed: false,
                Active: false,
                DisplayName: "PayPal",
                Description: "PayPal provider skeleton; disabled until a real adapter is configured.",
                IconUrl: null,
                DefaultDisplayOrder: 30,
                SupportedStoreIds: [],
                SupportedCurrencyCodes: [],
                SupportedCountryCodes: [],
                MinOrderTotal: null,
                MaxOrderTotal: null,
                MethodType: PaymentProviderMethodTypes.Redirect,
                RecurringCapable: false,
                SupportsAuthorize: false,
                SupportsCapture: false,
                SupportsVoid: false,
                SupportsRefund: false,
                SupportsPartialRefund: false,
                RequiresWebhookSignature: true);
        }

        private static ServiceResponse<PaymentProviderCapabilityDto> Failure(string message)
        {
            return new ServiceResponse<PaymentProviderCapabilityDto>(false, message)
            {
                ResponseType = ServiceResponseType.ValidationError,
            };
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}

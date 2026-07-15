namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Moq;
    using Xunit;

    public sealed class StorefrontCheckoutServiceTests
    {
        [Fact]
        public async Task PreviewAsync_RejectsStaleCartVersion_AndDoesNotCreateSession()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 15m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version - 1));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.CheckoutSessions);
            Assert.Empty(context.Orders);
        }

        [Fact]
        public async Task PreviewAsync_ReturnsValidationIssues_ForInvalidShippingAndEmail()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 2));
            var service = CreateCheckoutService(context, cartService);
            var request = CreateRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Version,
                customerEmail: "not-an-email",
                shippingEmail: "shipping-not-email",
                postalCode: "",
                countryCode: "USA");

            var result = await service.PreviewAsync(request);

            Assert.True(result.Success);
            Assert.NotNull(result.Payload);
            Assert.False(result.Payload!.IsValid);
            Assert.Equal("review", result.Payload.NextAction);
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "customer.email_invalid");
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "shipping.email_invalid");
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "shipping.postal_required");
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "shipping.country_invalid");
            Assert.Single(context.CheckoutSessions);
            Assert.Empty(context.Orders);
        }

        [Fact]
        public async Task PlaceOrderAsync_DuplicateIdempotencyKey_ReturnsSameOrder()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 25m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 2));
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            var first = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "checkout-retry-key"));
            var second = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload.CheckoutSessionId,
                preview.Payload.CartVersion,
                "checkout-retry-key"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.Payload!.OrderId, second.Payload!.OrderId);
            Assert.Equal(first.Payload.PaymentAttemptId, second.Payload.PaymentAttemptId);
            Assert.Single(context.Orders);
            var attempt = Assert.Single(context.PaymentAttempts);
            Assert.Equal(PaymentAttemptStates.Captured, attempt.State);
            Assert.Equal(first.Payload.OrderId, attempt.OrderId);
            Assert.Equal(CartSessionStates.Ordered, context.CartSessions.Single().State);
            Assert.Equal(8, context.Products.Single(item => item.Id == product.Id).Quantity);
        }

        [Fact]
        public async Task PlaceOrderAsync_RejectsStaleCartVersion()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 12m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));
            context.CartSessions.Single().Version++;
            context.SaveChanges();

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "stale-cart-version"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
            Assert.Equal(CartSessionStates.Active, context.CartSessions.Single().State);
        }

        [Fact]
        public async Task PlaceOrderAsync_RejectsProductUnpublishedAfterPreview()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 15m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));
            context.Products.Single(item => item.Id == product.Id).IsPublished = false;
            context.SaveChanges();

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "unpublished-after-preview"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
        }

        [Fact]
        public async Task PlaceOrderAsync_SnapshotsSelectedAttributesAndPersonalization()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 30m, stock: 10);
            product.ProductType = ProductTypes.CustomVariations;
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                SelectedAttributes: [new SelectedAttributeDto("Size", "XL")],
                PersonalizationHash: "personalization-hash",
                PersonalizationJson: "{\"text\":\"hello\"}",
                FulfillmentProviderKey: "pod",
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "snapshot-line-data"));

            Assert.True(result.Success);
            var line = context.OrderLines.Single();
            Assert.Contains("\"name\":\"Size\"", line.VariantAttributesJson);
            Assert.Contains("\"value\":\"XL\"", line.VariantAttributesJson);
            Assert.Equal("personalization-hash", line.PersonalizationHash);
            Assert.Equal("{\"text\":\"hello\"}", line.PersonalizationJson);
            Assert.Equal("pod", line.FulfillmentProviderKey);
            Assert.Equal(10, context.Products.Single(item => item.Id == product.Id).Quantity);
        }

        [Fact]
        public async Task PlaceOrderAsync_StripeCreatesRedirectAttemptWithoutOrder()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId, PaymentMethodKeys.Stripe);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 40m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            var provider = new FakePaymentProvider(
                PaymentMethodKeys.Stripe,
                request => new PaymentProviderSessionResult(
                    "cs_test_123",
                    null,
                    "redirect",
                    "https://checkout.stripe.test/session",
                    "{\"provider\":\"stripe\"}"));
            var service = CreateCheckoutService(context, cartService, provider);
            var preview = await service.PreviewAsync(CreateRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Version,
                paymentMethodKey: PaymentMethodKeys.Stripe));

            var first = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "stripe-session-key"));
            var second = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload.CheckoutSessionId,
                preview.Payload.CartVersion,
                "stripe-session-key"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Null(first.Payload!.OrderId);
            Assert.Null(first.Payload.Reference);
            Assert.Equal(first.Payload.PaymentAttemptId, second.Payload!.PaymentAttemptId);
            Assert.Equal("redirect", first.Payload.NextActionType);
            Assert.Equal("https://checkout.stripe.test/session", first.Payload.NextActionUrl);
            Assert.Empty(context.Orders);
            var attempt = Assert.Single(context.PaymentAttempts);
            Assert.Equal(PaymentAttemptStates.RequiresAction, attempt.State);
            Assert.Equal("cs_test_123", attempt.ProviderSessionId);
            Assert.Equal(CheckoutSessionStates.OrderPending, context.CheckoutSessions.Single().State);
            Assert.Equal(CartSessionStates.Active, context.CartSessions.Single().State);
        }

        [Fact]
        public async Task PlaceOrderAsync_StripeProviderFailureReturnsConflictWithoutOrder()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId, PaymentMethodKeys.Stripe);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 40m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            var provider = new FakePaymentProvider(PaymentMethodKeys.Stripe, _ => null);
            var service = CreateCheckoutService(context, cartService, provider);
            var preview = await service.PreviewAsync(CreateRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Version,
                paymentMethodKey: PaymentMethodKeys.Stripe));

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "stripe-missing-config"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
            Assert.Equal(PaymentAttemptStates.Failed, context.PaymentAttempts.Single().State);
        }

        [Fact]
        public async Task CheckoutAsync_WhenCheckoutFeatureDisabled_RejectsPreviewAndPlaceOrder()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var cartService = CreateCartService(context, productRepository);
            var service = CreateCheckoutService(context, cartService, checkoutEnabled: false);

            var preview = await service.PreviewAsync(CreateRequest(storeId, "cart-token", expectedCartVersion: 1));
            var placeOrder = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                Guid.NewGuid(),
                ExpectedCartVersion: 1,
                IdempotencyKey: "disabled-checkout"));

            Assert.False(preview.Success);
            Assert.Equal(ServiceResponseType.Conflict, preview.ResponseType);
            Assert.Equal("Checkout is disabled for this store.", preview.Message);
            Assert.False(placeOrder.Success);
            Assert.Equal(ServiceResponseType.Conflict, placeOrder.ResponseType);
            Assert.Equal("Checkout is disabled for this store.", placeOrder.Message);
        }

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            params IStorefrontPaymentProvider[] providers)
        {
            return CreateCheckoutService(context, cartService, checkoutEnabled: true, providers);
        }

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            bool checkoutEnabled,
            params IStorefrontPaymentProvider[] providers)
        {
            return new StorefrontCheckoutService(
                context,
                cartService,
                new StorefrontCustomerService(context),
                new StubStoreFeatureStateService(checkoutEnabled),
                new PaymentHandlerResolver([new CodPaymentHandler()]),
                new StorefrontPaymentProviderResolver(providers));
        }

        private static StorefrontCartService CreateCartService(
            CommerceNodeDbContext context,
            Mock<IProductReadRepository> productRepository)
        {
            return new StorefrontCartService(
                new StorefrontCartSessionService(context),
                productRepository.Object);
        }

        private static StorefrontCheckoutPreviewRequest CreateRequest(
            Guid storeId,
            string cartToken,
            int expectedCartVersion,
            string customerEmail = "customer@example.test",
            string shippingEmail = "customer@example.test",
            string postalCode = "10001",
            string countryCode = "US",
            string paymentMethodKey = PaymentMethodKeys.Cod)
        {
            return new StorefrontCheckoutPreviewRequest(
                storeId,
                cartToken,
                expectedCartVersion,
                customerEmail,
                "Customer One",
                paymentMethodKey,
                new StorefrontCheckoutShippingAddressDto(
                    "Customer One",
                    shippingEmail,
                    "5550100",
                    "100 Main St",
                    null,
                    "New York",
                    "NY",
                    postalCode,
                    countryCode));
        }

        private static Product CreatePublishedProduct(Guid storeId, decimal price, int stock)
        {
            var categoryId = Guid.NewGuid();
            return new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Name = "Published product",
                Slug = $"published-{Guid.NewGuid():N}",
                Price = price,
                Quantity = stock,
                IsPublished = true,
                PublishedOn = DateTime.UtcNow,
                ArchivedAt = null,
                ProductType = ProductTypes.Simple,
                CategoryId = categoryId,
                Category = new Category
                {
                    Id = categoryId,
                    StoreId = storeId,
                    Name = "Published category",
                    Slug = "published-category",
                    IsPublished = true,
                },
            };
        }

        private static void SeedPaymentMethod(
            CommerceNodeDbContext context,
            Guid storeId,
            string paymentMethodKey = PaymentMethodKeys.Cod)
        {
            context.StorePaymentMethods.Add(new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = paymentMethodKey,
                DisplayName = paymentMethodKey,
                Enabled = true,
                DisplayOrder = 10,
            });
            context.SaveChanges();
        }

        private static void SeedProduct(CommerceNodeDbContext context, Product product)
        {
            context.Products.Add(product);
            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-checkout-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StubStoreFeatureStateService : IStoreFeatureStateService
        {
            private readonly bool checkoutEnabled;

            public StubStoreFeatureStateService(bool checkoutEnabled)
            {
                this.checkoutEnabled = checkoutEnabled;
            }

            public Task<IReadOnlyList<StoreFeatureStateDto>> GetAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<StoreFeatureStateDto>> UpdateAsync(
                string featureKey,
                UpdateStoreFeatureStateRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<StoreFeatureStateSnapshot> ResolveAsync(Guid storeId, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> IsEnabledAsync(Guid storeId, string featureKey, CancellationToken cancellationToken = default)
            {
                var enabled = !string.Equals(featureKey, StoreFeatureKeys.Checkout, StringComparison.Ordinal)
                    || this.checkoutEnabled;
                return Task.FromResult(enabled);
            }
        }

        private sealed class FakePaymentProvider : IStorefrontPaymentProvider
        {
            private readonly Func<CreatePaymentProviderSessionRequest, PaymentProviderSessionResult?> createSession;

            public FakePaymentProvider(
                string providerKey,
                Func<CreatePaymentProviderSessionRequest, PaymentProviderSessionResult?> createSession)
            {
                this.ProviderKey = providerKey;
                this.createSession = createSession;
            }

            public string ProviderKey { get; }

            public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                var result = this.createSession(request);
                return result is null
                    ? Task.FromResult(new ServiceResponse<PaymentProviderSessionResult>(false, "Provider is not configured.")
                    {
                        ResponseType = ServiceResponseType.Conflict,
                    })
                    : Task.FromResult(new ServiceResponse<PaymentProviderSessionResult>(true, "Session created.")
                    {
                        Payload = result,
                        ResponseType = ServiceResponseType.Success,
                    });
            }
        }
    }
}

namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
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
        public async Task StartAsync_CreatesAndResumesCheckoutSession_ForSameStoreAndCart()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 15m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);

            var first = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload!.Token!));
            var second = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(first.Payload!.CheckoutSessionId, second.Payload!.CheckoutSessionId);
            Assert.Equal(1, first.Payload.CheckoutVersion);
            Assert.Equal(CheckoutSessionStates.Draft, first.Payload.State);
            Assert.Equal(CheckoutSteps.Entry, first.Payload.CurrentStep);
            Assert.Empty(first.Payload.CompletedSteps);
            Assert.Equal(first.Payload.CartVersion, first.Payload.LastValidatedCartVersion);
            Assert.Single(context.CheckoutSessions);
        }

        [Fact]
        public async Task StartAsync_RejectsEmptyCart()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload!.Token!));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Cart is empty.", result.Message);
            Assert.Empty(context.CheckoutSessions);
        }

        [Fact]
        public async Task LoadAsync_IsStoreAndCartScoped()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            var foreignProduct = CreatePublishedProduct(otherStoreId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(foreignProduct.Id))
                .ReturnsAsync(foreignProduct);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var otherCart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var foreignCart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(otherStoreId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, otherCart.Payload!.Token!, product.Id));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(otherStoreId, foreignCart.Payload!.Token!, foreignProduct.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload!.Token!));

            var sameStoreWrongCart = await service.LoadAsync(new StorefrontCheckoutSessionRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                otherCart.Payload!.Token!));
            var wrongStore = await service.LoadAsync(new StorefrontCheckoutSessionRequest(
                otherStoreId,
                start.Payload.CheckoutSessionId,
                foreignCart.Payload!.Token!));

            Assert.False(sameStoreWrongCart.Success);
            Assert.Equal(ServiceResponseType.NotFound, sameStoreWrongCart.ResponseType);
            Assert.False(wrongStore.Success);
            Assert.Equal(ServiceResponseType.NotFound, wrongStore.ResponseType);
        }

        [Fact]
        public async Task LoadAsync_WhenExpired_MarksSessionExpiredAndRejectsResume()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload!.Token!));
            var session = context.CheckoutSessions.Single();
            session.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();

            var result = await service.LoadAsync(new StorefrontCheckoutSessionRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal(CheckoutSessionStates.Expired, session.State);
            Assert.Equal(2, session.CheckoutVersion);
            Assert.Equal(CheckoutSteps.Entry, session.CurrentStep);
        }

        [Fact]
        public async Task CancelAsync_MarksSessionCancelledAndIncrementsVersion()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload!.Token!));

            var cancel = await service.CancelAsync(new StorefrontCheckoutSessionRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!));
            var load = await service.LoadAsync(new StorefrontCheckoutSessionRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!));

            Assert.True(cancel.Success);
            Assert.Equal(CheckoutSessionStates.Cancelled, cancel.Payload!.State);
            Assert.False(cancel.Payload.IsActive);
            Assert.Equal(2, cancel.Payload.CheckoutVersion);
            Assert.False(load.Success);
            Assert.Equal(ServiceResponseType.Conflict, load.ResponseType);
        }

        [Fact]
        public async Task LoadAsync_WhenCartVersionChanged_ResetsDownstreamCheckoutState()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            await cartService.UpdateLineAsync(new StorefrontCartUpdateLineRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Lines.Single().Id,
                Quantity: 2));
            var result = await service.LoadAsync(new StorefrontCheckoutSessionRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!));

            Assert.True(result.Success);
            Assert.Equal(CheckoutSessionStates.Draft, result.Payload!.State);
            Assert.Equal(CheckoutSteps.Entry, result.Payload.CurrentStep);
            Assert.Empty(result.Payload.CompletedSteps);
            Assert.Equal(2, result.Payload.CheckoutVersion);
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "cart.version_changed");
            Assert.Equal(result.Payload.CartVersion, result.Payload.LastValidatedCartVersion);
        }

        [Fact]
        public async Task UpdateAddressesAsync_WithDirectAddresses_SnapshotsAndResetsPaymentStep()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            var address = CreateAddress();

            var result = await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: address,
                ShippingAddress: address,
                UseBillingAddressAsShippingAddress: true));

            Assert.True(result.Success, result.Message);
            Assert.Equal(2, result.Payload!.CheckoutVersion);
            Assert.Equal(CheckoutSteps.PaymentMethod, result.Payload.CurrentStep);
            Assert.Contains(CheckoutSteps.BillingAddress, result.Payload.CompletedSteps);
            Assert.Contains(CheckoutSteps.ShippingAddress, result.Payload.CompletedSteps);
            var session = context.CheckoutSessions.Single();
            Assert.NotNull(session.BillingAddressSnapshotJson);
            Assert.Equal("billing", session.ShippingAddressSource);
            Assert.Equal("100 Main St", session.ShippingAddress1);
            Assert.Equal(string.Empty, session.PaymentMethodKey);
        }

        [Fact]
        public async Task UpdateAddressesAsync_RejectsSavedAddressSelection_WhenCustomerIsAnonymous()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            var result = await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: null,
                ShippingAddress: null,
                BillingAddressId: Guid.NewGuid(),
                ShippingAddressId: Guid.NewGuid()));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Null(context.CheckoutSessions.Single().BillingAddressSnapshotJson);
        }

        [Fact]
        public async Task UpdateAddressesAsync_UsesSavedAddress_WhenCustomerOwnsIt()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var appUserId = "customer-app-user";
            var savedAddressId = Guid.NewGuid();
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                AppUserId = appUserId,
                Email = "saved@example.test",
                NormalizedEmail = "SAVED@EXAMPLE.TEST",
                FullName = "Saved Customer",
            };
            context.CommerceCustomers.Add(customer);
            context.CommerceCustomerAddresses.Add(new CommerceCustomerAddress
            {
                Id = Guid.NewGuid(),
                PublicId = savedAddressId,
                StoreId = storeId,
                CustomerId = customer.Id,
                Customer = customer,
                FirstName = "Saved",
                LastName = "Customer",
                Address1 = "200 Saved St",
                City = "New York",
                PostalCode = "10002",
                CountryCode = "US",
                StateProvinceCode = "NY",
                Email = "saved@example.test",
            });
            await context.SaveChangesAsync();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            var result = await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: null,
                ShippingAddress: null,
                BillingAddressId: savedAddressId,
                ShippingAddressId: savedAddressId,
                CustomerAppUserId: appUserId));

            Assert.True(result.Success, result.Message);
            var session = context.CheckoutSessions.Single();
            Assert.Equal("saved", session.ShippingAddressSource);
            Assert.Equal("200 Saved St", session.ShippingAddress1);
            Assert.Contains("200 Saved St", session.BillingAddressSnapshotJson, StringComparison.Ordinal);
        }

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
        public async Task PreviewAsync_WhenPaymentMethodUnavailableForCountry_ReturnsValidationIssue()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(
                context,
                storeId,
                configure: method => method.SupportedCountryCodesJson = "[\"CA\"]");
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
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version, countryCode: "US"));

            Assert.True(result.Success);
            Assert.NotNull(result.Payload);
            Assert.False(result.Payload!.IsValid);
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "payment.method_unavailable");
            Assert.Equal("review", result.Payload.NextAction);
        }

        [Fact]
        public async Task PreviewAsync_UsesStoreDefaultCurrency_WhenCartLineSnapshotDiffers()
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
                Quantity: 1,
                CurrencyCode: "eur"));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            Assert.True(result.Success);
            Assert.NotNull(result.Payload);
            Assert.Equal("USD", result.Payload!.CurrencyCode);
            Assert.All(result.Payload.Lines, line => Assert.Equal("USD", line.CurrencyCode));
        }

        [Fact]
        public async Task PreviewAsync_KeepsDeliveryMetadataDisplayOnly()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ShippingRequired = false;
            product.FreeShipping = true;
            product.DeliveryEstimateText = "Ships in 2 days";
            product.Weight = 1.25m;
            product.Length = 10.5m;
            product.Width = 5.25m;
            product.Height = 2.75m;
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

            var result = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Payload);
            Assert.Equal(40m, result.Payload!.Subtotal);
            Assert.Equal(0m, result.Payload.ShippingTotal);
            Assert.Equal(40m, result.Payload.GrandTotal);
            Assert.Equal(1, result.Payload.CheckoutVersion);
            Assert.Equal(result.Payload.CartVersion, result.Payload.LastValidatedCartVersion);
            Assert.Equal(CheckoutSteps.Review, result.Payload.CurrentStep);
            Assert.Contains(CheckoutSteps.ShippingAddress, result.Payload.CompletedSteps);
            Assert.Contains(CheckoutSteps.PaymentMethod, result.Payload.CompletedSteps);
            var session = Assert.Single(context.CheckoutSessions);
            Assert.Equal(0m, session.ShippingTotal);
            Assert.Equal(session.Subtotal, session.GrandTotal);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenCartUsesConvertedCurrency_UsesSnapshotCurrencyForOrderAndPayment()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(
                context,
                storeId,
                configure: method => method.SupportedCurrencyCodesJson = "[\"EUR\"]");
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 10m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(
                context,
                productRepository,
                workingCurrencyCode: "EUR",
                conversionTargetCurrencyCode: "EUR",
                conversionRate: 0.9m);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1,
                CurrencyCode: "eur"));
            var service = CreateCheckoutService(context, cartService);

            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));
            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "converted-currency-order"));

            Assert.True(preview.Success, preview.Message);
            Assert.Equal("EUR", preview.Payload!.CurrencyCode);
            Assert.Equal(9.00m, preview.Payload.GrandTotal);
            Assert.All(preview.Payload.Lines, line => Assert.Equal("EUR", line.CurrencyCode));
            Assert.True(result.Success, result.Message);
            Assert.Equal("EUR", result.Payload!.CurrencyCode);
            Assert.Equal(9.00m, result.Payload.TotalAmount);
            var order = Assert.Single(context.Orders);
            Assert.Equal("EUR", order.CurrencyCode);
            Assert.Equal(9.00m, order.TotalAmount);
            Assert.Equal("USD", order.BaseCurrencyCode);
            Assert.Equal(10.00m, order.BaseTotalAmount);
            Assert.Equal(0.9m, order.ExchangeRate);
            Assert.Equal("manual", order.ExchangeRateProviderKey);
            Assert.Equal("test-rate", order.ExchangeRateSource);
            Assert.NotNull(order.ExchangeRateEffectiveAtUtc);
            var orderLine = Assert.Single(order.Lines);
            Assert.Equal("EUR", orderLine.CurrencyCode);
            Assert.Equal(10.00m, orderLine.BaseUnitPrice);
            Assert.Equal(9.00m, orderLine.ConvertedUnitPrice);
            Assert.Equal(9.00m, orderLine.LineTotal);
            Assert.Equal(10.00m, orderLine.BaseLineTotal);
            var attempt = Assert.Single(context.PaymentAttempts);
            Assert.Equal("EUR", attempt.CurrencyCode);
            Assert.Equal(9.00m, attempt.Amount);
            Assert.Equal("USD", attempt.BaseCurrencyCode);
            Assert.Equal(10.00m, attempt.BaseAmount);
            Assert.Equal(0.9m, attempt.ExchangeRate);
            Assert.Equal("manual", attempt.ExchangeRateProviderKey);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenPaymentMethodUnavailableForTotal_RejectsOrder()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
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
            var method = await context.StorePaymentMethods.SingleAsync(item => item.StoreId == storeId && item.PaymentMethodKey == PaymentMethodKeys.Cod);
            method.MinOrderTotal = 50m;
            await context.SaveChangesAsync();

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "payment-unavailable-total"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("Payment method is not available.", result.Message);
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
        public async Task PlaceOrderAsync_CopiesCheckoutAddressSnapshotAndDoesNotReadMutatedCustomerProfile()
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
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Version,
                customerEmail: "snapshot@example.test",
                shippingEmail: "ship-to@example.test",
                postalCode: "75001",
                countryCode: "fr"));

            Assert.True(preview.Success, preview.Message);
            var session = Assert.Single(context.CheckoutSessions);
            Assert.Equal("snapshot@example.test", session.CustomerEmail);
            Assert.Equal("Customer One", session.ShippingFullName);
            Assert.Equal("ship-to@example.test", session.ShippingEmail);
            Assert.Equal("100 Main St", session.ShippingAddress1);
            Assert.Equal("New York", session.ShippingCity);
            Assert.Equal("NY", session.ShippingState);
            Assert.Equal("75001", session.ShippingPostalCode);
            Assert.Equal("FR", session.ShippingCountryCode);

            var customer = Assert.Single(context.CommerceCustomers);
            customer.FullName = "Mutated Customer";
            customer.Email = "mutated@example.test";
            customer.Phone = "999999";
            await context.SaveChangesAsync();

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "address-snapshot-order"));

            Assert.True(result.Success, result.Message);
            var order = Assert.Single(context.Orders);
            Assert.Equal("snapshot@example.test", order.CustomerEmail);
            Assert.Equal("Customer One", order.CustomerName);
            Assert.Equal("Customer One", order.ShippingFullName);
            Assert.Equal("ship-to@example.test", order.ShippingEmail);
            Assert.Equal("5550100", order.ShippingPhone);
            Assert.Equal("100 Main St", order.ShippingAddress1);
            Assert.Equal("New York", order.ShippingCity);
            Assert.Equal("NY", order.ShippingState);
            Assert.Equal("75001", order.ShippingPostalCode);
            Assert.Equal("FR", order.ShippingCountryCode);
        }

        [Fact]
        public async Task PlaceOrderAsync_UsesSavedShippingAddressAndSnapshotsIt()
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
                Quantity: 1));
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                AppUserId = "customer-user-1",
                Email = "customer@example.test",
                NormalizedEmail = "CUSTOMER@EXAMPLE.TEST",
                FullName = "Customer One",
            };
            var address = new CommerceCustomerAddress
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                CustomerId = customer.Id,
                FirstName = "Saved",
                LastName = "Customer",
                Address1 = "500 Saved Street",
                City = "Boston",
                PostalCode = "02108",
                CountryCode = "US",
                StateProvinceCode = "MA",
                Email = "saved@example.test",
                Phone = "5551111",
                IsDefaultShipping = true,
            };
            context.CommerceCustomers.Add(customer);
            context.CommerceCustomerAddresses.Add(address);
            context.SaveChanges();
            var service = CreateCheckoutService(context, cartService);
            var previewRequest = CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version) with
            {
                ShippingAddress = null,
                ShippingAddressId = address.PublicId,
                CustomerAppUserId = customer.AppUserId,
            };

            var preview = await service.PreviewAsync(previewRequest);
            var session = Assert.Single(context.CheckoutSessions);
            address.Address1 = "Mutated Saved Street";
            await context.SaveChangesAsync();
            var placeOrder = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                add.Payload.Version,
                "saved-address-order"));

            Assert.True(preview.Success, preview.Message);
            Assert.True(preview.Payload!.IsValid);
            Assert.Equal("Saved Customer", session.ShippingFullName);
            Assert.Equal("saved@example.test", session.ShippingEmail);
            Assert.Equal("500 Saved Street", session.ShippingAddress1);
            Assert.True(placeOrder.Success, placeOrder.Message);
            var order = Assert.Single(context.Orders);
            Assert.Equal("Saved Customer", order.ShippingFullName);
            Assert.Equal("500 Saved Street", order.ShippingAddress1);
            Assert.Equal("Boston", order.ShippingCity);
        }

        [Fact]
        public async Task PreviewAsync_RejectsSavedAddressSelection_WhenCustomerIsAnonymous()
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
                Quantity: 1));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version) with
            {
                ShippingAddress = null,
                ShippingAddressId = Guid.NewGuid(),
            });

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.IsValid);
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "shipping.address_auth_required");
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
        public async Task PlaceOrderAsync_WhenCheckoutSessionExpired_BlocksOrderAndMarksExpired()
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
            var session = context.CheckoutSessions.Single();
            session.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "expired-checkout"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("Checkout session has expired.", result.Message);
            Assert.Empty(context.Orders);
            Assert.Equal(CheckoutSessionStates.Expired, session.State);
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
        public async Task PreviewAsync_ReturnsSellabilityIssue_WhenProductPurchaseIsDisabledAfterAdd()
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
            product.PurchasingDisabled = true;
            product.PurchasingDisabledReason = "Paused";

            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            Assert.True(preview.Success);
            Assert.False(preview.Payload!.IsValid);
            Assert.Contains(preview.Payload.Issues, issue => issue.Code == ProductPurchaseBlockReasons.PurchaseDisabled);
        }

        [Fact]
        public async Task PlaceOrderAsync_AllowsUnmanagedStockProductWithoutDeductingQuantity()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 15m, stock: 0);
            product.ManageStock = false;
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

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CartVersion,
                "unmanaged-stock-order"));

            Assert.True(result.Success, result.Message);
            Assert.Single(context.Orders);
            Assert.Equal(0, context.Products.Single(item => item.Id == product.Id).Quantity);
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
                new FixedStoreCurrencyResolver("USD"),
                new MoneyRoundingService(new CurrencyMetadataService()),
                new StorefrontCustomerService(context),
                new StubStoreFeatureStateService(checkoutEnabled),
                new PaymentHandlerResolver([new CodPaymentHandler()]),
                new StorefrontPaymentProviderResolver(providers));
        }

        private static StorefrontCartService CreateCartService(
            CommerceNodeDbContext context,
            Mock<IProductReadRepository> productRepository,
            string workingCurrencyCode = "USD",
            string? conversionTargetCurrencyCode = null,
            decimal conversionRate = 1m)
        {
            return new StorefrontCartService(
                new StorefrontCartSessionService(context),
                productRepository.Object,
                new FixedStoreCurrencyResolver("USD"),
                new FixedWorkingCurrencyResolver(workingCurrencyCode, "USD"),
                new FakeMoneyConversionService(conversionTargetCurrencyCode, conversionRate),
                new MoneyRoundingService(new CurrencyMetadataService()));
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

        private static StorefrontCheckoutShippingAddressDto CreateAddress(
            string email = "customer@example.test",
            string address1 = "100 Main St",
            string postalCode = "10001",
            string countryCode = "US")
        {
            return new StorefrontCheckoutShippingAddressDto(
                "Customer One",
                email,
                "5550100",
                address1,
                null,
                "New York",
                "NY",
                postalCode,
                countryCode);
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
            string paymentMethodKey = PaymentMethodKeys.Cod,
            Action<StorePaymentMethod>? configure = null)
        {
            var method = new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = paymentMethodKey,
                DisplayName = paymentMethodKey,
                Enabled = true,
                DisplayOrder = 10,
            };
            configure?.Invoke(method);

            context.StorePaymentMethods.Add(method);
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

        private sealed class FixedStoreCurrencyResolver : IStoreCurrencyResolver
        {
            private readonly string defaultCurrencyCode;

            public FixedStoreCurrencyResolver(string defaultCurrencyCode)
            {
                this.defaultCurrencyCode = defaultCurrencyCode;
            }

            public Task<string> ResolveDefaultCurrencyCodeAsync(
                Guid storeId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.defaultCurrencyCode);
            }
        }

        private sealed class FixedWorkingCurrencyResolver : IStorefrontWorkingCurrencyResolver
        {
            private readonly string currencyCode;
            private readonly string baseCurrencyCode;

            public FixedWorkingCurrencyResolver(string currencyCode, string? baseCurrencyCode = null)
            {
                this.currencyCode = currencyCode;
                this.baseCurrencyCode = baseCurrencyCode ?? currencyCode;
            }

            public Task<StorefrontWorkingCurrencyResolution> ResolveAsync(
                Guid storeId,
                string? requestedCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new StorefrontWorkingCurrencyResolution(
                    this.currencyCode,
                    this.baseCurrencyCode,
                    requestedCurrencyCode,
                    RequestedCurrencySupported: string.Equals(requestedCurrencyCode, this.currencyCode, StringComparison.OrdinalIgnoreCase),
                    CheckoutCurrencyEnabled: true,
                    Reason: "test"));
            }
        }

        private sealed class FakeMoneyConversionService : IMoneyConversionService
        {
            private readonly string? targetCurrencyCode;
            private readonly decimal rate;

            public FakeMoneyConversionService(string? targetCurrencyCode = null, decimal rate = 1m)
            {
                this.targetCurrencyCode = targetCurrencyCode;
                this.rate = rate;
            }

            public Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
                Guid storeId,
                decimal amount,
                string targetCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                if (!string.Equals(targetCurrencyCode, this.targetCurrencyCode, StringComparison.Ordinal))
                {
                    return Task.FromResult(new ServiceResponse<MoneyConversionResult>(false, "No active exchange rate is configured.")
                    {
                        ResponseType = ServiceResponseType.Conflict,
                    });
                }

                return Task.FromResult(new ServiceResponse<MoneyConversionResult>(true, "Currency conversion resolved.")
                {
                    Payload = new MoneyConversionResult(
                        amount,
                        "USD",
                        amount * this.rate,
                        targetCurrencyCode,
                        this.rate,
                        DateTimeOffset.UtcNow,
                        null,
                        "manual",
                        "test-rate"),
                    ResponseType = ServiceResponseType.Success,
                });
            }
        }
    }
}

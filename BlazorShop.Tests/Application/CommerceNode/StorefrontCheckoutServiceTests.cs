namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services.Contracts.Payment;
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
        public void StorefrontCheckoutService_DoesNotDependOnLegacyPayPalCaptureService()
        {
            var constructorParameterTypes = typeof(StorefrontCheckoutService)
                .GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.DoesNotContain(typeof(IPayPalPaymentService), constructorParameterTypes);
        }

        [Fact]
        public void StorefrontCheckoutService_CapturedPaymentPlacementUsesExecutionStrategyTransaction()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCheckoutService.cs");
            var strategyIndex = source.IndexOf("this.context.Database.CreateExecutionStrategy()", StringComparison.Ordinal);
            var transactionIndex = source.IndexOf("this.context.Database.BeginTransactionAsync(cancellationToken)", StringComparison.Ordinal);

            Assert.True(strategyIndex >= 0, "Captured checkout placement must create an EF execution strategy before starting a transaction.");
            Assert.True(transactionIndex > strategyIndex, "BeginTransactionAsync must run inside the execution strategy block.");
            Assert.Contains("return await executionStrategy.ExecuteAsync(async () =>", source, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCheckoutService_ConstructorRequiresDiAndDoesNotBuildFallbackDependencies()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCheckoutService.cs");
            var constructor = Assert.Single(typeof(StorefrontCheckoutService).GetConstructors());
            var parameters = constructor.GetParameters().ToDictionary(parameter => parameter.Name!, StringComparer.Ordinal);

            Assert.False(parameters["sellabilityResolver"].HasDefaultValue);
            Assert.False(parameters["addressValidationService"].HasDefaultValue);
            Assert.False(parameters["shippingCalculator"].HasDefaultValue);
            Assert.False(parameters["shippingTaxCalculator"].HasDefaultValue);
            Assert.False(parameters["orderPlacementService"].HasDefaultValue);
            Assert.False(parameters["paymentCoordinator"].HasDefaultValue);
            Assert.Equal(typeof(IProductSellabilityResolver), parameters["sellabilityResolver"].ParameterType);
            Assert.Equal(typeof(IAddressValidationService), parameters["addressValidationService"].ParameterType);
            Assert.Equal(typeof(IShippingCalculator), parameters["shippingCalculator"].ParameterType);
            Assert.Equal(typeof(IShippingTaxCalculator), parameters["shippingTaxCalculator"].ParameterType);
            Assert.Equal(typeof(IOrderPlacementService), parameters["orderPlacementService"].ParameterType);
            Assert.Equal(typeof(CheckoutPaymentCoordinator), parameters["paymentCoordinator"].ParameterType);
            Assert.DoesNotContain("IProductSellabilityResolver? sellabilityResolver = null", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IAddressValidationService? addressValidationService = null", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IShippingCalculator? shippingCalculator = null", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IShippingTaxCalculator? shippingTaxCalculator = null", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IOrderPlacementService? orderPlacementService = null", source, StringComparison.Ordinal);
            Assert.DoesNotContain("sellabilityResolver ?? new ProductSellabilityResolver()", source, StringComparison.Ordinal);
            Assert.DoesNotContain("addressValidationService ?? new AddressValidationService()", source, StringComparison.Ordinal);
            Assert.DoesNotContain("shippingCalculator ?? new ShippingCalculator", source, StringComparison.Ordinal);
            Assert.DoesNotContain("shippingTaxCalculator ?? new ZeroShippingTaxCalculator()", source, StringComparison.Ordinal);
            Assert.DoesNotContain("orderPlacementService ?? new OrderPlacementService", source, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeDi_RegistersCheckoutDependenciesRequiredForRequiredDiCutover()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs");

            Assert.Contains("AddScoped<IAddressValidationService, AddressValidationService>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IProductSellabilityResolver, ProductSellabilityResolver>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IShippingCalculator, ShippingCalculator>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IShippingTaxCalculator, ZeroShippingTaxCalculator>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<CheckoutPricingCalculator>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<CheckoutPaymentCoordinator>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IOrderPlacementService, OrderPlacementService>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCheckoutService, StorefrontCheckoutService>", source, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutAndOrderPlacement_OrderLineValidationRulesStayAlignedWhileValidationRunsTwice()
        {
            var checkoutSource = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCheckoutService.cs");
            var placementSource = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/OrderPlacementService.cs");
            var sharedValidationMessages = new[]
            {
                "Cart line quantity must be at least 1.",
                "Product is not available for this store.",
                "Selected product variant was not found.",
                "Product cannot be purchased right now.",
                "Cart line currency does not match checkout currency.",
                "Cart line price is invalid.",
            };

            foreach (var message in sharedValidationMessages)
            {
                Assert.Contains(message, checkoutSource, StringComparison.Ordinal);
                Assert.Contains(message, placementSource, StringComparison.Ordinal);
            }

            Assert.Contains("var lineResolution = await this.ResolveOrderLinesAsync", checkoutSource, StringComparison.Ordinal);
            Assert.Contains("var lines = await this.ResolveOrderLinesAsync", placementSource, StringComparison.Ordinal);
            Assert.Contains("this.orderPlacementService.PlaceAsync", checkoutSource, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutServiceTestBuilder_CanOverrideShippingCalculator()
        {
            using var context = CreateContext();
            var cartService = CreateCartService(context, new Mock<IProductReadRepository>());
            var shippingCalculator = new FakeShippingCalculator(_ => new ShippingCalculationResult(false, [], [], []));

            var service = new CheckoutServiceTestBuilder(context, cartService)
                .WithShippingCalculator(shippingCalculator)
                .Build();

            Assert.Same(shippingCalculator, GetPrivateField<IShippingCalculator>(service, "shippingCalculator"));
        }

        [Fact]
        public void CheckoutServiceTestBuilder_CanOverrideOrderPlacementService()
        {
            using var context = CreateContext();
            var cartService = CreateCartService(context, new Mock<IProductReadRepository>());
            var orderPlacementService = new FakeOrderPlacementService();

            var service = new CheckoutServiceTestBuilder(context, cartService)
                .WithOrderPlacementService(orderPlacementService)
                .Build();

            Assert.Same(orderPlacementService, GetPrivateField<IOrderPlacementService>(service, "orderPlacementService"));
        }

        [Fact]
        public void CommerceNodeOrderTrackingService_DoesNotSendEmailSynchronously()
        {
            var constructorParameterTypes = typeof(CommerceNodeOrderTrackingService)
                .GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.DoesNotContain(typeof(IEmailService), constructorParameterTypes);
        }

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
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
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
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
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
            Assert.Equal(40m, result.Payload.Subtotal);
            Assert.Equal(40m, result.Payload.GrandTotal);
        }

        [Fact]
        public async Task StartAsync_WhenCartVersionChanged_ResetsSessionTotalsBeforeReturning()
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
            var first = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            await cartService.UpdateLineAsync(new StorefrontCartUpdateLineRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Lines.Single().Id,
                Quantity: 2));
            var resumed = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            Assert.True(first.Success);
            Assert.True(resumed.Success);
            Assert.Equal(first.Payload!.CheckoutSessionId, resumed.Payload!.CheckoutSessionId);
            Assert.Equal(2, resumed.Payload.CheckoutVersion);
            Assert.Equal(resumed.Payload.CartVersion, resumed.Payload.LastValidatedCartVersion);
            Assert.Equal(40m, resumed.Payload.Subtotal);
            Assert.Equal(0m, resumed.Payload.ShippingTotal);
            Assert.Equal(0m, resumed.Payload.TaxTotal);
            Assert.Equal(40m, resumed.Payload.GrandTotal);
            Assert.Single(resumed.Payload.Lines);
            Assert.Equal(2, resumed.Payload.Lines.Single().Quantity);
            Assert.Contains(resumed.Payload.Issues, issue => issue.Code == "cart.version_changed");
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
            Assert.Equal(CheckoutSteps.ShippingMethod, result.Payload.CurrentStep);
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
            Assert.Equal(customer.Id, session.CustomerId);
            Assert.Equal("saved@example.test", session.CustomerEmail);
            Assert.Equal("Saved Customer", session.CustomerName);
            Assert.Equal("200 Saved St", session.ShippingAddress1);
            Assert.Contains("200 Saved St", session.BillingAddressSnapshotJson, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_SelectsFreeStandardAndResetsPayment()
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
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(CheckoutSteps.PaymentMethod, result.Payload!.CurrentStep);
            Assert.Contains(CheckoutSteps.ShippingMethod, result.Payload.CompletedSteps);
            Assert.True(result.Payload.ShippingRequired);
            var option = Assert.Single(result.Payload.ShippingOptions);
            Assert.Equal("free_standard", option.Key);
            Assert.True(option.Selected);
            Assert.Equal(option, result.Payload.SelectedShippingOption);
            Assert.Equal(0m, result.Payload.ShippingTotal);
            Assert.Equal(result.Payload.Subtotal, result.Payload.GrandTotal);
            Assert.Contains("free_standard", context.CheckoutSessions.Single().SelectedShippingOptionJson, StringComparison.Ordinal);
        }

        [Fact]
        public async Task StartAsync_CurrentShippingBaselineAlwaysRequiresShippingAndOffersFreeStandard()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ShippingRequired = false;
            product.FreeShipping = true;
            product.DeliveryEstimateText = "Digital delivery";
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            Assert.True(result.Success, result.Message);
            Assert.True(result.Payload!.ShippingRequired);
            var option = Assert.Single(result.Payload.ShippingOptions);
            Assert.Equal("free_standard", option.Key);
            Assert.Equal("USD", option.CurrencyCode);
            Assert.Equal(0m, option.Price);
            Assert.Null(result.Payload.SelectedShippingOption);
        }

        [Fact]
        public async Task StartAsync_ComputesShippingRequiredFromPersistedProductMetadata()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ShippingRequired = false;
            product.FreeShipping = true;
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);

            var result = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.ShippingRequired);
            Assert.Empty(result.Payload.ShippingOptions);
            Assert.Null(result.Payload.SelectedShippingOption);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_UsesCalculatedRateAndUpdatesGrandTotal()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var shippingCalculator = new FakeShippingCalculator(_ => new ShippingCalculationResult(
                ShippingRequired: true,
                Options:
                [
                    new ShippingOptionDto(
                        "ground",
                        "test",
                        "ground",
                        "Ground",
                        "Ground shipping",
                        7.25m,
                        "USD",
                        "3-5 days",
                        [],
                        [],
                        "test.ground"),
                ],
                Warnings: [],
                Errors: []));
            var service = CreateCheckoutService(context, cartService, shippingCalculator);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "ground"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(7.25m, result.Payload!.ShippingTotal);
            Assert.Equal(27.25m, result.Payload.GrandTotal);
            Assert.Equal("ground", result.Payload.SelectedShippingOption!.Key);
            Assert.True(result.Payload.SelectedShippingOption.Selected);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_AppliesPersistedProductShippingSurcharge()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ShippingRequired = true;
            product.FreeShipping = false;
            product.ShippingSurcharge = 3m;
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 2));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(6m, result.Payload!.ShippingTotal);
            Assert.Equal(46m, result.Payload.GrandTotal);
            Assert.Equal("free_standard", result.Payload.SelectedShippingOption!.Key);
            Assert.Equal(6m, result.Payload.SelectedShippingOption.Price);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_ConvertsBaseShippingRateToCheckoutCurrency()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 10m, stock: 10);
            product.ShippingRequired = true;
            product.FreeShipping = false;
            product.ShippingSurcharge = 2m;
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
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1,
                CurrencyCode: "eur"));
            var service = CreateCheckoutService(
                context,
                cartService,
                shippingCalculator: null,
                moneyConversionService: new FakeMoneyConversionService("EUR", 0.9m));
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            Assert.True(result.Success, result.Message);
            Assert.Equal("EUR", result.Payload!.CurrencyCode);
            Assert.Equal(9m, result.Payload.Subtotal);
            Assert.Equal(1.8m, result.Payload.ShippingTotal);
            Assert.Equal(10.8m, result.Payload.GrandTotal);
            Assert.Equal("EUR", result.Payload.SelectedShippingOption!.CurrencyCode);
            Assert.Equal(1.8m, result.Payload.SelectedShippingOption.Price);
            Assert.Equal(0m, result.Payload.TaxTotal);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_WhenShippingRateConversionMissing_ReturnsConflict()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 10m, stock: 10);
            product.ShippingRequired = true;
            product.FreeShipping = false;
            product.ShippingSurcharge = 2m;
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
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1,
                CurrencyCode: "eur"));
            var service = CreateCheckoutService(
                context,
                cartService,
                shippingCalculator: null,
                moneyConversionService: new FakeMoneyConversionService());
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("No active exchange rate is configured.", result.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_IncludesSelectedShippingTotalInOrderAndPaymentAmount()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var storePublicId = Guid.NewGuid();
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                PublicId = storePublicId,
                StoreKey = "snapshot-store",
                Name = "Snapshot Store",
                Status = CommerceStoreStatuses.Active,
                BaseUrl = "https://snapshot.example.test",
                CompanyName = "Snapshot LLC",
                CompanyEmail = "support@snapshot.example.test",
                CompanyPhone = "+15550100",
                CompanyAddress = "1 Snapshot Way",
            });
            context.SaveChanges();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var shippingCalculator = new FakeShippingCalculator(_ => new ShippingCalculationResult(
                ShippingRequired: true,
                Options:
                [
                    new ShippingOptionDto(
                        "ground",
                        "test",
                        "ground",
                        "Ground",
                        "Ground shipping",
                        7.25m,
                        "USD",
                        "3-5 days",
                        [],
                        [],
                        "test.ground"),
                ],
                Warnings: [],
                Errors: []));
            var service = CreateCheckoutService(context, cartService, shippingCalculator);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            var address = await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));
            var shipping = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "ground"));
            var payment = await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));
            var review = await service.ReviewAsync(new StorefrontCheckoutReviewRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                TermsAccepted: false,
                TermsVersion: null));

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                review.Payload!.CheckoutVersion,
                payment.Payload!.CartVersion,
                "ship-total-cod"));

            Assert.True(address.Success, address.Message);
            Assert.True(shipping.Success, shipping.Message);
            Assert.True(payment.Success, payment.Message);
            Assert.True(review.Success, review.Message);
            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Payload!.GuestAccessToken);
            Assert.Equal(64, result.Payload.GuestAccessToken!.Length);
            var order = Assert.Single(context.Orders);
            var attempt = Assert.Single(context.PaymentAttempts);
            Assert.NotNull(order.GuestAccessTokenHash);
            Assert.NotEqual(result.Payload.GuestAccessToken, order.GuestAccessTokenHash);
            Assert.True(order.GuestAccessTokenExpiresAtUtc > DateTimeOffset.UtcNow);
            Assert.Equal(27.25m, order.TotalAmount);
            Assert.Equal("ground", order.ShippingMethodKey);
            Assert.Equal("test", order.ShippingProviderSystemName);
            Assert.Equal("ground", order.ShippingMethodCode);
            Assert.Equal("Ground", order.ShippingMethodName);
            Assert.Equal(7.25m, order.ShippingTotal);
            Assert.Equal("USD", order.ShippingCurrencyCode);
            Assert.Equal("3-5 days", order.ShippingDeliveryEstimateText);
            Assert.Equal(ShippingStatuses.NotYetShipped, order.ShippingStatus);
            Assert.Equal(27.25m, attempt.Amount);
            Assert.Equal(
                ["order.created", "payment.captured"],
                context.OrderHistoryEntries.Select(item => item.EventType).OrderBy(item => item).ToArray());
            Assert.All(context.OrderHistoryEntries, item => Assert.True(item.VisibleToCustomer));
            Assert.Equal(7.25m, context.CheckoutSessions.Single().ShippingTotal);
            Assert.Equal(storePublicId, order.StorePublicId);
            Assert.Equal("snapshot-store", order.StoreKeySnapshot);
            Assert.Equal("Snapshot Store", order.StoreNameSnapshot);
            Assert.Equal("https://snapshot.example.test", order.StoreBaseUrlSnapshot);
            Assert.Equal("Snapshot LLC", order.StoreCompanyNameSnapshot);
            Assert.Equal("support@snapshot.example.test", order.StoreCompanyEmailSnapshot);
            Assert.Equal("+15550100", order.StoreCompanyPhoneSnapshot);
            Assert.Equal("1 Snapshot Way", order.StoreCompanyAddressSnapshot);
            Assert.Equal(20m, order.SubtotalAmount);
            Assert.Equal(7.25m, order.ShippingTotalAmount);
            Assert.Equal(0m, order.TaxTotalAmount);
            Assert.Equal(0m, order.DiscountTotalAmount);
            Assert.Equal(27.25m, order.GrandTotalAmount);
            Assert.Contains("\"address1\":\"100 Main St\"", order.BillingAddressSnapshotJson);
            Assert.Contains("\"address1\":\"100 Main St\"", order.ShippingAddressSnapshotJson);
            Assert.Contains("\"displayName\":\"Ground\"", order.ShippingMethodSnapshotJson);

            var persistedStore = context.CommerceStores.Single(item => item.Id == storeId);
            persistedStore.Name = "Changed Store";
            persistedStore.CompanyEmail = "changed@example.test";
            var checkout = context.CheckoutSessions.Single();
            checkout.BillingAddressSnapshotJson = checkout.BillingAddressSnapshotJson!.Replace("100 Main St", "Changed Billing", StringComparison.Ordinal);
            checkout.ShippingAddress1 = "Changed Shipping";
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var persistedOrder = context.Orders.AsNoTracking().Single();
            Assert.Equal("Snapshot Store", persistedOrder.StoreNameSnapshot);
            Assert.Equal("support@snapshot.example.test", persistedOrder.StoreCompanyEmailSnapshot);
            Assert.Contains("\"address1\":\"100 Main St\"", persistedOrder.BillingAddressSnapshotJson);
            Assert.Contains("\"address1\":\"100 Main St\"", persistedOrder.ShippingAddressSnapshotJson);

            var guestOrderService = new StorefrontGuestOrderService(
                context,
                new FixedStoreContext(storeId),
                new OrderReadModelAssembler(context));
            var lookup = await guestOrderService.GetAsync(new BlazorShop.Application.CommerceNode.Orders.StorefrontGuestOrderLookupRequest(
                persistedOrder.Reference,
                result.Payload.GuestAccessToken));
            var wrongToken = await guestOrderService.GetAsync(new BlazorShop.Application.CommerceNode.Orders.StorefrontGuestOrderLookupRequest(
                persistedOrder.Reference,
                "wrong-token"));
            var wrongStore = await new StorefrontGuestOrderService(
                    context,
                    new FixedStoreContext(Guid.NewGuid()),
                    new OrderReadModelAssembler(context))
                .GetAsync(
                new BlazorShop.Application.CommerceNode.Orders.StorefrontGuestOrderLookupRequest(
                    persistedOrder.Reference,
                    result.Payload.GuestAccessToken));

            Assert.True(lookup.Success, lookup.Message);
            Assert.Equal(persistedOrder.Reference, lookup.Payload!.Reference);
            Assert.NotNull(lookup.Payload.PaymentSummary);
            Assert.Equal(PaymentStatuses.Paid, lookup.Payload.PaymentSummary.PaymentStatus);
            Assert.Equal(PaymentMethodKeys.Cod, lookup.Payload.PaymentSummary.PaymentMethodKey);
            Assert.Equal(PaymentAttemptStates.Captured, lookup.Payload.PaymentSummary.AttemptState);
            Assert.Equal(27.25m, lookup.Payload.PaymentSummary.Amount);
            Assert.Equal("USD", lookup.Payload.PaymentSummary.CurrencyCode);
            Assert.Equal(
                ["order.created", "payment.captured"],
                lookup.Payload.HistoryEntries.Select(item => item.EventType).OrderBy(item => item).ToArray());
            Assert.All(lookup.Payload.HistoryEntries, item => Assert.True(item.VisibleToCustomer));
            Assert.False(wrongToken.Success);
            Assert.Equal(ServiceResponseType.NotFound, wrongToken.ResponseType);
            Assert.False(wrongStore.Success);
            Assert.Equal(ServiceResponseType.NotFound, wrongStore.ResponseType);
        }

        [Fact]
        public async Task PlaceOrderAsync_NonShippingOrderSnapshotsShippingNotRequired()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ShippingRequired = false;
            product.FreeShipping = true;
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "non-shipping-order"));

            Assert.True(preview.Success, preview.Message);
            Assert.True(result.Success, result.Message);
            var order = Assert.Single(context.Orders);
            Assert.Equal(ShippingStatuses.ShippingNotRequired, order.ShippingStatus);
            Assert.Null(order.ShippingMethodKey);
            Assert.Equal(0m, order.ShippingTotal);
            Assert.Equal("USD", order.ShippingCurrencyCode);
        }

        [Fact]
        public async Task SelectPaymentMethodAsync_AllowsNonShippingCartWithoutSelectedShippingMethod()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId);
            var productRepository = new Mock<IProductReadRepository>();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ShippingRequired = false;
            SeedProduct(context, product);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cartService = CreateCartService(context, productRepository);
            var cart = await cartService.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));

            var result = await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.ShippingRequired);
            Assert.Null(result.Payload.SelectedShippingOption);
            Assert.Equal(CheckoutSteps.Review, result.Payload.CurrentStep);
        }

        [Fact]
        public async Task PreviewAsync_WhenShippingCalculatorReturnsError_AddsValidationIssue()
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
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var shippingCalculator = new FakeShippingCalculator(_ => new ShippingCalculationResult(
                ShippingRequired: true,
                Options: [],
                Warnings: [],
                Errors: ["Shipping is not available for the selected country."]));
            var service = CreateCheckoutService(context, cartService, shippingCalculator);

            var result = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.IsValid);
            Assert.Contains(result.Payload.Issues, issue => issue.Code == "shipping.option_unavailable");
            Assert.Equal(0m, result.Payload.ShippingTotal);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_RejectsUnknownShippingOption()
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
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "unknown_provider"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Shipping option is not available.", result.Message);
            Assert.Null(context.CheckoutSessions.Single().SelectedShippingOptionJson);
        }

        [Fact]
        public async Task SelectShippingMethodAsync_BlocksWhenShippingAddressMissing()
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

            var result = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Null(context.CheckoutSessions.Single().SelectedShippingOptionJson);
        }

        [Fact]
        public async Task SelectPaymentMethodAsync_SelectsAvailableMethodAfterShippingMethod()
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
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));
            await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            var result = await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));

            Assert.True(result.Success, result.Message);
            Assert.Equal(CheckoutSteps.Review, result.Payload!.CurrentStep);
            Assert.Contains(CheckoutSteps.PaymentMethod, result.Payload.CompletedSteps);
            Assert.Equal(PaymentMethodKeys.Cod, result.Payload.PaymentMethodKey);
            Assert.Equal(PaymentMethodKeys.Cod, result.Payload.SelectedPaymentMethod!.Key);
            Assert.Contains(result.Payload.PaymentMethods, method => method.Key == PaymentMethodKeys.Cod && method.Selected);
        }

        [Fact]
        public async Task SelectPaymentMethodAsync_ProjectsOnlyMethodsAllowedForCurrencyCountryAndTotal()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(
                context,
                storeId,
                configure: method =>
                {
                    method.DisplayName = "Cash";
                    method.Description = "Pay on delivery.";
                    method.ShortDisplayText = "COD";
                    method.IconUrl = "/media/assets/cod.svg";
                    method.SupportedCurrencyCodesJson = "[\"USD\"]";
                    method.SupportedCountryCodesJson = "[\"US\"]";
                    method.MinOrderTotal = 10m;
                    method.MaxOrderTotal = 40m;
                });
            SeedPaymentMethod(
                context,
                storeId,
                PaymentMethodKeys.Stripe,
                method =>
                {
                    method.SupportedCurrencyCodesJson = "[\"EUR\"]";
                    method.SupportedCountryCodesJson = "[\"CA\"]";
                    method.MinOrderTotal = 30m;
                });
            SeedPaymentMethod(
                context,
                storeId,
                PaymentMethodKeys.PayPal,
                method =>
                {
                    method.SupportedCurrencyCodesJson = "[\"USD\"]";
                    method.SupportedCountryCodesJson = "[\"US\"]";
                    method.MaxOrderTotal = 10m;
                });
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
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(countryCode: "US"),
                ShippingAddress: CreateAddress(countryCode: "US"),
                UseBillingAddressAsShippingAddress: true));
            var shipping = await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            var option = Assert.Single(shipping.Payload!.PaymentMethods);
            Assert.Equal(PaymentMethodKeys.Cod, option.Key);
            Assert.Equal("Cash", option.DisplayName);
            Assert.Equal("Pay on delivery.", option.Description);
            Assert.Equal("COD", option.ShortDisplayText);
            Assert.Equal("/media/assets/cod.svg", option.IconUrl);
            Assert.Equal(PaymentMethodKeys.Cod, option.ProviderKey);
            Assert.Equal("none", option.NextActionKind);

            var rejected = await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Stripe));
            Assert.False(rejected.Success);
            Assert.Equal(ServiceResponseType.ValidationError, rejected.ResponseType);

            var selected = await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));
            Assert.True(selected.Success, selected.Message);
            Assert.Single(selected.Payload!.PaymentMethods);
            Assert.Equal(PaymentMethodKeys.Cod, selected.Payload.SelectedPaymentMethod!.Key);
        }

        [Fact]
        public async Task SelectPaymentMethodAsync_RejectsUnavailableMethod()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId, configure: method => method.Enabled = false);
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
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));
            await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            var result = await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal(string.Empty, context.CheckoutSessions.Single().PaymentMethodKey);
        }

        [Fact]
        public async Task ReviewAsync_AfterPaymentMethodSelection_ReturnsReviewProjectionAndMarksReady()
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
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(fullName: "Billing Customer"),
                ShippingAddress: CreateAddress(fullName: "Shipping Customer"),
                UseBillingAddressAsShippingAddress: false));
            await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));
            await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));

            var result = await service.ReviewAsync(new StorefrontCheckoutReviewRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                TermsAccepted: true,
                TermsVersion: "terms-v1"));

            Assert.True(result.Success, result.Message);
            Assert.True(result.Payload!.PlaceOrderAllowed);
            Assert.Equal(CheckoutSessionStates.Ready, result.Payload.State);
            Assert.Equal(CheckoutSteps.Review, result.Payload.CurrentStep);
            Assert.Equal(CheckoutSteps.PlaceOrder, result.Payload.NextRequiredStep);
            Assert.Contains(CheckoutSteps.Review, result.Payload.CompletedSteps);
            Assert.Equal("Billing Customer", result.Payload.BillingAddress!.FullName);
            Assert.Equal("Shipping Customer", result.Payload.ShippingAddress!.FullName);
            Assert.Equal("free_standard", result.Payload.SelectedShippingOption!.Key);
            Assert.Equal(PaymentMethodKeys.Cod, result.Payload.SelectedPaymentMethod!.Key);
            Assert.Single(result.Payload.Lines);
            Assert.Equal(20m, result.Payload.Subtotal);
            Assert.Equal(20m, result.Payload.GrandTotal);
            Assert.Equal("USD", result.Payload.CurrencyCode);
            Assert.False(result.Payload.TermsRequired);
            Assert.True(result.Payload.TermsAccepted);
            Assert.Equal("terms-v1", result.Payload.TermsVersion);
            Assert.NotNull(result.Payload.TermsAcceptedAtUtc);
            Assert.Empty(result.Payload.Issues);
        }

        [Fact]
        public async Task ReviewAsync_WhenPaymentMethodMissing_BlocksPlaceOrder()
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
            var add = await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));
            await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));

            var review = await service.ReviewAsync(new StorefrontCheckoutReviewRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                TermsAccepted: false,
                TermsVersion: null));
            var placeOrder = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                review.Payload!.CheckoutVersion,
                add.Payload!.Version,
                "missing-payment-review"));

            Assert.True(review.Success, review.Message);
            Assert.False(review.Payload!.PlaceOrderAllowed);
            Assert.Equal(CheckoutSessionStates.Draft, review.Payload.State);
            Assert.Equal(CheckoutSteps.PaymentMethod, review.Payload.NextRequiredStep);
            Assert.Contains(review.Payload.Issues, issue => issue.Code == "payment.method_required");
            Assert.False(placeOrder.Success);
            Assert.Equal(ServiceResponseType.Conflict, placeOrder.ResponseType);
        }

        [Fact]
        public async Task SelectPaymentMethodAsync_ClearsTermsAcknowledgement()
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
            await cartService.AddLineAsync(new StorefrontCartAddLineRequest(storeId, cart.Payload!.Token!, product.Id));
            var service = CreateCheckoutService(context, cartService);
            var start = await service.StartAsync(new StorefrontCheckoutStartRequest(storeId, cart.Payload.Token!));
            await service.UpdateAddressesAsync(new StorefrontCheckoutAddressStepRequest(
                storeId,
                start.Payload!.CheckoutSessionId,
                cart.Payload.Token!,
                BillingAddress: CreateAddress(),
                ShippingAddress: CreateAddress(),
                UseBillingAddressAsShippingAddress: true));
            await service.SelectShippingMethodAsync(new StorefrontCheckoutShippingMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                "free_standard"));
            await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));
            var accepted = await service.ReviewAsync(new StorefrontCheckoutReviewRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                TermsAccepted: true,
                TermsVersion: "terms-v1"));
            Assert.True(accepted.Payload!.TermsAccepted);

            await service.SelectPaymentMethodAsync(new StorefrontCheckoutPaymentMethodRequest(
                storeId,
                start.Payload.CheckoutSessionId,
                cart.Payload.Token!,
                PaymentMethodKeys.Cod));

            var session = context.CheckoutSessions.Single();
            Assert.False(session.TermsAccepted);
            Assert.Null(session.TermsVersion);
            Assert.Null(session.TermsAcceptedAtUtc);
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "checkout-retry-key"));
            var second = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload.CheckoutSessionId,
                preview.Payload.CheckoutVersion,
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
            Assert.Contains(context.PaymentAttemptAuditLogs, audit =>
                audit.PaymentAttemptId == attempt.Id
                && audit.EventType == "payment_attempt.captured"
                && audit.NewState == PaymentAttemptStates.Captured);
            Assert.Single(context.OrderHistoryEntries.Where(item => item.EventType == "order.created"));
            Assert.Single(context.CommerceTasks.Where(item => item.TaskType == OrderPlacementTaskTypes.OrderCreated));
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "stale-cart-version"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
            Assert.Equal(CartSessionStates.Active, context.CartSessions.Single().State);
        }

        [Fact]
        public async Task PlaceOrderAsync_RejectsStaleCheckoutVersion()
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

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CheckoutVersion + 1,
                preview.Payload.CartVersion,
                "stale-checkout-version"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("Checkout version is stale.", result.Message);
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "unmanaged-stock-order"));

            Assert.True(result.Success, result.Message);
            Assert.Single(context.Orders);
            Assert.Equal(0, context.Products.Single(item => item.Id == product.Id).Quantity);
            Assert.Single(context.CommerceTasks.Where(item => item.TaskType == OrderPlacementTaskTypes.OrderCreated));
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenStockHookFails_DoesNotPersistPlacementSideEffects()
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
                Quantity: 2));
            var placementService = new OrderPlacementService(
                context,
                new MoneyRoundingService(new CurrencyMetadataService()),
                new ProductSellabilityResolver(),
                new FailingOrderStockAdjustmentHook());
            var service = CreateCheckoutService(
                context,
                cartService,
                checkoutEnabled: true,
                shippingCalculator: null,
                moneyConversionService: null,
                orderPlacementService: placementService);
            var preview = await service.PreviewAsync(CreateRequest(storeId, cart.Payload.Token!, add.Payload!.Version));

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "stock-hook-fails"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
            Assert.Empty(context.PaymentAttempts);
            Assert.Empty(context.OrderHistoryEntries);
            Assert.Empty(context.CommerceTasks);
            Assert.Equal(CartSessionStates.Active, context.CartSessions.Single().State);
            Assert.Equal(10, context.Products.Single(item => item.Id == product.Id).Quantity);
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
                preview.Payload.CheckoutVersion,
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
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "stripe-session-key"));
            var second = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload.CheckoutSessionId,
                preview.Payload.CheckoutVersion,
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
            Assert.Contains(context.PaymentAttemptAuditLogs, audit =>
                audit.PaymentAttemptId == attempt.Id
                && audit.EventType == "payment_attempt.requires_action"
                && audit.NewState == PaymentAttemptStates.RequiresAction);
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
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "stripe-missing-config"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
            var attempt = context.PaymentAttempts.Single();
            Assert.Equal(PaymentAttemptStates.Failed, attempt.State);
            Assert.Contains(context.PaymentAttemptAuditLogs, audit =>
                audit.PaymentAttemptId == attempt.Id
                && audit.EventType == "payment_attempt.failed"
                && audit.NewState == PaymentAttemptStates.Failed);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenProviderCapabilityInactive_RejectsBeforeOrder()
        {
            using var context = CreateContext();
            var storeId = Guid.NewGuid();
            SeedPaymentMethod(context, storeId, PaymentMethodKeys.PayPal);
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
            var service = CreateCheckoutService(context, cartService);
            var preview = await service.PreviewAsync(CreateRequest(
                storeId,
                cart.Payload.Token!,
                add.Payload!.Version,
                paymentMethodKey: PaymentMethodKeys.PayPal));

            var result = await service.PlaceOrderAsync(new StorefrontPlaceOrderRequest(
                storeId,
                preview.Payload!.CheckoutSessionId,
                preview.Payload.CheckoutVersion,
                preview.Payload.CartVersion,
                "paypal-inactive-provider"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Empty(context.Orders);
            Assert.Empty(context.PaymentAttempts);
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
                ExpectedCheckoutVersion: 1,
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
            return CreateCheckoutService(context, cartService, checkoutEnabled: true, shippingCalculator: null, moneyConversionService: null, providers: providers);
        }

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            bool checkoutEnabled,
            params IStorefrontPaymentProvider[] providers)
        {
            return CreateCheckoutService(context, cartService, checkoutEnabled, shippingCalculator: null, moneyConversionService: null, providers: providers);
        }

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            IShippingCalculator shippingCalculator,
            params IStorefrontPaymentProvider[] providers)
        {
            return CreateCheckoutService(context, cartService, checkoutEnabled: true, shippingCalculator, moneyConversionService: null, providers: providers);
        }

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            IShippingCalculator? shippingCalculator,
            IMoneyConversionService moneyConversionService,
            params IStorefrontPaymentProvider[] providers)
        {
            return CreateCheckoutService(context, cartService, checkoutEnabled: true, shippingCalculator, moneyConversionService, providers: providers);
        }

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            bool checkoutEnabled,
            IShippingCalculator? shippingCalculator,
            IMoneyConversionService? moneyConversionService,
            IOrderPlacementService? orderPlacementService = null,
            params IStorefrontPaymentProvider[] providers)
        {
            IStorefrontPaymentProvider[] providerList = providers.Length == 0
                ? [new CodStorefrontPaymentProvider()]
                : providers;

            var builder = new CheckoutServiceTestBuilder(context, cartService)
                .WithCheckoutEnabled(checkoutEnabled)
                .WithProviders(providerList);
            if (shippingCalculator is not null)
            {
                builder.WithShippingCalculator(shippingCalculator);
            }

            if (moneyConversionService is not null)
            {
                builder.WithMoneyConversionService(moneyConversionService);
            }

            if (orderPlacementService is not null)
            {
                builder.WithOrderPlacementService(orderPlacementService);
            }

            return builder.Build();
        }

        private sealed class CheckoutServiceTestBuilder
        {
            private readonly CommerceNodeDbContext context;
            private readonly IStorefrontCartService cartService;
            private bool checkoutEnabled = true;
            private IStoreCurrencyResolver storeCurrencyResolver = new FixedStoreCurrencyResolver("USD");
            private IMoneyRoundingService moneyRoundingService = new MoneyRoundingService(new CurrencyMetadataService());
            private IMoneyConversionService moneyConversionService = new FakeMoneyConversionService();
            private readonly IStorefrontCustomerService customerService;
            private IStorefrontPaymentProvider[] providers = [new CodStorefrontPaymentProvider()];
            private IProductSellabilityResolver sellabilityResolver = new ProductSellabilityResolver();
            private IAddressValidationService addressValidationService = new AddressValidationService();
            private IShippingCalculator shippingCalculator = new ShippingCalculator([new InternalFreeStandardShippingProvider()]);
            private IShippingTaxCalculator shippingTaxCalculator = new ZeroShippingTaxCalculator();
            private IOrderPlacementService? orderPlacementService;

            public CheckoutServiceTestBuilder(
                CommerceNodeDbContext context,
                IStorefrontCartService cartService)
            {
                this.context = context;
                this.cartService = cartService;
                this.customerService = new StorefrontCustomerService(context);
            }

            public CheckoutServiceTestBuilder WithCheckoutEnabled(bool enabled)
            {
                this.checkoutEnabled = enabled;
                return this;
            }

            public CheckoutServiceTestBuilder WithMoneyConversionService(IMoneyConversionService service)
            {
                this.moneyConversionService = service;
                return this;
            }

            public CheckoutServiceTestBuilder WithShippingCalculator(IShippingCalculator service)
            {
                this.shippingCalculator = service;
                return this;
            }

            public CheckoutServiceTestBuilder WithOrderPlacementService(IOrderPlacementService service)
            {
                this.orderPlacementService = service;
                return this;
            }

            public CheckoutServiceTestBuilder WithProviders(params IStorefrontPaymentProvider[] paymentProviders)
            {
                this.providers = paymentProviders.Length == 0
                    ? [new CodStorefrontPaymentProvider()]
                    : paymentProviders;
                return this;
            }

            public StorefrontCheckoutService Build()
            {
                var providerList = this.providers;
                var placementService = this.orderPlacementService
                    ?? new OrderPlacementService(
                        this.context,
                        this.moneyRoundingService,
                        this.sellabilityResolver,
                        new DefaultOrderStockAdjustmentHook());
                var pricingCalculator = new CheckoutPricingCalculator(
                    this.context,
                    this.moneyRoundingService,
                    this.moneyConversionService,
                    this.shippingCalculator,
                    this.shippingTaxCalculator);
                var paymentCoordinator = new CheckoutPaymentCoordinator(
                    this.context,
                    new PaymentProviderCapabilityRegistry(providerList),
                    new StorefrontPaymentProviderResolver(providerList));

                return new StorefrontCheckoutService(
                    this.context,
                    this.cartService,
                    this.storeCurrencyResolver,
                    this.moneyRoundingService,
                    this.moneyConversionService,
                    this.customerService,
                    new StubStoreFeatureStateService(this.checkoutEnabled),
                    paymentCoordinator,
                    this.sellabilityResolver,
                    this.addressValidationService,
                    this.shippingCalculator,
                    this.shippingTaxCalculator,
                    placementService,
                    pricingCalculator);
            }
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
            string fullName = "Customer One",
            string email = "customer@example.test",
            string address1 = "100 Main St",
            string postalCode = "10001",
            string countryCode = "US")
        {
            return new StorefrontCheckoutShippingAddressDto(
                fullName,
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

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<CommerceCurrentStore>(
                    true,
                    "Store resolved.",
                    new CommerceCurrentStore(
                        PublicId: Guid.NewGuid(),
                        StoreKey: "test-store",
                        Name: "Test Store",
                        Status: CommerceStoreStatuses.Active,
                        BaseUrl: null,
                        PrimaryDomain: null,
                        ForceHttps: true,
                        CdnHost: null,
                        LogoUrl: null,
                        CompanyName: null,
                        CompanyEmail: null,
                        CompanyPhone: null,
                        CompanyAddress: null,
                        FaviconUrl: null,
                        PngIconUrl: null,
                        AppleTouchIconUrl: null,
                        MsTileImageUrl: null,
                        MsTileColor: null,
                        DefaultCurrencyCode: "USD",
                        DefaultCulture: "en-US",
                        SupportEmail: null,
                        SupportPhone: null,
                        MaintenanceModeEnabled: false,
                        MaintenanceMessage: null,
                        HtmlBodyId: null)));
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", this.storeId));
            }
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
                this.Descriptor = new PaymentProviderDescriptor(
                    providerKey,
                    providerKey,
                    Description: null,
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
            }

            public string ProviderKey { get; }

            public PaymentProviderDescriptor Descriptor { get; }

            public Task<ServiceResponse<PaymentProviderOperationResult>> CreatePaymentSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                var result = this.createSession(request);
                return result is null
                    ? Task.FromResult(PaymentProviderOperationResult.Failed(
                        ServiceResponseType.Conflict,
                        "Provider is not configured.",
                        "provider_session_failed"))
                    : Task.FromResult(PaymentProviderOperationResult.Succeeded(
                        "Session created.",
                        result.NextActionType,
                        result.NextActionUrl,
                        result.ProviderSessionId,
                        result.ProviderReference,
                        result.MetadataJson,
                        PaymentAttemptStates.RequiresAction));
            }

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

        private sealed class FailingOrderStockAdjustmentHook : IOrderStockAdjustmentHook
        {
            public Task<OrderStockAdjustmentResult> ApplyAsync(
                OrderStockAdjustmentRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(OrderStockAdjustmentResult.Failed(
                    ServiceResponseType.Conflict,
                    "Injected stock adjustment failure."));
            }
        }

        private sealed class FakeOrderPlacementService : IOrderPlacementService
        {
            public Task<OrderPlacementResult> PlaceAsync(
                OrderPlacementRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(OrderPlacementResult.Failed(
                    ServiceResponseType.Conflict,
                    "Injected placement failure."));
            }
        }

        private sealed class FakeShippingCalculator : IShippingCalculator
        {
            private readonly Func<ShippingOptionsRequest, ShippingCalculationResult> calculate;

            public FakeShippingCalculator(Func<ShippingOptionsRequest, ShippingCalculationResult> calculate)
            {
                this.calculate = calculate;
            }

            public Task<ServiceResponse<ShippingCalculationResult>> GetOptionsAsync(
                ShippingOptionsRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ServiceResponse<ShippingCalculationResult>(true, "Shipping calculated.")
                {
                    Payload = this.calculate(request),
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

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.NotNull(field);
            return Assert.IsAssignableFrom<T>(field.GetValue(instance));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate BlazorShop.sln from test output directory.");
        }
    }
}

namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
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

        private static StorefrontCheckoutService CreateCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService)
        {
            return new StorefrontCheckoutService(
                context,
                cartService,
                new StorefrontCustomerService(context));
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
            string countryCode = "US")
        {
            return new StorefrontCheckoutPreviewRequest(
                storeId,
                cartToken,
                expectedCartVersion,
                customerEmail,
                "Customer One",
                PaymentMethodKeys.Cod,
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

        private static void SeedPaymentMethod(CommerceNodeDbContext context, Guid storeId)
        {
            context.StorePaymentMethods.Add(new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                DisplayName = "Cash on Delivery",
                Enabled = true,
                DisplayOrder = 10,
            });
            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-checkout-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}

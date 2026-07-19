namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class OrderReadModelOptionsTests
    {
        [Fact]
        public void FactoryDefaults_MatchCurrentProjectionVisibilityMatrix()
        {
            var admin = OrderReadModelOptions.Admin();
            var customer = OrderReadModelOptions.Customer();
            var guest = OrderReadModelOptions.Guest();
            var internalQuery = OrderReadModelOptions.Internal();

            Assert.Equal(OrderReadVisibility.Admin, admin.Visibility);
            Assert.False(admin.IncludeTrackingEvents);
            Assert.True(admin.IncludePaymentAttemptPublicReference);
            Assert.True(admin.IncludeAdminNote);
            Assert.True(admin.IncludeUserId);
            Assert.True(admin.IncludeAllHistory);
            Assert.True(admin.UseProductNameFallback);
            Assert.False(admin.IncludeLineMoneyDetails);

            Assert.Equal(OrderReadVisibility.Customer, customer.Visibility);
            Assert.True(customer.IncludeTrackingEvents);
            Assert.False(customer.IncludePaymentAttemptPublicReference);
            Assert.False(customer.IncludeAdminNote);
            Assert.False(customer.IncludeUserId);
            Assert.False(customer.IncludeAllHistory);
            Assert.True(customer.UseProductNameFallback);
            Assert.True(customer.IncludeLineMoneyDetails);

            Assert.Equal(OrderReadVisibility.Guest, guest.Visibility);
            Assert.False(guest.IncludeTrackingEvents);
            Assert.True(guest.IncludePaymentAttemptPublicReference);
            Assert.False(guest.IncludeAdminNote);
            Assert.False(guest.IncludeUserId);
            Assert.False(guest.IncludeAllHistory);
            Assert.False(guest.UseProductNameFallback);
            Assert.True(guest.IncludeLineMoneyDetails);

            Assert.Equal(OrderReadVisibility.Internal, internalQuery.Visibility);
            Assert.True(internalQuery.IncludeTrackingEvents);
            Assert.True(internalQuery.IncludePaymentAttemptPublicReference);
            Assert.True(internalQuery.IncludeAdminNote);
            Assert.True(internalQuery.IncludeUserId);
            Assert.False(internalQuery.IncludeAllHistory);
            Assert.True(internalQuery.UseProductNameFallback);
            Assert.True(internalQuery.IncludeLineMoneyDetails);
        }

        [Fact]
        public async Task AssemblerSkeleton_ReturnsEmptyProjectionForEmptyInput()
        {
            await using var context = CreateContext();
            var assembler = new OrderReadModelAssembler(context);

            var result = await assembler.BuildAsync(Array.Empty<Order>(), OrderReadModelOptions.Customer());

            Assert.Empty(result);
        }

        [Fact]
        public void AssemblerChildLoaders_AreBatchedAndOptionGated()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Services/OrderReadModelAssembler.cs");

            Assert.Contains("LoadProductNamesAsync", source, StringComparison.Ordinal);
            Assert.Contains("LoadPaymentSummariesAsync", source, StringComparison.Ordinal);
            Assert.Contains("LoadHistoryEntriesAsync", source, StringComparison.Ordinal);
            Assert.Contains("LoadTrackingEventsAsync", source, StringComparison.Ordinal);
            Assert.Contains("Where(product => productIds.Contains(product.Id))", source, StringComparison.Ordinal);
            Assert.Contains("Where(attempt => attempt.OrderId.HasValue && orderIds.Contains(attempt.OrderId.Value))", source, StringComparison.Ordinal);
            Assert.Contains("OrderByDescending(attempt => attempt.UpdatedAtUtc)", source, StringComparison.Ordinal);
            Assert.Contains("options.IncludePaymentAttemptPublicReference ? attempt.PublicId : null", source, StringComparison.Ordinal);
            Assert.Contains("options.IncludeAllHistory || entry.VisibleToCustomer", source, StringComparison.Ordinal);
            Assert.Contains("if (!options.IncludeTrackingEvents)", source, StringComparison.Ordinal);
            Assert.Contains("if (!options.UseProductNameFallback)", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ActiveOrderReadServices_DoNotOwnReadModelProjectionCopies()
        {
            var servicePaths = new[]
            {
                "BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeAdminOrderService.cs",
                "BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCustomerOrderService.cs",
                "BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontGuestOrderService.cs",
                "BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeOrderQueryService.cs",
            };

            foreach (var path in servicePaths)
            {
                var source = ReadRepositoryFile(path);

                Assert.DoesNotContain("new GetOrder", source, StringComparison.Ordinal);
                Assert.DoesNotContain("new GetOrderLine", source, StringComparison.Ordinal);
                Assert.DoesNotContain("new GetOrderPaymentSummary", source, StringComparison.Ordinal);
                Assert.DoesNotContain("CreatePaymentSummary", source, StringComparison.Ordinal);
                Assert.DoesNotContain("MapOrdersAsync", source, StringComparison.Ordinal);
                Assert.DoesNotContain("MapAsync(Order", source, StringComparison.Ordinal);
                Assert.Contains("OrderReadModelAssembler", source, StringComparison.Ordinal);
            }
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"order-read-model-options-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
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

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}

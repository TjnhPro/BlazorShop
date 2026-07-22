namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;

    using Xunit;

    public sealed class StorefrontOrderEmailE2ERunnerTests
    {
        [Fact]
        public void OrderEmailE2ERunner_UsesCheckoutMailpitAndTransactionalMessageEvidence()
        {
            var source = ReadRepositoryFile("scripts/qa/storefront-order-email-e2e.js");

            Assert.Contains("MAILPIT_API_URL", source, StringComparison.Ordinal);
            Assert.Contains("placeCodOrder", source, StringComparison.Ordinal);
            Assert.Contains("order.created", source, StringComparison.Ordinal);
            Assert.Contains("order.placed", source, StringComparison.Ordinal);
            Assert.Contains("exactly-one", source, StringComparison.Ordinal);

            using var context = CreateContext();
            var designEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(MessageTemplate));
            var orderPlacedSeed = designEntity!.GetSeedData()
                .Single(seed => string.Equals(
                    Assert.IsType<string>(seed[nameof(MessageTemplate.SystemName)]),
                    TransactionalMessageTemplateSystemNames.OrderPlaced,
                    StringComparison.Ordinal));
            var orderPlacedBody = Assert.IsType<string>(orderPlacedSeed[nameof(MessageTemplate.BodyHtmlTemplate)]);

            Assert.Contains("{{Order.DetailUrl}}", orderPlacedBody, StringComparison.Ordinal);
        }

        [Fact]
        public void OrderEmailE2ERunner_CoversSmtpOutageRetryAndSenderIsolation()
        {
            var source = ReadRepositoryFile("scripts/qa/storefront-order-email-e2e.js");

            Assert.Contains("smtp-outage", source, StringComparison.Ordinal);
            Assert.Contains("retryQueuedMessage", source, StringComparison.Ordinal);
            Assert.Contains("store-sender-isolation", source, StringComparison.Ordinal);
            Assert.Contains("default-sender@example.local", source, StringComparison.Ordinal);
            Assert.Contains("s2-sender@example.local", source, StringComparison.Ordinal);
            Assert.Contains("X-Node-Key", source, StringComparison.Ordinal);
            Assert.Contains("X-Node-Secret", source, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5434;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=blazorshop_commerce_node_dev",
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(CommerceNodeDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure();
                    })
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                    && File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not find BlazorShop repository root.");
        }
    }
}

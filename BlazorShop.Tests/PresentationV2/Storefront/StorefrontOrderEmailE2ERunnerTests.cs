namespace BlazorShop.Tests.PresentationV2.Storefront
{
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
            Assert.Contains("Order.DetailUrl", ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDbContext.cs"), StringComparison.Ordinal);
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

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontEmailRecoveryE2ERunnerTests
    {
        [Fact]
        public void RecoveryE2ERunner_UsesMailpitAndRedactsResetTokenEvidence()
        {
            var source = ReadRepositoryFile("scripts/qa/storefront-email-recovery-e2e.js");

            Assert.Contains("MAILPIT_API_URL", source, StringComparison.Ordinal);
            Assert.Contains("clearMailpit", source, StringComparison.Ordinal);
            Assert.Contains("waitForMail", source, StringComparison.Ordinal);
            Assert.Contains("extractResetLink", source, StringComparison.Ordinal);
            Assert.Contains("redactResetLink", source, StringComparison.Ordinal);
            Assert.Contains("token=[redacted]", source, StringComparison.Ordinal);
        }

        [Fact]
        public void RecoveryE2ERunner_UsesKnownSeedCustomerAndAntiEnumerationCheck()
        {
            var source = ReadRepositoryFile("scripts/qa/storefront-email-recovery-e2e.js");

            Assert.Contains("qa.customer@example.local", source, StringComparison.Ordinal);
            Assert.Contains("QaCustomer123!", source, StringComparison.Ordinal);
            Assert.Contains("unknown.no-mailpit-message", source, StringComparison.Ordinal);
            Assert.Contains("/forgot-password", source, StringComparison.Ordinal);
            Assert.Contains("/reset-password", source, StringComparison.Ordinal);
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

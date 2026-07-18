namespace BlazorShop.Tests.Application.CommerceNode
{
    using Xunit;

    public sealed class EmailSmtpCaptureRuntimeTests
    {
        [Fact]
        public void CommerceNodeCompose_IncludesMailpitCaptureService()
        {
            var root = FindRepositoryRoot();
            var compose = File.ReadAllText(Path.Combine(root, "compose.commercenode.yml"));

            Assert.Contains("commercenode-mailpit", compose, StringComparison.Ordinal);
            Assert.Contains("axllent/mailpit", compose, StringComparison.Ordinal);
            Assert.Contains("\"1025:1025\"", compose, StringComparison.Ordinal);
            Assert.Contains("\"8025:8025\"", compose, StringComparison.Ordinal);
        }

        [Fact]
        public void V2LocalEnvironment_AllowsCaptureAndDisablesGlobalSmtpFallback()
        {
            var root = FindRepositoryRoot();
            var env = File.ReadAllText(Path.Combine(root, "scripts", "env", "v2-local.env"));

            Assert.Contains("COMMERCENODE_API__CommerceNode__EmailTransport__CaptureModeAllowed=true", env, StringComparison.Ordinal);
            Assert.Contains("COMMERCENODE_API__CommerceNode__EmailTransport__AllowGlobalEmailSettingsFallback=false", env, StringComparison.Ordinal);
            Assert.DoesNotContain("STOREFRONT_V2__EmailSettings", env, StringComparison.Ordinal);
            Assert.DoesNotContain("STOREFRONT_V2__SMTP", env, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DevelopmentSeeder_ConfiguresStoreScopedMailpitCaptureSenders()
        {
            var root = FindRepositoryRoot();
            var seeder = File.ReadAllText(Path.Combine(
                root,
                "BlazorShop.Infrastructure",
                "Data",
                "CommerceNode",
                "CommerceNodeDevelopmentSeeder.cs"));

            Assert.Contains("EnsureStoreEmailSettingsAsync", seeder, StringComparison.Ordinal);
            Assert.Contains("StoreEmailDeliveryModes.Capture", seeder, StringComparison.Ordinal);
            Assert.Contains("settings.SmtpHost = \"localhost\"", seeder, StringComparison.Ordinal);
            Assert.Contains("settings.SmtpPort = 1025", seeder, StringComparison.Ordinal);
            Assert.Contains("default-sender@example.local", seeder, StringComparison.Ordinal);
            Assert.Contains("s2-sender@example.local", seeder, StringComparison.Ordinal);
        }

        [Fact]
        public void LocalRunDocs_RecordMailpitCapturePorts()
        {
            var root = FindRepositoryRoot();
            var docs = File.ReadAllText(Path.Combine(root, "docs", "architecture", "07-deployment-and-local-run.md"));

            Assert.Contains("Mailpit", docs, StringComparison.Ordinal);
            Assert.Contains("http://localhost:8025", docs, StringComparison.Ordinal);
            Assert.Contains("localhost:1025", docs, StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                    && File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                    && Directory.Exists(Path.Combine(directory.FullName, "BlazorShop.PresentationV2")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not find BlazorShop repository root.");
        }
    }
}

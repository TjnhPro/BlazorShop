namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.DTOs.Admin.Settings;
    using Xunit;

    public sealed class TransactionalMessageBaselineTests
    {
        [Fact]
        public void DirectEmailCallSiteInventory_MatchesKnownBaseline()
        {
            var root = FindRepositoryRoot();
            var sourceRoots = new[]
            {
                "BlazorShop.Application",
                "BlazorShop.Domain",
                "BlazorShop.Infrastructure",
                "BlazorShop.PresentationV2",
            };
            var callSites = sourceRoots
                .SelectMany(sourceRoot => Directory.EnumerateFiles(Path.Combine(root, sourceRoot), "*.cs", SearchOption.AllDirectories))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Select(path => new
                {
                    RelativePath = Path.GetRelativePath(root, path).Replace('\\', '/'),
                    Text = File.ReadAllText(path),
                })
                .Where(file => file.Text.Contains("SendEmailAsync", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "BlazorShop.Application/Services/Authentication/DirectAccountEmailDispatcher.cs",
                    "BlazorShop.Application/Services/NewsletterService.cs",
                    "BlazorShop.Application/Services/Payment/CartService.cs",
                    "BlazorShop.Domain/Contracts/IEmailService.cs",
                    "BlazorShop.Infrastructure/Services/EmailService.cs",
                ],
                callSites);
        }

        [Fact]
        public void NotificationSettingsDto_DoesNotExposeSmtpSecrets()
        {
            var propertyNames = typeof(NotificationSettingsDto)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.Contains("SecretsConfigured", propertyNames);
            Assert.DoesNotContain("SmtpUsername", propertyNames);
            Assert.DoesNotContain("SmtpPassword", propertyNames);
            Assert.DoesNotContain("Username", propertyNames);
            Assert.DoesNotContain("Password", propertyNames);
        }

        [Fact]
        public void CommerceNodeRuntime_RegistersCommerceTaskWorker()
        {
            var root = FindRepositoryRoot();
            var program = File.ReadAllText(Path.Combine(
                root,
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "Program.cs"));

            Assert.Contains("AddHostedService<CommerceTaskWorker>", program, StringComparison.Ordinal);
            Assert.Contains("AddScoped<ICommerceTaskHandler", program, StringComparison.Ordinal);
            Assert.Contains("CommerceTaskWorkerOptions.SectionName", program, StringComparison.Ordinal);
            Assert.Contains("OrderCreatedTaskHandler", program, StringComparison.Ordinal);
            Assert.Contains("MessageDeliverTaskHandler", program, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeAccountEmails_UseQueuedDispatcherInsteadOfDirectSmtpDispatcher()
        {
            var root = FindRepositoryRoot();
            var dependencyInjection = File.ReadAllText(Path.Combine(
                root,
                "BlazorShop.Infrastructure",
                "Data",
                "CommerceNode",
                "DependencyInjection.cs"));

            Assert.Contains("AddScoped<IAccountEmailDispatcher, QueuedAccountEmailDispatcher>", dependencyInjection, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IAccountEmailDispatcher, DirectAccountEmailDispatcher>", dependencyInjection, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNodeOrderCreatedTask_QueuesOrderPlacedMessageWithoutDirectSmtpCall()
        {
            var root = FindRepositoryRoot();
            var handler = File.ReadAllText(Path.Combine(
                root,
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "Tasks",
                "OrderCreatedTaskHandler.cs"));

            Assert.Contains("QueueOrderPlacedAsync", handler, StringComparison.Ordinal);
            Assert.DoesNotContain("SendEmailAsync", handler, StringComparison.Ordinal);
        }

        [Fact]
        public void CurrentSmtpConfigurationShape_UsesStoreScopedTransportWithLocalCapture()
        {
            var root = FindRepositoryRoot();
            var commerceNodeAppSettings = File.ReadAllText(Path.Combine(
                root,
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "appsettings.json"));
            var commerceNodeDevelopmentSettings = File.ReadAllText(Path.Combine(
                root,
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "appsettings.Development.json"));
            var localEnv = File.ReadAllText(Path.Combine(root, "scripts", "env", "v2-local.env"));
            var commerceNodeCompose = File.ReadAllText(Path.Combine(root, "compose.commercenode.yml"));
            var productionCompose = File.ReadAllText(Path.Combine(root, "compose.production.yml"));

            Assert.DoesNotContain("EmailSettings", commerceNodeAppSettings, StringComparison.Ordinal);
            Assert.Contains("EmailTransport", commerceNodeDevelopmentSettings, StringComparison.Ordinal);
            Assert.Contains("AllowGlobalEmailSettingsFallback", commerceNodeDevelopmentSettings, StringComparison.Ordinal);
            Assert.Contains("CaptureModeAllowed", commerceNodeDevelopmentSettings, StringComparison.Ordinal);
            Assert.Contains("COMMERCENODE_API__CommerceNode__EmailTransport__AllowGlobalEmailSettingsFallback=false", localEnv, StringComparison.Ordinal);
            Assert.Contains("mailpit", commerceNodeCompose, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CommerceNode__EmailTransport__AllowGlobalEmailSettingsFallback: \"false\"", productionCompose, StringComparison.Ordinal);
            Assert.Contains("CommerceNode__EmailTransport__CaptureModeAllowed: \"false\"", productionCompose, StringComparison.Ordinal);
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

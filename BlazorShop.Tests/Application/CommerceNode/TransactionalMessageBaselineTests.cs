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
                    "BlazorShop.Infrastructure/Data/CommerceNode/Services/MessageDeliveryService.cs",
                    "BlazorShop.Infrastructure/Services/EmailService.cs",
                    "BlazorShop.Infrastructure/Services/OrderTrackingService.cs",
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

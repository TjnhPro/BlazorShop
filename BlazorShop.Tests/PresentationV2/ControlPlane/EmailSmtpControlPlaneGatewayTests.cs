namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using Xunit;

    public sealed class EmailSmtpControlPlaneGatewayTests
    {
        [Fact]
        public void ControlPlaneApi_ExposesStoreScopedEmailSettingsGatewayWithPolicies()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Controllers/CommerceGateway/ControlPlaneCommerceMessagesController.cs");

            Assert.Contains("api/controlplane/commerce/stores/{storePublicId:guid}/email-settings", source, StringComparison.Ordinal);
            Assert.Contains("GetEmailSettings", source, StringComparison.Ordinal);
            Assert.Contains("UpdateEmailSettings", source, StringComparison.Ordinal);
            Assert.Contains("RotateEmailPassword", source, StringComparison.Ordinal);
            Assert.Contains("ClearEmailPassword", source, StringComparison.Ordinal);
            Assert.Contains("SendEmailTest", source, StringComparison.Ordinal);
            Assert.Contains("ControlPlanePolicyNames.CommerceSettingsRead", source, StringComparison.Ordinal);
            Assert.Contains("ControlPlanePolicyNames.CommerceSettingsWrite", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ControlPlaneGateway_ExposesMessageTemplateAndQueueOperations()
        {
            var interfaceSource = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Commerce/Messages/IControlPlaneMessageClient.cs");
            var clientSource = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Commerce/Messages/ControlPlaneMessageClient.cs");

            foreach (var methodName in new[]
            {
                "ListMessageTemplatesAsync",
                "GetMessageTemplateAsync",
                "UpdateMessageTemplateAsync",
                "ResetMessageTemplateAsync",
                "PreviewMessageTemplateAsync",
                "ListQueuedMessagesAsync",
                "GetQueuedMessageAsync",
                "RetryQueuedMessageAsync",
                "CancelQueuedMessageAsync",
            })
            {
                Assert.Contains(methodName, interfaceSource, StringComparison.Ordinal);
                Assert.Contains(methodName, clientSource, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void ControlPlaneWeb_EmailPageCallsOnlyControlPlaneClient()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceEmailSettings.razor");

            Assert.Contains("@page \"/commerce-admin/email\"", source, StringComparison.Ordinal);
            Assert.Contains("MessageClient.GetEmailSettingsAsync", source, StringComparison.Ordinal);
            Assert.Contains("MessageClient.UpdateEmailSettingsAsync", source, StringComparison.Ordinal);
            Assert.Contains("MessageClient.SendEmailTestAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CatalogClient.", source, StringComparison.Ordinal);
            Assert.DoesNotContain("api/commerce/admin", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNode", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("current password", source, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CommerceNodeSwagger_DefinesStoreEmailOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            foreach (var operationId in new[]
            {
                "CommerceStoreEmailSettings_Get",
                "CommerceStoreEmailSettings_Update",
                "CommerceStoreEmailSettings_RotatePassword",
                "CommerceStoreEmailSettings_ClearPassword",
                "CommerceStoreEmailSettings_SendTest",
            })
            {
                Assert.Contains(operationId, source, StringComparison.Ordinal);
            }

            Assert.Contains("CommerceNodeApiResponse<StoreEmailSettingsResponse>", source, StringComparison.Ordinal);
            Assert.Contains("CommerceNodeApiResponse<SendStoreEmailTestResponse>", source, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ReadCommerceNodeSwaggerSource()
        {
            var swaggerDirectory = Path.Combine(
                FindRepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "Swagger");

            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(swaggerDirectory, "*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
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

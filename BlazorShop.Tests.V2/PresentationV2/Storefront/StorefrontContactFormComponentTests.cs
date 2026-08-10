namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Browser.Contact;
using BlazorShop.Storefront.Components.Contracts.Contact;
using BlazorShop.Storefront.Components.WasmHost.Content;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontContactFormComponentTests
{
    [Fact]
    public async Task WasmHostAppPrerendersSemanticFormWithSubjectField()
    {
        var controller = new RecordingContactController(
            new StorefrontContactFormSubmitResult(Success: true, DefaultMessage: "received"));

        var html = await RenderAppAsync(controller);

        Assert.Contains("data-storefront-component=\"contact-form-app\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-contact-form", html, StringComparison.Ordinal);
        Assert.Contains("name=\"Name\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"Email\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"Subject\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"Message\"", html, StringComparison.Ordinal);
        Assert.Contains("required", html, StringComparison.Ordinal);
        Assert.Contains("class=\"contact-form\"", html, StringComparison.Ordinal);
        Assert.Contains("Full name", html, StringComparison.Ordinal);
        Assert.Contains("Subject", html, StringComparison.Ordinal);
    }

    [Fact]
    public void WasmHostAppSubmitsThroughBrowserContactControllerOnly()
    {
        var source = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Content/StorefrontContactFormApp.razor"));

        Assert.Contains("@inject IStorefrontBrowserContactController ContactController", source, StringComparison.Ordinal);
        Assert.Contains("ContactController.SubmitAsync(request, this.Action)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/api/", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void HybridShellHostsWasmChildAtRenderModeBridgeWithoutBrowserController()
    {
        var source = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactForm.razor"));

        Assert.Contains("<StorefrontContactFormApp", source, StringComparison.Ordinal);
        Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", source, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component=\"contact-form\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IStorefrontBrowser", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/api/", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront", source, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAppAsync(RecordingContactController controller)
    {
        var services = new ServiceCollection()
            .AddSingleton<IStorefrontBrowserContactController>(controller)
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontContactFormApp.InitialState)] = new StorefrontContactFormState(
                    "Ada",
                    "ada@example.test",
                    "Subject",
                    "Message",
                    IsSubmitting: false,
                    Submitted: false,
                    ErrorCode: null,
                    DefaultMessage: null,
                    FieldErrors: new Dictionary<string, IReadOnlyList<string>>()),
                [nameof(StorefrontContactFormApp.Labels)] = new StorefrontContactFormLabels(
                    "Full name",
                    "Email address",
                    "Subject",
                    "Message",
                    "Send",
                    "Sending",
                    "Please review",
                    "Received",
                    "Retry"),
                [nameof(StorefrontContactFormApp.Classes)] = new StorefrontContactFormClasses(
                    Form: "contact-form",
                    Field: "contact-field",
                    Label: "contact-label",
                    Input: "contact-input",
                    Textarea: "contact-textarea",
                    Submit: "contact-submit"),
                [nameof(StorefrontContactFormApp.Action)] = new StorefrontContactFormActionDescriptor("contact/local"),
            });

            var component = await renderer.RenderComponentAsync<StorefrontContactFormApp>(parameters);
            return component.ToHtmlString();
        });
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class RecordingContactController : IStorefrontBrowserContactController
    {
        private readonly StorefrontContactFormSubmitResult result;

        public RecordingContactController(StorefrontContactFormSubmitResult result)
        {
            this.result = result;
        }

        public Task<StorefrontContactFormSubmitResult> SubmitAsync(
            StorefrontContactFormSubmitRequest request,
            StorefrontContactFormActionDescriptor? actionDescriptor = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.result);
        }
    }
}

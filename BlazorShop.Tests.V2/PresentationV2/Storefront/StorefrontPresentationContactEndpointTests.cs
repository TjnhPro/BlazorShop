namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontPresentationContactEndpointTests
{
    [Fact]
    public void MapStorefrontPresentationIncludesContactEndpoint()
    {
        var aggregation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs");

        Assert.Contains("endpoints.MapStorefrontPresentationContactEndpoints();", aggregation, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactEndpointUsesSameOriginLocalRouteAndExistingRuntimeContactClient()
    {
        var endpoint = ReadContactEndpoint();

        Assert.Contains("public const string ContactRoute = \"/api/contact\";", endpoint, StringComparison.Ordinal);
        Assert.Contains("app.MapPost(ContactRoute", endpoint, StringComparison.Ordinal);
        Assert.Contains("IStorefrontCurrentStoreProvider currentStoreProvider", endpoint, StringComparison.Ordinal);
        Assert.Contains("IStorefrontContactClient contactClient", endpoint, StringComparison.Ordinal);
        Assert.Contains("currentStoreProvider.ResolveAsync", endpoint, StringComparison.Ordinal);
        Assert.Contains("contactClient.SubmitAsync", endpoint, StringComparison.Ordinal);
        Assert.Contains("storeResolution.Store.StoreKey", endpoint, StringComparison.Ordinal);
        Assert.Contains("new StorefrontContactRequest", endpoint, StringComparison.Ordinal);
        Assert.Contains("StorefrontContactFormSubmitResult", endpoint, StringComparison.Ordinal);

        Assert.DoesNotContain("HttpClient", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("CommerceNodeBaseUrl", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront/stores", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("BlazorShop.Storefront.V2", endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactEndpointValidatesAntiforgeryAndRequiredFields()
    {
        var endpoint = ReadContactEndpoint();

        Assert.Contains("IAntiforgery antiforgery", endpoint, StringComparison.Ordinal);
        Assert.Contains("ValidateContactAntiforgeryAsync", endpoint, StringComparison.Ordinal);
        Assert.Contains("antiforgery.ValidateRequestAsync(httpContext)", endpoint, StringComparison.Ordinal);
        Assert.Contains("nameof(StorefrontLocalContactRequest.Name)", endpoint, StringComparison.Ordinal);
        Assert.Contains("nameof(StorefrontLocalContactRequest.Email)", endpoint, StringComparison.Ordinal);
        Assert.Contains("nameof(StorefrontLocalContactRequest.Subject)", endpoint, StringComparison.Ordinal);
        Assert.Contains("nameof(StorefrontLocalContactRequest.Message)", endpoint, StringComparison.Ordinal);
        Assert.Contains("EmailAddress.IsValid(request.Email)", endpoint, StringComparison.Ordinal);
        Assert.Contains("FieldErrors: fieldErrors", endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactEndpointMapsSuccessAndFailureToBrowserSafeResult()
    {
        var endpoint = ReadContactEndpoint();

        Assert.Contains("response.Success == true && response.Data?.Accepted == true", endpoint, StringComparison.Ordinal);
        Assert.Contains("Results.Ok(ContactSuccess(response.Data.Message))", endpoint, StringComparison.Ordinal);
        Assert.Contains("\"contact_rejected\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("\"service_unavailable\"", endpoint, StringComparison.Ordinal);
        Assert.Contains("catch (StorefrontApiException exception)", endpoint, StringComparison.Ordinal);
        Assert.Contains("TraceId: Activity.Current?.TraceId.ToString()", endpoint, StringComparison.Ordinal);
        Assert.Contains("Retryable: retryable", endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactLocalRequestKeepsSubjectTruthful()
    {
        var contract = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/Contracts/StorefrontContactLocalContracts.cs");

        Assert.Contains("public string? Name", contract, StringComparison.Ordinal);
        Assert.Contains("public string? Email", contract, StringComparison.Ordinal);
        Assert.Contains("public string? Subject", contract, StringComparison.Ordinal);
        Assert.Contains("public string? Message", contract, StringComparison.Ordinal);
    }

    private static string ReadContactEndpoint()
    {
        return ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationContactEndpoints.cs");
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}

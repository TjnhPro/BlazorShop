namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontCheckoutFormPatternTests
{
    [Fact]
    public void CheckoutForm_OwnsPostActionAntiforgeryCartVersionAndIdempotency()
    {
        var form = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutForm.razor");
        var submit = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutSubmit.razor");
        var applicationScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");

        Assert.Contains("<form method=\"post\" action=\"@StorefrontRoutes.Checkout\"", form);
        Assert.Contains("<AntiforgeryToken />", form);
        Assert.Contains("name=\"@StorefrontCheckoutFormFieldNames.CartVersion\"", form);
        Assert.Contains("value=\"@Context.CartVersion\"", form);
        Assert.Contains("name=\"@StorefrontCheckoutFormFieldNames.IdempotencyKey\"", form);
        Assert.Contains("value=\"@Context.IdempotencyKey\"", form);
        Assert.Contains("name=\"@StorefrontCheckoutFormFieldNames.UseShippingAddressAsBillingAddress\"", form);
        Assert.Contains("data-storefront-checkout-form", form);
        Assert.Contains("data-storefront-checkout-submit", submit);
        Assert.Contains("checkoutFormSelector = \"[data-storefront-checkout-form]\"", applicationScript);
        Assert.Contains("checkoutSubmitSelector = \"[data-storefront-checkout-submit]\"", applicationScript);
        Assert.Contains("boundCheckoutForms", applicationScript);
        Assert.Contains("candidate.addEventListener(\"submit\", () => disableCheckoutSubmitters(candidate))", applicationScript);
        Assert.Contains("button.disabled = true", applicationScript);
    }

    [Fact]
    public void CheckoutFormFieldNames_MatchPresentationEndpointDto()
    {
        var fieldNames = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutFormFieldNames.cs");

        foreach (var field in new[]
        {
            "CartVersion",
            "IdempotencyKey",
            "CustomerEmail",
            "CustomerName",
            "PaymentMethodKey",
            "ShippingAddressId",
            "BillingAddressId",
            "UseShippingAddressAsBillingAddress",
            "ShippingFullName",
            "ShippingEmail",
            "ShippingPhone",
            "ShippingAddress1",
            "ShippingAddress2",
            "ShippingCity",
            "ShippingState",
            "ShippingPostalCode",
            "ShippingCountryCode",
        })
        {
            Assert.Contains($"nameof(global::BlazorShop.Storefront.Presentation.Services.StorefrontCheckoutForm.{field})", fieldNames);
        }
    }

    [Fact]
    public void CheckoutAddressFields_RenderCountryOptionsFromContext()
    {
        var addressFields = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutAddressFields.razor");

        Assert.Contains("@if (Context.HasAddressCountries)", addressFields);
        Assert.Contains("@foreach (var country in Context.AddressCountries)", addressFields);
        Assert.Contains("name=\"@StorefrontCheckoutFormFieldNames.ShippingCountryCode\"", addressFields);
        Assert.Contains("value=\"@Context.DefaultShippingCountryCode\"", addressFields);
        Assert.Contains("required=\"@Context.PostalCodeRequired\"", addressFields);
    }

    [Fact]
    public void CheckoutPaymentFields_PostCanonicalPaymentMethodKey()
    {
        var paymentFields = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutPaymentFields.razor");

        Assert.Contains("name=\"@StorefrontCheckoutFormFieldNames.PaymentMethodKey\"", paymentFields);
        Assert.Contains("value=\"@method.Key\"", paymentFields);
        Assert.Contains("checked=\"@(method == Context.PaymentMethods[0])\"", paymentFields);
    }

    [Fact]
    public void V2AndStarterCheckoutViews_DoNotSelfAuthorCheckoutPostContracts()
    {
        var v2 = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");
        var starter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CheckoutPage.razor");
        var hosts = string.Concat(v2, starter);

        Assert.DoesNotContain("<form method=\"post\"", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("<AntiforgeryToken", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"CartVersion\"", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"IdempotencyKey\"", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"PaymentMethodKey\"", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"ShippingCountryCode\"", hosts, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutForm", hosts, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutAddressFields", hosts, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutPaymentFields", hosts, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutSubmit", hosts, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
    }
}

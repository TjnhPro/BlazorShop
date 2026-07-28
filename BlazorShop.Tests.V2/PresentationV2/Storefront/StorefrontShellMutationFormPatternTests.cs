namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontShellMutationFormPatternTests
{
    [Fact]
    public void CurrencyPreferenceForm_PostsCurrencyCodeAndSafeReturnUrl()
    {
        var form = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Shell/StorefrontCurrencyPreferenceForm.razor");

        Assert.Contains("<form method=\"post\" action=\"@Context.PreferenceAction\"", form);
        Assert.Contains("<AntiforgeryToken />", form);
        Assert.Contains("name=\"@StorefrontShellMutationFormFieldNames.CurrencyPreference.ReturnUrl\"", form);
        Assert.Contains("value=\"@Context.ReturnUrl\"", form);
        Assert.Contains("name=\"@StorefrontShellMutationFormFieldNames.CurrencyPreference.CurrencyCode\"", form);
        Assert.Contains("@foreach (var currencyCode in Context.SupportedCurrencyCodes)", form);
        Assert.Contains("selected=\"@IsSelectedCurrency(currencyCode)\"", form);
    }

    [Fact]
    public void LogoutForm_PostsSafeReturnUrl()
    {
        var form = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Shell/StorefrontLogoutForm.razor");

        Assert.Contains("<form method=\"post\" action=\"@StorefrontRoutes.Logout\"", form);
        Assert.Contains("<AntiforgeryToken />", form);
        Assert.Contains("name=\"@StorefrontShellMutationFormFieldNames.Logout.ReturnUrl\"", form);
        Assert.Contains("value=\"@Context.LogoutReturnUrl\"", form);
    }

    [Fact]
    public void ShellMutationFormFieldNames_MatchEndpointDtos()
    {
        var fieldNames = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Shell/StorefrontShellMutationFormFieldNames.cs");

        Assert.Contains("nameof(global::BlazorShop.Storefront.Presentation.Endpoints.StorefrontCurrencyPreferenceForm.CurrencyCode)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Presentation.Endpoints.StorefrontCurrencyPreferenceForm.ReturnUrl)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Presentation.Services.StorefrontLogoutForm.ReturnUrl)", fieldNames);
    }

    [Fact]
    public void V2HeaderAndAccountMenu_DoNotSelfAuthorMutationFormContracts()
    {
        var header = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor");
        var accountMenu = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontAccountMenu.razor");
        var hosts = string.Concat(header, accountMenu);

        Assert.DoesNotContain("<form method=\"post\"", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("<AntiforgeryToken", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"CurrencyCode\"", hosts, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"ReturnUrl\"", hosts, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCurrencyPreferenceForm", hosts, StringComparison.Ordinal);
        Assert.Contains("<StorefrontLogoutForm", hosts, StringComparison.Ordinal);
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

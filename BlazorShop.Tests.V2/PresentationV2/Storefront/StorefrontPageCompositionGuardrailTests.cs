extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontPageCompositionGuardrailTests
    {
        [Theory]
        [InlineData("About.razor")]
        [InlineData("Faq.razor")]
        [InlineData("FAQ.razor")]
        [InlineData("Privacy.razor")]
        [InlineData("Terms.razor")]
        [InlineData("CustomerService.razor")]
        public void ContentPages_DoNotReappearAsDedicatedRouteFiles(string fileName)
        {
            var pageFiles = EnumerateStorefrontPageFiles()
                .Select(Path.GetFileName)
                .ToList();

            Assert.DoesNotContain(pageFiles, name => string.Equals(name, fileName, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("Home.razor", "@page \"/\"")]
        [InlineData("ProductPage.razor", "@page \"/product/{Slug}\"")]
        [InlineData("CategoryPage.razor", "@page \"/category/{Slug}\"")]
        [InlineData("SearchPage.razor", "@page \"/search\"")]
        [InlineData("NewReleases.razor", "@page \"/new-releases\"")]
        [InlineData("TodaysDeals.razor", "@page \"/todays-deals\"")]
        [InlineData("StorefrontPage.razor", "@page \"/pages/{Slug}\"")]
        [InlineData("CartPage.razor", "@page \"/my-cart\"")]
        [InlineData("CheckoutPage.razor", "@page \"/checkout\"")]
        [InlineData("SignInPage.razor", "@page \"/signin\"")]
        [InlineData("RegisterPage.razor", "@page \"/register\"")]
        [InlineData("ForgotPasswordPage.razor", "@page \"/forgot-password\"")]
        [InlineData("ResetPasswordPage.razor", "@page \"/reset-password\"")]
        [InlineData("PaymentSuccessPage.razor", "@page \"/payment-success\"")]
        [InlineData("PaymentCancelPage.razor", "@page \"/payment-cancel\"")]
        [InlineData("MaintenancePage.razor", "@page \"/maintenance\"")]
        public void RoutePages_KeepExpectedRouteDeclarations(string fileName, string routeDeclaration)
        {
            var pagePath = FindStorefrontPageFile(fileName);

            Assert.NotNull(pagePath);
            Assert.Contains(routeDeclaration, File.ReadAllText(pagePath!), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(nameof(StorefrontRoutes.About), "/pages/about-us")]
        [InlineData(nameof(StorefrontRoutes.Faq), "/pages/faq")]
        [InlineData(nameof(StorefrontRoutes.Privacy), "/pages/privacy")]
        [InlineData(nameof(StorefrontRoutes.Terms), "/pages/terms")]
        [InlineData(nameof(StorefrontRoutes.CustomerService), "/pages/customer-service")]
        public void ContentRouteConstants_PointToDynamicPagesRenderer(string routeName, string expected)
        {
            var actual = routeName switch
            {
                nameof(StorefrontRoutes.About) => StorefrontRoutes.About,
                nameof(StorefrontRoutes.Faq) => StorefrontRoutes.Faq,
                nameof(StorefrontRoutes.Privacy) => StorefrontRoutes.Privacy,
                nameof(StorefrontRoutes.Terms) => StorefrontRoutes.Terms,
                nameof(StorefrontRoutes.CustomerService) => StorefrontRoutes.CustomerService,
                _ => throw new ArgumentOutOfRangeException(nameof(routeName), routeName, null),
            };

            Assert.Equal(expected, actual);
            Assert.StartsWith(StorefrontRoutes.PagesBase + "/", actual, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(StorefrontRoutes.Cart)]
        [InlineData(StorefrontRoutes.Checkout)]
        [InlineData(StorefrontRoutes.SignIn)]
        [InlineData(StorefrontRoutes.Register)]
        [InlineData(StorefrontRoutes.ForgotPassword)]
        [InlineData(StorefrontRoutes.ResetPassword)]
        [InlineData(StorefrontRoutes.Logout)]
        [InlineData(StorefrontRoutes.AccountProfile)]
        [InlineData(StorefrontRoutes.AccountChangePassword)]
        [InlineData(StorefrontRoutes.AccountAddresses)]
        [InlineData(StorefrontRoutes.AccountOrders)]
        [InlineData(StorefrontRoutes.PaymentSuccess)]
        [InlineData(StorefrontRoutes.PaymentCancel)]
        [InlineData(StorefrontRoutes.Maintenance)]
        public void PrivateAndApplicationRoutes_AreNotSitemapStaticRoutes(string route)
        {
            Assert.True(
                StorefrontIndexingPolicy.IsPrivateNoIndexPath(route) || route.StartsWith(StorefrontRoutes.Account, StringComparison.Ordinal),
                $"{route} must be noindex or account-scoped.");

            Assert.DoesNotContain(StorefrontRoutes.SitemapStaticPages, item => string.Equals(item.Path, route, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void SearchRoute_IsNoIndexAndNotASitemapStaticRoute()
        {
            Assert.True(StorefrontIndexingPolicy.IsSearchNoIndexPath(StorefrontRoutes.Search));
            Assert.DoesNotContain(StorefrontRoutes.SitemapStaticPages, item => string.Equals(item.Path, StorefrontRoutes.Search, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DynamicContentRenderer_WiresTemplatePresentationAndStructuredData()
        {
            var markup = File.ReadAllText(FindStorefrontPageFile("StorefrontPage.razor")!);

            Assert.Contains("@inject IStorefrontPagePresentationResolver PresentationResolver", markup, StringComparison.Ordinal);
            Assert.Contains("@inject IStorefrontStructuredDataComposer StructuredDataComposer", markup, StringComparison.Ordinal);
            Assert.Contains("<SeoHead Metadata=\"_metadata\" StructuredData=\"_structuredData\" />", markup, StringComparison.Ordinal);
            Assert.Contains("PresentationResolver.Resolve(_page)", markup, StringComparison.Ordinal);
            Assert.Contains("ComposeStructuredDataAsync(routePath, _page, _presentation)", markup, StringComparison.Ordinal);
            Assert.Contains("data-storefront-page-template", markup, StringComparison.Ordinal);
        }

        private static string? FindStorefrontPageFile(string fileName)
        {
            return EnumerateStorefrontPageFiles()
                .SingleOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.Ordinal));
        }

        private static IReadOnlyList<string> EnumerateStorefrontPageFiles()
        {
            return Directory
                .GetFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages"), "*.razor", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}

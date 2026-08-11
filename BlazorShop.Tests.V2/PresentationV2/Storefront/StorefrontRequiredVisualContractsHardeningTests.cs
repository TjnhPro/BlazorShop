namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontRequiredVisualContractsHardeningTests
    {
        [Fact]
        public void CartPage_RequiresPresentationOwnedContextAndPassesCartRootContracts()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor");

            Assert.Contains("[Parameter, EditorRequired]", page, StringComparison.Ordinal);
            Assert.Contains("public StorefrontCartPageContext Context { get; set; } = default!;", page, StringComparison.Ordinal);
            Assert.Contains("ArgumentNullException.ThrowIfNull(Context);", page, StringComparison.Ordinal);
            Assert.DoesNotContain("new StorefrontCartPageContext", page, StringComparison.Ordinal);
            Assert.DoesNotContain("= new(", page, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLinkContext.Default", page, StringComparison.Ordinal);

            foreach (var requiredAttribute in new[]
            {
                "InitialCart=\"Context.Cart\"",
                "InitialAlerts=\"Context.Alerts\"",
                "DataMode=\"StorefrontFeatureDataMode.InitialSnapshot\"",
                "Actions=\"@Context.CartActions\"",
                "CheckoutUrl=\"@Context.CheckoutUrl\"",
                "ContinueShoppingUrl=\"@Context.ContinueShoppingUrl\"",
                "SecondaryShoppingUrl=\"@Context.Links.Home.Href\""
            })
            {
                Assert.Contains(requiredAttribute, page, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void CartView_RequiresRootWiringWithoutOwningFallbackRoutesOrDescriptors()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/StorefrontCartView.razor");

            foreach (var requiredParameter in new[]
            {
                "StorefrontBrowserCart? InitialCart",
                "IReadOnlyList<StorefrontBrowserCartAlert> InitialAlerts",
                "StorefrontFeatureDataMode DataMode",
                "StorefrontCartActionDescriptor Actions",
                "StorefrontCartViewClasses Classes",
                "string CheckoutUrl",
                "string ContinueShoppingUrl",
                "string SecondaryShoppingUrl"
            })
            {
                AssertParameterIsEditorRequired(component, requiredParameter);
            }

            Assert.DoesNotContain("InitialAlerts { get; set; } = []", component, StringComparison.Ordinal);
            Assert.DoesNotContain("DataMode { get; set; } = StorefrontFeatureDataMode.BrowserFetch", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Actions { get; set; } = StorefrontCartActionDescriptor.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Classes { get; set; } = StorefrontCartViewClasses.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("CheckoutUrl { get; set; } = \"/checkout\"", component, StringComparison.Ordinal);
            Assert.DoesNotContain("ContinueShoppingUrl { get; set; } = \"/search\"", component, StringComparison.Ordinal);
            Assert.DoesNotContain("SecondaryShoppingUrl { get; set; } = \"/\"", component, StringComparison.Ordinal);

            foreach (var validation in new[]
            {
                "ArgumentNullException.ThrowIfNull(InitialAlerts);",
                "ArgumentNullException.ThrowIfNull(Actions);",
                "ArgumentNullException.ThrowIfNull(Classes);",
                "ArgumentException.ThrowIfNullOrWhiteSpace(CheckoutUrl);",
                "ArgumentException.ThrowIfNullOrWhiteSpace(ContinueShoppingUrl);",
                "ArgumentException.ThrowIfNullOrWhiteSpace(SecondaryShoppingUrl);"
            })
            {
                Assert.Contains(validation, component, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("ArgumentNullException.ThrowIfNull(InitialCart)", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Actions == StorefrontCartActionDescriptor.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Classes == StorefrontCartViewClasses.Empty", component, StringComparison.Ordinal);
            Assert.Contains("CartController.Initialize(InitialCart, InitialAlerts, DataMode, Actions);", component, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutShell_RequiresRootWiringWithoutOwningFallbackStateOrDescriptors()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/StorefrontCheckoutShell.razor");

            foreach (var requiredParameter in new[]
            {
                "StorefrontBrowserCheckoutState InitialState",
                "bool ShowPanel",
                "StorefrontFeatureDataMode DataMode",
                "StorefrontCheckoutActionDescriptor Actions",
                "StorefrontCheckoutViewClasses Classes"
            })
            {
                AssertParameterIsEditorRequired(component, requiredParameter);
            }

            Assert.DoesNotContain("StorefrontBrowserCheckoutDefaults.EmptyState(\"Checkout is not available yet.\")", component, StringComparison.Ordinal);
            Assert.DoesNotContain("ShowPanel { get; set; } = true", component, StringComparison.Ordinal);
            Assert.DoesNotContain("DataMode { get; set; } = StorefrontFeatureDataMode.BrowserFetch", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Actions { get; set; } = StorefrontCheckoutActionDescriptor.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Classes { get; set; } = StorefrontCheckoutViewClasses.Empty", component, StringComparison.Ordinal);

            foreach (var validation in new[]
            {
                "ArgumentNullException.ThrowIfNull(InitialState);",
                "ArgumentNullException.ThrowIfNull(Actions);",
                "ArgumentNullException.ThrowIfNull(Classes);"
            })
            {
                Assert.Contains(validation, component, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("Actions == StorefrontCheckoutActionDescriptor.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Classes == StorefrontCheckoutViewClasses.Empty", component, StringComparison.Ordinal);
            Assert.Contains("CheckoutController.Initialize(InitialState, ShowPanel, DataMode, Actions);", component, StringComparison.Ordinal);
            Assert.Contains("DataMode != StorefrontFeatureDataMode.InitialSnapshot", component, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckoutPage_RequiresContextAndPassesCheckoutShellContractsInEveryBranch()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");

            Assert.Contains("[Parameter, EditorRequired]", page, StringComparison.Ordinal);
            Assert.Contains("public StorefrontCheckoutPageContext Context { get; set; } = default!;", page, StringComparison.Ordinal);
            Assert.Contains("ArgumentNullException.ThrowIfNull(Context);", page, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(page, "<StorefrontCheckoutSection"));

            foreach (var requiredAttribute in new[]
            {
                "InitialState=\"Context.CheckoutState\"",
                "DataMode=\"StorefrontFeatureDataMode.InitialSnapshot\"",
                "Actions=\"@Context.CheckoutActions\"",
                "ShowPanel=\"false\""
            })
            {
                Assert.Equal(2, CountOccurrences(page, requiredAttribute));
            }
        }

        [Fact]
        public void AccountApp_RequiresRootWiringWhileKeepingOptionalMessagesAndNullablePresenceValues()
        {
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor");

            foreach (var requiredParameter in new[]
            {
                "string? Path",
                "int PageNumber",
                "string? AntiforgeryFieldName",
                "string? AntiforgeryRequestToken",
                "IReadOnlyList<AccountNavigationItem> NavigationItems",
                "AccountRouteDescriptor RouteDescriptor",
                "AccountNavigationClasses NavigationClasses",
                "StorefrontAccountProfileActionDescriptor ProfileActions",
                "StorefrontAccountPasswordActionDescriptor PasswordActions",
                "StorefrontAccountFormClasses AccountFormClasses",
                "StorefrontAccountAddressActionDescriptor AddressActions",
                "StorefrontAccountAddressBookClasses AddressClasses",
                "StorefrontAccountOrderActionDescriptor OrderActions",
                "StorefrontAccountOrderListClasses OrderListClasses",
                "StorefrontAccountOrderDetailClasses OrderDetailClasses",
                "StorefrontAccountShellClasses ShellClasses"
            })
            {
                AssertParameterIsEditorRequired(app, requiredParameter);
            }

            Assert.Contains("public string? Error { get; set; }", app, StringComparison.Ordinal);
            Assert.Contains("public string? Saved { get; set; }", app, StringComparison.Ordinal);
            Assert.DoesNotContain("[Parameter, EditorRequired]\r\n    public string? Error", NormalizeNewLines(app), StringComparison.Ordinal);
            Assert.DoesNotContain("[Parameter, EditorRequired]\r\n    public string? Saved", NormalizeNewLines(app), StringComparison.Ordinal);

            Assert.DoesNotContain("PageNumber { get; set; } = 1", app, StringComparison.Ordinal);
            Assert.DoesNotContain("NavigationItems { get; set; } = []", app, StringComparison.Ordinal);
            Assert.DoesNotContain("RouteDescriptor { get; set; } = AccountRouteDescriptor.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("NavigationClasses { get; set; } = AccountNavigationClasses.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("ProfileActions { get; set; } = StorefrontAccountProfileActionDescriptor.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("PasswordActions { get; set; } = StorefrontAccountPasswordActionDescriptor.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("AccountFormClasses { get; set; } = StorefrontAccountFormClasses.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("AddressActions { get; set; } = StorefrontAccountAddressActionDescriptor.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("AddressClasses { get; set; } = StorefrontAccountAddressBookClasses.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("OrderActions { get; set; } = StorefrontAccountOrderActionDescriptor.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("OrderListClasses { get; set; } = StorefrontAccountOrderListClasses.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("OrderDetailClasses { get; set; } = StorefrontAccountOrderDetailClasses.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("ShellClasses { get; set; } = StorefrontAccountShellClasses.Empty", app, StringComparison.Ordinal);

            foreach (var validation in new[]
            {
                "ArgumentNullException.ThrowIfNull(NavigationItems);",
                "ArgumentNullException.ThrowIfNull(RouteDescriptor);",
                "ArgumentNullException.ThrowIfNull(NavigationClasses);",
                "ArgumentNullException.ThrowIfNull(ProfileActions);",
                "ArgumentNullException.ThrowIfNull(PasswordActions);",
                "ArgumentNullException.ThrowIfNull(AccountFormClasses);",
                "ArgumentNullException.ThrowIfNull(AddressActions);",
                "ArgumentNullException.ThrowIfNull(AddressClasses);",
                "ArgumentNullException.ThrowIfNull(OrderActions);",
                "ArgumentNullException.ThrowIfNull(OrderListClasses);",
                "ArgumentNullException.ThrowIfNull(OrderDetailClasses);",
                "ArgumentNullException.ThrowIfNull(ShellClasses);"
            })
            {
                Assert.Contains(validation, app, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("ArgumentNullException.ThrowIfNull(Path)", app, StringComparison.Ordinal);
            Assert.DoesNotContain("ArgumentNullException.ThrowIfNull(AntiforgeryFieldName)", app, StringComparison.Ordinal);
            Assert.DoesNotContain("ArgumentNullException.ThrowIfNull(AntiforgeryRequestToken)", app, StringComparison.Ordinal);
            Assert.DoesNotContain("== AccountRouteDescriptor.Empty", app, StringComparison.Ordinal);
            Assert.DoesNotContain("== StorefrontAccount", app, StringComparison.Ordinal);
            Assert.Contains("AccountRouteParser.Resolve(Path, RouteDescriptor)", app, StringComparison.Ordinal);
            Assert.Contains("nameof(StorefrontAccountOrderList.PageNumber), PageNumber", app, StringComparison.Ordinal);
            Assert.DoesNotContain("Math.Max(1, PageNumber)", app, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountHostPage_RequiresContextAndPassesAccountAppRootContracts()
        {
            var host = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor");
            var pageService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Account/StorefrontAccountPageService.cs");

            Assert.Contains("[Parameter, EditorRequired]", host, StringComparison.Ordinal);
            Assert.Contains("public StorefrontAccountPageContext Context { get; set; } = default!;", host, StringComparison.Ordinal);
            Assert.Contains("ArgumentNullException.ThrowIfNull(Context);", host, StringComparison.Ordinal);
            Assert.Contains("Math.Max(1, page)", pageService, StringComparison.Ordinal);

            foreach (var requiredAttribute in new[]
            {
                "Path=\"@Context.Path\"",
                "PageNumber=\"@Context.Page\"",
                "AntiforgeryFieldName=\"@Context.AntiforgeryFieldName\"",
                "AntiforgeryRequestToken=\"@Context.AntiforgeryRequestToken\"",
                "NavigationItems=\"@Context.NavigationItems\"",
                "RouteDescriptor=\"@Context.RouteDescriptor\"",
                "NavigationClasses=\"StorefrontAccountViewOptions.NavigationClasses\"",
                "ProfileActions=\"@Context.ProfileActions\"",
                "PasswordActions=\"@Context.PasswordActions\"",
                "AccountFormClasses=\"StorefrontAccountViewOptions.FormClasses\"",
                "AddressActions=\"@Context.AddressActions\"",
                "AddressClasses=\"StorefrontAccountViewOptions.AddressClasses\"",
                "OrderActions=\"@Context.OrderActions\"",
                "OrderListClasses=\"StorefrontAccountViewOptions.OrderListClasses\"",
                "OrderDetailClasses=\"StorefrontAccountViewOptions.OrderDetailClasses\"",
                "ShellClasses=\"StorefrontAccountViewOptions.ShellClasses\""
            })
            {
                Assert.Contains(requiredAttribute, host, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void V2RootPages_DoNotCreateFallbackStorefrontPageContexts()
        {
            var pagesDirectory = Path.Combine(
                RepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.Storefront.V2",
                "Pages");

            foreach (var pagePath in Directory.EnumerateFiles(pagesDirectory, "*.razor", SearchOption.AllDirectories))
            {
                var page = File.ReadAllText(pagePath);
                Assert.DoesNotMatch(@"public\s+Storefront\w*PageContext\s+Context\s*\{\s*get;\s*set;\s*\}\s*=\s*new\s*\(", page);
                Assert.DoesNotMatch(@"new\s+Storefront\w*PageContext\s*\(", page);
            }
        }

        [Fact]
        public void V2WasmRootComponents_DoNotOwnRouteActionOrClassDefaults()
        {
            var rootComponents = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/StorefrontCartView.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/StorefrontCheckoutShell.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor"
            };

            foreach (var relativePath in rootComponents)
            {
                var component = ReadRepositoryFile(relativePath);

                foreach (var forbiddenRootDefault in new[]
                {
                    "Actions { get; set; } = StorefrontCartActionDescriptor.Empty",
                    "Classes { get; set; } = StorefrontCartViewClasses.Empty",
                    "CheckoutUrl { get; set; } = \"/checkout\"",
                    "ContinueShoppingUrl { get; set; } = \"/search\"",
                    "SecondaryShoppingUrl { get; set; } = \"/\"",
                    "InitialState { get; set; } = StorefrontBrowserCheckoutDefaults.EmptyState",
                    "ShowPanel { get; set; } = true",
                    "Actions { get; set; } = StorefrontCheckoutActionDescriptor.Empty",
                    "Classes { get; set; } = StorefrontCheckoutViewClasses.Empty",
                    "PageNumber { get; set; } = 1",
                    "NavigationItems { get; set; } = []",
                    "RouteDescriptor { get; set; } = AccountRouteDescriptor.Empty",
                    "NavigationClasses { get; set; } = AccountNavigationClasses.Empty",
                    "ProfileActions { get; set; } = StorefrontAccountProfileActionDescriptor.Empty",
                    "PasswordActions { get; set; } = StorefrontAccountPasswordActionDescriptor.Empty",
                    "AccountFormClasses { get; set; } = StorefrontAccountFormClasses.Empty",
                    "AddressActions { get; set; } = StorefrontAccountAddressActionDescriptor.Empty",
                    "AddressClasses { get; set; } = StorefrontAccountAddressBookClasses.Empty",
                    "OrderActions { get; set; } = StorefrontAccountOrderActionDescriptor.Empty",
                    "OrderListClasses { get; set; } = StorefrontAccountOrderListClasses.Empty",
                    "OrderDetailClasses { get; set; } = StorefrontAccountOrderDetailClasses.Empty",
                    "ShellClasses { get; set; } = StorefrontAccountShellClasses.Empty"
                })
                {
                    Assert.DoesNotContain(forbiddenRootDefault, component, StringComparison.Ordinal);
                }
            }
        }

        private static void AssertParameterIsEditorRequired(string source, string declaration)
        {
            Assert.Matches(
                @"\[Parameter,\s*EditorRequired\]\r\n\s*public\s+" + System.Text.RegularExpressions.Regex.Escape(declaration),
                NormalizeNewLines(source));
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static string NormalizeNewLines(string source) => source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string RepositoryRoot()
        {
            var current = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "BlazorShop.sln")))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }

            throw new DirectoryNotFoundException("Could not find repository root.");
        }
    }
}

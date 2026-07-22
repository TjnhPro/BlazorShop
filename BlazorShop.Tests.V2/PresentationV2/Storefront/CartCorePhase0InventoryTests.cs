namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class CartCorePhase0InventoryTests
    {
        private static readonly string[] BrowserOwnedCartRequestFields =
        [
            "CustomerId",
            "AppUserId",
            "UserId",
            "StoreId",
            "UnitPrice",
            "LineTotal",
            "Discount",
            "Tax",
            "Status",
        ];

        [Theory]
        [InlineData("StorefrontCreateCartSessionRequest")]
        [InlineData("StorefrontCartLineCreateRequest")]
        [InlineData("StorefrontCartLineUpdateRequest")]
        [InlineData("StorefrontCartValidateRequest")]
        public void CommerceNode_PublicCartRequestContractsDoNotExposeServerOwnedFields(string className)
        {
            var contracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/CartContracts.cs");
            var classBody = ExtractClassBody(contracts, className);

            foreach (var field in BrowserOwnedCartRequestFields)
            {
                Assert.DoesNotContain(field, classBody, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void CommerceNode_CartControllerDerivesStoreAndTokenServerSide()
        {
            var controllers = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCartController.cs");

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/cart\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[FromHeader(Name = CartTokenHeaderName)] string cartToken", controllers, StringComparison.Ordinal);
            Assert.Contains("new StorefrontCartCreateOrResumeRequest(storeId.Value, request.CartToken)", controllers, StringComparison.Ordinal);
            Assert.DoesNotContain("request.CustomerId", controllers, StringComparison.Ordinal);
            Assert.DoesNotContain("request.AppUserId", controllers, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceNode_CartContractMappingDoesNotTrustBrowserIdentityOrPrice()
        {
            var mappings = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/CartMappings.cs");
            var addLineMapping = ExtractMethodBody(mappings, "this StorefrontCartLineCreateRequest request");
            var updateLineMapping = ExtractMethodBody(mappings, "this StorefrontCartLineUpdateRequest request");

            Assert.Contains("storeId,", addLineMapping, StringComparison.Ordinal);
            Assert.Contains("cartToken,", addLineMapping, StringComparison.Ordinal);
            Assert.Contains("request.ProductId", addLineMapping, StringComparison.Ordinal);
            Assert.Contains("request.Quantity", addLineMapping, StringComparison.Ordinal);
            Assert.Contains("storeId,", updateLineMapping, StringComparison.Ordinal);
            Assert.Contains("cartToken,", updateLineMapping, StringComparison.Ordinal);

            var combined = addLineMapping + updateLineMapping;
            foreach (var field in BrowserOwnedCartRequestFields)
            {
                Assert.DoesNotContain("request." + field, combined, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StorefrontV2_LocalCartEndpointsKeepBrowserPayloadCustomerSafe()
        {
            var cartEndpoints = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCartEndpoints.cs");
            var addLineRoute = ExtractLambdaBody(cartEndpoints, "app.MapPost(\"/api/cart/lines\"");
            var updateLineRoute = ExtractLambdaBody(cartEndpoints, "app.MapPut(\"/api/cart/lines/{lineId:guid}\"");

            Assert.Contains("ValidateLocalCartAntiforgeryAsync", addLineRoute, StringComparison.Ordinal);
            Assert.Contains("new StorefrontCartLineCreateRequest", addLineRoute, StringComparison.Ordinal);
            Assert.Contains("ProductId = request.ProductId", addLineRoute, StringComparison.Ordinal);
            Assert.Contains("Quantity = request.Quantity", addLineRoute, StringComparison.Ordinal);
            Assert.Contains("ValidateLocalCartAntiforgeryAsync", updateLineRoute, StringComparison.Ordinal);
            Assert.Contains("request.Quantity < 1", updateLineRoute, StringComparison.Ordinal);

            var combined = addLineRoute + updateLineRoute;
            foreach (var field in BrowserOwnedCartRequestFields)
            {
                Assert.DoesNotContain(field + " =", combined, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StorefrontV2_CartTokenCookieIsHttpOnlyAndLegacyCartCookieIsDeletedAfterImport()
        {
            var tokenService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontCartTokenService.cs");

            Assert.Contains("StorefrontCookieNames.CartToken", tokenService, StringComparison.Ordinal);
            Assert.Contains("HttpOnly = true", tokenService, StringComparison.Ordinal);
            Assert.Contains("SameSite = SameSiteMode.Lax", tokenService, StringComparison.Ordinal);
            Assert.Contains("Secure = !this.environment.IsDevelopment()", tokenService, StringComparison.Ordinal);
            Assert.Contains("DeleteLegacyCartCookie(httpContext);", tokenService, StringComparison.Ordinal);
            Assert.Contains("ProductId = item.ProductId", tokenService, StringComparison.Ordinal);
            Assert.Contains("Quantity = Math.Max(1, item.Quantity)", tokenService, StringComparison.Ordinal);
            Assert.DoesNotContain("UnitPrice = item.UnitPrice", tokenService, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_CartPageConsumesServerProjection()
        {
            var cartPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CartPage.razor.cs");
            var support = ReadStorefrontLocalEndpointSupportSource();

            Assert.DoesNotContain("GetProductByIdAsync", cartPage, StringComparison.Ordinal);
            Assert.DoesNotContain("LoadProductsAsync", cartPage, StringComparison.Ordinal);
            Assert.Contains("cartItem.DisplayName", cartPage, StringComparison.Ordinal);
            Assert.Contains("cartItem.LineTotal", cartPage, StringComparison.Ordinal);
            Assert.Contains("cartItem.QuantityMaximum", cartPage, StringComparison.Ordinal);
            Assert.Contains("cartItem.Warnings", cartPage, StringComparison.Ordinal);
            Assert.Contains("CheckoutAllowed", cartPage, StringComparison.Ordinal);
            Assert.Contains("GrandTotal", support, StringComparison.Ordinal);
            Assert.Contains("Adjustments", support, StringComparison.Ordinal);
        }

        private static string ExtractClassBody(string source, string className)
        {
            return ExtractBlock(source, "class " + className);
        }

        private static string ExtractMethodBody(string source, string marker)
        {
            return ExtractBlock(source, marker);
        }

        private static string ExtractLambdaBody(string source, string marker)
        {
            var markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate marker '{marker}'.");
            }

            var arrowIndex = source.IndexOf("=>", markerIndex, StringComparison.Ordinal);
            if (arrowIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate lambda arrow for marker '{marker}'.");
            }

            return ExtractBlockFromIndex(source, arrowIndex, marker);
        }

        private static string ExtractBlock(string source, string marker)
        {
            var markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate marker '{marker}'.");
            }

            return ExtractBlockFromIndex(source, markerIndex, marker);
        }

        private static string ExtractBlockFromIndex(string source, int startIndex, string marker)
        {
            var openBraceIndex = source.IndexOf('{', startIndex);
            if (openBraceIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate opening brace for marker '{marker}'.");
            }

            var depth = 0;
            for (var index = openBraceIndex; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source[openBraceIndex..(index + 1)];
                    }
                }
            }

            throw new InvalidOperationException($"Could not locate closing brace for marker '{marker}'.");
        }

        private static string ReadStorefrontLocalEndpointSupportSource()
        {
            var root = FindStorefrontSupportRepositoryRoot();
            var endpointDirectory = Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "Endpoints");
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(endpointDirectory, "StorefrontLocalEndpointSupport*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }
        private static string FindStorefrontSupportRepositoryRoot()
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
        private static string ReadRepositoryFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
        }
    }
}

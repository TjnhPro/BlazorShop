namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class AddressCorePhase0InventoryTests
    {
        private static readonly string[] BrowserOwnedAddressRequestFields =
        [
            "CustomerId",
            "AppUserId",
            "UserId",
            "StoreId",
            "CreatedAt",
            "CreatedOn",
            "UpdatedAt",
            "UpdatedOn",
            "DeletedAt",
            "DeletedOn",
        ];

        private static readonly string[] OrderSnapshotAddressFields =
        [
            "ShippingFullName",
            "ShippingEmail",
            "ShippingPhone",
            "ShippingAddress1",
            "ShippingAddress2",
            "ShippingCity",
            "ShippingState",
            "ShippingPostalCode",
            "ShippingCountryCode",
        ];

        [Theory]
        [InlineData("StorefrontCheckoutShippingAddress")]
        [InlineData("StorefrontCheckoutPreviewRequest")]
        [InlineData("StorefrontPlaceOrderRequest")]
        public void CommerceNode_PublicCheckoutAddressContractsDoNotExposeServerOwnedFields(string className)
        {
            var contracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontApiContracts.cs");
            var classBody = ExtractClassBody(contracts, className);

            foreach (var field in BrowserOwnedAddressRequestFields.Concat(OrderSnapshotAddressFields))
            {
                Assert.DoesNotContain(field, classBody, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void CommerceNode_CheckoutControllerDerivesStoreScopeFromRoute()
        {
            var controllers = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");
            var checkoutController = ExtractClassBody(controllers, "StorefrontScopedCheckoutController");

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/checkout\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("ResolveStoreIdAsync(cancellationToken)", checkoutController, StringComparison.Ordinal);
            Assert.Contains("request.ToApplicationRequest(storeId.Value, cartToken)", checkoutController, StringComparison.Ordinal);
            Assert.Contains("storeId.Value,", checkoutController, StringComparison.Ordinal);
            Assert.DoesNotContain("request.StoreId", checkoutController, StringComparison.Ordinal);
            Assert.DoesNotContain("request.CustomerId", checkoutController, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_CheckoutLocalPayloadDoesNotSendServerOwnedAddressFields()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            Assert.Contains("ShippingAddress = new StorefrontCheckoutPreviewShippingAddress", program, StringComparison.Ordinal);
            Assert.Contains("CustomerEmail = form.CustomerEmail", program, StringComparison.Ordinal);
            Assert.Contains("CustomerName = form.CustomerName", program, StringComparison.Ordinal);

            foreach (var field in BrowserOwnedAddressRequestFields)
            {
                Assert.DoesNotContain(field + " =", program, StringComparison.Ordinal);
            }
        }

        private static string ExtractClassBody(string source, string className)
        {
            return ExtractBlock(source, "class " + className);
        }

        private static string ExtractBlock(string source, string marker)
        {
            var markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate marker '{marker}'.");
            }

            var openBraceIndex = source.IndexOf('{', markerIndex);
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

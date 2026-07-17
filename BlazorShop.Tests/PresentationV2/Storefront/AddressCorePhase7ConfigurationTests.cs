namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class AddressCorePhase7ConfigurationTests
    {
        [Fact]
        public void AddressFieldConfigurationContract_KeepsFutureOverrideShape()
        {
            var contracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontApiContracts.cs");
            var configuration = ExtractBlock(contracts, "record StorefrontAddressFieldConfigurationResponse");

            Assert.Contains("bool CompanyEnabled", configuration, StringComparison.Ordinal);
            Assert.Contains("bool PhoneEnabled", configuration, StringComparison.Ordinal);
            Assert.Contains("bool PhoneRequired", configuration, StringComparison.Ordinal);
            Assert.Contains("bool PostalCodeRequired", configuration, StringComparison.Ordinal);
            Assert.Contains("bool BillingAddressEnabled", configuration, StringComparison.Ordinal);
            Assert.Contains("bool UseShippingAddressAsBillingDefault", configuration, StringComparison.Ordinal);
            Assert.Contains("IReadOnlyList<string> StateProvinceRequiredCountryCodes", configuration, StringComparison.Ordinal);
        }

        [Fact]
        public void AddressFieldConfiguration_RemainsStorefrontScopedAndAnonymous()
        {
            var controllers = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs");
            var controller = ExtractBlock(controllers, "class StorefrontScopedAddressController");

            Assert.Contains("[Route(\"api/storefront/stores/{storeKey}/address\")]", controllers, StringComparison.Ordinal);
            Assert.Contains("[HttpGet(\"configuration\")]", controller, StringComparison.Ordinal);
            Assert.Contains("[AllowAnonymous]", controller, StringComparison.Ordinal);
            Assert.Contains("GetConfigurationAsync(cancellationToken)", controller, StringComparison.Ordinal);
            Assert.Contains("configuration.ToStorefrontContract()", controller, StringComparison.Ordinal);
        }

        [Fact]
        public void AddressCore_DoesNotIntroduceControlPlaneAddressSettingsUi()
        {
            var controlPlaneWebFiles = Directory.GetFiles(
                Path.Combine(FindRepositoryRoot(), "BlazorShop.PresentationV2", "BlazorShop.ControlPlane.Web"),
                "*.*",
                SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

            foreach (var path in controlPlaneWebFiles)
            {
                var source = File.ReadAllText(path);
                Assert.DoesNotContain("StorefrontAddressFieldConfiguration", source, StringComparison.Ordinal);
                Assert.DoesNotContain("customer/addresses", source, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("address/configuration", source, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string ExtractBlock(string source, string marker)
        {
            var markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate marker '{marker}'.");
            }

            var parenIndex = source.IndexOf('(', markerIndex);
            var braceIndex = source.IndexOf('{', markerIndex);
            var openIndex = (parenIndex, braceIndex) switch
            {
                (< 0, < 0) => -1,
                (< 0, _) => braceIndex,
                (_, < 0) => parenIndex,
                _ => Math.Min(parenIndex, braceIndex),
            };

            if (openIndex < 0)
            {
                throw new InvalidOperationException($"Could not locate block start for marker '{marker}'.");
            }

            var openCharacter = source[openIndex];
            var closeCharacter = openCharacter == '(' ? ')' : '}';
            var depth = 0;
            for (var index = openIndex; index < source.Length; index++)
            {
                if (source[index] == openCharacter)
                {
                    depth++;
                }
                else if (source[index] == closeCharacter)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source[openIndex..(index + 1)];
                    }
                }
            }

            throw new InvalidOperationException($"Could not locate block end for marker '{marker}'.");
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
}

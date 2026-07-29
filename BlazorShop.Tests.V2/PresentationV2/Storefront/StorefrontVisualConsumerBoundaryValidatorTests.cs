namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontVisualConsumerBoundaryValidatorTests
{
    private readonly StorefrontVisualConsumerBoundaryValidator validator = new();

    [Fact]
    public void F1_51_SharedValidator_PassesStorefrontV2()
    {
        var violations = validator.Validate(StorefrontV2Profile());

        Assert.Empty(violations);
    }

    [Fact]
    public void F1_51_SharedValidator_PassesStarter()
    {
        var violations = validator.Validate(StarterProfile());

        Assert.Empty(violations);
    }

    [Fact]
    public void F1_51_SharedValidator_PassesGeneratedProofWhenPresent()
    {
        var generatedRoot = RepositoryPath("artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
        var generatedProject = Path.Combine(generatedRoot, "BlazorShop.Storefront.GeneratedProof.csproj");
        var starterCartPage = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CartPage.razor");
        if (!File.Exists(generatedProject))
        {
            return;
        }

        if (File.GetLastWriteTimeUtc(generatedProject) < File.GetLastWriteTimeUtc(starterCartPage))
        {
            return;
        }

        var violations = validator.Validate(new StorefrontVisualConsumerProfile(
            "GeneratedProof",
            generatedRoot,
            "BlazorShop.Storefront.GeneratedProof.csproj",
            AllowedProjectReferenceFragments: [],
            AllowedPackageReferences:
            [
                "BlazorShop.Storefront.Presentation",
                "BlazorShop.Storefront.Components",
                "Microsoft.AspNetCore.Components.WebAssembly.Server",
            ],
            AllowedSourceRelativePaths: []));

        Assert.Empty(violations);
    }

    [Fact]
    public void F1_51_SharedValidator_FailsNegativeFixturesWithActionableMessages()
    {
        var fixtureRoot = CreateFixtureRoot();
        try
        {
            File.WriteAllText(
                Path.Combine(fixtureRoot, "BadStorefront.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <ItemGroup>
                    <ProjectReference Include="..\BlazorShop.Application\BlazorShop.Application.csproj" />
                    <PackageReference Include="BlazorShop.Storefront.Runtime" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """);
            Directory.CreateDirectory(Path.Combine(fixtureRoot, "Services"));
            File.WriteAllText(
                Path.Combine(fixtureRoot, "Services", "BadService.cs"),
                "public sealed class BadService { }");
            Directory.CreateDirectory(Path.Combine(fixtureRoot, "Components"));
            File.WriteAllText(
                Path.Combine(fixtureRoot, "Components", "BadTransport.razor"),
                """
                @inject IServiceProvider Services
                <button @onclick="Submit">Send</button>
                @code {
                    private Task Submit()
                    {
                        var client = Services.GetRequiredService<IStorefrontCartClient>();
                        return Task.CompletedTask;
                    }
                }
                """);
            File.WriteAllText(
                Path.Combine(fixtureRoot, "Components", "BadBrowser.js"),
                """
                document.addEventListener("storefront:product-purchase:add-line-succeeded", () => {});
                const app = window.blazorShopStorefront.application;
                window.blazorShopStorefront.bindings.addToCart.addPurchaseLine();
                window.blazorShopStorefront.bindings.productSelection.previewPurchase();
                window.blazorShopStorefront.application.cart.clear();
                addPurchaseLine(root, button); previewPurchase(root, button);
                app.cart.addLine({ ProductId: "1", ProductVariantId: "2", SelectedAttributes: [], CurrencyCode: "USD" });
                app.productSelection.preview("/api/product-selection-preview", { productId: "1", productVariantId: "2", selectedAttributes: [], currencyCode: "USD" });
                const selection = { skuText: "SKU SAFE", gtinText: "GTIN SAFE", stockText: "In stock", priceText: "$10.00" };
                if (preview.canAddToCart && preview.stockQuantity > 0 && preview.isAvailable && preview.validationMessages.length === 0) {
                    console.log(preview.unitPrice, preview.formattedUnitPrice, preview.formattedComparePrice, preview.sku, preview.gtin);
                }
                if (preview["canAddToCart"] && preview['stockQuantity'] > 0) {
                    console.log(preview["sku"], preview['gtin']);
                }
                fetch('/api/consent'); new XMLHttpRequest();
                """);
            File.WriteAllText(
                Path.Combine(fixtureRoot, "Components", "BadClient.cs"),
                "public sealed class BadClient : IStorefrontCartClient { }");

            var violations = validator.Validate(new StorefrontVisualConsumerProfile(
                "BadFixture",
                fixtureRoot,
                "BadStorefront.csproj",
                AllowedProjectReferenceFragments: [],
                AllowedPackageReferences: [],
                AllowedSourceRelativePaths: []));

            Assert.Contains(violations, violation => violation.Forbidden.Contains("ProjectReference", StringComparison.Ordinal));
            Assert.Contains(violations, violation => violation.Forbidden.Contains("PackageReference:BlazorShop.Storefront.Runtime", StringComparison.Ordinal));
            Assert.Contains(violations, violation => violation.Forbidden == "folder:Services");
            Assert.Contains(violations, violation => violation.Forbidden == "GetRequiredService<");
            Assert.Contains(violations, violation => violation.Forbidden == "fetch(");
            Assert.Contains(violations, violation => violation.Forbidden == "XMLHttpRequest");
            Assert.Contains(violations, violation => violation.Forbidden == ": IStorefront");
            Assert.Contains(violations, violation => violation.Forbidden == "application.cart");
            Assert.Contains(violations, violation => violation.Forbidden == "blazorShopStorefront.application");
            Assert.Contains(violations, violation => violation.Forbidden == "blazorShopStorefront.bindings.addToCart");
            Assert.Contains(violations, violation => violation.Forbidden == "blazorShopStorefront.bindings.productSelection");
            Assert.Contains(violations, violation => violation.Forbidden == "addPurchaseLine(");
            Assert.Contains(violations, violation => violation.Forbidden == "previewPurchase(");
            Assert.Contains(violations, violation => violation.Forbidden == "cart.addLine");
            Assert.Contains(violations, violation => violation.Forbidden == "productSelection.preview");
            Assert.Contains(violations, violation => violation.Forbidden == "ProductId:");
            Assert.Contains(violations, violation => violation.Forbidden == "productId:");
            Assert.Contains(violations, violation => violation.Forbidden == "preview.canAddToCart");
            Assert.Contains(violations, violation => violation.Forbidden == "preview.stockQuantity");
            Assert.Contains(violations, violation => violation.Forbidden == "preview.formattedUnitPrice");
            Assert.Contains(violations, violation => violation.Forbidden == "preview.sku");
            Assert.Contains(violations, violation => violation.Forbidden == "preview.gtin");
            Assert.Contains(violations, violation => violation.Forbidden == "preview[\"canAddToCart\"]");
            Assert.Contains(violations, violation => violation.Forbidden == "preview['stockQuantity']");
            Assert.Contains(violations, violation => violation.Forbidden == "preview[\"sku\"]");
            Assert.Contains(violations, violation => violation.Forbidden == "preview['gtin']");
            Assert.DoesNotContain(violations, violation => violation.Forbidden == "skuText");
            Assert.DoesNotContain(violations, violation => violation.Forbidden == "gtinText");
            Assert.DoesNotContain(violations, violation => violation.Forbidden == "stockText");
            Assert.DoesNotContain(violations, violation => violation.Forbidden == "priceText");
            Assert.DoesNotContain(violations, violation => violation.Forbidden == "storefront:product-purchase:add-line-succeeded");
            Assert.All(violations, violation =>
            {
                Assert.False(string.IsNullOrWhiteSpace(violation.RelativePath));
                Assert.Contains("Owner:", violation.ToString(), StringComparison.Ordinal);
                Assert.Contains("Remediation:", violation.ToString(), StringComparison.Ordinal);
            });
        }
        finally
        {
            if (Directory.Exists(fixtureRoot))
            {
                Directory.Delete(fixtureRoot, recursive: true);
            }
        }
    }

    private static StorefrontVisualConsumerProfile StorefrontV2Profile()
    {
        return new StorefrontVisualConsumerProfile(
            "StorefrontV2",
            RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2"),
            "BlazorShop.Storefront.V2.csproj",
            AllowedProjectReferenceFragments:
            [
                "BlazorShop.ServiceDefaults",
                "BlazorShop.Storefront.Components",
                "BlazorShop.Storefront.Presentation",
                "BlazorShop.Storefront.V2.WASM",
            ],
            AllowedPackageReferences:
            [
                "Microsoft.AspNetCore.Components.WebAssembly.Server",
                "Microsoft.Extensions.Http.Resilience",
            ],
            AllowedSourceRelativePaths: []);
    }

    private static StorefrontVisualConsumerProfile StarterProfile()
    {
        return new StorefrontVisualConsumerProfile(
            "Starter",
            RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"),
            "BlazorShop.Storefront.Starter.csproj",
            AllowedProjectReferenceFragments:
            [
                "BlazorShop.Storefront.Presentation",
            ],
            AllowedPackageReferences:
            [
                "BlazorShop.Storefront.Components",
                "Microsoft.AspNetCore.Components.WebAssembly.Server",
            ],
            AllowedSourceRelativePaths: []);
    }

    private static string CreateFixtureRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"blazorshop-visual-boundary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string RepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}

namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Reflection;

using BlazorShop.Storefront.Components.Contracts.Components;
using BlazorShop.Storefront.Components.Hybrid.Content;
using BlazorShop.Storefront.Components.Ssr.Brand;
using BlazorShop.Storefront.Components.WasmHost.Catalog;
using BlazorShop.Storefront.Components.WasmHost.System;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class StorefrontComponentDescriptorTests
{
    private static readonly string[] ModeProjectDirectories =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
    ];

    [Fact]
    public void ValidDescriptorPasses()
    {
        var descriptor = new StorefrontComponentDescriptor(
            "brand-logo",
            StorefrontComponentMode.Ssr,
            StorefrontComponentCategory.Brand,
            typeof(ComponentFixture));

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("BrandLogo")]
    [InlineData("brand_logo")]
    [InlineData("brand/logo")]
    [InlineData("brand.logo")]
    [InlineData("brand--logo")]
    public void InvalidKeyFails(string key)
    {
        var descriptor = new StorefrontComponentDescriptor(
            key,
            StorefrontComponentMode.Ssr,
            StorefrontComponentCategory.Brand,
            typeof(ComponentFixture));

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void InvalidModeFails()
    {
        var descriptor = new StorefrontComponentDescriptor(
            "brand-logo",
            (StorefrontComponentMode)999,
            StorefrontComponentCategory.Brand,
            typeof(ComponentFixture));

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Mode", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidCategoryFails()
    {
        var descriptor = new StorefrontComponentDescriptor(
            "brand-logo",
            StorefrontComponentMode.Ssr,
            (StorefrontComponentCategory)999,
            typeof(ComponentFixture));

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Category", StringComparison.Ordinal));
    }

    [Fact]
    public void NullComponentTypeFails()
    {
        var descriptor = new StorefrontComponentDescriptor(
            "brand-logo",
            StorefrontComponentMode.Ssr,
            StorefrontComponentCategory.Brand,
            null!);

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("ComponentType", StringComparison.Ordinal));
    }

    [Fact]
    public void TypeNotImplementingIComponentFails()
    {
        var descriptor = new StorefrontComponentDescriptor(
            "brand-logo",
            StorefrontComponentMode.Ssr,
            StorefrontComponentCategory.Brand,
            typeof(NotAComponent));

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("IComponent", StringComparison.Ordinal));
    }

    [Fact]
    public void RazorComponentFixtureImplementingIComponentPasses()
    {
        var descriptor = new StorefrontComponentDescriptor(
            "cart-summary",
            StorefrontComponentMode.WasmHost,
            StorefrontComponentCategory.Cart,
            typeof(ComponentFixture));

        var result = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RepositoryReferenceDescriptorInventoryMatchesCurrentMvp()
    {
        var descriptorCandidates = DiscoverRepositoryDescriptors()
            .Select(candidate => candidate.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactFormDescriptor.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Brand/StorefrontBrandLogoDescriptor.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Catalog/StorefrontDiscountedProductRailDescriptor.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/System/StorefrontHybridRuntimeProbeDescriptor.cs",
            ],
            descriptorCandidates);
    }

    [Fact]
    public void RepositoryPublicDescriptorsAreSemanticallyValid()
    {
        var descriptors = DiscoverRepositoryDescriptors();

        Assert.Equal(
            [
                "brand-logo",
                "contact-form",
                "discounted-product-rail",
                "hybrid-runtime-probe",
            ],
            descriptors.Select(candidate => candidate.Descriptor.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray());

        foreach (var candidate in descriptors)
        {
            var validation = StorefrontComponentDescriptorValidator.Validate(candidate.Descriptor);

            Assert.True(validation.IsValid, $"{candidate.RelativePath}: {string.Join("; ", validation.Errors)}");
            Assert.True(Enum.IsDefined(candidate.Descriptor.Mode));
            Assert.True(Enum.IsDefined(candidate.Descriptor.Category));
            Assert.True(typeof(IComponent).IsAssignableFrom(candidate.Descriptor.ComponentType));
        }
    }

    [Fact]
    public void RepositoryPublicDescriptorKeysAreUnique()
    {
        var duplicateKeys = DiscoverRepositoryDescriptors()
            .GroupBy(candidate => candidate.Descriptor.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(candidate => candidate.RelativePath).OrderBy(path => path, StringComparer.Ordinal))}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(duplicateKeys);
    }

    [Fact]
    public void DescriptorContractDoesNotOwnRouteRenderModeThemeOrRegistryMetadata()
    {
        var propertyNames = typeof(StorefrontComponentDescriptor)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(StorefrontComponentDescriptor.Category),
                nameof(StorefrontComponentDescriptor.ComponentType),
                nameof(StorefrontComponentDescriptor.Key),
                nameof(StorefrontComponentDescriptor.Mode),
            ],
            propertyNames);
    }

    [Fact]
    public void BrandLogoDescriptorIsValidAndMatchesSsrMode()
    {
        var descriptor = StorefrontBrandLogoDescriptor.Descriptor;

        var validation = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.Equal("brand-logo", descriptor.Key);
        Assert.Equal(StorefrontComponentMode.Ssr, descriptor.Mode);
        Assert.Equal(StorefrontComponentCategory.Brand, descriptor.Category);
        Assert.Equal(typeof(StorefrontBrandLogo), descriptor.ComponentType);
    }

    [Fact]
    public void ContactFormDescriptorIsValidAndMatchesHybridMode()
    {
        var descriptor = StorefrontContactFormDescriptor.Descriptor;

        var validation = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.Equal("contact-form", descriptor.Key);
        Assert.Equal(StorefrontComponentMode.Hybrid, descriptor.Mode);
        Assert.Equal(StorefrontComponentCategory.Content, descriptor.Category);
        Assert.Equal(typeof(StorefrontContactForm), descriptor.ComponentType);
    }

    [Fact]
    public void DiscountedProductRailDescriptorIsValidAndMatchesWasmHostMode()
    {
        var descriptor = StorefrontDiscountedProductRailDescriptor.Descriptor;

        var validation = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.Equal("discounted-product-rail", descriptor.Key);
        Assert.Equal(StorefrontComponentMode.WasmHost, descriptor.Mode);
        Assert.Equal(StorefrontComponentCategory.Catalog, descriptor.Category);
        Assert.Equal(typeof(StorefrontDiscountedProductRail), descriptor.ComponentType);
    }

    [Fact]
    public void HybridRuntimeProbeDescriptorIsValidAndCanLiveInWasmHostProject()
    {
        var descriptor = StorefrontHybridRuntimeProbeDescriptor.Descriptor;

        var validation = StorefrontComponentDescriptorValidator.Validate(descriptor);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.Equal("hybrid-runtime-probe", descriptor.Key);
        Assert.Equal(StorefrontComponentMode.Hybrid, descriptor.Mode);
        Assert.Equal(StorefrontComponentCategory.System, descriptor.Category);
        Assert.Equal(typeof(StorefrontHybridRuntimeProbe), descriptor.ComponentType);
        Assert.Equal("BlazorShop.Storefront.Components.WasmHost", descriptor.ComponentType.Assembly.GetName().Name);
    }

    [Fact]
    public void ContactFormAppDoesNotPublishPublicDescriptor()
    {
        var wasmHostDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost");
        var descriptorCandidates = Directory.EnumerateFiles(wasmHostDirectory, "*", SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor")
            .Where(file =>
            {
                var source = File.ReadAllText(file);
                return source.Contains("StorefrontComponentDescriptor", StringComparison.Ordinal) &&
                    source.Contains("StorefrontContactFormApp", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.Empty(descriptorCandidates);
    }

    private sealed class ComponentFixture : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NotAComponent
    {
    }

    private static StorefrontComponentDescriptor CreateDescriptor(StorefrontComponentMode mode)
    {
        return new StorefrontComponentDescriptor(
            "brand-logo",
            mode,
            StorefrontComponentCategory.Brand,
            typeof(ComponentFixture));
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static IReadOnlyList<RepositoryDescriptorCandidate> DiscoverRepositoryDescriptors()
    {
        return ModeProjectDirectories
            .Select(RepositoryPath)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            .Where(IsActiveSourceFile)
            .Where(file => File.ReadAllText(file).Contains("StorefrontComponentDescriptor", StringComparison.Ordinal))
            .Select(CreateDescriptorCandidate)
            .OrderBy(candidate => candidate.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static RepositoryDescriptorCandidate CreateDescriptorCandidate(string file)
    {
        var relativePath = Path.GetRelativePath(RepositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
        var descriptorHolderType = ResolveDescriptorHolderType(file, ResolveAssemblyNameFromPath(relativePath));
        var property = descriptorHolderType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{descriptorHolderType.FullName} must expose a public static Descriptor property.");
        var descriptor = property.GetValue(null) as StorefrontComponentDescriptor
            ?? throw new InvalidOperationException($"{descriptorHolderType.FullName}.Descriptor must return StorefrontComponentDescriptor.");

        return new RepositoryDescriptorCandidate(relativePath, descriptor);
    }

    private static Type ResolveDescriptorHolderType(string file, string assemblyName)
    {
        var source = File.ReadAllText(file);
        var namespaceName = source
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("namespace ", StringComparison.Ordinal))
            .Select(line => line["namespace ".Length..].Trim().TrimEnd(';'))
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"{file} must declare a file-scoped namespace.");
        var typeName = Path.GetFileNameWithoutExtension(file);

        return Type.GetType($"{namespaceName}.{typeName}, {assemblyName}", throwOnError: true)!
            ?? throw new InvalidOperationException($"{namespaceName}.{typeName} could not be loaded from {assemblyName}.");
    }

    private static string ResolveAssemblyNameFromPath(string relativePath)
    {
        if (relativePath.StartsWith("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/", StringComparison.Ordinal))
        {
            return "BlazorShop.Storefront.Components.Ssr";
        }

        if (relativePath.StartsWith("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/", StringComparison.Ordinal))
        {
            return "BlazorShop.Storefront.Components.Hybrid";
        }

        if (relativePath.StartsWith("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/", StringComparison.Ordinal))
        {
            return "BlazorShop.Storefront.Components.WasmHost";
        }

        throw new InvalidOperationException($"Descriptor file is outside a known mode project: {relativePath}");
    }

    private static bool IsActiveSourceFile(string file)
    {
        return Path.GetExtension(file) is ".cs" or ".razor"
            && !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed record RepositoryDescriptorCandidate(
        string RelativePath,
        StorefrontComponentDescriptor Descriptor);
}

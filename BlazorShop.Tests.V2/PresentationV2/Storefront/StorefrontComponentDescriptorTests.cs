namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Components;
using BlazorShop.Storefront.Components.Ssr.Brand;

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

    [Theory]
    [InlineData("BlazorShop.Storefront.Components.Ssr", StorefrontComponentMode.Ssr)]
    [InlineData("BlazorShop.Storefront.Components.Hybrid", StorefrontComponentMode.Hybrid)]
    [InlineData("BlazorShop.Storefront.Components.WasmHost", StorefrontComponentMode.WasmHost)]
    public void OwnerModeResolverMapsKnownModeAssemblies(
        string assemblyName,
        StorefrontComponentMode expectedMode)
    {
        var mode = StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(assemblyName);

        Assert.Equal(expectedMode, mode);
    }

    [Theory]
    [InlineData("BlazorShop.Storefront.Components")]
    [InlineData("")]
    [InlineData(null)]
    public void OwnerModeResolverTreatsUnknownEmptyOrNullAssembliesAsNotApplicable(string? assemblyName)
    {
        var mode = StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(assemblyName);

        Assert.Null(mode);
    }

    [Fact]
    public void OwnerModeResolverTreatsNonModeComponentAssembliesAsNotApplicable()
    {
        var mode = StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(typeof(ComponentFixture));

        Assert.Null(mode);
    }

    [Theory]
    [InlineData(StorefrontComponentMode.Ssr)]
    [InlineData(StorefrontComponentMode.Hybrid)]
    [InlineData(StorefrontComponentMode.WasmHost)]
    public void DescriptorModeConsistencyPassesWhenDescriptorModeMatchesOwnerMode(StorefrontComponentMode mode)
    {
        var result = StorefrontComponentDescriptorModeOwnership.Validate(
            CreateDescriptor(mode),
            mode);

        Assert.True(result.IsApplicable);
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Fact]
    public void DescriptorModeConsistencySkipsUnknownOwnerMode()
    {
        var result = StorefrontComponentDescriptorModeOwnership.Validate(
            CreateDescriptor(StorefrontComponentMode.Ssr),
            null);

        Assert.False(result.IsApplicable);
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(StorefrontComponentMode.Ssr, StorefrontComponentMode.Hybrid)]
    [InlineData(StorefrontComponentMode.Hybrid, StorefrontComponentMode.WasmHost)]
    [InlineData(StorefrontComponentMode.WasmHost, StorefrontComponentMode.Ssr)]
    public void DescriptorModeConsistencyFailsWhenDescriptorModeDiffersFromOwnerMode(
        StorefrontComponentMode descriptorMode,
        StorefrontComponentMode ownerMode)
    {
        var result = StorefrontComponentDescriptorModeOwnership.Validate(
            CreateDescriptor(descriptorMode),
            ownerMode);

        Assert.True(result.IsApplicable);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Contains($"declares mode '{descriptorMode}'", result.Error, StringComparison.Ordinal);
        Assert.Contains($"owning assembly mode is '{ownerMode}'", result.Error, StringComparison.Ordinal);
        Assert.Contains("brand-logo", result.Error, StringComparison.Ordinal);
        Assert.Contains(typeof(ComponentFixture).FullName!, result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoryModeProjectsExposeExpectedPhaseTwoBrandDescriptorOnly()
    {
        var descriptorCandidates = ModeProjectDirectories
            .Select(RepositoryPath)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor")
            .Where(file => File.ReadAllText(file).Contains("StorefrontComponentDescriptor", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(RepositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Brand/StorefrontBrandLogoDescriptor.cs"],
            descriptorCandidates);
    }

    [Fact]
    public void BrandLogoDescriptorIsValidAndMatchesSsrMode()
    {
        var descriptor = StorefrontBrandLogoDescriptor.Descriptor;

        var validation = StorefrontComponentDescriptorValidator.Validate(descriptor);
        var ownership = StorefrontComponentDescriptorModeOwnership.Validate(
            descriptor,
            StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(descriptor.ComponentType));

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.True(ownership.IsValid, ownership.Error);
        Assert.Equal("brand-logo", descriptor.Key);
        Assert.Equal(StorefrontComponentMode.Ssr, descriptor.Mode);
        Assert.Equal(StorefrontComponentCategory.Brand, descriptor.Category);
        Assert.Equal(typeof(StorefrontBrandLogo), descriptor.ComponentType);
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

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static class StorefrontComponentDescriptorModeOwnership
    {
        public static StorefrontComponentMode? ResolveOwnerMode(Type componentType)
        {
            return ResolveOwnerMode(componentType.Assembly.GetName().Name);
        }

        public static StorefrontComponentMode? ResolveOwnerMode(string? assemblyName)
        {
            return assemblyName switch
            {
                "BlazorShop.Storefront.Components.Ssr" => StorefrontComponentMode.Ssr,
                "BlazorShop.Storefront.Components.Hybrid" => StorefrontComponentMode.Hybrid,
                "BlazorShop.Storefront.Components.WasmHost" => StorefrontComponentMode.WasmHost,
                _ => null,
            };
        }

        public static StorefrontComponentDescriptorModeConsistencyResult Validate(
            StorefrontComponentDescriptor descriptor,
            StorefrontComponentMode? ownerMode)
        {
            if (ownerMode is null)
            {
                return StorefrontComponentDescriptorModeConsistencyResult.NotApplicable;
            }

            if (descriptor.Mode == ownerMode.Value)
            {
                return StorefrontComponentDescriptorModeConsistencyResult.Valid;
            }

            var componentType = descriptor.ComponentType;
            var componentTypeName = componentType?.FullName ?? "<null>";
            var assemblyName = componentType?.Assembly.GetName().Name ?? "<null>";

            return StorefrontComponentDescriptorModeConsistencyResult.Invalid(
                $"Component descriptor '{descriptor.Key}' declares mode '{descriptor.Mode}', but owning assembly mode is '{ownerMode.Value}'. Component type: '{componentTypeName}'. Assembly: '{assemblyName}'.");
        }
    }

    private sealed record StorefrontComponentDescriptorModeConsistencyResult(
        bool IsApplicable,
        bool IsValid,
        string? Error)
    {
        public static StorefrontComponentDescriptorModeConsistencyResult Valid { get; } = new(true, true, null);

        public static StorefrontComponentDescriptorModeConsistencyResult NotApplicable { get; } = new(false, true, null);

        public static StorefrontComponentDescriptorModeConsistencyResult Invalid(string error)
        {
            return new StorefrontComponentDescriptorModeConsistencyResult(true, false, error);
        }
    }
}

namespace BlazorShop.Tests.PresentationV2.Storefront;

using System.Reflection;

using BlazorShop.Storefront.Components.Contracts.Components;
using BlazorShop.Storefront.Components.Hybrid.Content;
using BlazorShop.Storefront.Components.Ssr.Brand;
using BlazorShop.Storefront.Components.WasmHost.Catalog;

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
    public void RepositoryModeProjectsExposeExpectedReferenceDescriptorsOnly()
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
            ],
            descriptorCandidates);
    }

    [Fact]
    public void RepositoryModeProjectDescriptorsAreValidAndOwnedByTheirModeProjects()
    {
        var descriptors = DiscoverRepositoryDescriptors();

        Assert.Equal(
            [
                "brand-logo",
                "contact-form",
                "discounted-product-rail",
            ],
            descriptors.Select(candidate => candidate.Descriptor.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray());

        foreach (var candidate in descriptors)
        {
            var validation = StorefrontComponentDescriptorValidator.Validate(candidate.Descriptor);
            var ownership = StorefrontComponentDescriptorModeOwnership.Validate(
                candidate.Descriptor,
                candidate.OwnerMode);
            var componentOwnerMode = StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(candidate.Descriptor.ComponentType);

            Assert.True(validation.IsValid, $"{candidate.RelativePath}: {string.Join("; ", validation.Errors)}");
            Assert.True(ownership.IsValid, $"{candidate.RelativePath}: {ownership.Error}");
            Assert.Equal(candidate.OwnerMode, candidate.Descriptor.Mode);
            Assert.Equal(candidate.OwnerMode, componentOwnerMode);
            Assert.True(Enum.IsDefined(candidate.Descriptor.Category));
            Assert.True(typeof(IComponent).IsAssignableFrom(candidate.Descriptor.ComponentType));
        }
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

    [Fact]
    public void ContactFormDescriptorIsValidAndMatchesHybridMode()
    {
        var descriptor = StorefrontContactFormDescriptor.Descriptor;

        var validation = StorefrontComponentDescriptorValidator.Validate(descriptor);
        var ownership = StorefrontComponentDescriptorModeOwnership.Validate(
            descriptor,
            StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(descriptor.ComponentType));

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.True(ownership.IsValid, ownership.Error);
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
        var ownership = StorefrontComponentDescriptorModeOwnership.Validate(
            descriptor,
            StorefrontComponentDescriptorModeOwnership.ResolveOwnerMode(descriptor.ComponentType));

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.True(ownership.IsValid, ownership.Error);
        Assert.Equal("discounted-product-rail", descriptor.Key);
        Assert.Equal(StorefrontComponentMode.WasmHost, descriptor.Mode);
        Assert.Equal(StorefrontComponentCategory.Catalog, descriptor.Category);
        Assert.Equal(typeof(StorefrontDiscountedProductRail), descriptor.ComponentType);
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
        var ownerMode = ResolveOwnerModeFromPath(relativePath);
        var descriptorHolderType = ResolveDescriptorHolderType(file, ownerMode);
        var property = descriptorHolderType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{descriptorHolderType.FullName} must expose a public static Descriptor property.");
        var descriptor = property.GetValue(null) as StorefrontComponentDescriptor
            ?? throw new InvalidOperationException($"{descriptorHolderType.FullName}.Descriptor must return StorefrontComponentDescriptor.");

        return new RepositoryDescriptorCandidate(relativePath, ownerMode, descriptor);
    }

    private static Type ResolveDescriptorHolderType(string file, StorefrontComponentMode ownerMode)
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
        var assemblyName = ownerMode switch
        {
            StorefrontComponentMode.Ssr => "BlazorShop.Storefront.Components.Ssr",
            StorefrontComponentMode.Hybrid => "BlazorShop.Storefront.Components.Hybrid",
            StorefrontComponentMode.WasmHost => "BlazorShop.Storefront.Components.WasmHost",
            _ => throw new ArgumentOutOfRangeException(nameof(ownerMode), ownerMode, null),
        };

        return Type.GetType($"{namespaceName}.{typeName}, {assemblyName}", throwOnError: true)!
            ?? throw new InvalidOperationException($"{namespaceName}.{typeName} could not be loaded from {assemblyName}.");
    }

    private static StorefrontComponentMode ResolveOwnerModeFromPath(string relativePath)
    {
        if (relativePath.StartsWith("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/", StringComparison.Ordinal))
        {
            return StorefrontComponentMode.Ssr;
        }

        if (relativePath.StartsWith("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/", StringComparison.Ordinal))
        {
            return StorefrontComponentMode.Hybrid;
        }

        if (relativePath.StartsWith("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/", StringComparison.Ordinal))
        {
            return StorefrontComponentMode.WasmHost;
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
        StorefrontComponentMode OwnerMode,
        StorefrontComponentDescriptor Descriptor);

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

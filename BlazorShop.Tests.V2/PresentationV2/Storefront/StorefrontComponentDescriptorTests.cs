namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Components;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class StorefrontComponentDescriptorTests
{
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

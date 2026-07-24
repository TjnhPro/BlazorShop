# BlazorShop.Storefront.Sample

Generated deterministic storefront sample.

- Source: BlazorShop.Storefront.Starter
- Store key: sample
- Commerce Node base URL is configured server-side.
- Package versions are pinned in StorefrontPackageVersions.props.

Build after packing local packages:

dotnet restore BlazorShop.Storefront.Sample.csproj
dotnet build BlazorShop.Storefront.Sample.csproj --no-restore

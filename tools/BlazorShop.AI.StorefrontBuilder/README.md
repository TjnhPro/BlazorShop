# BlazorShop.AI.StorefrontBuilder

Development-time visual reverse-engineering tooling for generated BlazorShop storefronts.

This folder is intentionally outside production runtime projects. It does not contain a production `.csproj`, does not get referenced by V2 runtime projects, and is used only by developers or agents running StorefrontBuilder workflows.

Target generated projects use:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}
```

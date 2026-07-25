# Storefront Independence Boundary QA

## SIB0 - Scope lock, baseline and inventory

Date: 2026-07-25.

Baseline commit: `f4ade924`.

Working tree at start:

- No tracked in-flight code changes were present.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Properties/launchSettings.json` existed as unrelated untracked local file and was not touched.
- `docs/refactor-control-Commerce-storefront/Storefront Independence Boundary.todo.md` is the active phase plan for this work.

Dependency inventory:

| Area | Current state |
| --- | --- |
| `Storefront.V2` project references | References `ServiceDefaults`, `Storefront.Client`, `Storefront.Components`, `Storefront.Runtime`, `Storefront.WASM`, and offending `Web.SharedV2`. |
| `Storefront.WASM` project references | References only `Storefront.Components`. |
| `Storefront.Components` project references | No project references; browser component package only. |
| `Storefront.Runtime` project references | References only `Storefront.Client`. |
| `Storefront.Client` project references | No forbidden project references. |
| `Storefront.Starter` package references | Uses package references for `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime`. |

Offenders:

| File | Current dependency | Symbol/pattern used | Target owner | Replacement action | Required test |
| --- | --- | --- | --- | --- | --- |
| `BlazorShop.Storefront.V2.csproj` | `BlazorShop.Web.SharedV2` project reference | ProjectReference | None | Remove after Storefront-local symbols exist. | Storefront V2 build and architecture guard. |
| `Dockerfile` | `BlazorShop.Web.SharedV2` build copy | Shared project copy | None | Remove copy lines. | Static scan and publish/build. |
| `tailwind.config.js` | `BlazorShop.Web.SharedV2` source scan | Shared Razor/C# scan | None | Remove shared source globs. | Static scan plus Storefront asset build. |
| `Configuration/StorefrontRateLimitIdentity.cs` | `BlazorShop.Web.SharedV2` | `StorefrontCookieNames.CartToken` | Storefront V2 host | Move cookie names to V2-local constants. | Rate-limit/cart token focused tests. |
| `Endpoints/Storefront*.cs` | `BlazorShop.Web.SharedV2` | `StorefrontCookieNames.*` | Storefront V2 host | Use V2-local constants. | Cart/checkout/auth/consent endpoint tests. |
| `Services/StorefrontCartTokenService.cs` | `BlazorShop.Web.SharedV2` | `StorefrontCookieNames.Cart`, `CartToken` | Storefront V2 host | Use V2-local constants with unchanged string values. | Cart token tests. |
| `Services/StorefrontDisplayContextProvider.cs` | `BlazorShop.Web.SharedV2` | `StorefrontCookieNames.CurrencyPreference` | Storefront V2 host | Use V2-local constants. | Currency preference tests. |
| `Services/StorefrontSessionResolver.cs` | `BlazorShop.Web.SharedV2` | `RoleNames.Admin` | Storefront V2 host | Add V2-local role constant. | Maintenance/admin access tests. |

Baseline verification:

- `dotnet build BlazorShop.sln -m:1`: passed after `dotnet build-server shutdown` and stopping stale `testhost` locks. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist notice.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~Architecture|FullyQualifiedName~StorefrontContractOwnershipTests|FullyQualifiedName~StorefrontHostCompositionTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontEndpointDependencyBoundaryTests"`: passed 164/164.

Decision:

- No cart/runtime behavior change is in flight. Boundary work can proceed.
- SIB1 may add guardrails while accepting the known `Storefront.V2 -> Web.SharedV2` offender until SIB3 removes it.

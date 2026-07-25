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

## SIB1 - Guardrails first

Implementation notes:

- Added `StorefrontIndependenceBoundaryTests` for Storefront source/project dependency boundaries.
- The tests distinguish allowed HTTP contract dependency through `Storefront.Runtime -> Storefront.Client` from forbidden source/project dependency on Commerce Node or Control Plane implementation projects.
- The temporary `Storefront.V2_WebSharedV2OffendersAreLimitedUntilSib3` guard records the exact known Web.SharedV2 offender set. SIB3 must replace this allowlist with a zero-offender assertion after the dependency is removed.
- No runtime behavior was changed in this phase.

Verification:

- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndependenceBoundaryTests"`: passed 8/8.
- Failure messages include offending file/reference paths through `AssertNoProjectReferences` and `AssertNoSourceFragments`.
- Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist notice.

## SIB2 - Storefront-owned constants and host primitives extraction

Implementation notes:

- Added V2-local `StorefrontCookieNames` in `BlazorShop.Storefront.Configuration` with unchanged values: `my-cart`, `bs-cart-token`, and `bs-currency`.
- Added V2-local `StorefrontRoleNames.Admin` with unchanged value `Admin`.
- Replaced Storefront V2 source imports of `BlazorShop.Web.SharedV2` in cart, checkout, auth, consent, media, SEO, local endpoint support, rate-limit identity, display context, cart-token service, and session resolver.
- Updated focused tests to reference the Storefront V2-local constants where they need cookie names.
- No cookie names, role semantics, cart token behavior, or endpoint routes were changed.

Verification:

- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontDisplayContextProviderTests|FullyQualifiedName~CartCorePhase0InventoryTests|FullyQualifiedName~SecurityPrivacyPhase0InventoryTests|FullyQualifiedName~SecurityPrivacyPhase2RateLimitTests|FullyQualifiedName~StorefrontSessionResolverTests"`: passed 67/67.
- `StorefrontIndependenceBoundaryTests` now limits remaining `Storefront.V2 -> Web.SharedV2` offenders to `BlazorShop.Storefront.V2.csproj`, `Dockerfile`, and `tailwind.config.js`; SIB3 owns those build-file removals.
- Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist notice.

## SIB3 - Remove Storefront.V2 project/build dependency on Web.SharedV2

Implementation notes:

- Removed the `BlazorShop.Web.SharedV2` project reference from `BlazorShop.Storefront.V2.csproj`.
- Removed `Web.SharedV2` project/source copy lines from the Storefront V2 Dockerfile.
- Added explicit Dockerfile copies for the allowed `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` project references.
- Removed `Web.SharedV2` Razor/C# globs from Storefront V2 Tailwind content scanning.
- Tightened `StorefrontIndependenceBoundaryTests.StorefrontV2_DoesNotReferenceOrImportWebSharedV2` to require zero Storefront V2 `Web.SharedV2` offenders.

Verification:

- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`: passed with 0 warnings.
- `dotnet publish BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --configuration Debug --output artifacts/tmp/sib3-storefront-v2-publish /p:UseAppHost=false`: passed.
- `rg "BlazorShop\.Web\.SharedV2|Web\.SharedV2|BlazorShop.Web.SharedV2.csproj" BlazorShop.PresentationV2/BlazorShop.Storefront.V2`: no matches.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndependenceBoundaryTests"`: passed 8/8.
- Existing test warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist notice.

## SIB4 - Storefront business model and DTO boundary audit

Implementation notes:

- Extended `StorefrontIndependenceBoundaryTests` to guard every Storefront platform project against `Web.SharedV2.Models`, `Application.DTOs`, and backend/core business namespace imports.
- Added a specific guard for `Storefront.V2/Services/Contracts` so V2-local BFF contracts stay local and do not import backend/shared business DTOs.
- Added component feature model checks for server-owned/admin-owned fields such as store/customer/user ownership, publication flags, credentials, secrets, and cost fields.
- Added Runtime checks that keep route/layout/cookie/Razor host primitives out of `Storefront.Runtime`.
- Added Client checks that prevent handwritten request/response/DTO clones outside the OpenAPI-generated client source.
- No DTO migration was required in this phase; existing V2 service contracts are same-origin BFF or host projection contracts, and generated Storefront API contracts remain in `Storefront.Client`.

Verification:

- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndependenceBoundaryTests"`: passed 13/13.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 72/72.
- Static scan for `BlazorShop.Web.SharedV2.Models`, `BlazorShop.Application.DTOs`, `BlazorShop.Application`, `BlazorShop.Domain`, and `BlazorShop.Infrastructure` across Storefront platform projects found no source offenders.

## SIB5 - Storefront API access boundary hardening

Implementation notes:

- Extended `docs/storefront-platform/storefront-client-exception-registry.md` with active Storefront V2 manual-client exceptions for cart merge, saved-address checkout, protected customer account, and auth/session forms.
- Added guardrails that browser projects (`Storefront.WASM` and `Storefront.Components`) do not reference `Storefront.Client`, Commerce Node route paths, Commerce Node base URLs, node credentials, or protected tokens.
- Added guardrails that Storefront V2 host source does not call Control Plane routes or read Control Plane/node credential settings.
- Added guardrails that active manual `StorefrontApiClient` exception usages remain registered with owner, test, and revisit trigger.
- The allowed API access shape remains V2 SSR/BFF -> Runtime -> Client -> Commerce Node Storefront HTTP API, while browser/WASM calls same-origin `/api/*`.

Verification:

- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndependenceBoundaryTests"`: passed 16/16.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontBffBoundaryHardeningTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontCommerceFlowCutoverTests"`: passed 17/17.
- Static scan for Control Plane routes/namespaces, node credentials, Commerce Node API project namespace, and backend/core namespaces across Storefront platform projects found no forbidden source/API boundary references.
- Browser network assertion against `http://localhost:18598/product/qa-simple-product-100` clicked add-to-cart and observed same-origin `POST /api/cart/lines`; no browser request to `localhost:5180`, `/api/storefront/stores/*`, or Control Plane routes. Evidence: `output/playwright/sib5-api-access-boundary-network.json` and `.png`.
- Local V2 runtime was stopped after the browser assertion.

## SIB6 - Starter and generated storefront independence contract

Implementation notes:

- Updated StorefrontBuilder, Starter, generated storefront, system map, runtime-boundary, project-folder, contract-ownership, ADR, agent, and visual reverse engineering docs so Starter/generated storefronts explicitly ban all `BlazorShop.Web.SharedV2`/`Web.SharedV2` references, not only `Web.SharedV2.Models`.
- Updated generated storefront validation, generated proof isolation, generated sample release, Starter isolation, and sample generation scripts to fail on `BlazorShop.Web.SharedV2`/`Web.SharedV2` references.
- Extended architecture tests so Starter/generated consumer documentation, scripts, and validators carry the `Web.SharedV2` prohibition.
- No production browser QA was run in this phase by design; SIB6 is a compile/static contract proof phase.

Verification:

- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontBuilderVisualGenerationTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 69/69.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1`: passed, including Client/Runtime/Components package pack, generated proof restore/build, StorefrontBuilder static validation, and isolation gate.
- `rg "BlazorShop.Web.SharedV2|Web.SharedV2" BlazorShop.PresentationV2/BlazorShop.Storefront.Starter -g "*.cs" -g "*.razor" -g "*.csproj"`: no matches.

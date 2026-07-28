# Storefront Visual Only Phase 1 Boundary Todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-07-27
Scope: finish Phase 1 so `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, and future `BlazorShop.Storefront.{Name}` hosts are visual consumers of the shared Storefront application engine.

## Verified current context

- `BlazorShop.Storefront.V2/Program.cs` is already short, but still passes V2-owned rate limit and HTTP client resolver hooks into `AddStorefrontV2Services`.
- `BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs` still composes Runtime, Presentation, antiforgery, rate limiting, Razor components, and shell/navigation services.
- `BlazorShop.Storefront.V2/Configuration/StorefrontApplicationBuilderExtensions.cs` still owns forwarded headers, static files, current-store middleware, public redirect middleware, and rate limiter ordering.
- `BlazorShop.Storefront.V2` still has `Services/`, `Services/Contracts/`, `Configuration/`, `Options/`, `Models/`, and `Endpoints/`.
- V2 layout/components still inject application services and load data:
  - `Components/Layout/StorefrontHeader.razor`
  - `Components/Layout/StorefrontFooter.razor`
  - `Components/Layout/StorefrontAccountMenu.razor`
  - `Components/Catalog/ProductCard.razor`
  - `Components/Seo/StorefrontBrandHead.razor`
- V2 auth and checkout visual views still write browser mutation form contracts directly:
  - `Theme/Pages/Auth/V2AuthPageView.razor`
  - `Pages/Hybrid/Commerce/CheckoutPage.razor`
  - `Components/Layout/StorefrontHeader.razor`
  - `Components/Layout/StorefrontAccountMenu.razor`
- Product page decisions are partly duplicated between V2 and Presentation:
  - Presentation has `StorefrontProductPageMapper` and `StorefrontProductSummaryMapper`.
  - V2 still calculates fresh badge, compare state, stock text, SKU/GTIN labels, quantity fallback, and purchase block messages.
- `StorefrontFoundationViewSet.CreateMinimal(...)` is still used by V2 and Starter registrations.
- `StorefrontFoundationViewOptionsValidator` validates only `IComponent`; context compatibility is checked mostly when the slot renders.
- V2 `_Imports.razor` still imports application namespaces such as `Storefront.Models`, `Storefront.Services`, `Storefront.Services.Contracts`, `System.Net.Http.Json`, and `Microsoft.AspNetCore.Http`.
- V2 `.csproj` still directly references `BlazorShop.Storefront.Runtime`.
- Starter also composes Runtime/Presentation/antiforgery/static files itself, so the cleanup cannot be V2-only.

## Target final shape

After Phase 1, V2 should only own:

- [ ] `Program.cs` thin bootstrap.
- [ ] `appsettings*.json` and deployment config values.
- [ ] view registration.
- [ ] layouts/pages/components visual markup.
- [ ] `wwwroot/css`, `wwwroot/images`, `wwwroot/fonts`.
- [ ] visual copy.
- [ ] pure UI state.
- [ ] WASM component placement.

V2 must not own:

- [ ] `Services/` application services.
- [ ] `Services/Contracts/`.
- [ ] middleware.
- [ ] application configuration helpers.
- [ ] business models.
- [ ] Commerce Node API resolution.
- [ ] store key resolution.
- [ ] navigation data loading.
- [ ] session loading.
- [ ] application caching.
- [ ] rate limiting policy.
- [ ] auth/checkout form contracts.
- [ ] business decisions.
- [ ] direct Runtime/Client reference.

Final V2 bootstrap target:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddStorefrontApplication(builder.Configuration)
    .AddV2PresentationViews();

var app = builder.Build();

app.UseStorefrontApplication();
app.MapStorefrontApplication<V2FoundationViewRegistration>();

app.Run();
```

The exact generic shape can be adjusted during implementation, but ownership must remain:

```text
Presentation = application engine, BFF, forms, page contexts, middleware, route endpoint mapping
Runtime      = Commerce Node generated-client integration and store-scoped generated transport
V2/Starter/{Name} = host config values, visual registration, markup, assets, copy
```

## Dependencies

- [ ] Complete or keep in-flight with this plan: `Storefront V2 Manual Client Retirement.todo.md`.
- [ ] Do not close this plan until F1.25 is closed:
  - [ ] no `StorefrontApiClient` in V2.
  - [ ] no V2 class implements Presentation `IStorefront*Client`.
  - [ ] no V2 manual Commerce Node transport.
- [ ] Update `docs/architecture/03-runtime-boundaries.md` during this plan because the current architecture doc still says V2 owns some current-store/session/host-specific API adapter behavior. This plan intentionally moves that ownership to Presentation/Runtime.

## Non-goals

- [ ] Do not rewrite Commerce Node storefront APIs.
- [ ] Do not move ecommerce truth such as pricing, sellability, cart validity, checkout rules, or order placement out of Commerce Node APIs.
- [ ] Do not redesign V2 visual UI unless a visual-only cutover exposes a real rendering bug.
- [ ] Do not collapse Runtime into Presentation.
- [ ] Do not make generated storefronts reference V2 or Starter.
- [ ] Do not make browser/WASM call Commerce Node directly.
- [ ] Do not remove V2.WASM browser components; only keep their boundary browser-safe.

## Phase order

```text
F1.26  Visual-only guardrails red
F1.27  Shared Storefront application bootstrap
F1.28  Runtime/store configuration ownership
F1.29  Current-store guard
F1.30  Public redirect and SEO runtime policy
F1.31  Rate limiting and BFF security policy
F1.32  Shell/navigation context
F1.33  Header/Footer/Account visual cutover
F1.34  Auth fixed form patterns
F1.35  Checkout fixed form pattern
F1.36  Currency/logout mutation patterns
F1.37  Product decision context
F1.38  Required view registration validation
F1.39  Startup context compatibility validation
F1.40  V2 imports cleanup
F1.41  V2 dependency/namespace cleanup
F1.42  Starter visual-only parity
F1.43  GeneratedProof isolation
F1.44  QA and closure gate
```

Implementation rule for every phase:

```text
characterization/guardrail test
-> Presentation/Runtime replacement
-> host switch
-> V2 and Starter proof
-> delete V2-owned source
-> permanent guardrail
```

## Phase F1.26 - Lock visual-only guardrails first

Goal: create failing tests before moving code so Foundation cannot be closed while V2 still has application logic.

- [x] Add an architecture test group, for example `StorefrontVisualOnlyBoundaryTests`.
- [x] Test V2 visual folders:
  - [x] `Components/`
  - [x] `Pages/`
  - [x] `Theme/Pages/`
  - [x] `Layouts/` if introduced.
- [x] In visual folders, forbid:

```text
@inject IStorefront*
IStorefrontRuntime*
HttpClient
IHttpClientFactory
IConfiguration
IOptions<
HttpContext
RequestDelegate
StorefrontApiEndpointResolver
StorefrontStoreKeyResolver
GetRequiredService
MapGet
MapPost
MapPut
MapDelete
```

- [x] In registered view components, forbid data-loading lifecycle methods unless the component is explicitly browser-only state and has no application service injection:

```text
OnInitializedAsync
OnParametersSetAsync
```

- [x] Forbid V2 classes named like application services:

```text
*Middleware
*Provider
*Resolver
*Client
*PageService
*Policy
```

- [x] Allow narrow visual helpers only:
  - [x] `CssClassBuilder`.
  - [x] `ImageAspectRatio`.
  - [x] `VisualComponentOptions`.
  - [x] other helpers with no DI, no HTTP, no config, no business fields.
- [x] Add a V2 source-folder guard that disallows new active files under:
  - [x] `Services/`
  - [x] `Services/Contracts/`
  - [x] `Middleware/`
  - [x] `Configuration/` except temporary registration file during migration.
  - [x] `Options/` except temporary host config DTO during migration.
  - [x] `Models/` except pure visual state during migration.
- [x] Exit criteria:
  - [x] New tests are red against current source.
  - [x] Test output names each blocker category, not one generic failure.
  - [x] No wide allowlist that hides application logic.

Evidence:

- Added `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontVisualOnlyBoundaryTests.cs`.
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 advisories.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal` failed as expected: 4 failed, 0 passed.
- Red blocker groups are visual application injection/framework plumbing, visual async data-loading lifecycle methods, application-service named classes, and active application logic folders.

## Phase F1.27 - Create shared Storefront application bootstrap

Goal: Presentation owns full application registration, pipeline, and endpoint mapping. V2/Starter call one bootstrap API.

- [x] Add `BlazorShop.Storefront.Presentation.Hosting.StorefrontApplicationServiceCollectionExtensions`.
- [ ] Implement:

```csharp
services.AddStorefrontApplication(configuration);
```

- [x] `AddStorefrontApplication` owns:
  - [x] Runtime options binding from configuration.
  - [x] `AddStorefrontRuntime(...)`.
  - [x] `AddStorefrontPlatformRuntime(...)`.
  - [x] `AddStorefrontPresentation(configuration)`.
  - [x] page services.
  - [x] BFF endpoint dependencies.
  - [x] antiforgery.
  - [x] rate limiter.
  - [x] current-store guard dependencies.
  - [x] public redirect dependencies.
  - [x] navigation/shell services.
  - [x] session services.
  - [x] view validation services.
  - [x] Razor components.
  - [x] optional WASM support flag.
- [x] Add `BlazorShop.Storefront.Presentation.Hosting.StorefrontApplicationBuilderExtensions`.
- [ ] Implement:

```csharp
app.UseStorefrontApplication();
```

- [x] `UseStorefrontApplication` owns:
  - [x] forwarded headers.
  - [x] HTTPS/HSTS policy where host-neutral and safe.
  - [x] static files.
  - [x] current-store middleware.
  - [x] public redirect middleware.
  - [x] rate limiter.
  - [x] Presentation middleware.
  - [x] antiforgery ordering.
- [x] Implement:

```csharp
app.MapStorefrontApplication<TViewRegistration>();
```

- [x] `MapStorefrontApplication` owns:
  - [x] Presentation BFF endpoints.
  - [x] auth endpoints.
  - [x] cart endpoints.
  - [x] checkout endpoints.
  - [x] account endpoints.
  - [x] consent endpoints.
  - [x] preferences endpoints.
  - [x] media endpoints.
  - [x] robots.
  - [x] sitemap.
  - [x] favicon/default static helpers if host-neutral.
  - [x] Razor components with required additional assemblies.
- [x] Keep a lower-level escape hatch only if needed:
  - [x] `AddStorefrontPresentation(...)` may remain internal/public for tests.
  - [x] `UseStorefrontPresentation(...)` may remain if Starter/generated proof still needs it during migration.
  - [x] Mark old aliases obsolete only if external package compatibility requires it.
- [x] Switch V2 Program to call the new bootstrap but keep old V2 extension in place until tests pass.
- [x] Switch Starter Program to call the same bootstrap.
- [x] Exit criteria:
  - [x] V2 no longer calls `AddStorefrontV2Services`.
  - [x] V2 no longer calls `UseStorefrontV2HostPipeline`.
  - [x] V2 no longer passes `StorefrontRateLimitPolicies.ConfigureStorefrontRateLimiter`.
  - [x] V2 no longer passes `StorefrontApiEndpointResolver.ConfigureStorefrontHttpClient`.
  - [x] Starter no longer manually registers Runtime/Presentation/antiforgery/static files.

Evidence:

- Added `AddStorefrontApplication(builder.Configuration)`, `UseStorefrontApplication()`, and `MapStorefrontApplication(...)` in `BlazorShop.Storefront.Presentation.Hosting`.
- `MapStorefrontApplication(...)` uses `typeof(ViewRegistration)` rather than a generic type argument because current V2/Starter registration classes are static.
- Moved shared options, rate limiting, store resolution, current-store/public redirect middleware, and shell navigation providers/contracts from V2 source paths into `BlazorShop.Storefront.Presentation`.
- V2 Program now calls shared bootstrap and passes only the V2 WASM assembly for component discovery.
- Starter Program now calls shared bootstrap and no longer manually registers Runtime, Presentation, Razor components, antiforgery, or static-file middleware.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 advisories.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontApplicationBootstrapTests" -v:minimal` passed: 3 passed, 0 failed.

## Phase F1.28 - Move runtime/store configuration ownership

Goal: V2 keeps configuration values only. Runtime/Presentation.Hosting interpret them.

- [x] Add Presentation hosting options:
  - [x] `StorefrontApplicationOptions`.
  - [x] `StorefrontStoreResolutionOptions` if not already shared.
  - [x] `StorefrontRuntimeBindingOptions`.
  - [x] `StorefrontPublicUrlOptions` integration.
- [x] Move or recreate validators in Presentation:
  - [x] store key required in production when current-store resolution is enabled.
  - [x] Commerce Node base URL required and absolute.
  - [x] public URL base URL validation.
  - [x] forwarded headers validation.
- [x] Move store key resolution to Runtime or Presentation.Hosting:

```text
Api:StoreKey
StoreKey
STORE_KEY
```

- [x] Move Commerce Node base URL resolution to Runtime/Presentation bootstrap:
  - [x] support current `Api:BaseUrl` if still used.
  - [x] support Starter options for package template compatibility.
  - [x] normalize trailing slash.
  - [x] produce generated client base address expected by Runtime.
- [x] Remove from V2 after switch:
  - [x] `Configuration/StorefrontApiEndpointResolver.cs`
  - [x] `Configuration/StorefrontStoreKeyResolver.cs`
  - [x] `Options/StorefrontApiOptions.cs` if no longer needed.
  - [x] V2-specific validators tied to API/store resolution.
- [x] Add source gate:

```powershell
rg -n "StorefrontApiEndpointResolver|StorefrontStoreKeyResolver|StorefrontApiOptions" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"
```

- [x] Expected after phase: no matches.
- [x] Exit criteria:
  - [x] V2 `appsettings*.json` keeps values only.
  - [x] V2 source has no configuration interpretation code.
  - [x] Runtime generated clients still call `api/storefront/stores/{storeKey}/*` through Runtime context.

Evidence:

- Added `StorefrontApplicationOptions` and `StorefrontRuntimeBindingOptions` under `BlazorShop.Storefront.Presentation/Options`.
- `AddStorefrontApplication(...)` now binds `StorefrontApplicationOptions`, shared `StorefrontStoreResolutionOptions`, `StorefrontRuntimeBindingOptions`, `StorefrontPublicUrlOptions`, `StorefrontApiOptions`, `ClientAppOptions`, rate limiting, and forwarded headers in Presentation.
- `StorefrontApiEndpointResolver` and `StorefrontStoreKeyResolver` are Presentation-owned and resolve `Api:BaseUrl`, `Api:StoreKey`, `Storefront:CommerceNodeBaseUrl`, `Storefront:StoreKey`, `StoreKey`, and `STORE_KEY`.
- Removed old V2 configuration interpretation extensions after V2/Starter switched to `AddStorefrontApplication(...)`.
- Source gate `rg -n "StorefrontApiEndpointResolver|StorefrontStoreKeyResolver|StorefrontApiOptions" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"` returned no matches.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 advisories.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontApplicationBootstrapTests" -v:minimal` passed: 4 passed, 0 failed.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontApiEndpointResolverTests" -v:minimal` passed: 8 passed, 0 failed.

## Phase F1.29 - Move current-store application guard

Goal: current-store readiness and maintenance policy are Presentation-owned for every storefront host.

- [x] Move behavior from V2 `StorefrontCurrentStoreMiddleware` into Presentation.Hosting.
- [x] Presentation middleware owns:
  - [x] route skip policy for static assets and health endpoints.
  - [x] current-store resolution before catalog/settings/customer/cart/checkout/SEO/media reads.
  - [x] disabled/missing/unavailable store mapping.
  - [x] maintenance redirect/page behavior.
  - [x] `404` for missing store.
  - [x] `503` for unavailable or misconfigured store.
  - [x] response headers for private/noindex/no-cache where applicable.
  - [x] discovery document behavior for maintenance/not-ready store.
- [x] Keep store truth in Commerce Node Storefront API; Presentation only maps the runtime result into host behavior.
- [x] Remove from V2:
  - [x] `Services/StorefrontCurrentStoreMiddleware.cs`
  - [x] direct `UseMiddleware<StorefrontCurrentStoreMiddleware>()`
- [x] Add tests:
  - [x] Presentation middleware skips static assets.
  - [x] Presentation middleware protects storefront pages.
  - [x] unavailable store returns correct status/header.
  - [x] maintenance store redirects or renders maintenance according to existing policy.
  - [x] unknown store never falls back to another store.
- [x] Exit criteria:
  - [x] New Storefront host gets current-store behavior by calling `UseStorefrontApplication()`.
  - [x] V2 has no current-store middleware source.

Evidence:

- `StorefrontCurrentStoreMiddleware` is Presentation-owned and registered by `UseStorefrontApplication()`.
- Added `StorefrontV2Source_DoesNotOwnCurrentStoreApplicationGuard` source gate to `StorefrontApplicationBootstrapTests`.
- Preserved the existing `X-Robots-Tag` wire value `noindex, nofollow` after moving middleware onto `Presentation.PagePatterns.StorefrontResponseHeaders`.
- Source gate `rg -n "StorefrontCurrentStoreMiddleware|UseMiddleware<StorefrontCurrentStoreMiddleware>" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"` returned no matches.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 advisories.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareRegressionTests|FullyQualifiedName~StorefrontCurrentStoreProviderTests" -v:minimal` passed: 11 passed, 0 failed.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontApplicationBootstrapTests" -v:minimal` passed: 5 passed, 0 failed.

## Phase F1.30 - Move public redirect and SEO runtime policy

Goal: redirect resolution and SEO runtime protection are shared application behavior, not V2 behavior.

- [x] Move from V2 to Presentation:
  - [x] `StorefrontPublicRedirectMiddleware`.
  - [x] `RedirectBlockReason`.
  - [x] redirect status validation.
  - [x] invalid target protection.
  - [x] loop protection.
  - [x] header-injection protection.
  - [x] SEO runtime logging hook.
  - [x] request filtering.
- [x] Register middleware from `UseStorefrontApplication()` after current-store guard and before route rendering.
- [x] Add tests:
  - [x] active redirect returns expected status.
  - [x] invalid external or header-injection target is blocked.
  - [x] redirect loop is blocked.
  - [x] missing redirect falls through to route rendering.
  - [x] store scope is preserved.
- [x] Remove from V2:
  - [x] `Services/StorefrontPublicRedirectMiddleware.cs`
  - [x] direct middleware registration.
- [x] Exit criteria:
  - [x] V2 and Starter share identical redirect behavior.
  - [x] V2 has no redirect middleware code.

Evidence:

- `StorefrontPublicRedirectMiddleware` is Presentation-owned and registered by `UseStorefrontApplication()` after the current-store guard.
- Added `StorefrontPublicRedirectMiddlewareTests` for valid redirect, invalid external target, scheme-relative target, header-injection target, loop fall-through, missing redirect fall-through, and static asset skip.
- Added `StorefrontV2Source_DoesNotOwnPublicRedirectApplicationPolicy` source gate.
- Source gate `rg -n "StorefrontPublicRedirectMiddleware|UseMiddleware<StorefrontPublicRedirectMiddleware>|RedirectBlockReason" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"` returned no matches.
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 advisories.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPublicRedirectMiddlewareTests|FullyQualifiedName~StorefrontApplicationBootstrapTests" -v:minimal` passed: 13 passed, 0 failed.

## Phase F1.31 - Move rate limiting and BFF security policy

Goal: Presentation.Hosting owns browser/BFF security execution. Hosts only provide config values.

- [x] Move to Presentation.Hosting:
  - [x] `StorefrontRateLimitPolicies`
  - [x] `StorefrontRateLimitIdentity`
  - [x] `StorefrontRateLimitingOptions`
  - [x] `StorefrontRateLimitPolicyOptions`
  - [x] 429 response contract.
  - [x] private/no-cache response header behavior.
- [x] Keep configuration values host-local:

```json
{
  "Storefront": {
    "RateLimiting": {
      "Enabled": true
    }
  }
}
```

- [x] Presentation implementation owns:
  - [x] rate-limit policy names.
  - [x] store/route/actor partitioning.
  - [x] cart mutation limits.
  - [x] `Retry-After`.
  - [x] safe JSON error response.
  - [x] rate-limit error code.
- [x] Remove V2 imports/usages:
  - [x] `System.Threading.RateLimiting`
  - [x] `Microsoft.AspNetCore.RateLimiting`
  - [x] `StorefrontResponseHeaders`
  - [x] `StorefrontLocalCartErrorResponse`
- [x] Add tests:
  - [x] cart BFF rate limit partitions by store/route/actor.
  - [x] actor falls back to remote IP when cart token missing.
  - [x] `429` has retry/private/noindex headers.
  - [x] disabled rate limiting does not register middleware.
- [x] Exit criteria:
  - [x] V2 does not configure rate limiting policy.
  - [x] V2 only carries config values.

Evidence:

- Deleted dead V2 `Endpoints/StorefrontLocalEndpointSupport.cs` and duplicate V2 `Services/StorefrontResponseHeaders.cs`; Presentation owns local endpoint support and response headers.
- Updated rate-limit source assertions to read Presentation hosting/configuration/options paths.
- Added `StorefrontV2Source_DoesNotConfigureRateLimitingPolicy` source gate.
- Source gate `rg -n "StorefrontRateLimitPolicies|StorefrontRateLimitIdentity|StorefrontRateLimitingOptions|StorefrontResponseHeaders|StorefrontLocalCartErrorResponse|AddRateLimiter|UseRateLimiter" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"` returned no matches.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal` passed.
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 advisories.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~SecurityPrivacyPhase2RateLimitTests" -v:minimal` passed: 33 passed, 0 failed.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontApplicationBootstrapTests" -v:minimal` passed: 7 passed, 0 failed.

## Phase F1.32 - Create shell/navigation context service

Goal: Header/Footer/Account/Menu/Search/Currency contexts are prepared by Presentation once per request.

- [x] Add Presentation context records:

```csharp
StorefrontShellContext
StorefrontHeaderContext
StorefrontFooterContext
StorefrontAccountMenuContext
StorefrontNavigationContext
StorefrontSearchContext
StorefrontCurrencyContext
StorefrontBrandContext
StorefrontShellLink
StorefrontShellMenu
```

- [x] Add service:

```csharp
IStorefrontShellContextService
StorefrontShellContextService
```

- [x] Service owns:
  - [x] load display context.
  - [x] load navigation menus.
  - [x] load content links.
  - [x] load search categories.
  - [x] load account session summary.
  - [x] prepare currency options.
  - [x] prepare safe return URLs.
  - [x] prepare safe application URLs.
  - [x] request-scoped caching.
- [x] Replace V2-only services by Presentation equivalents:
  - [x] `IStorefrontNavigationProvider`
  - [x] `IStorefrontPageNavigationProvider`
  - [x] `IStorefrontClientAppUrlResolver`
- [x] Add tests:
  - [x] context service loads data once per request.
  - [x] account summary is anonymous when no session exists.
  - [x] search categories are safe and sorted.
  - [x] currency context contains current/supported/default codes.
  - [x] safe return URL never becomes external.
- [x] Exit criteria:
  - [x] Header/Footer/Account menu can render from context only.
  - [x] V2 no longer owns navigation provider services.

Evidence:

- Added `IStorefrontShellContextService`, `StorefrontShellContextService`, and Presentation shell/header/footer/account/navigation/search/currency/brand records.
- Registered the shell context service in Presentation DI and kept navigation/page/client-app URL provider ownership in Presentation.
- Added `StorefrontShellContextServiceTests` for request caching, anonymous account state, sorted/safe categories, currency context, and safe return fallback.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontShellContextServiceTests" -v:minimal`
  - `rg -n "StorefrontNavigationProvider|StorefrontPageNavigationProvider|StorefrontClientAppUrlResolver|IStorefrontNavigationProvider|IStorefrontPageNavigationProvider|IStorefrontClientAppUrlResolver" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"` only finds remaining visual component consumption to remove in F1.33, not provider ownership implementations.

## Phase F1.33 - Remove data loading from Header/Footer/Account visual components

Goal: visual components render supplied context only.

- [x] Update Presentation layout/page contexts to include shell context:
  - [x] main layout context.
  - [x] page context wrappers where needed.
  - [x] auth/account/cart/checkout contexts where layout needs account/session/currency.
- [x] Update V2 components:
  - [x] `StorefrontHeader` receives `StorefrontHeaderContext`.
  - [x] `StorefrontFooter` receives `StorefrontFooterContext`.
  - [x] `StorefrontAccountMenu` receives `StorefrontAccountMenuContext`.
  - [x] `StorefrontBrandHead` receives brand/head context or moves to Presentation head slot data.
  - [x] `ProductCard` receives formatted summary model and display context data, not providers.
- [x] Remove from V2 visual components:
  - [x] `@inject IStorefrontCatalogClient`
  - [x] `@inject IStorefrontDisplayContextProvider`
  - [x] `@inject IStorefrontPageNavigationProvider`
  - [x] `@inject IStorefrontNavigationProvider`
  - [x] `@inject IStorefrontSessionResolver`
  - [x] `HttpContext`
  - [x] `OnInitializedAsync` data loading.
- [x] Keep allowed UI state:
  - [x] mobile menu open/close.
  - [x] modal/details open/close.
  - [x] CSS selection state.
  - [x] local browser-only progressive enhancement.
- [x] Add tests:
  - [x] visual-only guard now passes for layout/header/footer/account components.
  - [x] rendered header contains search/category/currency/account data from context.
  - [x] rendered footer contains company/legal/support links from context.
- [x] Exit criteria:
  - [x] zero service injection in registered V2 layout/header/footer/account menu.
  - [x] zero API/session/navigation loading in visual components.

Evidence:

- Added Presentation `StorefrontFoundationLayout` and `StorefrontFoundationApplicationHead` hosts. They load `StorefrontShellContext` and pass it into registered V2 visual components.
- Updated V2 `MainLayout`, `StorefrontHeader`, `StorefrontFooter`, `StorefrontAccountMenu`, and `StorefrontBrandHead` to consume supplied context only.
- Updated `ProductCard`/`ProductGrid` to consume `ProductSummaryItem`; related product summaries are now mapped in Presentation by `StorefrontProductPageMapper`.
- Updated `StorefrontBrandingMarkupTests`, `StorefrontProductPageServiceTests`, and added `F1_33_V2ShellVisualComponents_RenderSuppliedContextOnly`.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontBrandingMarkupTests" -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontProductPageServiceTests" -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~F1_33_V2ShellVisualComponents_RenderSuppliedContextOnly" -v:minimal`
  - `rg -n "@inject IStorefront|IStorefrontRuntime|HttpClient|IHttpClientFactory|IConfiguration|IOptions<|HttpContext|RequestDelegate|StorefrontApiEndpointResolver|StorefrontStoreKeyResolver|GetRequiredService|Map(Get|Post|Put|Delete)|OnInitializedAsync|OnParametersSetAsync" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontFooter.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontAccountMenu.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor`

## Phase F1.34 - Move auth mutation contracts into fixed Presentation form patterns

Goal: V2 owns visual arrangement and copy; Presentation owns auth browser mutation contracts.

Current note: Presentation already has auth form models and endpoints. This phase adds fixed form components/patterns so hosts do not manually write security-sensitive fields.

- [x] Add Presentation form components or form builders:
  - [x] `StorefrontSignInForm`
  - [x] `StorefrontRegisterForm`
  - [x] `StorefrontForgotPasswordForm`
  - [x] `StorefrontResetPasswordForm`
- [x] Presentation owns:
  - [x] form method.
  - [x] form action.
  - [x] antiforgery token.
  - [x] return URL hidden field.
  - [x] captcha token field name and purpose.
  - [x] recovery token hidden field.
  - [x] required HTML attributes.
  - [x] field names matching endpoints.
  - [x] security contract.
- [x] V2 supplies:
  - [x] classes.
  - [x] labels.
  - [x] button content.
  - [x] validation placement.
  - [x] surrounding section layout.
- [x] Starter supplies neutral classes/copy in the same pattern.
- [x] Remove from V2 auth visual view:
  - [x] raw `<form method="post">` for sign-in/register/forgot/reset.
  - [x] direct `<AntiforgeryToken />` in auth view.
  - [x] hardcoded hidden names for `ReturnUrl`, `CaptchaToken`, `Email`, `Token`.
- [x] Add tests:
  - [x] auth forms post to Presentation auth routes.
  - [x] form field names match endpoint form DTOs.
  - [x] register disabled policy does not render submit form.
  - [x] reset form includes token/email only through Presentation pattern.
- [x] Exit criteria:
  - [x] V2 does not self-author auth POST contracts.

Evidence:

- Added Presentation auth form components and `StorefrontAuthFormFieldNames` constants backed by endpoint form DTO `nameof(...)` values.
- Updated V2 `V2AuthPageView` to use Presentation form components while supplying classes, labels, button content, and surrounding layout.
- Updated Starter `AuthShellPage` to use the same Presentation form components with neutral classes/copy.
- Added `StorefrontAuthFormPatternTests`.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontAuthFormPatternTests" -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.SignIn_ReturnsStorefrontLoginPage" -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Register_WhenRegistrationDisabled_RendersDisabledStateWithoutSubmit" -v:minimal`
  - Fixed-string `rg` checks for raw auth POST form/hidden field ownership in V2 and Starter auth views found no matches.
- Note: a broader combined auth smoke-test filter timed out after 185s and was stopped; the two targeted auth render smoke tests above passed.

## Phase F1.35 - Move checkout form contract into Presentation

Goal: checkout mutation contract is fixed and reusable; host only controls layout.

- [x] Add Presentation components/patterns:
  - [x] `StorefrontCheckoutForm`
  - [x] `StorefrontCheckoutAddressFields`
  - [x] `StorefrontCheckoutPaymentFields`
  - [x] `StorefrontCheckoutSubmit`
  - [x] optional `StorefrontCheckoutLegalAcknowledgement`
- [x] Presentation owns:
  - [x] `form action`.
  - [x] antiforgery.
  - [x] `CartVersion`.
  - [x] `IdempotencyKey`.
  - [x] checkout session identity if required.
  - [x] address field names.
  - [x] required country/state behavior.
  - [x] billing/shipping flags.
  - [x] payment method field name.
  - [x] submit semantics.
- [x] V2 owns:
  - [x] grid layout.
  - [x] section order.
  - [x] labels/copy.
  - [x] CSS classes.
  - [x] summary placement.
- [x] Starter uses the same Presentation form patterns.
- [x] Remove from V2 checkout view:
  - [x] raw `<form method="post">` for checkout.
  - [x] direct `<AntiforgeryToken />`.
  - [x] direct hidden `CartVersion`.
  - [x] direct hidden `IdempotencyKey`.
  - [x] direct `PaymentMethodKey` field ownership.
  - [x] direct `ShippingCountryCode` contract ownership.
- [x] Add tests:
  - [x] checkout form field names match Presentation endpoint DTO.
  - [x] idempotency key is always posted.
  - [x] cart version is always posted.
  - [x] country options render from context.
  - [x] single payment method still posts the canonical key.
- [x] Exit criteria:
  - [x] V2 checkout view renders form pattern, not security/field contract.

Evidence:

- Added Presentation checkout components and `StorefrontCheckoutFormFieldNames` constants backed by endpoint form DTO `nameof(...)` values.
- Updated V2 checkout page to keep page grid/summary/layout while rendering the Presentation form/address/payment/submit pattern.
- Updated Starter checkout page to use the same Presentation checkout form pattern.
- Added `StorefrontCheckoutFormPatternTests` and updated the existing address lookup markup test to read address-field markers from the Presentation component.
- Verification:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal`
  - `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontCheckoutFormPatternTests" -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontBrandingMarkupTests.CheckoutPage_RendersAddressLookupThroughRuntimeFacade" -v:minimal`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Checkout_PostWithStaleCartVersionRedirectsWithoutPlacingOrder" -v:minimal`
  - Fixed-string `rg` checks for raw checkout POST form/field ownership in V2 and Starter checkout views found no matches.

## Phase F1.36 - Move currency and logout mutation patterns

Goal: small mutation forms are fixed by Presentation, not manually written in Header/Account visual components.

- [x] Add Presentation components/patterns:
  - [x] `StorefrontCurrencyPreferenceForm`
  - [x] `StorefrontLogoutForm`
- [x] Presentation owns:
  - [x] action.
  - [x] method.
  - [x] antiforgery.
  - [x] hidden return URL.
  - [x] currency field name.
  - [x] safe return URL.
- [x] V2 owns:
  - [x] select/button markup through child content or class options.
  - [x] mobile/desktop placement.
- [x] Remove from V2:
  - [x] raw currency POST form in `StorefrontHeader`.
  - [x] raw logout POST form in `StorefrontAccountMenu`.
  - [x] direct `HttpContext` return URL construction.
- [x] Add tests:
  - [x] currency form posts `CurrencyCode` and safe `ReturnUrl`.
  - [x] logout form posts safe `ReturnUrl`.
  - [x] external return URL cannot be rendered.
- [x] Exit criteria:
  - [x] V2 visual components no longer author mutation form contracts.

Evidence:

- Added `StorefrontCurrencyPreferenceForm`, `StorefrontLogoutForm`, and canonical shell mutation field names under Storefront Presentation.
- V2 header/account menu now consume Presentation shell mutation forms and retain only placement/classes/submit content.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontShellMutationFormPatternTests" -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontShellContextServiceTests.GetAsync_WhenRequestPathLooksExternal_FallsBackToHome" -v:minimal`
- Fixed-string `rg` checks for raw POST/antiforgery ownership in `StorefrontHeader` and `StorefrontAccountMenu` found no matches.

## Phase F1.37 - Consolidate product decision context in Presentation

Goal: V2 product and product-card views render decisions prepared by Presentation.

Current note: Presentation already has product mappers. This phase should consolidate existing mapper logic and remove V2 duplication, not create a second product business layer.

- [x] Extend Presentation product context/view models:

```csharp
StorefrontProductPageContext
StorefrontProductPricingView
StorefrontProductAvailabilityView
StorefrontProductPurchaseView
StorefrontProductVariantView
StorefrontProductBadgeView
StorefrontProductNavigationView
```

- [x] Presentation supplies:
  - [x] `CanAddToCart`.
  - [x] `AvailabilityState`.
  - [x] `AvailabilityLabel`.
  - [x] `StockLabel`.
  - [x] `PriceDisplay`.
  - [x] `ComparePriceDisplay`.
  - [x] `DefaultSkuLabel`.
  - [x] `DefaultGtinLabel`.
  - [x] `PurchaseMessage`.
  - [x] `PurchaseBlockMessage`.
  - [x] `MinQuantity`.
  - [x] `MaxQuantity`.
  - [x] `InitialStockValue`.
  - [x] variant option labels/prices/stock labels.
  - [x] fresh/new badge state.
- [x] Presentation catalog summary mapper supplies direct-add/card decisions:
  - [x] purchase paused.
  - [x] direct add allowed.
  - [x] direct add stock value.
  - [x] purchase block message.
  - [x] formatted display/compare prices.
- [x] V2 only controls:
  - [x] CSS class by `AvailabilityState`.
  - [x] badge placement.
  - [x] gallery layout.
  - [x] variant list layout.
  - [x] non-business visual copy.
- [x] Remove from V2 direct reads/calculations:
  - [x] `PurchaseBlockReasons`.
  - [x] `ManageStock`.
  - [x] `AvailableQuantity`.
  - [x] `MinOrderQuantity`.
  - [x] `MaxOrderQuantity`.
  - [x] `EffectivePrice` fallback.
  - [x] raw variant stock interpretation.
  - [x] raw fresh-arrival date arithmetic.
- [x] Add tests:
  - [x] mapper converts purchase block reason to display message.
  - [x] mapper sets add-to-cart disabled for hard block.
  - [x] mapper formats compare price only when greater than display price.
  - [x] mapper handles unmanaged stock.
  - [x] V2 product view no longer references raw business fields.
- [x] Exit criteria:
  - [x] V2 product views consume prepared context only.

Evidence:

- Extended Storefront Presentation product page context with pricing, availability, purchase, variant, badge, and navigation view records.
- Consolidated product page decisions into `StorefrontProductPageMapper` and catalog card decisions into `StorefrontProductSummaryMapper`.
- V2 product page now renders prepared context values and keeps only availability-state CSS mapping plus layout/badge placement.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontProductDecisionContextTests" -v:minimal`
- Fixed-string/regex `rg` check for raw product business fields in `V2ProductPageView` found no matches.

## Phase F1.38 - Tighten required visual view registration

Goal: production storefront cannot silently use empty fallback for required slots.

- [ ] Replace production use of:

```csharp
StorefrontFoundationViewSet.CreateMinimal(...)
```

- [x] V2 registration must create full explicit `StorefrontFoundationViewSet`:
  - [x] `ApplicationHead`.
  - [x] `ApplicationScripts`.
  - [x] `MainLayout`.
  - [x] `HomePage`.
  - [x] `CategoryPage`.
  - [x] `ProductPage`.
  - [x] `SearchPage`.
  - [x] `DealsPage`.
  - [x] `NewReleasesPage`.
  - [x] `ContentPage`.
  - [x] `CartPage`.
  - [x] `CheckoutPage`.
  - [x] `PaymentResultPage`.
  - [x] `AuthPage`.
  - [x] `AccountPage`.
  - [x] `MaintenanceState`.
  - [x] `NotFoundState`.
  - [x] `ServiceUnavailableState`.
  - [x] `ErrorState`.
- [x] Starter registration must do the same.
- [x] Keep `StorefrontFoundationEmptyView` only for:
  - [x] tests.
  - [x] optional visual slots, if optional slots are introduced.
  - [x] explicit no-op application asset slots only if documented.
- [x] Update validator:
  - [x] missing required slot fails startup.
  - [x] empty fallback in required production slot fails startup.
  - [x] route component assigned as visual slot fails if inappropriate.
- [x] Add tests:
  - [x] V2 registration does not call `CreateMinimal`.
  - [x] Starter registration does not call `CreateMinimal`.
  - [x] missing product/checkout/error slot fails options validation.
- [x] Exit criteria:
  - [x] production registration cannot hide missing visual work.

Evidence:

- V2 and Starter foundation registrations now assign every required slot explicitly and no longer call `StorefrontFoundationViewSet.CreateMinimal`.
- Added explicit V2 error state view, Starter error state context support, and documented Starter no-op `ApplicationScripts` slot.
- `StorefrontFoundationViewOptionsValidator` now rejects null/non-component slots, `StorefrontFoundationEmptyView` in required slots, and route components in visual slots.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests" -v:minimal`
- `rg -n "CreateMinimal" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs` found no matches.

## Phase F1.39 - Validate context compatibility at startup

Goal: wrong slot context fails during application startup, not when a user visits the route.

- [x] Define expected context per slot:

| Slot | Expected context |
| --- | --- |
| `HomePage` | `StorefrontHomePageContext` |
| `CategoryPage` | `StorefrontCategoryPageContext` |
| `ProductPage` | `StorefrontProductPageContext` |
| `SearchPage` | `StorefrontSearchPageContext` |
| `DealsPage` | `StorefrontDealsPageContext` |
| `NewReleasesPage` | `StorefrontNewReleasesPageContext` |
| `ContentPage` | `StorefrontContentPageContext` |
| `CartPage` | `StorefrontCartPageContext` |
| `CheckoutPage` | `StorefrontCheckoutPageContext` |
| `PaymentResultPage` | `StorefrontPaymentResultPageContext` |
| `AuthPage` | `StorefrontAuthPageContext` |
| `AccountPage` | `StorefrontAccountPageContext` |
| `MaintenanceState` | `StorefrontSystemStateContext` |
| `NotFoundState` | `StorefrontSystemStateContext` |
| `ServiceUnavailableState` | `StorefrontSystemStateContext` |
| `ErrorState` | `StorefrontSystemStateContext` or explicit error context |

- [x] Update `StorefrontFoundationViewOptionsValidator` to validate:
  - [x] component implements `IComponent`.
  - [x] component has public `[Parameter] Context`.
  - [x] Context type is expected type or assignable.
  - [x] required slot is not empty fallback.
  - [x] component is not a route component when a visual view is expected.
- [x] Keep runtime `StorefrontFoundationViewTypeValidator` as defensive check.
- [x] Add tests:
  - [x] wrong product context fails `ValidateOnStart`.
  - [x] missing `Context` parameter fails `ValidateOnStart`.
  - [x] assignable context type passes.
  - [x] empty fallback in required slot fails.
- [x] Exit criteria:
  - [x] context mismatch is caught before first request.

Evidence:

- Added expected context map to `StorefrontFoundationViewOptionsValidator` for all page/state slots while leaving app head/scripts/layout as context-free asset/layout slots.
- Added dedicated `StorefrontDealsPageContext` and `StorefrontNewReleasesPageContext`, then updated Presentation routes/services plus V2/Starter views to use the slot-specific context types.
- Kept `StorefrontFoundationViewTypeValidator` unchanged as runtime defensive validation.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests" -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests" -v:minimal`

## Phase F1.40 - Clean V2 `_Imports.razor`

Goal: visual developers should not get application/service namespaces globally.

- [x] Remove from V2 `_Imports.razor`:

```text
System.Net.Http.Json
BlazorShop.Storefront.Models
BlazorShop.Storefront.Services
BlazorShop.Storefront.Services.Contracts
Microsoft.AspNetCore.Http
Runtime namespaces
Client namespaces
IConfiguration/IOptions-related namespaces if present
```

- [x] Keep:
  - [x] Presentation page contexts.
  - [x] visual contracts.
  - [x] browser-safe Components primitives.
  - [x] Blazor rendering namespaces.
  - [x] V2 visual namespaces.
  - [x] V2.WASM component namespaces for component placement.
- [x] Fix compile errors by adding narrow file-level imports only when visual-safe.
- [x] Add guardrail:
  - [x] V2 `_Imports.razor` cannot import Services/Contracts/Runtime/Client/HttpContext.
- [x] Exit criteria:
  - [x] V2 visual source cannot inject application services just because global imports make it easy.

Evidence:

- Removed global V2 imports for `System.Net.Http.Json`, Storefront models/services/contracts, Storefront Runtime/Client, and `Microsoft.AspNetCore.Http`.
- Kept Presentation page-context namespaces and visual/component contracts in `_Imports.razor`.
- Added narrow file-level imports for visual-safe route helpers, shell/display contracts, catalog enum usage, and the antiforgery head `HttpContext` dependency.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests.F1_40_V2Imports_DoNotExposeApplicationServiceOrTransportNamespaces" -v:minimal`
- `rg -n "(@using global::System.Net.Http.Json|@using BlazorShop.Storefront.Models|@using BlazorShop.Storefront.Services$|@using BlazorShop.Storefront.Services.Contracts|@using BlazorShop.Storefront.Runtime|@using BlazorShop.Storefront.Client|@using Microsoft.AspNetCore.Http)" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor` found no matches.

## Phase F1.41 - Clean V2 dependency and namespace ownership

Goal: V2 references only host visual dependencies.

- [x] Remove direct V2 project reference to:
  - [x] `BlazorShop.Storefront.Runtime`.
  - [x] `BlazorShop.Storefront.Client` if ever present.
  - [x] backend/core/API projects if ever present.
- [x] Keep V2 references:
  - [x] `BlazorShop.Storefront.Presentation`.
  - [x] `BlazorShop.Storefront.Components`.
  - [x] `BlazorShop.Storefront.V2.WASM`.
  - [x] `BlazorShop.ServiceDefaults` if bootstrap still requires it.
  - [x] `Microsoft.AspNetCore.Components.WebAssembly.Server` if V2 still hosts WASM.
- [x] Rename V2 namespaces away from generic shared names:

```text
BlazorShop.Storefront.V2.Layout
BlazorShop.Storefront.V2.Pages
BlazorShop.Storefront.V2.Components
BlazorShop.Storefront.V2.Presentation
BlazorShop.Storefront.V2.Visual
```

- [x] Delete empty/non-visual directories:
  - [x] `Services/`
  - [x] `Services/Contracts/`
  - [x] `Configuration/`
  - [x] `Options/`
  - [x] `Models/`
  - [x] `Middleware/`
  - [x] `Endpoints/` if moved to Presentation.
- [x] Add tests:
  - [x] V2 csproj has no Runtime/Client reference.
  - [x] V2 has no source under forbidden folders.
  - [x] V2 namespaces do not use shared application namespace for non-visual source.
- [x] Exit criteria:
  - [x] V2 is visually identifiable by project references, folders, and namespaces.

Evidence:

- Removed V2 direct `BlazorShop.Storefront.Runtime` project reference while keeping Presentation, Components, V2.WASM, ServiceDefaults, and WebAssembly Server dependencies.
- Moved the antiforgery head component to `BlazorShop.Storefront.Presentation.Components.Security` so V2 no longer owns `IAntiforgery`/`HttpContext` logic in visual folders.
- Wrapped checkout/payment result page context values in Presentation-owned view records so V2 no longer compiles against generated `BlazorShop.Storefront.Client` DTOs through public context properties.
- Set V2 root namespace to `BlazorShop.Storefront.V2` and renamed explicit component/page namespaces to V2-owned namespaces.
- Deleted empty V2 application folders under `Services/`, `Configuration/`, `Options/`, `Models/`, `Middleware/`, and `Endpoints/`.
- Added F1.41 guardrails to `StorefrontVisualOnlyBoundaryTests` for V2 project references, forbidden source folders, and namespace declarations.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests" -v:minimal`

## Phase F1.42 - Prove Starter visual-only parity

Goal: Starter is the neutral minimal visual consumer of the same Storefront application engine.

- [x] Switch Starter to:

```csharp
builder.Services
    .AddStorefrontApplication(builder.Configuration)
    .AddStarterFoundationViews();

app.UseStorefrontApplication();
app.MapStorefrontApplication<StarterFoundationViewRegistration>();
```

- [x] Remove Starter manual registration:
  - [x] direct `AddStorefrontRuntime`.
  - [x] direct `AddStorefrontPlatformRuntime`.
  - [x] direct `AddStorefrontPresentation`.
  - [x] direct `AddRazorComponents`.
  - [x] direct `AddAntiforgery`.
  - [x] direct `UseStaticFiles`.
  - [x] direct `MapStorefrontPresentation`.
  - [x] direct `MapRazorComponents`, unless wrapped by `MapStorefrontApplication`.
- [x] Remove Starter application logic:
  - [x] service injection in pages.
  - [x] API loading.
  - [x] middleware.
  - [x] runtime registration.
  - [x] handwritten form contracts.
  - [x] business decisions.
- [x] Evaluate `StarterFeatureActivationService`:
  - [x] If it is visual feature toggle only, keep it under Starter visual/config namespace.
  - [x] If it affects capability/business behavior, move capability activation to Presentation/Runtime.
- [x] Add tests:
  - [x] Starter has no service injection in visual pages.
  - [x] Starter has no direct Runtime/Client usage in source.
  - [x] Starter host starts with shared bootstrap.
- [x] Exit criteria:
  - [x] `V2 = rich visual consumer`.
  - [x] `Starter = neutral visual consumer`.
  - [x] both use the same Presentation application engine.

Evidence:

- Starter `Program.cs` now composes only `AddStorefrontApplication(builder.Configuration).AddStarterFoundationViews()`, `UseStorefrontApplication()`, and `MapStorefrontApplication(typeof(StarterFoundationViewRegistration))`.
- Removed Starter runtime/options/feature DI source (`StarterStorefrontOptions`, `StarterFeatureActivationService`) and empty `Services/`, `Endpoints/`, and `Options/` folders.
- Kept `Features/feature-manifest.json` as generation metadata; runtime visibility in Starter HomePage now reads the supplied Presentation `Context.FeatureCapabilities` without service injection.
- Removed Starter source imports for `BlazorShop.Storefront.Runtime`, feature service namespaces, options namespaces, and `Microsoft.Extensions.Options`.
- Added F1.42 guardrails to `StorefrontVisualOnlyBoundaryTests` for Starter page injection, Runtime/Client source usage, and shared bootstrap ownership.
- Updated older independence/cutover guardrails to treat retired V2 application folders and V2 Runtime reference as the expected state.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore -v:minimal`
- `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal`
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests" -v:minimal`

## Phase F1.43 - Generated storefront isolation proof

Goal: prove a new storefront can be generated as visual-only without referencing V2/Starter/Runtime/Client directly.

- [x] Create or update `GeneratedProof` workflow under StorefrontBuilder QA.
- [x] Generated proof project contains only:
  - [x] `Program`.
  - [x] view registration.
  - [x] layouts.
  - [x] page views.
  - [x] visual components.
  - [x] CSS/assets.
  - [x] store-local copy.
- [x] Generated proof must not reference:
  - [x] V2.
  - [x] Starter.
  - [x] Runtime directly, unless package compatibility metadata still requires it and source does not compile against it.
  - [x] Client directly.
  - [x] Commerce Node API.
  - [x] Control Plane API/Web.
  - [x] Application/Domain/Infrastructure.
  - [x] `Web.SharedV2`.
- [x] Generated proof uses:
  - [x] `BlazorShop.Storefront.Presentation`.
  - [x] `BlazorShop.Storefront.Components`.
  - [x] Storefront application bootstrap.
  - [x] local view registration.
- [x] Add QA script checks:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1
.\scripts\qa\run-storefront-builder-isolation-gate.ps1
```

- [x] Exit criteria:
  - [x] generated proof builds.
  - [x] generated proof serves main routes.
  - [x] generated proof isolation gate passes.

Evidence:

- Updated StorefrontBuilder static validation to require generated Runtime/Presentation/Components package references while treating `StorefrontClientPackageVersion` as compatibility metadata owned by Runtime transport.
- Updated generated proof/browser QA workflow so the proof host runs in Development for fixture-independent route smoke and fails on non-zero visual/commerce QA exit codes.
- Updated Starter/generated appsettings template to emit `PublicUrl:BaseUrl` and `ClientApp:BaseUrl` required by the shared Presentation application validator.
- Updated commerce-regression QA to report explicit fixture gaps for product interaction/SEO selectors when no live catalog fixture is configured, while still enforcing route rendering and no direct Commerce Node browser calls.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1`: passed.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -RunBrowserQa`: passed; generated `visual-qa-report.md` and `functional-commerce-report.md` under the ignored proof artifact.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontBuilder" -v:minimal`: passed, 33 tests.

## Phase F1.44 - QA and closure gate

Goal: close Phase 1 only after architecture, host, browser, and network gates pass.

### Build gate

- [x] Build:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj
```

### Architecture gate

- [x] Focused tests prove:
  - [x] V2 has zero application services.
  - [x] V2 has zero middleware.
  - [x] V2 has zero client/runtime injection.
  - [x] V2 has zero application data loading.
  - [x] V2 has zero manual mutation contracts.
  - [x] V2 has zero business decisions.
  - [x] V2 has zero route/SEO/status ownership.
  - [x] Starter has the same visual-only boundary.
  - [x] Generated proof has the same isolation boundary.
- [x] Run:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontBuilderFoundationTests"
```

### Host-independent DI gate

- [x] Test `AddStorefrontApplication()` can resolve application graph without V2 or Starter registrations.
- [x] Test V2 and Starter only add view registrations and host assemblies.
- [x] Test no Presentation service references V2/Starter/generated storefront projects.

### HTTP smoke gate

- [x] Run V2 host smoke:
  - [x] `/`
  - [x] category route.
  - [x] product route.
  - [x] search route.
  - [x] content route.
  - [x] cart route.
  - [x] checkout route.
  - [x] payment result route.
  - [x] sign in.
  - [x] register.
  - [x] forgot password.
  - [x] reset password.
  - [x] account route.
  - [x] maintenance route.
  - [x] not found route.
  - [x] `robots.txt`.
  - [x] `sitemap.xml`.
- [x] Run Starter host smoke for the same route set where supported.

### Browser QA gate

- [x] Start local stack:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [x] Playwright browser tests:
  - [x] product render.
  - [x] add to cart.
  - [x] cart update/remove.
  - [x] checkout start.
  - [x] COD place order.
  - [x] sign in.
  - [x] register disabled policy.
  - [x] password recovery UI submit path.
  - [x] account profile.
  - [x] address book.
  - [x] order list/detail.
  - [x] logout.
  - [x] currency preference.
  - [x] public redirect.
  - [x] store maintenance.
  - [x] not found.

### Network audit gate

- [x] Browser only calls:
  - [x] same-origin BFF endpoints.
  - [x] static assets.
  - [x] media.
- [x] Browser must not call:
  - [x] direct Commerce Node `api/storefront/stores/{storeKey}/*`.
  - [x] Commerce Admin `api/commerce/*`.
  - [x] Control Plane APIs.
  - [x] removed `api/internal/*`.
  - [x] legacy `/api/public/*`.

### CI gate

- [x] Add required CI checks or documented CI commands for:
  - [x] visual-only architecture tests.
  - [x] host-independent DI tests.
  - [x] generated proof isolation.
  - [x] Storefront client regeneration gate if contract touched.
  - [x] browser E2E release suite before production.

- [x] Stop local stack if started:

```powershell
.\scripts\stop-v2-local.ps1
```

## Final definition of done

- [x] Presentation owns application bootstrap.
- [x] Presentation owns Runtime registration.
- [x] Presentation owns current-store middleware.
- [x] Presentation owns public redirect middleware.
- [x] Presentation owns rate limiting and BFF security policy.
- [x] Presentation owns navigation providers.
- [x] Presentation creates Header/Footer/Account contexts.
- [x] V2 Header/Footer/Account only render context.
- [x] Presentation owns auth form contracts/patterns.
- [x] Presentation owns checkout form contract/pattern.
- [x] Presentation owns currency/logout mutation contracts/patterns.
- [x] Presentation owns product purchase/price/stock presentation decisions.
- [x] Required visual slots cannot silently fall back to empty components.
- [x] Context compatibility is validated at startup.
- [x] V2 does not inject `IStorefront*` services.
- [x] V2 does not use `HttpClient`, Runtime, or Client.
- [x] V2 has no Middleware, Services, or shared Contracts.
- [x] V2 has no direct Runtime project/package reference.
- [x] V2 only contains thin host shell and visual source.
- [x] Starter reaches the same visual-only boundary.
- [x] GeneratedProof works independently.
- [x] Architecture, smoke, browser QA, and network audit pass.
- [x] CI protects the boundary.
- [x] `docs/architecture/03-runtime-boundaries.md` and `docs/architecture/10-v2-contract-ownership.md` are updated to match the final ownership.

2026-07-28 evidence:

- Build gate passed for Client, Runtime, Presentation, Components, V2.WASM, V2, and Starter with `dotnet build ... --no-restore -v:minimal`.
- Architecture/host-independent gate passed: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontBuilderFoundationTests|FullyQualifiedName~StorefrontApplicationBootstrapTests" -v:minimal` passed 83/83.
- V2 smoke was run by functional groups because the whole class exceeds short command timeouts: SignIn 4/4, checkout-empty 1/1, auth/register/recovery 21/21, account/currency/logout plus shell context 17/17, and SEO/system/cart/checkout/payment 19/19 passed. Starter host smoke passed 8/8.
- Generated proof passed static isolation plus browser/network QA: `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -RunBrowserQa`.
- Local V2 runtime started with `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`; release smoke passed; `run-storefront-registration-policy-e2e.ps1 -Headless`, `run-storefront-email-recovery-e2e.ps1 -Headless`, and `run-storefront-order-email-e2e.ps1 -Headless` exited 0. Reports under `.gstack/qa-reports/` show registration disabled/enabled, password recovery reset/login, COD order placement, order list/detail/receipt, and network guardrails with no direct retired flow calls or 5xx responses.
- Local runtime was stopped with `.\scripts\stop-v2-local.ps1`.
- CI guardrails were added in `.github/workflows/ci.yml` and `.github/workflows/storefront-builder.yml` for visual-only boundary tests, client regeneration, generated proof isolation, and generated proof browser/network gates.

## Autoplan decision audit

| Decision | Result | Reason |
| --- | --- | --- |
| Make V2 visual-only | Approved | Current source proves V2 still owns application policy; future generated storefronts need a clean visual consumer pattern. |
| Move bootstrap to Presentation | Approved | Presentation already owns App/Routes/BFF/page services and is the right application engine boundary. |
| Keep Runtime separate | Approved | Runtime remains server/BFF generated-client integration; collapsing it into Presentation would blur package responsibilities. |
| Move current-store and redirect middleware | Approved | These policies must be identical for V2, Starter, and generated storefronts. |
| Move auth/checkout forms into Presentation patterns | Approved with constraint | Presentation owns field/security contract; hosts keep layout/classes/copy. |
| Product decision logic | Approved as consolidation | Presentation already has mappers; remove V2 duplication instead of adding a second decision layer. |
| Clean Starter too | Required | Starter currently composes application graph manually; visual-only boundary must apply to both rich and minimal hosts. |
| GeneratedProof | Required | Without proof, generated storefronts can still drift into copying V2 logic. |
| Browser QA | Required | Cart/account/checkout/order flows are integration-heavy; build/smoke tests are not enough for production confidence. |

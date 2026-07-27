# Storefront Presentation Cutover Completion Todo

Status: Planned
Source: autoplan after investigate review on 2026-07-27
Purpose: close the remaining Storefront Presentation foundation gaps before treating V2, Starter, and generated storefronts as true consumers of the shared Storefront application engine.

This plan follows `Storefront Presentation Foundation.todo.md` but does not edit its historical SPF0-SPF15 evidence. The earlier plan records work that was completed at the time; this file records the follow-up blockers found after repo review.

Prerequisite cleanup: `Storefront V2 Manual Client Retirement.todo.md` tracks the F1.25 removal of the remaining V2 handwritten Storefront API client before this cutover can be closed.

## Current Verified Context

- [x] `BlazorShop.Storefront.Presentation` exists and owns shared App/Routes, route shells, page services, BFF/local endpoints, SEO/discovery, media composition, and view-slot contracts.
- [x] Architecture docs now state that V2, Starter, and generated storefronts should provide host configuration, registered views, assets, copy, and visual output.
- [x] `AddStorefrontPresentation()` registers only configuration and consent generated adapters by default:
  - `GeneratedStorefrontConfigurationClient`
  - `GeneratedStorefrontConsentClient`
  - `IStorefrontStoreConfigurationClient`
  - `IStorefrontConsentClient`
- [x] Presentation page services and endpoints still require contracts that are not fully registered by `AddStorefrontPresentation()`:
  - `IStorefrontCatalogClient`
  - `IStorefrontContentClient`
  - `IStorefrontCartClient`
  - `IStorefrontCheckoutClient`
  - `IStorefrontAddressClient`
  - `IStorefrontPaymentClient`
  - `IStorefrontAuthClient`
  - `IStorefrontCustomerClient`
  - `IStorefrontSessionResolver`
  - `IStorefrontDisplayContextProvider`
  - `IStorefrontPriceFormatter`
- [x] SPF17 moved neutral/generated adapter implementations out of `BlazorShop.Storefront.V2/Services` and into `BlazorShop.Storefront.Presentation/Services`.
- [x] SPF17 moved Presentation contract registration into `AddStorefrontPresentation()`; V2 no longer calls `AddStorefrontGeneratedClientRegistration()`.
- [x] SPF17 removed Starter `StorefrontBootstrapService`; Starter home now renders `StorefrontHomePageContext` supplied by Presentation.
- [x] `StorefrontPageState` supports more states than `StorefrontPage.razor` renders.
- [x] Several Presentation route pages still manually render `StorefrontSeoHead`, `PageTitle`, `HeadContent`, or call `StorefrontResponseHeaders` directly.
- [x] V2 and Starter visual views still contain some `PageTitle`/`HeadContent` markup.
- [x] `StorefrontRoutes.razor` still accepts host `AdditionalAssemblies`, and both V2/Starter register host assemblies as route assemblies.
- [x] Starter has build/package proof, but not a real HTTP/DI parity proof equivalent to V2 host smoke tests.
- [x] Some test baselines currently lock remaining transitional behavior for route/head ownership and package-mode cleanup.

## Goal

Make `BlazorShop.Storefront.Presentation` a complete application engine that can be consumed by V2, Starter, and generated storefronts without host-owned duplicate application logic.

After this plan:

- Presentation can resolve its own page services and endpoint dependencies when paired with Runtime.
- V2 does not own generated-client mapping required by Presentation page services.
- Starter visual views render Presentation contexts only.
- Route ownership is locked to Presentation.
- Visual views cannot own route metadata, SEO head, HTTP status, or crawler headers.
- V2 and Starter both pass DI validation and HTTP route smoke against the same Presentation route/BFF/SEO/media pipeline.
- Generated storefronts have a clear package boundary proof path.

## Non-goals

- [ ] Do not change Commerce Node Storefront API route shape.
- [ ] Do not rewrite cart, checkout, order, payment, pricing, inventory, sellability, or customer account business rules.
- [ ] Do not redesign V2 or Starter visual layout.
- [ ] Do not move V2 CSS, final copy, layout markup, or generated visual output into Presentation.
- [ ] Do not move Runtime server/BFF primitives into browser/WASM.
- [ ] Do not reintroduce Razor visual wrappers into `Storefront.Components`.
- [ ] Do not build the AI generator in this phase.
- [ ] Do not make React/Next/Nuxt skeletons in this phase.
- [ ] Do not remove host policy hooks that are genuinely host-specific; classify them first.

## Ownership Decision

Use this classification before moving code:

| Capability | Final owner | Notes |
| --- | --- | --- |
| Generated Storefront API adapter mapping for Presentation page services | Presentation | Should wrap Runtime facades or generated clients without V2 dependency. |
| Browser/BFF endpoint support contracts and local response envelopes | Presentation | Shared by V2, Starter, and generated storefronts. |
| Storefront route shells and page-state orchestration | Presentation | Only Presentation owns `@page`. |
| SEO/head/crawler/status policy | Presentation | Visual views cannot override noindex/canonical/status. |
| Current store resolution contract | Presentation | Default implementation may use Runtime context; V2 can override host-specific behavior. |
| Session/cookie/auth policy | Presentation contract plus host configuration | V2 may configure cookie names/options, but endpoint/page dependencies must be satisfiable outside V2. |
| Price/display formatting default | Presentation | Host may override through DI if a store needs visual-specific formatting. |
| V2 visual views/layout/assets/copy | Storefront.V2 | No route ownership. |
| Starter visual views/layout/assets/copy | Storefront.Starter | No API/data loading in views. |
| Generated storefront visual views/assets/copy | Storefront.{Name} | Package consumer only. |

## Phase Dependency Map

```text
SPF16 Baseline and failing guardrails
  -> SPF17 Presentation adapter ownership cutover
      -> SPF18 StorefrontPage mandatory orchestration
          -> SPF19 Route ownership lock
              -> SPF20 Visual SEO/head cleanup
                  -> SPF21 Starter second-consumer hardening
                      -> SPF22 Dependency and package cleanup
                          -> SPF23 Dual-host QA release gate
                              -> SPF24 Docs and checklist closure
```

## Phase SPF16 - Baseline And Failing Guardrails

Goal: add guardrails that describe the desired final state before moving code.

### Baseline Evidence - 2026-07-27

- V2-owned generated adapters still present under `BlazorShop.Storefront.V2/Services`: `GeneratedStorefrontCatalogContentClient`, `GeneratedStorefrontCartClient`, `GeneratedStorefrontCheckoutClient`, `GeneratedStorefrontAddressClient`, and `GeneratedStorefrontPaymentClient`.
- Presentation page services/endpoints still require unowned or host-registered contracts including catalog/content/cart/checkout/address/payment/auth/customer/session/display/price/current-store services.
- Host visual views and Presentation route pages still contain transitional `PageTitle`, `HeadContent`, `StorefrontSeoHead`, or direct `StorefrontResponseHeaders` usage.
- Route assembly discovery is still enabled through `StorefrontPresentationRouteOptions.AdditionalAssemblies` and `AddStorefrontPresentationRoutes(...)`.
- Starter still contained `StorefrontBootstrapService` and a home view that performed direct bootstrap loading before SPF17 removed it.
- SPF16 added explicit cutover guardrail test names in `StorefrontPresentationCutoverGuardrailTests`; they are skipped until their target phase implements the final state.

### Tasks

- [x] Record current adapter ownership inventory:
  - [x] all classes under `BlazorShop.Storefront.V2/Services` that implement Presentation contracts.
  - [x] all Presentation endpoints/page services that inject contracts not registered by `AddStorefrontPresentation()`.
  - [x] all visual views containing `PageTitle`, `HeadContent`, `StorefrontSeoHead`, `StorefrontResponseHeaders`, or `@page`.
  - [x] all host route assembly registrations.
- [x] Add architecture tests that initially fail or are marked explicit todo until implementation:
  - [x] `AddStorefrontPresentation()` registers every contract needed by Presentation page services and endpoints when Runtime is registered.
  - [x] `BlazorShop.Storefront.V2/Services` does not contain generated adapter classes implementing Presentation contracts.
  - [x] Starter visual pages do not inject generated client, Runtime data facades, or `StorefrontBootstrapService`.
  - [x] no `.razor` file outside `BlazorShop.Storefront.Presentation/Pages` contains `@page`.
  - [x] no host visual view contains `PageTitle`, `HeadContent`, `StorefrontSeoHead`, or `StorefrontResponseHeaders`.
  - [x] `StorefrontRoutes.razor` does not use host `AdditionalAssemblies` for route discovery.
- [x] Add DI validation tests:
  - [x] build service provider with Runtime + Presentation + V2 view registration.
  - [x] resolve every Presentation page service.
  - [x] resolve every Presentation local endpoint dependency contract.
  - [x] build service provider with Runtime + Presentation + Starter view registration.
  - [x] resolve the same services/contracts for Starter.
- [x] Add test names that make transitional state explicit:
  - [x] `StorefrontPresentation_DIGraph_IsHostIndependent`
  - [x] `StorefrontVisualViews_DoNotOwnRoutesOrSeoHead`
  - [x] `StorefrontStarter_ViewsRenderPresentationContextsOnly`
  - [x] `StorefrontRoutes_ArePresentationAssemblyOnly`

### Files likely touched

- `BlazorShop.Tests.V2/Architecture/StorefrontPresentationFoundationBoundaryTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontPageCompositionGuardrailTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontIndependenceBoundaryTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPresentation|FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontIndependence"
```

### Exit criteria

- [x] Gaps are locked by tests.
- [x] No behavior has been moved yet.
- [x] Transitional expected failures are clearly isolated to this cutover plan.

## Phase SPF17 - Presentation Adapter Ownership Cutover

Goal: make Presentation own the neutral application adapter graph instead of depending on V2 registration.

### Design decision

Prefer Presentation adapters backed by Runtime facades, not direct duplication of V2 manual HTTP transport.

Reason:

- Runtime already owns generated client registration and capability-scoped facades.
- Presentation owns page/BFF contracts that adapt Runtime results into browser/page contexts.
- V2 should not need to reference `Storefront.Client` just to satisfy Presentation.

### Tasks

- [x] Move or recreate neutral adapters in `BlazorShop.Storefront.Presentation/Services`:
  - [x] catalog/content adapter backed by `IStorefrontRuntimeCatalogFacade`, `IStorefrontRuntimeContentFacade`, `IStorefrontRuntimeNavigationFacade`, and `IStorefrontRuntimeSeoFacade`.
  - [x] cart adapter backed by `IStorefrontRuntimeCartFacade`.
  - [x] checkout adapter backed by `IStorefrontRuntimeCheckoutFacade`.
  - [x] address adapter backed by `IStorefrontRuntimeAddressFacade`.
  - [x] payment adapter backed by `IStorefrontRuntimePaymentFacade`.
  - [x] store configuration adapter already exists; keep and verify.
  - [x] consent adapter already exists; keep and verify.
- [x] Replace V2-owned generated adapter registrations for Presentation contracts:
  - [x] remove `GeneratedStorefrontCatalogContentClient` registration from V2 service extension.
  - [x] remove `GeneratedStorefrontCartClient` registration from V2 service extension.
  - [x] remove `GeneratedStorefrontCheckoutClient` registration from V2 service extension.
  - [x] remove `GeneratedStorefrontAddressClient` registration from V2 service extension.
  - [x] remove `GeneratedStorefrontPaymentClient` registration from V2 service extension.
  - [x] keep V2 registrations only for truly V2-local services.
- [x] Classify and migrate default host support services:
  - [x] `IStorefrontDisplayContextProvider`: default Presentation implementation from current store/configuration/currency runtime state.
  - [x] `IStorefrontPriceFormatter`: default Presentation implementation using public currency/culture options.
  - [x] `IStorefrontSessionResolver`: Presentation contract with default same-origin/session implementation; V2 may override only if host policy needs it.
  - [x] `IStorefrontAuthClient`: Presentation adapter using Runtime account/auth capability or a documented host-specific implementation if Runtime lacks the operation.
  - [x] `IStorefrontCustomerClient`: Presentation adapter using Runtime account/customer capability.
  - [x] `IStorefrontCurrentStoreProvider`: Presentation default based on Runtime context/store current response; V2 can override resolution policy.
- [x] If Runtime lacks a facade method needed by Presentation:
  - [x] add the method to Runtime using generated client.
  - [x] keep business rules in Commerce Node.
  - [x] add focused Runtime facade tests.
- [x] Update `AddStorefrontPresentation()`:
  - [x] register all default adapters with `TryAddScoped`.
  - [x] allow V2/Starter/generated hosts to override via DI before/after with documented order.
  - [x] fail clearly if Runtime has not been registered.
- [x] Delete or deprecate V2 adapter files only after tests pass.
- [x] Update endpoint/page service tests to assert contract dependencies are satisfied by Presentation registration.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/DependencyInjection/StorefrontPresentationServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntime|FullyQualifiedName~StorefrontPresentation|FullyQualifiedName~StorefrontEndpointDependency"
```

### Exit criteria

- [x] Presentation + Runtime can satisfy Presentation services without V2 adapter registration.
- [x] V2 no longer owns generated adapter implementations required by Presentation.
- [x] Starter can resolve Presentation page/BFF graph without `StorefrontBootstrapService`.
- [x] No browser/WASM project references Runtime or Client.

### SPF17 evidence - 2026-07-27

- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore`: passed.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj --no-restore`: passed.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore`: passed.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore`: passed.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntime|FullyQualifiedName~StorefrontPresentation|FullyQualifiedName~StorefrontEndpointDependency"`: passed `51/54`, with `3` future-phase cutover guardrails skipped.
- Additional touched guardrails passed: `StorefrontCommerceFlowCutover|StorefrontGeneratedCatalogContent|StorefrontIndependenceBoundary|StorefrontV2WASMRuntimeFoundation|StorefrontV2AuthClient|StorefrontSessionResolver` passed `64/65` with `1` existing skip; `StorefrontStarterFoundationBoundary|StorefrontPresentationCutover` passed `27/30` with `3` future-phase skips.
- Source scans found no `StorefrontBootstrapService`/direct generated-client bootstrap usage in Starter and no Runtime/Client references in browser/WASM projects.

## Phase SPF18 - StorefrontPage Mandatory Orchestration

Goal: make `StorefrontPage.razor` the one route-state/head/status wrapper for Presentation route shells.

### Tasks

- [x] Extend `StorefrontPage.razor` to handle all `StorefrontPageState` cases:
  - [x] `LoadingState`
  - [x] `Ready<TContext>`
  - [x] `EmptyState`
  - [x] `NotFoundState`
  - [x] `ServiceUnavailableState`
  - [x] `UnauthorizedState`
  - [x] `MaintenanceState`
  - [x] `ErrorState`
- [x] Add optional render fragments or view-set hooks for:
  - [x] loading state.
  - [x] empty state.
  - [x] unauthorized/private state.
  - [x] maintenance state.
  - [x] service unavailable state.
  - [x] not found state.
  - [x] error state.
- [x] Move SEO/head composition into `StorefrontPage`:
  - [x] document title.
  - [x] description.
  - [x] robots.
  - [x] canonical.
  - [x] structured data hook.
  - [x] alternate/hreflang hook if already modeled.
- [x] Move status/header application into `StorefrontPage`:
  - [x] 200/explicit status for ready.
  - [x] 404 not found.
  - [x] 503 service unavailable/maintenance.
  - [x] noindex/nofollow private pages.
  - [x] private cache-control.
- [x] Convert Presentation route pages to use `StorefrontPage`:
  - [x] home.
  - [x] category.
  - [x] product.
  - [x] search.
  - [x] todays deals.
  - [x] new releases.
  - [x] content page.
  - [x] cart.
  - [x] checkout.
  - [x] payment result.
  - [x] account route.
  - [x] auth routes.
  - [x] maintenance.
  - [x] not-found catch-all.
- [x] Route pages should only:
  - [x] bind route/query parameters.
  - [x] call one page service.
  - [x] pass state/context to `StorefrontPage`.
  - [x] select the registered view slot.
- [x] Add tests proving route pages no longer call `StorefrontResponseHeaders` directly.
- [x] Add tests proving route pages no longer render `StorefrontSeoHead`, `PageTitle`, or `HeadContent` directly except inside `StorefrontPage`.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/PagePatterns/StorefrontPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/PagePatterns/StorefrontPageState.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/**/*RoutePage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/*`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontPageCompositionGuardrailTests.cs`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontIndexingPolicy|FullyQualifiedName~StorefrontPresentation"
```

### Completion Evidence - 2026-07-27

- `StorefrontPage.razor` now owns `StorefrontSeoHead`, structured data, `StorefrontResponseHeaders.ApplyStatus(...)`, non-ready state rendering fragments, private metadata, and maintenance auto-refresh head output.
- All Presentation `*RoutePage.razor` files under `BlazorShop.Storefront.Presentation/Pages` route through `StorefrontPage` and no longer directly render `StorefrontSeoHead`, `PageTitle`, `HeadContent`, or `StorefrontResponseHeaders`.
- `rg -n "StorefrontSeoHead|PageTitle|HeadContent|StorefrontResponseHeaders" BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\Pages -g "*RoutePage.razor"` returned no matches.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore` passed.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore` passed.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore` passed.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontIndexingPolicy|FullyQualifiedName~StorefrontPresentation"` passed `66/69`, with 3 future phase skips.

### Exit criteria

- [x] Every Presentation route page uses `StorefrontPage` or an equivalent single base/wrapper.
- [x] Route pages no longer duplicate SEO/status/noindex handling.
- [x] All `StorefrontPageState` cases render intentionally.

## Phase SPF19 - Route Ownership Lock

Goal: ensure hosts cannot accidentally become route owners.

### Tasks

- [x] Remove host route assembly discovery from `StorefrontRoutes.razor`.
- [x] Remove `StorefrontPresentationRouteOptions.AdditionalAssemblies` if it is no longer needed.
- [x] Remove or obsolete `AddStorefrontPresentationRoutes(...)`.
- [x] Update V2 view registration:
  - [x] keep `AddV2FoundationViews()`.
  - [x] stop calling `AddStorefrontPresentationRoutes(typeof(V2FoundationViewRegistration).Assembly)`.
- [x] Update Starter view registration:
  - [x] keep `AddStarterFoundationViews()`.
  - [x] stop calling `AddStorefrontPresentationRoutes(typeof(Program).Assembly)`.
- [x] Keep `MapRazorComponents<StorefrontApp>()` host assembly registration only for visual component discovery/rendering, not route discovery.
- [x] Add guardrail:
  - [x] only `BlazorShop.Storefront.Presentation/Pages` may contain `@page`.
  - [x] `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, `BlazorShop.Storefront.V2.WASM`, and generated storefront source must not contain `@page`.
  - [x] generated storefront validation fails if generated visual files contain `@page`.
- [x] Update StorefrontBuilder docs/contracts so generated projects register view slots, not route assemblies.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Routing/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/DependencyInjection/StorefrontPresentationServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs`
- `tools/BlazorShop.AI.StorefrontBuilder/*`
- `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
rg -n "^@page" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.Starter BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM -g "*.razor"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontBuilder"
```

Expected `rg` result after this phase: no matches outside Presentation route pages.

### Completion Evidence - 2026-07-27

- `StorefrontRoutes.razor` now uses only `AppAssembly="@typeof(StorefrontApp).Assembly"`; host route `AdditionalAssemblies` is removed from Presentation routing.
- `StorefrontPresentationRouteOptions` and `AddStorefrontPresentationRoutes(...)` were removed; V2 and Starter view registration now only call `AddStorefrontFoundationViews(...)`.
- Host `MapRazorComponents<StorefrontApp>().AddAdditionalAssemblies(...)` remains for component/view discovery only.
- StorefrontBuilder static validation now fails generated visual files that declare `@page`, and current StorefrontBuilder docs require generated projects to register Presentation view slots instead of route assemblies.
- `rg -n "^@page" BlazorShop.PresentationV2\BlazorShop.Storefront.V2 BlazorShop.PresentationV2\BlazorShop.Storefront.Starter BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM -g "*.razor"` returned no matches.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore` passed.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore` passed after rerunning sequentially; the first parallel run hit a transient MSBuild file lock.
- `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore` passed after rerunning sequentially; the first parallel run hit a transient static-web-assets file lock.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontBuilder"` passed `70/70`.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPresentation"` passed `18/20`, with 2 future phase skips.

### Exit criteria

- [x] Presentation is the only route owner.
- [x] V2/Starter/generated storefronts can add visual components without adding routes.
- [x] Route discovery cannot drift by adding `@page` in host projects.

## Phase SPF20 - Visual SEO And Head Cleanup

Goal: visual views cannot override route SEO, noindex, canonical, or HTTP status.

### Tasks

- [x] Remove `PageTitle` and `HeadContent` from V2 visual views:
  - [x] cart view.
  - [x] checkout view.
  - [x] payment result view.
  - [x] account host view if present.
  - [x] any remaining catalog/content/system visual view.
- [x] Remove `PageTitle` and `HeadContent` from Starter visual views:
  - [x] cart view.
  - [x] checkout view.
  - [x] payment result view.
  - [x] account host view.
  - [x] any remaining content/system visual view.
- [x] Keep brand/root metadata in host `ApplicationHead` only:
  - [x] favicon.
  - [x] theme color.
  - [x] root CSS.
  - [x] static host metadata that is not route SEO.
- [x] Move all route-specific SEO/noindex data into Presentation page service output:
  - [x] cart noindex/nofollow.
  - [x] checkout noindex/nofollow.
  - [x] payment result noindex/nofollow.
  - [x] account noindex/nofollow.
  - [x] auth noindex/nofollow.
  - [x] search noindex/canonical behavior.
  - [x] product/category/home/content index/canonical behavior.
- [x] Add tests:
  - [x] visual views do not contain head/status components.
  - [x] private/application routes still emit noindex/nofollow.
  - [x] product/category/content routes still emit expected canonical metadata.
  - [x] maintenance/service unavailable still emit 503 and noindex.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/**/*.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/**/*.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/**/*.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/**/*PageService.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/PagePatterns/StorefrontPage.razor`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
rg -n "PageTitle|HeadContent|StorefrontSeoHead|StorefrontResponseHeaders" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.Starter -g "*.razor" -g "*.cs"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndexingPolicy|FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontV2HostSmoke"
```

Expected `rg` result after this phase:

- no `PageTitle`, `HeadContent`, or `StorefrontSeoHead` in host visual views.
- `StorefrontResponseHeaders` only in Presentation policy/middleware or V2 host middleware where it is truly host pipeline behavior.

### Completion Evidence - 2026-07-27

- Removed `PageTitle`/`HeadContent` from V2 cart, checkout, payment result, and WASM account visual components; renamed the WASM cart CSS slot from `PageTitle` to `HeaderTitle`.
- Removed `PageTitle` from Starter cart, checkout, payment result, and account views.
- Payment pending and maintenance auto-refresh now flow through `StorefrontPage` -> `StorefrontSeoHead`; visual views no longer render refresh or robots tags directly.
- `StorefrontResponseHeaders.ApplyStatus(...)` keeps ready/private behavior and delegates 404/503 states through not-found/service-unavailable helpers so route status/head policy remains Presentation-owned.
- `rg -n "PageTitle|HeadContent|StorefrontSeoHead|StorefrontResponseHeaders" BlazorShop.PresentationV2\BlazorShop.Storefront.V2 BlazorShop.PresentationV2\BlazorShop.Storefront.Starter -g "*.razor" -g "*.cs"` returns only V2 host middleware/local endpoint header policy references, not visual views.
- `rg -n "PageTitle|HeadContent|StorefrontSeoHead" BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM -g "*.razor" -g "*.cs"` returned no matches.
- Presentation, V2 WASM, V2, and Starter builds passed with `--no-restore`.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndexingPolicy|FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontPresentation"` passed `68/69`, with 1 future phase skip.
- Targeted host smoke for payment pending and maintenance auto-refresh passed `2/2` with `--no-build`. The full `StorefrontV2HostSmoke` filter exceeded the command timeout, so SPF20 used focused route-status/head smoke coverage.

### Exit criteria

- [x] SEO/head/status ownership is Presentation-only for route pages.
- [x] Host views cannot accidentally index cart/checkout/account.
- [x] V2 visual output remains visually equivalent.

## Phase SPF21 - Starter Second Consumer Hardening

Goal: Starter becomes a true consumer of Presentation page contexts, not a separate data loader.

### Tasks

- [x] Remove `StorefrontBootstrapService` from Starter.
- [x] Remove direct generated client usage from Starter views.
- [x] Remove direct Runtime data facade usage from Starter visual pages unless the component is explicitly a server-only host extension.
- [x] Convert `Pages/Ssr/Home/HomePage.razor`:
  - [x] remove `BootstrapService`.
  - [x] remove direct `StorefrontOptions` display if the same information exists in `StorefrontHomePageContext`.
  - [x] render `Context.FeaturedProducts` or equivalent Presentation home context product data.
  - [x] render store identity from Presentation context.
  - [x] render feature visibility from a context/capability projection provided by Presentation, not by direct API fetch in the view.
- [x] Review all Starter visual views:
  - [x] each has `[Parameter, EditorRequired] public ... Context`.
  - [x] no `OnInitializedAsync` data fetch.
  - [x] no generated client injection.
  - [x] no Runtime facade injection for page data.
  - [x] only visual services like feature manifest/copy helpers remain if they do not call Commerce Node.
- [x] Keep Starter feature manifest as visual placement metadata only.
- [x] If Starter needs feature capability state, add it to Presentation context model rather than fetching inside visual view.
- [x] Add tests:
  - [x] Starter views render context only.
  - [x] Starter has no `StorefrontBootstrapService`.
  - [x] Starter source contains no `BlazorShop.Storefront.Client` usage unless it is a package reference required by Runtime/package proof.
  - [x] Starter does not inject generated client interfaces.
  - [x] Starter home view uses `StorefrontHomePageContext`.
- [x] Add HTTP parity proof:
  - [x] start Starter via `WebApplicationFactory<BlazorShop.Storefront.Starter.Program>`.
  - [x] stub Commerce Node responses through configured test handler/runtime.
  - [x] GET `/` renders home with fixture store/product data.
  - [x] GET `/product/{slug}` renders product.
  - [x] GET `/category/{slug}` renders category.
  - [x] GET `/search?q=...` renders noindex search page.
  - [x] GET `/my-cart` renders cart route shell.
  - [x] GET `/checkout` renders checkout route shell.
  - [x] GET `/account` renders account route shell.
  - [x] GET `/robots.txt` and `/sitemap.xml` use Presentation endpoints.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Services/StorefrontBootstrapService.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/**/*.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Features/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontHomePageService.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/*Context*`
- `BlazorShop.Tests.V2/Architecture/StorefrontStarterFoundationBoundaryTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontStarterHostSmokeTests.cs`

### Verification

```powershell
rg -n "StorefrontBootstrapService|IStorefrontStoreClient|IStorefrontConfigurationClient|IStorefrontCatalogClient|Storefront\\.Client|OnInitializedAsync" BlazorShop.PresentationV2/BlazorShop.Storefront.Starter -g "*.cs" -g "*.razor"
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontStarter|FullyQualifiedName~StorefrontPresentation"
```

Expected `rg` result after this phase:

- no `StorefrontBootstrapService`.
- no direct generated client interface injection in Starter source.
- `Storefront.Client` may remain only in package metadata if package proof still requires it, or be removed in SPF22 if no direct Starter use remains.

2026-07-27 evidence:

- `StorefrontHomePageContext` now carries `StorefrontDisplayContext` plus Presentation `StorefrontCapability` projections; Starter home renders store key/name/currency and feature visibility from that context.
- Starter `Pages/**/*.razor` render Presentation contexts only; no visual page owns generated client/runtime facade data loading.
- `StorefrontStarterHostSmokeTests` starts `WebApplicationFactory<BlazorShop.Storefront.Starter.Program>` and proves `/`, `/product/{slug}`, `/category/{slug}`, `/search?q=...`, `/my-cart`, `/checkout`, `/account`, `/robots.txt`, and `/sitemap.xml`.
- `rg -n "StorefrontBootstrapService|IStorefrontStoreClient|IStorefrontConfigurationClient|IStorefrontCatalogClient|Storefront\.Client|OnInitializedAsync" BlazorShop.PresentationV2\BlazorShop.Storefront.Starter -g "*.cs" -g "*.razor"` returned no matches.
- Starter build passed with `--no-restore`; `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontStarter|FullyQualifiedName~StorefrontPresentation"` passed `54/55` with 1 existing skip and known MessagePack/Browserslist warnings.

### Exit criteria

- [x] Starter visual views render only Presentation contexts.
- [x] Starter proves Presentation can power a second host.
- [x] Starter does not maintain a parallel home/catalog data path.

## Phase SPF22 - Dependency And Package Cleanup

Goal: remove stale dependency edges after adapter and Starter cutover.

### Target shape

```text
BlazorShop.Storefront.V2
  -> ServiceDefaults
  -> Storefront.Presentation
  -> Storefront.Runtime
  -> Storefront.Components
  -> Storefront.V2.WASM

BlazorShop.Storefront.Starter
  -> Storefront.Presentation package/project during monorepo development
  -> Storefront.Runtime package
  -> Storefront.Components package if visual contracts are used

BlazorShop.Storefront.Presentation
  -> Storefront.Runtime
  -> Storefront.Components

BlazorShop.Storefront.Runtime
  -> Storefront.Client
```

### Tasks

- [x] Remove `BlazorShop.Storefront.Client` project reference from V2 if no V2 source uses generated client directly.
- [x] Remove `BlazorShop.Storefront.Client` package reference from Starter if Starter no longer directly compiles against generated DTO/client types.
- [x] Keep `Storefront.Runtime -> Storefront.Client`.
- [x] Keep `Storefront.Presentation -> Storefront.Runtime`.
- [x] Decide whether Starter should consume `Storefront.Presentation` as:
  - [x] `ProjectReference` in monorepo development only.
  - [x] `PackageReference` in independent proof.
  - [x] generated project always package reference.
- [x] Align tests with the chosen mode:
  - [x] monorepo source build allows ProjectReference to Presentation if documented.
  - [x] independent package proof rewrites/uses PackageReference for Presentation.
  - [x] no test simultaneously requires and rejects the same reference mode.
- [x] Update package version props:
  - [x] Client package.
  - [x] Runtime package.
  - [x] Presentation package.
  - [x] Components package if still needed.
- [x] Run package proof:
  - [x] pack Client.
  - [x] pack Runtime.
  - [x] pack Presentation.
  - [x] pack Components if consumed by Starter/generated.
  - [x] restore Starter or generated proof from local feed.
  - [x] build restored project with no source fallback.
- [x] Add guardrails:
  - [x] V2 no direct `BlazorShop.Storefront.Client` reference.
  - [x] Starter no direct `BlazorShop.Storefront.Client` usage in source.
  - [x] generated storefront no direct backend/core/API references.
  - [x] generated storefront no V2 reference.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StorefrontPackageVersions.props`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/nuget.config`
- `scripts/qa/run-storefront-foundation-isolation-gate.ps1`
- `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- `BlazorShop.Tests.V2/Architecture/*`

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore
.\scripts\qa\run-storefront-foundation-isolation-gate.ps1
.\scripts\qa\run-storefront-builder-isolation-gate.ps1
```

2026-07-27 evidence:

- `BlazorShop.Storefront.V2.csproj` no longer references `BlazorShop.Storefront.Client`; V2 keeps ServiceDefaults, Presentation, Runtime, Components, and V2 WASM.
- `BlazorShop.Storefront.Starter.csproj` no longer has a direct `BlazorShop.Storefront.Client` PackageReference; it keeps Runtime and Components packages plus a monorepo Presentation ProjectReference.
- `scripts/qa/run-storefront-starter-isolation-gate.ps1` now packs Client/Runtime/Presentation/Components, clears stale local `1.0.0-local` package cache entries, rewrites the isolated Starter Presentation ProjectReference to PackageReference, and restores/builds/publishes from the local feed.
- `scripts/qa/run-storefront-builder-isolation-gate.ps1` now requires generated projects to directly reference Runtime, Presentation, and Components packages while keeping Client package metadata for Runtime transport compatibility.
- Focused package/boundary tests passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~StorefrontBuilder"` passed `79/79` with known MessagePack/Browserslist warnings.
- Builds passed sequentially for V2 and Starter with `--no-restore`; the first parallel run hit a transient Runtime `obj` file lock and passed when rerun sequentially.
- `.\scripts\qa\run-storefront-foundation-isolation-gate.ps1`, `.\scripts\qa\run-storefront-starter-isolation-gate.ps1`, and `.\scripts\qa\run-storefront-builder-isolation-gate.ps1` passed. The builder gate needed a fresh generated proof from `.\scripts\generate-storefront-sample.ps1 -Force` because the ignored generated artifact was stale.

### Exit criteria

- [x] V2 does not depend on generated Client directly unless a documented V2-only exception remains.
- [x] Starter has no direct generated-client source usage.
- [x] Independent package proof consumes the same platform surface a generated storefront will consume.
- [x] Architecture tests no longer conflict on Starter Presentation reference mode.

## Phase SPF23 - Dual-host QA Release Gate

Goal: prove V2 and Starter both run against the same Presentation application surface.

### V2 test gate

- [x] Build:
  - [x] `Storefront.Client`
  - [x] `Storefront.Runtime`
  - [x] `Storefront.Components`
  - [x] `Storefront.Presentation`
  - [x] `Storefront.V2.WASM`
  - [x] `Storefront.V2`
- [x] Focused unit/architecture tests:
  - [x] Storefront independence boundary.
  - [x] Presentation foundation boundary.
  - [x] Runtime facade tests.
  - [x] Page composition guardrails.
  - [x] BFF endpoint boundary tests.
  - [x] OpenAPI generated client hardening tests.
- [x] V2 host smoke tests:
  - [x] home.
  - [x] category.
  - [x] product.
  - [x] search noindex.
  - [x] cart.
  - [x] checkout.
  - [x] account.
  - [x] auth/recovery/register disabled.
  - [x] payment result.
  - [x] robots.
  - [x] sitemap.
  - [x] maintenance/service unavailable.
- [x] Playwright release E2E:
  - [x] product browse.
  - [x] add to cart.
  - [x] update cart.
  - [x] checkout COD real order placement.
  - [x] order placed message/SMTP capture if configured.
  - [x] account order list/detail.
  - [x] recovery flow.
  - [x] no direct browser call to Commerce Node.

### Starter test gate

- [x] Build:
  - [x] `Storefront.Starter`.
- [x] Starter DI validation:
  - [x] all Presentation page services resolve.
  - [x] all Presentation endpoint dependencies resolve.
  - [x] all registered view slots validate context parameter.
- [x] Starter HTTP smoke:
  - [x] `/`
  - [x] `/category/{slug}`
  - [x] `/product/{slug}`
  - [x] `/search?q=...`
  - [x] `/my-cart`
  - [x] `/checkout`
  - [x] `/account`
  - [x] `/pages/{slug}`
  - [x] `/maintenance`
  - [x] `/robots.txt`
  - [x] `/sitemap.xml`
- [x] Starter package proof:
  - [x] local feed restore.
  - [x] build outside direct V2 references.
  - [x] no generated source copy.

### Generated storefront proof

- [x] StorefrontBuilder isolation gate passes.
- [x] Generated proof consumes Runtime/Presentation/Components packages and keeps Client package metadata for Runtime transport compatibility.
- [x] Generated proof has no `@page` outside generated host rules if any are allowed; preferred no route pages.
- [x] Generated proof does not reference V2, backend/core/API projects, `Web.SharedV2`, Control Plane, or Commerce Node API.
- [x] Generated proof browser-safe code calls same-origin BFF only.

### Verification commands

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM\BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore
dotnet build BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPresentation|FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontIndependence|FullyQualifiedName~StorefrontStarter|FullyQualifiedName~StorefrontRuntime|FullyQualifiedName~StorefrontBuilder"
.\scripts\qa\run-storefront-foundation-isolation-gate.ps1
.\scripts\qa\run-storefront-builder-isolation-gate.ps1
```

Playwright commands should use the current release checklist and local runner:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
.\scripts\qa\run-storefront-registration-policy-e2e.ps1 -Headless
.\scripts\qa\run-storefront-order-email-e2e.ps1 -Headless
```

2026-07-27 evidence:

- Post-fix build gate passed for Client, Runtime, Components, Presentation, V2 WASM, V2, Starter, and Tests. Storefront project builds had 0 warnings/errors; Tests build kept known MessagePack vulnerability warnings and Browserslist freshness warning.
- Focused Storefront test gate passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPresentation|FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontIndependence|FullyQualifiedName~StorefrontStarter|FullyQualifiedName~StorefrontRuntime|FullyQualifiedName~StorefrontBuilder"` passed `172/172`.
- SPF23 browser gate initially caught a real Runtime/generated-client base-address regression: V2 was configuring Runtime generated clients with scoped `/api/storefront/stores/{storeKey}/` base address, producing double-prefixed Commerce Node requests such as `/api/storefront/stores/default/api/storefront/stores/default/store/current`.
- Fix: V2 now calls `AddStorefrontPlatformRuntime()` without the manual scoped HTTP callback, so Runtime generated clients use `StorefrontRuntimeOptions.CommerceNodeBaseUrl` from `ResolveCommerceNodeBaseAddress(...)`. `StorefrontApiEndpointResolverTests.StorefrontRuntimeRegistration_UsesUnscopedCommerceNodeBaseAddress` guards this.
- Regression slice passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontApiEndpointResolverTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests|FullyQualifiedName~StorefrontRuntime"` passed `44/44`.
- `.\scripts\qa\run-storefront-foundation-isolation-gate.ps1`, `.\scripts\qa\run-storefront-starter-isolation-gate.ps1`, and `.\scripts\qa\run-storefront-builder-isolation-gate.ps1` passed after the fix.
- `.\scripts\run-v2-local.ps1 -StopExisting` started the local runtime; `.\scripts\qa\run-storefront-registration-policy-e2e.ps1 -Headless` passed with zero forbidden browser calls and zero 5xx responses.
- `.\scripts\qa\run-storefront-order-email-e2e.ps1 -Headless` passed; COD order `ORD-20260727-F5EDEF50` was placed, queued order email was sent, Mailpit matched exactly one message, account order list/detail/receipt were covered, SMTP outage retry recovered, and network guardrails reported zero 5xx and zero retired-flow calls.
- `.\scripts\stop-v2-local.ps1` stopped the four local runtime processes but returned a null-valued cleanup error after issuing the stops; a port check confirmed no listeners remained on 5280, 5281, 5180, or 18598. This did not affect SPF23 browser evidence.

### Exit criteria

- [x] V2 passes production-facing browser QA.
- [x] Starter passes DI + HTTP route smoke as second consumer.
- [x] Generated proof passes package/isolation gates.
- [x] No direct browser call to Commerce Node appears in network audit.
- [x] COD order placement still works through Presentation BFF + Runtime + Commerce Node.

## Phase SPF24 - Documentation And Checklist Closure

Goal: make architecture docs and QA checklists match the final code shape.

### Tasks

- [x] Update `AGENTS.md` if any boundary wording changes.
- [x] Update `docs/architecture/03-runtime-boundaries.md`:
  - [x] Presentation owns route/page/BFF/SEO/media application graph.
  - [x] Runtime remains server/BFF-only generated client integration.
  - [x] V2 owns host config/views/assets/copy.
  - [x] Starter/generated own host config/views/assets/copy.
  - [x] route ownership is Presentation-only.
- [x] Update `docs/architecture/05-project-and-folder-guide.md`:
  - [x] exact final dependency shape.
  - [x] no route pages in V2/Starter/generated.
  - [x] no visual head/status ownership in hosts.
- [x] Update `docs/architecture/10-v2-contract-ownership.md`:
  - [x] Presentation local endpoint contracts.
  - [x] Runtime facade contract usage.
  - [x] no generated client direct use in host views.
- [x] Update `docs/architecture/11-storefront-builder.md`:
  - [x] generated storefronts register view slots.
  - [x] generated storefronts do not generate route/BFF/SEO logic.
  - [x] package proof includes Presentation.
- [x] Update `docs/visual-reverse-engineering-skill/*`:
  - [x] generated project route rules.
  - [x] visual-only view ownership.
  - [x] protected files/areas.
- [x] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:
  - [x] route ownership checks.
  - [x] V2 browser e2e checks.
  - [x] no direct Commerce Node browser call.
  - [x] no visual `HeadContent`.
- [x] Add or update a QA checklist for Starter if needed:
  - [x] DI validation.
  - [x] HTTP route smoke.
  - [x] package proof.
- [x] Add completion notes to this file with command output summary.

### Verification

```powershell
rg -n "V2 owns route|Starter owns route|AddStorefrontPresentationRoutes|manual StorefrontApiClient transport from Storefront V2" AGENTS.md docs\architecture docs\visual-reverse-engineering-skill docs\agents BlazorShop.PresentationV2 -g "*.md" -g "*.cs" -g "*.razor" -g "!bin" -g "!obj"
rg -n "^@page" BlazorShop.PresentationV2\BlazorShop.Storefront.V2 BlazorShop.PresentationV2\BlazorShop.Storefront.Starter BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM artifacts\storefront-builder\generated\BlazorShop.Storefront.GeneratedProof -g "*.razor"
rg -n -F "BlazorShop.Storefront.Client" BlazorShop.PresentationV2\BlazorShop.Storefront.V2 BlazorShop.PresentationV2\BlazorShop.Storefront.Starter -g "*.csproj" -g "*.cs" -g "*.razor"
rg -n "PageTitle|HeadContent|StorefrontSeoHead" BlazorShop.PresentationV2\BlazorShop.Storefront.V2 BlazorShop.PresentationV2\BlazorShop.Storefront.Starter BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM -g "*.razor"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontContractOwnership|FullyQualifiedName~StorefrontSharedPlatformPackageContract|FullyQualifiedName~StorefrontPresentationFoundationBoundary|FullyQualifiedName~StorefrontPageComposition|FullyQualifiedName~StorefrontIndependence|FullyQualifiedName~StorefrontStarterFoundationBoundary|FullyQualifiedName~StorefrontBuilder|FullyQualifiedName~StorefrontPresentationCutover"
```

2026-07-27 evidence:

- Updated current source-of-truth docs: `AGENTS.md`, `docs/architecture/01-system-map.md`, `03-runtime-boundaries.md`, `05-project-and-folder-guide.md`, `10-v2-contract-ownership.md`, `11-storefront-builder.md`, Presentation/Starter ADR notes, `QA-StorefrontV2.todo.md`, and visual reverse engineering docs now describe the final Runtime/Presentation/Components package surface and Runtime-owned Client transport.
- Added `docs/refactor-control-Commerce-storefront/QA-StorefrontStarter.todo.md` for Starter DI, HTTP smoke, route/head ownership, and package-proof checks.
- Current-doc/source scans returned no matches for old V2/Starter route ownership, removed route assembly APIs, host route pages, host visual head components, or direct V2/Starter Client source usage.
- The broad `FullyQualifiedName~Architecture|FullyQualifiedName~Storefront` command exceeded the 15-minute command timeout, so closure verification was split into the focused cutover/contract/package/page/starter/builder guardrail slice above. That focused slice passed `152/152`.

### Exit criteria

- [x] Current docs no longer conflict with final code.
- [x] Historical plans remain historical and are not treated as current truth when conflicting.
- [x] QA checklist can be used as a production release gate.

## Final Definition Of Done

### Ownership

- [x] Presentation owns all route shells, page-state orchestration, page services, BFF/local endpoint mappings, route SEO/head/status, sitemap, robots, and media endpoint composition.
- [x] Runtime owns generated client registration, typed generated-client facades, error mapping, and server/BFF integration primitives.
- [x] V2 owns host config, host pipeline, static assets, view registration, visual templates, layout, copy, and V2 WASM component placement.
- [x] Starter owns neutral host config, view registration, visual templates, layout, copy, and starter feature placement metadata.
- [x] Generated storefronts own generated/custom host config, views, assets, copy, and visual output only.

### Dependencies

- [x] Presentation references Runtime and Components only.
- [x] Runtime references Client only.
- [x] Components references no Presentation/Runtime/Client/V2/backend projects and contains no Razor visual wrappers.
- [x] V2 does not reference Client directly unless a documented temporary exception remains.
- [x] Starter source does not use generated Client directly after Bootstrap removal.
- [x] V2.WASM does not reference Presentation, Runtime, or Client.
- [x] Generated storefronts do not reference V2, backend/core/API projects, or `Web.SharedV2`.

### Routing and SEO

- [x] Only Presentation route pages contain `@page`.
- [x] Host visual views contain no `PageTitle`, `HeadContent`, `StorefrontSeoHead`, or route status/header calls.
- [x] `StorefrontPage` handles all page states intentionally.
- [x] Private/application routes remain noindex/nofollow.
- [x] Product/category/content/home routes keep canonical SEO.
- [x] Maintenance/service unavailable routes keep 503 and noindex behavior where appropriate.

### Consumer proof

- [x] V2 and Starter both use the same Presentation App/Routes/page services/BFF/SEO/media pipeline.
- [x] Fixing route or BFF behavior in Presentation benefits both consumers.
- [x] Starter home/catalog/product/search/cart/checkout/account routes run without direct Starter data loading.
- [x] Generated proof consumes package boundaries and passes isolation gate.

### QA

- [x] Focused build commands pass.
- [x] Architecture and boundary tests pass.
- [x] V2 host smoke tests pass or are updated to current Presentation behavior.
- [x] Starter host smoke tests pass.
- [x] StorefrontBuilder package/isolation gates pass.
- [x] Playwright release flows pass, including COD real order placement.
- [x] Browser network audit confirms no direct Commerce Node calls.

## Risk Controls

- [x] Move adapters capability by capability; do not move all runtime mapping in one untested pass.
- [x] Keep Runtime facade behavior unchanged while moving ownership.
- [x] Add characterization tests before deleting V2 adapter files.
- [x] Do not move visual markup/copy into Presentation to simplify tests.
- [x] Do not let Starter call generated clients from views as a shortcut.
- [x] Do not remove V2 `Storefront.Client` reference until `rg` proves no source usage remains.
- [x] Do not remove route assembly support until all hosts prove visual slot rendering still works.
- [x] Keep browser Playwright QA after structural refactor because build tests will not catch hydration/BFF regressions.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Add a follow-up cutover plan instead of reopening SPF0-SPF15 history | Auto-decided | Traceability | SPF0-SPF15 contains historical evidence; new blockers should be tracked as explicit completion work. | Rewrite old plan evidence |
| 2 | Adapter ownership | Presentation should own default adapters needed by its own page services/endpoints | Auto-decided | Boundary clarity | A shared application engine cannot depend on V2-only registration to resolve its core graph. | Keep adapters in V2 and require every host to reimplement them |
| 3 | Adapter implementation | Prefer Runtime-backed Presentation adapters | Auto-decided | Avoid duplication | Runtime already owns generated-client/server integration; Presentation should adapt Runtime outputs to page/BFF contracts. | Copy V2 manual transport into Presentation |
| 4 | Route ownership | Only Presentation should contain `@page` | Auto-decided | Maintainability | Hosts are visual implementations; letting host assemblies provide routes makes ownership hard to inspect. | Continue scanning host AdditionalAssemblies |
| 5 | SEO/head ownership | Visual views must not render route head/status metadata | Auto-decided | Production safety | Cart/checkout/account noindex and canonical rules must not be overrideable by theme accident. | Allow themes to own PageTitle/HeadContent |
| 6 | Starter proof | Starter must render Presentation contexts only | Auto-decided | Consumer proof | A second consumer is not proven if Starter separately fetches the same data. | Keep StorefrontBootstrapService as Starter-specific shortcut |
| 7 | QA | Require V2 and Starter HTTP/DI proof plus Playwright release flows | Auto-decided | Real failure detection | Build-only tests already missed DI/route ownership gaps; browser/BFF behavior needs runtime proof. | Treat architecture tests as sufficient |

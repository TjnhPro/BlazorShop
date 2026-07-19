# V2 Architecture Boundary Hardening Todo

Status: in progress  
Created: 2026-07-19  
Source: investigate review of V2 architecture/codebase hotspots  
Purpose: turn the current V2 architecture findings into an executable phased refactor plan without rewriting the product or breaking existing Commerce flows.

## Scope

This plan covers the architecture hardening issues verified against the current codebase:

- Move generic Commerce Node admin HTTP transport details out of `BlazorShop.Application`.
- Replace Control Plane commerce gateway result contracts with the existing `ApplicationResult<T>` model.
- Split oversized Control Plane capability gateways, starting with product-related operations.
- Remove production nullable dependencies and fallback `new` behavior that can make tests and production run different dependency graphs.
- Deduplicate cart/checkout/order line resolution and product selection logic.
- Move store scope resolution away from ambient Infrastructure `HttpContext` reads into a scoped execution context resolved at Presentation.
- Clean up Storefront frontend contract ownership and stop endpoint code from depending on the concrete `StorefrontApiClient`.
- Split verified large files by cohesion without changing behavior.
- Isolate active V2 build/test surface from legacy Presentation references.
- Add guardrail tests so the architecture does not regress.

## Non-goals

- [ ] Do not rewrite Control Plane, Commerce Node, Storefront V2, cart, checkout, or order placement from scratch.
- [ ] Do not add a new ecommerce module system.
- [ ] Do not move V2 persistence into legacy `AppDbContext`.
- [ ] Do not reintroduce `api/internal/*`, direct Control Plane Web -> Commerce Node calls, or browser access to Commerce Admin APIs.
- [ ] Do not remove legacy projects from the repository in this phase; isolate active V2 first.
- [ ] Do not introduce generated clients until contract snapshots and current typed-client behavior are protected.

## Verified Current Evidence

- [x] Phase 0 baseline: `BlazorShop.Application/ControlPlane/CommerceGateway/CommerceNodeAdminGatewayDtos.cs` contained `ICommerceNodeAdminGatewayTransport` with `HttpMethod`, HTTP path, and status-bearing result. Resolved in Phase 1B.
- [x] `ControlPlaneCommerceCatalogResult<T>` had 236 generic references at Phase 0. After Store configuration, Security/privacy, Shipping, and Payment migrations in Phase 1C.1-1C.4, current migration baseline is 214 references.
- [x] `ApplicationResult<T>`, `ApplicationError`, and `ApplicationErrorKind.RemoteFailure` already exist under `BlazorShop.Application/Common/Results`.
- [x] `IControlPlaneProductGateway` has 31 current methods spanning product CRUD, product SEO, product import, variation template, category media, variant, and inventory in one interface.
- [x] `BlazorShop.Application/CommerceNode/Carts/StorefrontCartService.cs` still accepts nullable `IProductSelectionResolver` and falls back to `new ProductSelectionResolver(...)`.
- [x] `StorefrontPageService`, `CommerceNodeAdminShipmentService`, `CommerceNodeOrderTrackingService`, `InternalFreeStandardShippingProvider`, `StorefrontCartSessionService`, `VariationTemplateService`, and `StorefrontDisplayContextProvider` still use nullable dependencies or fallback behavior.
- [ ] `OrderPlacementService.ResolveOrderLinesAsync` and `StorefrontCheckoutService.ResolveOrderLinesAsync` contain duplicated order line resolution.
- [x] `Web.SharedV2` contains Authentication, Product, Category, Payment, SEO, Pages, and Discovery model folders despite architecture docs limiting it to browser/auth/storage/toast/helper utilities.
- [x] Storefront endpoints/pages/components and the concrete client file still inject or reference concrete `StorefrontApiClient` in 22 files even though feature-specific client interfaces exist.
- [x] `CommerceStoreContext` in Infrastructure reads route store key from `IHttpContextAccessor`.
- [x] Six Storefront scoped controllers still repeat `ResolveStoreIdAsync` wrappers around `ICommerceStoreContext.GetCurrentStoreIdAsync`.
- [x] Current large files: `CommerceNodeSwaggerExtensions.cs` 1675 lines, `CommerceNodeDevelopmentSeeder.cs` 1787, `ControlPlaneDbContext.cs` 712 with 27 `modelBuilder.Entity`, `CommerceProducts.razor` 1691, `StorefrontLocalEndpointSupport.cs` 813.
- [x] `BlazorShop.Tests.csproj` references both legacy `BlazorShop.Presentation/*` and active `BlazorShop.PresentationV2/*`.
- [x] No `BlazorShop.V2.slnf` exists in the repository at the time of this plan.

## Execution Principles

- [ ] Characterization tests before behavior-preserving refactors.
- [ ] Keep active V2 routes and response shapes stable unless a phase explicitly changes and snapshots the contract.
- [ ] Replace compatibility seams with adapters temporarily; remove adapters only after consumers are moved.
- [ ] Refactor one capability group at a time.
- [ ] Commit per completed phase when implementation starts.
- [ ] Update QA todo files when runtime behavior or release checks are affected.
- [ ] Run focused test sets first, then broader V2 build/test at phase gates.

## Phase Dependency Map

```text
Phase 0: Baseline and guardrails
  -> Phase 1: Application result and transport boundary
      -> Phase 2: Product gateway capability split
  -> Phase 3: Deterministic DI
      -> Phase 4: Cart/checkout/order line dedupe
  -> Phase 5: Store execution context
  -> Phase 6: Storefront client/contract boundary
  -> Phase 7: Cohesion splits for large files
  -> Phase 8: V2 build/test isolation
  -> Phase 9: Final guardrails and QA release gate
```

## Phase 0 - Baseline And Safety Net

Goal: lock current behavior before moving architecture boundaries.

### Tasks

- [x] Create an architecture baseline test class for Control Plane gateway boundaries.
- [x] Add a test proving `ControlPlaneCommerceCatalogResult<T>` still has the current known reference count range before migration.
- [x] Add a test proving `ICommerceNodeAdminGatewayTransport` is currently the only Application gateway contract exposing `HttpMethod`/HTTP path/status.
- [x] Add a test inventory for current nullable production dependencies, initially allowlisting existing offenders.
- [x] Add a test inventory for Storefront endpoint concrete `StorefrontApiClient` injection, initially allowlisting current files.
- [x] Add a test inventory for `Web.SharedV2/Models` business-model folders, initially allowlisting current folders.
- [x] Add a test inventory for Storefront scoped controller `ResolveStoreIdAsync` duplication, initially allowlisting current controllers.
- [x] Add a file-size/cohesion baseline test for known hotspots so later phases can prove line count reduction or split completion.
- [x] Record current focused command outputs in this file or matching QA todo after implementation.

### Suggested Tests

- [x] `BlazorShop.Tests/PresentationV2/ControlPlane/ControlPlaneArchitectureBoundaryTests.cs`
- [ ] `BlazorShop.Tests/PresentationV2/Storefront/StorefrontArchitectureBoundaryTests.cs`
- [x] `BlazorShop.Tests/Architecture/V2ArchitectureBoundaryBaselineTests.cs` was added as the shared architecture baseline test location.

### Verification

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ArchitectureBoundary|FullyQualifiedName~ApplicationResultStandardization"` - Passed: 44, Failed: 0. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.

### Done When

- [x] Tests capture the current smell inventory without failing.
- [x] No production code behavior changed.
- [x] Next phases can turn allowlist entries into forbidden rules one group at a time.

## Phase 1 - Application Result And Transport Boundary

Goal: remove generic HTTP transport shape from Application and make Control Plane commerce capability gateways return `ApplicationResult<T>`.

### Phase 1A - Add Transitional Mapping Layer

- [x] Add a small mapping helper near Control Plane gateway infrastructure to convert `CommerceNodeAdminGatewayResult<T>` into `ApplicationResult<T>`.
- [x] Map `Validation`, `NotFound`, and `RemoteFailure` to `ApplicationError.Validation`, `ApplicationError.NotFound`, and `ApplicationError.RemoteFailure`.
- [x] Preserve upstream status as `ApplicationError.Metadata["upstreamStatusCode"]` when present.
- [x] Preserve operator-safe upstream messages but do not expose raw exception details.
- [x] Add `ApplicationMediaContent` as the Application-safe value for binary responses with `Content`, `ContentType`, optional `FileName`, and optional metadata.
- [x] Add tests for mapper behavior: success, validation, not found, remote failure, upstream status metadata, null/empty payload, binary media.

Phase 1A focused verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeAdminGatewayApplicationResultMapper|FullyQualifiedName~ApplicationResultStandardization|FullyQualifiedName~ArchitectureBoundary"` - Passed: 53, Failed: 0. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors; Tailwind completed with existing Browserslist stale notice.

### Phase 1B - Move Generic Transport Contract Out Of Application

- [x] Create an Infrastructure transport contract under `BlazorShop.Infrastructure/Data/ControlPlane/CommerceGateway`.
- [x] Move `CommerceNodeAdminGatewayResult<T>`, media transport result, and transport failure enum into Infrastructure transport contracts.
- [x] Keep capability interfaces in `BlazorShop.Application/ControlPlane/CommerceGateway/*`.
- [x] Update `CommerceNodeAdminGatewayTransport` and `ControlPlaneCommerceGatewayBase` to use the Infrastructure transport contract.
- [x] Update `DependencyInjection.cs` registration to register the concrete transport without exposing it through Application.
- [x] Remove Application-level `ICommerceNodeAdminGatewayTransport` after all compile errors are resolved.
- [x] Add architecture test: `BlazorShop.Application/ControlPlane/CommerceGateway` must not contain `HttpMethod`, `HttpStatusCode`, or `HttpClient` transport primitives.

Phase 1B focused verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ControlPlaneCommerceGatewayStoreMapping|FullyQualifiedName~CommerceNodeAdminGatewayApplicationResultMapper|FullyQualifiedName~ArchitectureBoundary"` - Passed: 44, Failed: 0. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors; Tailwind completed with existing Browserslist stale notice.

### Phase 1C - Migrate Gateway Return Types By Capability

Migrate in this order to limit blast radius:

- [x] Store configuration gateway.
- [x] Security/privacy gateway.
- [x] Shipping gateway.
- [x] Payment gateway.
- [ ] Currency gateway.
- [ ] Content/navigation gateway.
- [ ] Order gateway.
- [ ] Media gateway.
- [ ] Product gateway after Phase 2 split or through temporary adapters.

For each capability:

- [x] Change Store configuration Application interface return type from `ControlPlaneCommerceCatalogResult<T>` to `ApplicationResult<T>`.
- [x] Update Store configuration Infrastructure gateway implementation.
- [x] Update Control Plane API controller mapping to continue returning the same Control Plane API envelope and HTTP statuses through an `ApplicationResult<T>` overload.
- [ ] Update Control Plane Web client only if API surface changes; expected outcome is no Web API contract change.
- [x] Control Plane Web client unchanged because API surface did not change.
- [x] Update focused gateway/static tests for Store configuration migration.
- [x] Delete migrated `ControlPlaneCommerceCatalogResult<T>` references for Store configuration capability.

Phase 1C.2 Security/privacy:

- [x] Change Security/privacy Application interface return type from `ControlPlaneCommerceCatalogResult<T>` to `ApplicationResult<T>`.
- [x] Update Security/privacy Infrastructure gateway implementation to use `SendApplicationAsync`.
- [x] Control Plane API/Web route and response surface unchanged through the existing `ApplicationResult<T>` controller-base overload.
- [x] Current `ControlPlaneCommerceCatalogResult<T>` migration baseline after this phase: 222 references.

Phase 1C.2 focused verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~SecurityPrivacy|FullyQualifiedName~CommerceNodeAdminGatewayApplicationResultMapper|FullyQualifiedName~ApplicationResultStandardization|FullyQualifiedName~ArchitectureBoundary"` - Passed: 114, Failed: 0. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors; Tailwind completed with existing Browserslist stale notice.

Phase 1C.3 Shipping:

- [x] Change Shipping Application interface return type from `ControlPlaneCommerceCatalogResult<T>` to `ApplicationResult<T>`.
- [x] Update Shipping Infrastructure gateway implementation to use `SendApplicationAsync`.
- [x] Control Plane API/Web route and response surface unchanged through the existing `ApplicationResult<T>` controller-base overload.
- [x] Current `ControlPlaneCommerceCatalogResult<T>` migration baseline after this phase: 218 references.

Phase 1C.3 focused verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~Shipping|FullyQualifiedName~ControlPlaneCommerceGatewayStoreMapping|FullyQualifiedName~CommerceNodeAdminGatewayApplicationResultMapper|FullyQualifiedName~ApplicationResultStandardization|FullyQualifiedName~ArchitectureBoundary"` - Passed: 117, Failed: 0. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors; Tailwind completed with existing Browserslist stale notice.

Phase 1C.4 Payment:

- [x] Change Payment Application interface return type from `ControlPlaneCommerceCatalogResult<T>` to `ApplicationResult<T>`.
- [x] Update Payment Infrastructure gateway implementation to use `SendApplicationAsync`.
- [x] Control Plane API/Web route and response surface unchanged through the existing `ApplicationResult<T>` controller-base overload.
- [x] Current `ControlPlaneCommerceCatalogResult<T>` migration baseline after this phase: 214 references.

Phase 1C.4 focused verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~Payment|FullyQualifiedName~ControlPlaneCommerceGatewayStoreMapping|FullyQualifiedName~CommerceNodeAdminGatewayApplicationResultMapper|FullyQualifiedName~ApplicationResultStandardization|FullyQualifiedName~ArchitectureBoundary"` - Passed: 175, Failed: 0, Skipped: 2 existing skipped CartService payment tests. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors; Tailwind completed with existing Browserslist stale notice.

Phase 1C.1 focused verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ControlPlaneCommerceGatewayStoreMapping|FullyQualifiedName~CommerceNodeAdminGatewayApplicationResultMapper|FullyQualifiedName~ApplicationResultStandardization|FullyQualifiedName~ArchitectureBoundary"` - Passed: 71, Failed: 0. Existing warnings: MessagePack/Microsoft.OpenApi advisories, Browserslist stale.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` - Build succeeded, 0 warnings, 0 errors; Tailwind completed with existing Browserslist stale notice.

### Phase 1D - Retire Catalog-Named Result

- [ ] Remove `ControlPlaneCommerceCatalogResult<T>` once reference count reaches zero.
- [ ] Remove old gateway failure enum if no longer used outside transport.
- [ ] Add guardrail test: no `ControlPlaneCommerceCatalogResult` references outside historical docs/migrations.
- [ ] Add guardrail test: Application gateway interfaces use `ApplicationResult<T>` only.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ControlPlaneCommerce|FullyQualifiedName~ApplicationResult|FullyQualifiedName~ArchitectureBoundary"`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore`

### Done When

- [ ] No Application public gateway contract exposes HTTP transport primitives.
- [ ] Capability gateways return `ApplicationResult<T>`.
- [ ] Control Plane API response envelope and status behavior remain compatible.
- [ ] Old catalog-named result is removed from active code.

## Phase 2 - Product Gateway Capability Split

Goal: split `IControlPlaneProductGateway` into actual capability boundaries without breaking Control Plane products page or API routes.

### Target Interfaces

- [ ] `IControlPlaneProductGateway`: product query/get/create/update/archive and product variant CRUD if variant remains product-owned for now.
- [ ] `IControlPlaneProductSeoGateway`: product SEO and SEO slug lifecycle operations.
- [ ] `IControlPlaneProductImportGateway`: upload/list/get/import rows.
- [ ] `IControlPlaneVariationTemplateGateway`: template/options/values CRUD.
- [ ] `IControlPlaneInventoryGateway`: inventory query, product stock update, variant stock update.
- [ ] Category primary media operations move to `IControlPlaneCategoryGateway` or `IControlPlaneMediaGateway`; choose one owner and document it in the interface comment.

### Tasks

- [ ] Add new interfaces under existing `BlazorShop.Application/ControlPlane/CommerceGateway/*` capability folders.
- [ ] Split `ControlPlaneProductGateway` implementation mechanically into multiple classes, preserving route paths and DTOs.
- [ ] Keep existing `ControlPlaneCommerceProductsController` routes stable initially.
- [ ] Inject the specific capability gateway into controller action groups.
- [ ] If one controller becomes too mixed, split controller files by route group only after tests pass.
- [ ] Update Control Plane API DI registrations.
- [ ] Update Control Plane Web commerce clients only if they currently call a product-only API wrapper that now maps to a different controller.
- [ ] Move category media methods from product gateway and update all call sites.
- [ ] Add tests that each capability interface stays under a documented method-count threshold, default maximum 15 methods unless exception comment exists.

### Controller Migration Strategy

- [ ] First keep `ControlPlaneCommerceProductsController` as a stable route owner with multiple injected gateways.
- [ ] After compile/test, optionally split into `Products`, `ProductSeo`, `ProductImports`, `VariationTemplates`, and `Inventory` controllers while keeping route prefixes and operation IDs stable.
- [ ] Do not change Control Plane Web page behavior in this phase.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ControlPlaneCommerceProduct|FullyQualifiedName~ControlPlaneVariant|FullyQualifiedName~Inventory|FullyQualifiedName~CommerceNodeAdminStoreOpenApiMetadata"`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore`

### Done When

- [ ] Product gateway no longer owns SEO, import, variation templates, category media, or inventory.
- [ ] Control Plane product/import/variation/inventory UI still builds.
- [ ] Route and OpenAPI metadata remain stable or are intentionally snapshot-updated.

## Phase 3 - Deterministic Production DI

Goal: production services must not silently change behavior when a dependency is missing.

### Target Offenders

- [ ] `StorefrontCartService`.
- [ ] `StorefrontPageService`.
- [ ] `CommerceNodeAdminShipmentService`.
- [ ] `InternalFreeStandardShippingProvider`.
- [ ] `StorefrontDisplayContextProvider`.
- [ ] `VariationTemplateService` nullable cache if still present.
- [ ] `PublicCatalogService` optional storefront page dependency if it remains active V2 relevant.

### Tasks

- [ ] Add a production-constructor guardrail test that rejects nullable interface dependencies with default `null` in active V2 production service constructors.
- [ ] Allow explicit exceptions only for DTOs/options/cancellation/factory test helpers, not production service collaborators.
- [ ] Replace `StorefrontCartService` nullable `IProductSelectionResolver` with required dependency.
- [ ] Remove `new ProductSelectionResolver(...)` fallback.
- [ ] Replace nullable `IOptions<StorefrontCartOptions>` with required `IOptions<StorefrontCartOptions>` or `IOptionsMonitor<StorefrontCartOptions>`.
- [ ] Update cart tests to use a service factory/builder instead of depending on nullable defaults.
- [ ] Make `StorefrontPageService` navigation cache, slug policy, slug history, and SEO redirect automation explicit required dependencies, or register Noop implementations if behavior must be optional.
- [ ] Make `CommerceNodeAdminShipmentService` transactional message dependency explicit; if email hook can be disabled, register `NoopCommerceTransactionalMessageService`.
- [ ] Make `InternalFreeStandardShippingProvider` settings dependency explicit; if fallback settings are required for local tests, register `DefaultStoreShippingSettingsService` explicitly in tests only.
- [ ] Make `StorefrontDisplayContextProvider` require `IStorefrontStoreConfigurationClient` and `IHttpContextAccessor`; remove `new HttpContextAccessor()`.
- [ ] Add test factories for services currently instantiated directly in unit tests.
- [ ] Update DI registrations in `BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs` and Storefront V2 service registration.

### Noop Rules

- [ ] Noop implementation names must start with `Noop` and live near the capability contract or Infrastructure implementation.
- [ ] Noop behavior must be explicit and logged/observable where operator-facing behavior is skipped.
- [ ] Noop must never be the hidden fallback of a nullable constructor parameter.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontCartService|FullyQualifiedName~StorefrontPageService|FullyQualifiedName~Shipment|FullyQualifiedName~StorefrontDisplayContextProvider|FullyQualifiedName~ArchitectureBoundary"`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore`

### Done When

- [ ] Active V2 production constructors do not use nullable service dependencies as mode switches.
- [ ] Test setup uses builders/fakes explicitly.
- [ ] Missing DI dependency fails startup/tests instead of silently changing runtime behavior.

## Phase 4 - Cart, Checkout, And Order Line Dedupe

Goal: use one internal path for resolving cart lines into checkout/order line snapshots.

### Tasks

- [ ] Add characterization tests around current `OrderPlacementService.ResolveOrderLinesAsync` behavior through public service methods.
- [ ] Add characterization tests around current `StorefrontCheckoutService.ResolveOrderLinesAsync` behavior through review/place-order flows.
- [ ] Cover quantity less than 1, missing product, missing variant, non-purchasable product, currency mismatch, missing price snapshot, zero/negative price, rounding, and successful variant line.
- [ ] Introduce `internal sealed CheckoutOrderLineResolver` in CommerceNode Infrastructure services.
- [ ] Keep it internal unless more than one test seam needs a public fake.
- [ ] Move shared query and mapping logic from checkout/order placement into resolver.
- [ ] Return a small internal result with `Success`, `ResponseType`, `Message`, and resolved line snapshots.
- [ ] Inject resolver into `StorefrontCheckoutService` and `OrderPlacementService`.
- [ ] Remove duplicate private `ResolveOrderLinesAsync` methods.
- [ ] Reuse the existing `ProductSellabilityResolver` and `IMoneyRoundingService` behavior without changing sellability rules.
- [ ] Make cart add/update use only `IProductSelectionResolver`.
- [ ] Delete `StorefrontCartService.ResolveProductForCartAsync`, `CartProductResolution`, duplicate selected-attribute normalization, and dead helper records after tests prove no callers remain.
- [ ] Keep line display mapping separate from selection validation; display can still parse selected attributes for projection.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontCartService|FullyQualifiedName~StorefrontCheckoutService|FullyQualifiedName~OrderPlacementService|FullyQualifiedName~ProductSelectionResolver"`
- [ ] Focused checkout/payment/order tests that placed COD order before must still pass.
- [ ] Storefront Playwright release checklist does not need rerun in this phase unless browser behavior changes; if browser mutation behavior changes, run cart/checkout subset.

### Done When

- [ ] Checkout and order placement share one order-line resolver.
- [ ] Cart product selection uses one resolver path.
- [ ] No duplicated selected-attribute validation remains in cart outside display-only projection.
- [ ] All existing cart/checkout/order behavior remains compatible.

## Phase 5 - Store Scope Execution Context

Goal: resolve store scope once in Presentation and stop Infrastructure from interpreting HTTP route/query/header/host directly.

### Target Model

- [ ] Add `StoreExecutionContext` or equivalent scoped Application-safe type with `StoreId`, `StoreKey`, optional `Host`, readiness fields if needed, and source metadata.
- [ ] Add `IStoreExecutionContextAccessor` or scoped context holder in Application if services must read ambient store.
- [ ] Presentation middleware/filter resolves store hints from route/query/host and writes the scoped context.
- [ ] Infrastructure services read resolved store id from the context, not `HttpContext`.

### Tasks

- [ ] Add tests for current Storefront route store resolution: route `storeKey`, missing route, wrong store, disabled/maintenance store, unknown host, public media behavior.
- [ ] Add tests for Commerce Admin store-scoped query `storeKey` and no `X-Store-Key`.
- [ ] Add `StoreExecutionContext` in Application CommerceNode store/context namespace.
- [ ] Add Commerce Node API middleware or endpoint filter for `api/storefront/stores/{storeKey}/*`.
- [ ] Add Commerce Admin middleware/filter for `api/commerce/admin/*` query `storeKey`.
- [ ] Preserve public media resolution behavior separately because clean media URLs may use host/rewrite instead of route.
- [ ] Change `CommerceStoreContext` to read from `StoreExecutionContext` first.
- [ ] Keep a temporary compatibility path for public media/host resolution only, with tests and a TODO to remove when media is explicitly handled.
- [ ] Replace repeated controller `ResolveStoreIdAsync` methods with a shared base helper or direct context access.
- [ ] Move service-level `ResolveStoreIdAsync` duplication into one helper where explicit store id has not yet been threaded through commands.
- [ ] Long-term task: update high-value commands/queries to accept `StoreId` explicitly and remove ambient usage from those services.

### Migration Order

- [ ] Storefront scoped controllers: address, cart, checkout, consent, customer addresses, payments.
- [ ] Storefront read-only controllers: catalog, configuration, currency, store, pages, navigation, SEO.
- [ ] Commerce Admin controllers that use `ICommerceStoreContext`.
- [ ] Infrastructure services with private `ResolveStoreIdAsync`.
- [ ] Public media controllers and media services last.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StoreScope|FullyQualifiedName~StorefrontScoped|FullyQualifiedName~CommerceNodeProductStoreScope|FullyQualifiedName~Media"`
- [ ] `GET /swagger/storefront/swagger.json` still contains `api/storefront/stores/{storeKey}/*`.
- [ ] `GET /swagger/commerce-admin/swagger.json` still shows required `storeKey` query for admin store-scoped endpoints.

### Done When

- [ ] Storefront route scope is resolved once per request.
- [ ] Commerce Admin query scope is resolved once per request.
- [ ] Infrastructure no longer parses route/query/header/host for normal store-scoped operations.
- [ ] Public media host/rewrite behavior remains protected by tests.

## Phase 6 - Storefront Frontend Contract Boundary

Goal: stop using `Web.SharedV2` as a business DTO bucket and make Storefront endpoint dependencies capability-specific.

### Phase 6A - Endpoint Dependency Cleanup

- [ ] Change `StorefrontAccountEndpoints` to inject `IStorefrontCustomerClient`, `IStorefrontAddressClient`, or auth-specific interface instead of concrete `StorefrontApiClient`.
- [ ] Change `StorefrontCartEndpoints` to inject `IStorefrontCartClient`.
- [ ] Change `StorefrontCheckoutEndpoints` and `StorefrontLocalEndpointSupport` to inject `IStorefrontCheckoutClient`, `IStorefrontAddressClient`, `IStorefrontPaymentClient`, and related feature interfaces.
- [ ] Change `StorefrontConsentEndpoints` to inject `IStorefrontConsentClient`.
- [ ] Change auth form endpoints to inject the smallest required auth/customer interfaces.
- [ ] Add guardrail test: endpoint mapping files must not declare `StorefrontApiClient apiClient` parameters.
- [ ] Keep `StorefrontApiClient` as the concrete partial implementation behind interfaces for now.

### Phase 6B - Contracts Ownership Inventory

- [ ] Inventory all `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Models/*` business models and current consumers.
- [ ] Classify each model:
  - Move to Storefront V2 contracts.
  - Move to Control Plane Web contracts.
  - Replace with generated client model later.
  - Delete because no active consumer remains.
- [ ] Add guardrail test that allows current folders only during migration and fails when new business folders are added.

### Phase 6C - Interim Storefront Contracts

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts` models or a small `BlazorShop.Storefront.Contracts` project only if sharing with WASM/components requires it.
- [ ] Do not put Control Plane admin DTOs into Storefront contracts.
- [ ] Move Storefront-only catalog/cart/checkout/account/payment/page/SEO contracts out of `Web.SharedV2` incrementally.
- [ ] Update using aliases and client serialization tests.
- [ ] Keep public API DTOs in Commerce Node API contract layer and Application use-case DTOs separate.

### Phase 6D - Generated Client Preparation

- [ ] Confirm Storefront OpenAPI snapshots are current.
- [ ] Confirm operation IDs are stable for all Storefront operations used by Storefront V2.
- [ ] Add a generated-client smoke command/test if current tests only validate schema generation partially.
- [ ] Decide generated client target after contracts are clean: Storefront only first, then Control Plane if useful.
- [ ] Do not introduce generation into the same phase as moving all models; keep it as a later reversible step.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontApiClient|FullyQualifiedName~StorefrontWasmRuntimeFoundation|FullyQualifiedName~ControlPlaneArchitectureBoundary|FullyQualifiedName~CommerceNodeStorefrontOpenApiContract"`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore`

### Done When

- [ ] Storefront endpoints depend on feature interfaces, not concrete `StorefrontApiClient`.
- [ ] `Web.SharedV2` stops accepting new business DTOs.
- [ ] Storefront contract ownership is documented and guarded by tests.

## Phase 7 - Cohesion Splits For Hotspot Files

Goal: split large files mechanically by responsibility without behavior changes.

### Phase 7A - Commerce Node Swagger Extensions

- [ ] Split `CommerceNodeSwaggerExtensions.cs` into one operation metadata filter per feature group.
- [ ] Keep shared helper methods in a small internal helper file.
- [ ] Preserve operation IDs, summaries, security schemes, and response schemas.
- [ ] Add/keep tests that compare OpenAPI path and operation snapshots before and after split.

### Phase 7B - Commerce Node Development Seeder

- [ ] Introduce `ICommerceNodeDevelopmentSeedStep` or equivalent internal seed step abstraction.
- [ ] Keep `CommerceNodeDevelopmentSeeder` as orchestrator only.
- [ ] Split seed steps by store/config, catalog/products, media, content/navigation/SEO, account/order, email/settings.
- [ ] Ensure each step is idempotent and does not overwrite runtime config after Control Plane edits.
- [ ] Add tests or static assertions for no overwrite of logo/favicon/currency/culture/email/maintenance state after initial seed.

### Phase 7C - Control Plane DbContext Configuration

- [ ] Move `ControlPlaneDbContext` entity mapping to `IEntityTypeConfiguration<T>` classes grouped by aggregate.
- [ ] Call `ApplyConfigurationsFromAssembly(...)`.
- [ ] Ensure migrations do not change when only moving configuration.
- [ ] Run model snapshot/model build tests.

### Phase 7D - Control Plane Product Page

- [ ] Split `CommerceProducts.razor` into `.razor` + `.razor.cs` first.
- [ ] Extract components only after code-behind split passes:
  - Product basic editor.
  - Product SEO editor.
  - Product media panel.
  - Product inventory panel.
  - Product variants panel.
  - Product import actions if currently embedded.
- [ ] Do not introduce page-state interfaces unless a second consumer appears.
- [ ] Preserve route, permissions, forms, validation, and service calls.

### Phase 7E - Other Control Plane Pages

- [ ] Prioritize pages over 800 lines: `Stores.razor`, `CommercePages.razor`, `CommerceEmailSettings.razor`.
- [ ] Then pages 600-800 lines: orders, currencies, categories, users, navigation.
- [ ] Use page-specific components before shared components unless two pages share real behavior.

### Phase 7F - Storefront Local Endpoint Support

- [ ] Split `StorefrontLocalEndpointSupport.cs` by account/cart/checkout/common formatting.
- [ ] Keep local endpoint behavior and antiforgery flow unchanged.
- [ ] Update security/privacy tests that read this file.

### Verification

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~OpenApi|FullyQualifiedName~LayoutAssetFoundation|FullyQualifiedName~ControlPlane|FullyQualifiedName~SecurityPrivacy|FullyQualifiedName~CommerceNodeDbContextModel"`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore`
- [ ] Check EF migration diff is empty for configuration-only DbContext split.

### Done When

- [ ] Hotspot files are split by cohesion and no behavior changes are intended.
- [ ] Existing OpenAPI snapshots and UI static tests remain stable.
- [ ] Development seeding remains idempotent and non-destructive to runtime config.

## Phase 8 - V2 Build And Test Isolation

Goal: make active V2 development and CI independent from legacy Presentation while keeping legacy available for reference.

### Tasks

- [ ] Create `BlazorShop.V2.slnf` including active V2 projects, shared core projects, AppHost if needed, ServiceDefaults, and V2 test projects once split exists.
- [ ] Do not include legacy `BlazorShop.Presentation/*` projects in the V2 solution filter.
- [ ] Split tests into at least:
  - `BlazorShop.Tests.V2` for active V2 architecture, Commerce Node, Control Plane, Storefront V2, Application/Infrastructure V2 behavior.
  - Existing `BlazorShop.Tests` can remain mixed until migration is complete.
  - Optional later `BlazorShop.Tests.Legacy` for legacy Presentation tests.
- [ ] Move V2-only static architecture tests first.
- [ ] Move V2 Commerce Node OpenAPI and scoped Storefront tests.
- [ ] Move V2 Control Plane tests.
- [ ] Move V2 Storefront WASM/browser host tests.
- [ ] Keep legacy tests compiling in existing project until a legacy test project is approved.
- [ ] Update docs/architecture local run or contributor docs with the V2 solution filter command.

### Verification

- [ ] `dotnet build BlazorShop.V2.slnf --no-restore`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore`
- [ ] Existing `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore` still passes or is documented as legacy/mixed until split is complete.

### Done When

- [ ] V2 build/test can run without legacy Presentation project references.
- [ ] Legacy remains reference-only and does not block normal V2 architecture work.
- [ ] CI/local docs identify V2 as the active target.

## Phase 9 - Final Guardrails And QA Release Gate

Goal: prevent the same architecture issues from returning.

### Guardrail Tests

- [ ] Application gateway interfaces do not expose `HttpMethod`, `HttpClient`, `HttpStatusCode`, raw path transport, or transport result types.
- [ ] Control Plane commerce gateway capability interfaces use `ApplicationResult<T>`.
- [ ] `ControlPlaneCommerceCatalogResult<T>` is absent from active code.
- [ ] Capability interface method count stays under configured threshold unless documented.
- [ ] Active V2 production constructors do not use nullable collaborator dependencies with default `null`.
- [ ] Active V2 production services do not instantiate fallback collaborators with `new` when DI should supply them.
- [ ] Storefront endpoint files do not inject concrete `StorefrontApiClient`.
- [ ] `Web.SharedV2` does not contain Product/Category/Payment/SEO/Page/Discovery business models after migration.
- [ ] Infrastructure store context does not parse route/query/header/host for normal Storefront/Admin store scope.
- [ ] Storefront browser network release checks continue to reject direct calls to Commerce Admin, Control Plane, legacy, or `api/internal/*`.
- [ ] V2 solution filter excludes legacy Presentation projects.
- [ ] Known hotspot file size thresholds are enforced or documented with an exception.

### QA Checklist Updates

- [ ] Update `QA-ControlPlane.todo.md` with gateway/result split checks.
- [ ] Update `QA-CommerceNode.todo.md` with store execution context and seeder idempotency checks.
- [ ] Update `QA-StorefrontV2.todo.md` with Storefront endpoint/client boundary and browser network checks if behavior changes.
- [ ] Update `Storefront Playwright E2E Release.todo.md` only if browser behavior, routes, or release evidence requirements change.

### Final Verification

- [ ] `dotnet build BlazorShop.V2.slnf --no-restore` when Phase 8 exists.
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore` when Phase 8 exists.
- [ ] Until Phase 8 exists: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore`.
- [ ] `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`.
- [ ] Fetch `http://localhost:5180/swagger/commerce-admin/swagger.json`.
- [ ] Fetch `http://localhost:5180/swagger/storefront/swagger.json`.
- [ ] Run Storefront Playwright release subset if Storefront browser behavior changed.

### Done When

- [ ] All architecture guardrails pass.
- [ ] All affected QA todo files are updated.
- [ ] V2 runtime starts locally.
- [ ] Storefront scoped routes, Control Plane gateway routes, cart, checkout, order, SMTP/message flows remain compatible.

## Risk Register

| Risk | Phase | Impact | Mitigation |
|---|---:|---|---|
| Changing gateway return types breaks Control Plane API mapping | 1 | Control Plane Web operations fail | migrate by capability, keep API envelope stable, add controller mapping tests |
| Moving transport internal breaks DI registrations | 1 | Control Plane API startup failure | update DI in same phase, build Control Plane API after every capability |
| Product gateway split changes route/operation metadata | 2 | generated clients/tests fail | preserve controller route first; split controller only after interface split |
| Removing nullable fallback breaks existing unit tests | 3 | test failures unrelated to runtime | add explicit test builders/fakes; Noop implementations only when intentional |
| Dedupe order line resolver changes checkout totals/order snapshots | 4 | real order placement regression | characterization tests before extraction; COD focused tests after |
| Store execution context breaks public media host resolution | 5 | product/media URLs return wrong store or 404 | handle public media as separate migration path with store isolation tests |
| Moving Web.SharedV2 models causes large using churn | 6 | frontend build failures | move model groups incrementally, keep aliases, build each frontend |
| Seeder split accidentally overwrites runtime store config | 7 | Control Plane store edits lost in dev/QA | idempotency tests and explicit no-overwrite policy |
| V2 test split hides legacy regression unintentionally | 8 | legacy tests stop running unknowingly | keep legacy tests in old project until separate legacy CI decision |

## Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|
| 1 | Migrate gateway result contracts to `ApplicationResult<T>` before deleting old result type | Auto-decided | Boundary correctness | Existing `ApplicationResult<T>` already supports `ApplicationError.Metadata` and `RemoteFailure`; keeping catalog-named result keeps capability drift alive | Keep `ControlPlaneCommerceCatalogResult<T>` and rename only |
| 2 | Split product gateway by capability but preserve routes first | Auto-decided | Do not break consumers | Current Control Plane UI/API consumers depend on stable routes more than interface shape | Split interfaces and controllers/routes in one commit |
| 3 | Remove nullable production dependencies using required DI or explicit Noop services | Auto-decided | Deterministic runtime | Hidden fallback `new` creates production/test divergence and masks DI errors | Keep nullable constructors for easier tests |
| 4 | Extract `CheckoutOrderLineResolver` as internal class, not public interface | Auto-decided | Smallest useful abstraction | There is one implementation and multiple real consumers; interface is not needed until a second policy/fake is valuable | Add public interface immediately |
| 5 | Resolve store scope in Presentation and expose scoped execution context | Auto-decided | Layer boundary | Infrastructure should not parse HTTP route/query/header for normal store scope | Keep `CommerceStoreContext` as HTTP parser forever |
| 6 | Clean endpoint injection before generated client migration | Auto-decided | Reduce blast radius | Feature interfaces already exist; using them first gives value before larger contract migration | Start with generated client immediately |
| 7 | Split hotspots mechanically after boundary/DI phases | Auto-decided | Reviewability | Large-file movement is safer once behavior-sensitive seams are protected | Split all large files before tests/guardrails |
| 8 | Create V2 solution filter/test isolation as an artifact, not an assumption | Auto-decided | Truth from codebase | Current repo has no `BlazorShop.V2.slnf`; plan must create it explicitly | Refer to a non-existent filter as current CI |

## Implementation Order Checklist

- [ ] Phase 0 complete and committed.
- [ ] Phase 1A complete and committed.
- [ ] Phase 1B complete and committed.
- [ ] Phase 1C complete per capability and committed in small batches.
- [ ] Phase 1D complete and committed.
- [ ] Phase 2 complete and committed.
- [ ] Phase 3 complete and committed.
- [ ] Phase 4 complete and committed.
- [ ] Phase 5 complete and committed.
- [ ] Phase 6A complete and committed.
- [ ] Phase 6B/6C complete and committed.
- [ ] Phase 6D decision made after generated-client readiness check.
- [ ] Phase 7 complete by subphase and committed in small batches.
- [ ] Phase 8 complete and committed.
- [ ] Phase 9 complete and committed.

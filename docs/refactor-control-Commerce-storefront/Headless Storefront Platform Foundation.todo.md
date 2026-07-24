# Headless Storefront Platform Foundation Todo

Status: In progress
Source: autoplan review on 2026-07-24 after investigate review of Headless Storefront Platform and Storefront.V2 decoupling
Purpose: make Commerce Node a framework-neutral Storefront API platform, keep Storefront V2 as the first real storefront consumer, and create the client/runtime boundary needed before a future Storefront Starter or AI-generated storefront is built.

## Current Verified Codebase Context

- [x] `BlazorShop.CommerceNode.API` already exposes a Storefront API surface under `api/storefront/stores/{storeKey}/*`.
- [x] Commerce Node Storefront controllers already live under capability-specific files in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront`.
- [x] Commerce Node already has Storefront API contracts under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront`.
- [x] Commerce Node Swagger already has a separate document named `storefront`.
- [x] `CommerceNodeStorefrontOpenApiContractTests` already validates Storefront OpenAPI metadata, schemas, security metadata, snapshots, and TypeScript client generation.
- [x] `BlazorShop.Storefront.Components` does not reference `Application`, `Domain`, `Infrastructure`, Control Plane, or Commerce Node API projects.
- [x] `BlazorShop.Storefront.WASM` only references `BlazorShop.Storefront.Components`.
- [x] `BlazorShop.Storefront.V2` still references `BlazorShop.Application` and `BlazorShop.Web.SharedV2`.
- [x] `BlazorShop.Storefront.V2` source still imports Application DTOs/contracts and `Web.SharedV2.Models` in pages, services, endpoint mappings, SEO, navigation, cart, checkout, and account areas.
- [x] Storefront OpenAPI snapshot currently includes payment provider callback/webhook operations, which are not frontend client operations.
- [x] Storefront public configuration currently exposes feature flags as flat booleans, not a richer `supported/enabled/reason` capability projection.
- [x] Storefront V2 already has same-origin BFF/local endpoints under `/api/cart`, `/api/account`, `/api/checkout`, `/api/consent`, `/api/media`, and SEO/media helpers.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client` now exists as the generated Storefront API client; no `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Starter`, or `BlazorShop.Storefront.Features.*` projects exist yet.

## Goal

Create a foundation where:

- `CommerceNode.API` is the authoritative headless ecommerce backend and Storefront API platform.
- Storefront public API contracts are framework-neutral, generator-safe, and safe for AI/frontend consumers.
- OpenAPI is the canonical machine-readable contract.
- Storefront V2 consumes Commerce Node through HTTP/OpenAPI clients instead of compile-time backend/core DTO dependencies.
- Browser/WASM code continues to call same-origin BFF endpoints and never knows Commerce Node URL, node credentials, or access-token storage internals.
- Storefront V2 remains a real storefront implementation, not a neutral Starter/template.
- A future `BlazorShop.Storefront.Starter` can be built after the foundation without copying backend logic or Storefront V2 code.

## Non-goals

- [ ] Do not build `BlazorShop.Storefront.Starter` in this foundation.
- [ ] Do not build a React/Next/Nuxt storefront in this foundation.
- [ ] Do not build the AI generator in this foundation.
- [ ] Do not split repositories in this foundation.
- [ ] Do not publish public NuGet/npm SDK packages in this foundation.
- [ ] Do not turn `Storefront.V2` into a neutral template.
- [ ] Do not redesign Storefront V2 UI.
- [ ] Do not move checkout business rules, payment rules, pricing, sellability, cart validation, or order placement into frontend code.
- [ ] Do not let WASM call Commerce Node protected APIs directly.
- [ ] Do not add handwritten duplicate API DTOs in frontend when the schema should come from OpenAPI.
- [ ] Do not package every feature module prematurely before a generated client and decoupled V2 prove the boundary.

## Target Architecture

```text
Backend
  BlazorShop.Domain
  BlazorShop.Application
  BlazorShop.Infrastructure
  BlazorShop.CommerceNode.API
      -> Storefront Client API
      -> Commerce Admin API
      -> Provider Callback/Webhook API

Contracts
  Storefront OpenAPI document
      -> generated C# client
      -> generated TypeScript client

Storefront Platform Packages
  BlazorShop.Storefront.Client
      -> generated transport/contracts only
  BlazorShop.Storefront.Runtime
      -> optional later shared runtime primitives

Storefront Implementations
  BlazorShop.Storefront.V2
      -> real storefront consumer
  BlazorShop.Storefront.Starter
      -> future neutral skeleton
  BlazorShop.Storefront.{Name}
      -> future generated storefront
```

Forbidden final dependencies:

```text
Storefront.V2 -> Domain
Storefront.V2 -> Application
Storefront.V2 -> Infrastructure
Storefront.V2 -> CommerceNode.API
Storefront.V2 -> ControlPlane.API

Storefront.Client -> Domain/Application/Infrastructure/API projects
Storefront.Runtime -> Domain/Application/Infrastructure/API projects
Storefront.WASM -> Commerce Node direct protected API
```

## Canonical Frontend Flow

Public SSR:

```text
Storefront V2 SSR page/service
    -> generated C# Storefront client
        -> CommerceNode API /api/storefront/stores/{storeKey}/*
```

Protected browser/WASM:

```text
Storefront.WASM component
    -> same-origin /api/*
        -> Storefront V2 BFF endpoint
            -> generated C# Storefront client
                -> CommerceNode API /api/storefront/stores/{storeKey}/*
```

Rules:

- Browser client does not know Commerce Node base URL.
- Browser client does not hold node credentials.
- Browser client does not hold access tokens in local storage.
- Browser mutations use antiforgery.
- BFF resolves current store/session/cart token and forwards safe commands.
- BFF does not duplicate ecommerce business truth.

## Phase Dependency Map

```text
F0 Role and boundary lock
  -> F1 Current dependency audit
      -> F2 Storefront API contract hardening
          -> F3 Generated C# Storefront client
              -> F4 Browser/BFF boundary hardening
                  -> F5 Storefront V2 capability migration
                      -> F6 Runtime/package boundary only where proven
                          -> F7 Compliance, packaging, isolation gate
                              -> F8 Starter readiness decision
```

## F0 - Role And Boundary Lock

Goal: make the project roles explicit so later work does not accidentally turn Storefront V2 into Starter or leak backend dependencies into frontend packages.

### Tasks

- [x] Add an ADR under `docs/architecture/adr/` or an architecture page if ADR folder does not exist.
- [x] Record roles:
  - [x] `CommerceNode.API` = headless ecommerce backend + Storefront API platform.
  - [x] `Storefront.V2` = first real storefront consumer.
  - [x] `Storefront.Starter` = future neutral skeleton, not part of this foundation.
  - [x] `Storefront.{Name}` = future independent generated storefront.
- [x] Update `docs/architecture/01-system-map.md` with the target flow.
- [x] Update `docs/architecture/05-project-and-folder-guide.md` with future `Storefront.Client` and optional `Storefront.Runtime` ownership.
- [x] Update `docs/architecture/10-v2-contract-ownership.md` with the foundation rule:
  - [x] Storefront public HTTP contracts are canonical at Commerce Node API boundary.
  - [x] Generated clients are frontend-readable contracts.
  - [x] Frontend view models are allowed, but not duplicate API DTO clones.
- [x] Add architecture test placeholders for final dependency rules.
- [x] Add an explicit note that V2 must not be copied as Starter source.

### Files likely touched

- `docs/architecture/01-system-map.md`
- `docs/architecture/05-project-and-folder-guide.md`
- `docs/architecture/10-v2-contract-ownership.md`
- `docs/architecture/adr/*`
- `BlazorShop.Tests.V2/Architecture/*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Architecture"
```

### Done when

- [x] Roles are documented.
- [x] No doc calls Storefront V2 the Starter.
- [x] Future projects have clear ownership and forbidden dependencies.

## F1 - Current Dependency Audit

Goal: produce a complete migration inventory before removing `Application` and `Web.SharedV2` dependencies from Storefront V2.

### Audit categories

Classify every usage into:

```text
A. Public API contract
B. Generated transport
C. Frontend orchestration
D. Frontend presentation
E. Backend-only business logic
F. Shared hosting/observability
```

### Tasks

- [x] Inventory `Storefront.V2.csproj` project references:
  - [x] `BlazorShop.Application`
  - [x] `BlazorShop.Web.SharedV2`
  - [x] `BlazorShop.ServiceDefaults`
  - [x] `BlazorShop.Storefront.Components`
  - [x] `BlazorShop.Storefront.WASM`
- [x] Inventory all `using BlazorShop.Application.*` in Storefront V2.
- [x] Inventory all `using BlazorShop.Web.SharedV2.Models*` in Storefront V2.
- [x] Inventory Storefront V2 services/contracts that alias Application DTOs.
- [x] Inventory BFF/local endpoint request/response types currently mixed into endpoint support files.
- [x] Inventory current handwritten Storefront API clients:
  - [x] `StorefrontApiClient.*`
  - [x] `StorefrontApiTransport`
  - [x] `StorefrontApiRoutes`
  - [x] capability interfaces in `Services/Contracts`.
- [x] Inventory Razor pages/components that directly use backend DTOs.
- [x] Inventory SEO/navigation/sitemap contracts still coming from Application/Web.SharedV2.
- [x] Inventory public Storefront API contracts in Commerce Node that still use Application DTOs internally.
- [x] Create migration table:

| Current type/service | Current owner | Used by | Problem | Replacement | Target owner | Migration phase |
| --- | --- | --- | --- | --- | --- | --- |

- [x] Add the migration table to this plan or a sibling audit file.

### Suggested commands

```powershell
rg -n "using BlazorShop\.Application|BlazorShop\.Application" BlazorShop.PresentationV2\BlazorShop.Storefront.V2
rg -n "BlazorShop\.Web\.SharedV2|Web\.SharedV2\.Models" BlazorShop.PresentationV2\BlazorShop.Storefront.V2
rg -n "Application|Domain|Infrastructure|CommerceNode.API|ControlPlane.API" BlazorShop.PresentationV2\BlazorShop.Storefront.Components BlazorShop.PresentationV2\BlazorShop.Storefront.WASM
```

### Done when

- [x] Every backend/core dependency in Storefront V2 has an owner and replacement plan.
- [x] Migration order is known per capability.
- [x] No code behavior has changed.

## F2 - Storefront API Contract Hardening

Goal: make Storefront OpenAPI safe for generated frontend clients and AI/frontend consumers.

### F2.1 Split frontend client API from provider/webhook API

- [x] Keep frontend/client operations in `/swagger/storefront/swagger.json`.
- [x] Remove provider callback/webhook operations from the frontend Storefront document.
- [x] Add a separate document if needed:
  - [x] `/swagger/storefront-provider/swagger.json`, or
  - [x] keep callbacks outside generated client docs and document them as provider integration APIs.
- [x] Add tests that `StorefrontPayments_HandleProviderCallback` and `StorefrontPayments_HandleWebhook` do not appear in the frontend client OpenAPI.
- [x] Keep runtime callback routes working unless a separate payment/provider plan changes them.

### F2.2 Public contract ownership

- [x] Verify public Storefront schemas do not expose:
  - [x] Domain entities.
  - [x] EF models.
  - [x] admin DTOs.
  - [x] credentials/secrets.
  - [x] internal row IDs where public IDs are expected.
  - [x] server-owned mutation fields.
- [x] Move any Storefront public contract still owned by `Application` into Commerce Node API Storefront contracts or a dedicated generated-contract source.
- [x] Keep Application DTO usage behind mapping code until migration is complete.

### F2.3 Error contract

- [x] Standardize expected error responses around machine-readable fields:

```json
{
  "success": false,
  "code": "cart.version_conflict",
  "message": "Cart has changed.",
  "traceId": "...",
  "fieldErrors": {}
}
```

- [x] Decide whether to extend current `CommerceNodeApiResponse<T>` or use `CommerceNodeApiErrorResponse` consistently for non-2xx responses.
- [x] Add canonical error code registry for Storefront client flow:
  - [x] auth.
  - [x] account.
  - [x] cart.
  - [x] checkout.
  - [x] payment.
  - [x] catalog/content.
  - [x] store unavailable/maintenance.
- [x] Add tests that frontend control flow can use `code` and never parse `message`.

### F2.4 Capability projection

- [x] Replace or augment flat public feature flags with machine-readable capability entries:

```json
{
  "features": {
    "cart": { "supported": true, "enabled": true },
    "checkout": { "supported": true, "enabled": true },
    "reviews": { "supported": true, "enabled": false, "reason": "disabled" },
    "wishlist": { "supported": false, "enabled": false, "reason": "not_installed" }
  }
}
```

- [x] Keep backward-compatible flat flags temporarily if Storefront V2 depends on them.
- [x] Add capability keys for only currently real/planned Storefront features:
  - [x] customer accounts.
  - [x] registration.
  - [x] cart.
  - [x] checkout.
  - [x] payments.
  - [x] newsletter.
  - [x] recommendations.
  - [x] contact form.
  - [n/a] reviews only if backend support is present or explicitly planned.
- [x] Do not expose provider secrets or internal settings in public configuration.

### F2.5 Generator safety and compatibility

- [x] Keep stable operation IDs.
- [x] Keep named string enum values for client-facing filters/sorts.
- [x] Keep non-null collection rules.
- [x] Add breaking-change diff guard for:
  - [x] removed path.
  - [x] removed operation ID.
  - [x] removed schema.
  - [x] removed property.
  - [x] property type change.
  - [x] optional to required change.
  - [x] enum value removal.
  - [x] response status removal.
  - [x] security scheme removal/change.
- [x] Refresh OpenAPI snapshots only after intentional contract changes are reviewed.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/*`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/*`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/*`
- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots/*`
- `docs/architecture/09-api-contract-standards.md`
- `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

### Done when

- [x] Frontend Storefront OpenAPI has no provider callback/webhook operations.
- [x] Storefront public schemas are safe.
- [x] Error contracts expose stable `code`.
- [x] Capability projection is machine-readable.
- [x] TypeScript generation proof still passes.

## F3 - Generated C# Storefront Client Foundation

Goal: create an independent generated C# Storefront client that Storefront V2 SSR/BFF can use instead of handwritten backend DTO/service coupling.

### Project

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Client
```

or, if the repository prefers non-presentation shared packages:

```text
BlazorShop.Storefront.Client
```

Final location decided in F3: `BlazorShop.PresentationV2/BlazorShop.Storefront.Client`.

### Responsibilities

- [x] Generated request/response DTOs from Storefront OpenAPI.
- [x] Generated typed HTTP clients.
- [x] JSON serialization configuration.
- [x] HTTP status/error deserialization.
- [x] cancellation token propagation.
- [x] route construction including `storeKey`.
- [x] correlation/trace propagation hooks.
- [x] optional retry policy hooks as extension points.

### Not allowed

- [x] Razor components.
- [x] CSS/layout/assets.
- [x] browser local storage.
- [x] cart UI state.
- [x] checkout UI state.
- [x] ecommerce business rules.
- [x] handwritten duplicate API DTOs.
- [x] references to `Domain`, `Application`, `Infrastructure`, `CommerceNode.API`, `ControlPlane.API`, or `Storefront.V2`.

### Tasks

- [x] Choose generator tool and pin version in repo.
- [x] Add checked-in generator configuration.
- [x] Generate C# client from `/swagger/storefront/swagger.json`.
- [x] Configure namespace, nullable reference types, and collection nullability.
- [x] Add deterministic generation script.
- [x] Add compile test for generated client.
- [x] Add source guard that generated files are not hand-edited.
- [n/a] Add a small typed facade if generated client shape is too raw:
  - [n/a] configuration.
  - [n/a] catalog.
  - [n/a] cart.
  - [n/a] checkout.
  - [n/a] customer/account.
  - [n/a] orders.
  - [n/a] payments.
- [x] Do not create one large handwritten client that mirrors the current `StorefrontApiClient`.
- [x] Keep TypeScript strict generation proof for future React/Next consumers.

### Verification

```powershell
dotnet build BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~OpenApi|FullyQualifiedName~GeneratedClient"
```

### Done when

- [x] Generated C# client compiles without backend/core project references.
- [x] Generated TypeScript client still compiles in strict mode.
- [x] Storefront V2 can begin capability-by-capability migration.

## F4 - Browser/BFF Boundary Hardening

Goal: preserve safe browser behavior while replacing internal transport with the generated Storefront client.

### Current BFF/local endpoint groups

- [x] `/api/cart`
- [x] `/api/product-selection-preview`
- [x] `/api/account/*`
- [x] `/api/checkout/*`
- [x] `/api/consent/*`
- [x] public media helper routes: `/media/products/*` and `/media/assets/*`
- [x] SEO/sitemap/robots helpers where applicable.

### Tasks

- [x] Document BFF responsibilities:
  - [x] resolve current store.
  - [x] resolve HttpOnly session.
  - [x] attach Commerce access token server-side.
  - [x] attach/resolve cart token.
  - [x] validate antiforgery on mutations.
  - [x] normalize Commerce API errors.
  - [x] return only safe frontend responses.
- [x] Document BFF non-responsibilities:
  - [x] no price calculation.
  - [x] no sellability calculation.
  - [x] no cart validity decision.
  - [x] no checkout business rule.
  - [x] no order creation outside Commerce checkout/place-order use case.
- [x] Move local endpoint DTOs out of large endpoint support files into capability-specific local contract files.
- [x] Keep local endpoint response shapes stable for current WASM components.
- [x] Add central local error mapping:
  - [x] 401 sign-in required.
  - [x] 403 forbidden.
  - [x] 409 conflict/cart drift.
  - [x] 422 validation where applicable.
  - [x] 500 safe generic failure.
- [x] Add tests proving WASM/browser client code only calls same-origin `/api/*`.
- [x] Add tests proving local endpoints do not inject concrete backend HTTP clients directly when a capability abstraction exists.
- [n/a] After F3, migrate BFF transport from handwritten `StorefrontApiClient` to generated client capability by capability. This is accounted for by F5.1-F5.6 capability migration commits after the BFF boundary is locked.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/*`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`
- `docs/architecture/03-runtime-boundaries.md`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontEndpoint|FullyQualifiedName~Bff|FullyQualifiedName~Cart|FullyQualifiedName~Checkout|FullyQualifiedName~Account"
```

### Done when

- [x] WASM does not know Commerce Node URL.
- [x] Protected browser flows go through BFF.
- [x] BFF endpoint contracts are local/frontend-safe.
- [x] BFF contains no duplicated ecommerce business truth.

## F5 - Storefront V2 Capability-by-capability Decoupling

Goal: remove `Application` and backend-owned business DTO dependencies from Storefront V2 without changing Storefront V2 design or behavior.

### Migration rules

- [x] Keep route URLs stable.
- [x] Keep SSR/SEO behavior stable.
- [x] Keep Storefront V2 visual design and composition.
- [x] Keep same-origin BFF for browser protected flows.
- [x] Replace backend DTO/service usage with generated client DTOs or Storefront-owned presentation view models.
- [x] Do not clone generated DTOs as handwritten API DTOs.
- [x] Create view models only when they are presentation/composition models.
- [x] Move backend/core dependency removal in small capability commits.

### F5.1 Store bootstrap, configuration, maintenance, locale, currency

- [x] Replace Application store/config DTO usage.
- [x] Replace flat feature flag consumption with capability projection where available.
- [x] Keep current maintenance redirect/page behavior.
- [x] Keep current currency/culture display behavior.
- [x] Update public config/client tests.

### F5.2 Catalog, product, search, content, navigation, SEO

- [x] Replace catalog DTOs from `Application` and `Web.SharedV2.Models.Product/Category`.
- [x] Replace page/content DTOs from `Application` and `Web.SharedV2.Models.Pages`.
- [x] Replace SEO DTOs from `Application` and `Web.SharedV2.Models.Seo`.
- [x] Keep product detail projection authoritative from backend:
  - [x] gallery.
  - [x] price.
  - [x] sellability.
  - [x] variants.
  - [x] breadcrumb.
  - [x] SEO.
- [x] Keep category/search server route query behavior.
- [x] Keep sitemap/robots behavior.

2026-07-24 evidence: Storefront V2 now routes catalog/content/navigation/SEO through `GeneratedStorefrontCatalogContentClient`, projects generated Storefront client DTOs into Storefront-owned presentation models under `BlazorShop.Storefront.Models`, and source guard `StorefrontV2_CatalogContentNavigationAndSeoUseStorefrontOwnedModels` blocks reintroducing Application/Web.SharedV2 catalog/content/SEO imports.

### F5.3 Auth, customer, account

- [x] Replace Application auth DTO usage in Storefront V2.
- [x] Keep HttpOnly refresh cookie behavior.
- [x] Keep account BFF same-origin endpoints.
- [x] Keep account pages noindex.
- [x] Keep forgot/reset/register disabled policy behavior.
- [x] Keep customer order authorization through backend.

2026-07-24 evidence: Storefront V2 auth request models moved to `BlazorShop.Storefront.Models`; source guard `StorefrontV2_AuthDoesNotUseApplicationUserIdentityDtos` blocks `Application.DTOs.UserIdentity` from returning. `StorefrontAuthClient` remains a manual Storefront transport for auth because F5.3 must preserve `Set-Cookie` capture/copy for HttpOnly refresh-cookie behavior.

### F5.4 Cart

- [x] Replace cart/session DTO usage.
- [x] Keep guest cart/customer cart/merge behavior.
- [x] Keep cart token as server/BFF concern.
- [x] Keep add/update/remove/recalculate commands server-authoritative.
- [x] Keep product selection preview server-authoritative.

2026-07-24 evidence: Storefront cart/session and product-selection preview request models now use Storefront-owned `StorefrontSelectedAttribute`; legacy cart cookie import reads Storefront-owned `StorefrontLegacyCartItem` instead of `Web.SharedV2.Models.Payment.ProcessCart`. `StorefrontCartTokenService` still owns the HttpOnly cart-token cookie, legacy-cart import, customer cart merge, and cart mutation forwarding. `CartCorePhase0InventoryTests.StorefrontV2_CartContractsDoNotUseBackendOrLegacyCartDtos` guards against `SelectedAttributeDto`, `ProcessCart`, and VariationTemplate imports returning to the cart/session path.

### F5.5 Checkout, orders, payment result

- [x] Replace checkout DTO usage.
- [x] Keep checkout state, validation, idempotency, and place-order backend-owned.
- [x] Keep COD/payment redirect/result behavior.
- [x] Keep order history/detail projection backend-owned.
- [x] Do not expose provider webhook/callback in frontend SDK.

2026-07-24 evidence: Storefront checkout/payment/order contracts now use Storefront-owned payment method and selected-attribute shapes instead of `Application.DTOs.Payment.GetPaymentMethod` or VariationTemplate `SelectedAttributeDto`. Customer order paging resolves through Storefront-owned `PagedResult`. `CheckoutOrderPaymentContracts_DoNotUseBackendDtosOrExposeProviderCallbacks` guards against backend DTO imports and provider callback/webhook contracts in the Storefront V2 checkout/order/payment path. Host smoke covers provider redirect, completed COD/order redirect cookie cleanup, stale cart-version blocking, and payment success/cancel result rendering.

### F5.6 Consent, newsletter, contact, recommendations

- [x] Replace remaining contract usage.
- [x] Keep consent visitor cookie behavior server-owned.
- [x] Keep captcha/consent hooks safe.
- [x] Keep recommendations as optional capability projection.

2026-07-24 evidence: Consent contracts/client/endpoints and configuration capability contracts no longer import backend/Application or Web.SharedV2 business-model DTO namespaces. The BFF still resolves and writes `bs-consent-visitor` server-side before forwarding `X-Consent-Visitor`. Generated configuration mapping keeps captcha configuration and feature flags for newsletter/recommendations as optional capability projection. `ConsentAndCapabilityContracts_DoNotUseBackendDtos` guards the F5.6 source set.

### Final F5 cleanup

- [x] Remove `BlazorShop.Application` ProjectReference from `Storefront.V2`.
- [x] Remove business-model dependency on `BlazorShop.Web.SharedV2`.
- [x] Keep `Web.SharedV2` only if still needed for genuinely shared browser utilities and allowed by architecture docs.
- [x] Update Dockerfile to stop copying backend/core projects solely for Storefront V2 build if no longer needed.
- [x] Add source tests blocking new Application/Web.SharedV2 business DTO usages in Storefront V2.

2026-07-24 evidence: Storefront V2 removed its `BlazorShop.Application` project reference, localized `ClientAppOptions` and `SeoRuntimeLogger`, removed `Application`/`Web.SharedV2.Models` source imports, and stopped copying Application/Domain projects in the Storefront Dockerfile. `Web.SharedV2` remains only for shared browser utility constants such as storefront cookie names and Tailwind content scanning. `HeadlessStorefrontFoundationBoundaryTests.StorefrontV2_DoesNotReferenceBackendProjectsOrSharedBusinessModels` guards the final F5 dependency boundary.

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront|FullyQualifiedName~Contract|FullyQualifiedName~Boundary"
```

### Done when

- [x] `Storefront.V2.csproj` has no `Application`, `Domain`, `Infrastructure`, `CommerceNode.API`, or `ControlPlane.API` references.
- [x] Storefront V2 source has no backend/core business namespace usage.
- [x] Storefront V2 behavior remains unchanged in focused and browser tests.

## F6 - Runtime And Feature Module Boundary

Goal: create only the shared runtime/module foundation that is proven necessary after generated client and V2 decoupling.

### Design decision

Do not package every feature prematurely. Start with small shared runtime primitives if V2 decoupling shows repeated code that Starter will need.

### Candidate project

```text
BlazorShop.Storefront.Runtime
```

### Allowed responsibilities

- [ ] Store context abstraction.
- [ ] Storefront API client registration helpers.
- [ ] public configuration/capability reader.
- [ ] normalized error pipeline.
- [ ] auth/session bridge contracts.
- [ ] BFF/browser-safe result mapping primitives.
- [ ] neutral feature activation helpers.

### Not allowed

- [ ] Storefront V2 layout/design.
- [ ] V2 CSS/assets.
- [ ] store-specific composition.
- [ ] backend business rules.
- [ ] provider secrets.
- [ ] Domain/Application/Infrastructure/API project references.

### Feature module boundary

Only create `Storefront.Features.*` projects after a real repeated need exists. Until then, keep portable presentation components in `Storefront.Components/Features/*`.

Feature availability rule:

```text
installed in build
+ backend capability supported
+ store feature enabled
+ presentation placed
= visible/usable feature
```

### Tasks

- [x] Identify repeated runtime code after F5 migration.
- [n/a] Create `Storefront.Runtime` only if it removes real duplication or is required for Starter readiness.
- [x] Add architecture tests for Runtime dependencies.
- [x] Document module ownership map.
- [x] Keep Storefront V2 design/composition in Storefront V2.

2026-07-24 evidence: F6 review found no justified `Storefront.Runtime` extraction yet. Store context resolution, HttpOnly session/cart-token handling, antiforgery, BFF error normalization, and local endpoint response mapping are still Storefront V2 host responsibilities with only one real storefront consumer. The module ownership map in `docs/architecture/05-project-and-folder-guide.md` keeps generated API transport in `Storefront.Client`, portable UI in `Storefront.Components/Features/*`, and Storefront V2 route/BFF/SEO/design/deployment ownership in Storefront V2. `HeadlessStorefrontFoundationBoundaryTests.FutureStorefrontPlatformProjects_DoNotReferenceBackendOrStorefrontV2` now guards optional Runtime project placements if they are introduced later.

### Verification

```powershell
# Runtime build is n/a because F6 intentionally did not create BlazorShop.Storefront.Runtime.
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Runtime|FullyQualifiedName~Architecture"
```

### Done when

- [x] Runtime exists only if justified.
- [x] Runtime has no backend/core dependencies.
- [x] Starter can later reuse runtime without copying V2 design.

## F7 - Compliance, Packaging, And Isolation Gate

Goal: prove Storefront V2 can be built/run as an independent frontend consumer of the Storefront API.

### Architecture tests

- [ ] `Storefront.V2` does not reference backend/core/API projects.
- [ ] `Storefront.Client` does not reference backend/core/API projects.
- [ ] `Storefront.Runtime`, if present, does not reference backend/core/API projects.
- [ ] `Storefront.Components` remains backend-independent.
- [ ] `Storefront.WASM` remains backend-independent.
- [ ] `Web.SharedV2/Models` business model freeze remains enforced.

### Generated client tests

- [ ] Storefront OpenAPI parses.
- [ ] OpenAPI reader validation passes.
- [ ] C# client generation runs.
- [ ] generated C# client compiles.
- [ ] TypeScript client generation runs.
- [ ] generated TypeScript client compiles with `strict` and `strictNullChecks`.
- [ ] frontend SDK excludes webhook/provider callback operations.
- [ ] error contracts compile and expose `code`.

### Local package consumer proof

- [ ] Pack `Storefront.Client`.
- [ ] Pack `Storefront.Runtime` if present.
- [ ] Create temporary consumer storefront project in `obj` or test output.
- [ ] Restore from local NuGet feed.
- [ ] Build consumer without backend ProjectReference.
- [ ] Fail if consumer can compile only because backend source is available.

### Isolation run

- [ ] Build/publish Commerce Node API.
- [ ] Run Commerce Node API with configured store fixture.
- [ ] Build/publish Storefront V2 separately.
- [ ] Configure Storefront V2 with Commerce API URL and store key.
- [ ] Run Storefront V2 without backend source references.
- [ ] Execute API integration smoke.
- [ ] Execute Playwright browser smoke.

### Functional QA

- [ ] store bootstrap.
- [ ] maintenance state.
- [ ] home/catalog.
- [ ] category.
- [ ] product.
- [ ] search.
- [ ] content page.
- [ ] login/register/logout.
- [ ] forgot/reset password.
- [ ] profile/address.
- [ ] guest cart.
- [ ] customer cart.
- [ ] cart merge.
- [ ] checkout preview.
- [ ] shipping/payment selection.
- [ ] place order with COD in test store.
- [ ] order history.
- [ ] order detail.
- [ ] payment result.
- [ ] SEO metadata.
- [ ] sitemap.
- [ ] robots.
- [ ] media isolation.
- [ ] 401/403/409/422 error flows.

### Deliverables

- [ ] architecture test suite.
- [ ] generated client test suite.
- [ ] local package feed proof.
- [ ] isolated build/run script.
- [ ] Playwright evidence.
- [ ] release checklist update.
- [ ] Foundation completion report.

### Done when

- [ ] Storefront V2 builds and runs as a frontend API consumer.
- [ ] No backend source project reference is required to build Storefront V2.
- [ ] Playwright core journey passes.

## F8 - Starter Readiness Decision

Goal: decide whether the foundation is ready for a separate `BlazorShop.Storefront.Starter` phase.

### Starter readiness checklist

- [ ] Foundation Definition of Done is complete.
- [ ] Storefront OpenAPI is clean and frontend-safe.
- [ ] Storefront Client can be consumed by PackageReference.
- [ ] Storefront V2 has no backend/core project references.
- [ ] SSR/BFF/browser boundaries are documented and tested.
- [ ] Capability projection supports feature enablement.
- [ ] Generated C# and TypeScript clients compile.
- [ ] Compatibility gate catches breaking changes.
- [ ] Isolation Playwright smoke passes.
- [ ] Storefront V2 remains a real storefront, not a neutral template.

### Starter phase scope after approval

Future `BlazorShop.Storefront.Starter` should include:

- [ ] neutral project skeleton.
- [ ] route map.
- [ ] SSR/Hybrid/WASM conventions.
- [ ] generated client usage.
- [ ] store bootstrap.
- [ ] layout/header/footer/menu.
- [ ] feature module placement examples.
- [ ] loading/skeleton/error/empty states.
- [ ] SEO/metadata examples.
- [ ] BFF examples for protected flows.
- [ ] AI storefront generation guide.
- [ ] generator manifest.
- [ ] Starter QA checklist.

### Done when

- [ ] A separate Starter plan can begin without re-opening backend/client boundary decisions.

## Foundation Definition Of Done

### Commerce Node API

- [ ] Storefront API is framework-neutral.
- [x] Storefront client OpenAPI excludes provider webhook/callback operations.
- [x] Public contracts do not expose unsafe/internal schemas.
- [x] operation IDs are stable.
- [x] non-null collections and validation metadata are generator-safe.
- [x] machine-readable error codes exist.
- [x] capability projection exists.
- [x] breaking-change gate exists.

### Storefront Client

- [x] Generated C# client compiles.
- [x] Generated TypeScript client compiles strict.
- [x] Client has no backend/core project references.
- [x] Generated files are deterministic and not hand-edited.
- [ ] Local package consumer proof passes.

### Storefront V2

- [ ] Still has its own design/composition/deployment.
- [x] Does not reference `Domain`, `Application`, `Infrastructure`, Commerce Node API, or Control Plane API.
- [x] Uses HTTP/OpenAPI client for Commerce Storefront API.
- [x] Protected browser flows go through same-origin BFF.
- [ ] Does not duplicate ecommerce business rules.
- [ ] Build/publish/run works independently.

### QA

- [ ] architecture tests pass.
- [ ] OpenAPI tests pass.
- [ ] generated client tests pass.
- [ ] package consumer proof passes.
- [ ] isolated runtime smoke passes.
- [ ] Playwright core journey passes.
- [ ] QA docs are updated with evidence.

## Implementation Order And Commit Plan

- [x] Commit 1: F0 architecture role/boundary lock and tests.
- [x] Commit 2: F1 dependency audit document and migration table.
- [x] Commit 3: F2 OpenAPI surface split and provider/webhook exclusion from frontend SDK.
- [x] Commit 4: F2 error contract/capability projection hardening.
- [x] Commit 5: F3 generated C# client project and generator tests.
- [x] Commit 6: F4 BFF boundary cleanup and local endpoint contract split.
- [x] Commit 7: F5.1 configuration/store bootstrap migration.
- [x] Commit 8: F5.2 catalog/content/navigation/SEO migration.
- [x] Commit 9: F5.3 auth/customer/account migration.
- [x] Commit 10: F5.4 cart migration.
- [x] Commit 11: F5.5 checkout/orders/payments migration.
- [x] Commit 12: F5.6 consent/newsletter/contact/recommendations migration.
- [x] Commit 13: F5 final dependency removal and Dockerfile cleanup.
- [x] Commit 14: F6 runtime boundary only if justified.
- [ ] Commit 15: F7 package/isolation/Playwright gate and completion report.
- [ ] Commit 16: F8 Starter readiness decision docs.

Each commit must be buildable. Do not combine mechanical dependency removal with behavior changes unless the phase explicitly requires it.

## Risk Controls

- [ ] Keep Storefront V2 routes stable.
- [ ] Keep Commerce Node Storefront route shape stable.
- [ ] Keep Storefront V2 UI/design unchanged unless a separate UI phase approves changes.
- [ ] Keep checkout/order/payment server-authoritative.
- [ ] Keep account/order authorization server-authoritative.
- [ ] Keep browser protected flow behind BFF.
- [ ] Keep OpenAPI snapshots and compatibility checks reviewed.
- [ ] Keep Provider/webhook routes out of frontend SDK.
- [ ] Keep generated code deterministic.
- [ ] Keep no secrets/internal settings in public configuration or generated clients.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Treat Commerce Node as Storefront API platform | Auto-decided | Backend owns business truth | Current Commerce Node already owns Storefront scoped APIs and ecommerce rules. | Moving ecommerce logic into frontend skeletons. |
| 2 | Scope | Keep Storefront V2 as real storefront, not Starter | User direction preserved | Preserve product identity | V2 has real design, deployment, QA, and consumer role. | Making V2 a neutral template. |
| 3 | Contracts | OpenAPI is canonical frontend-readable contract | Auto-decided | Avoid guessing and duplicate DTOs | React/Next/AI generator should consume generated types, not inspect C# service code. | Sharing C# services/DTO dumps as frontend contract. |
| 4 | OpenAPI | Remove provider callback/webhook from frontend client document | Auto-decided | Least privilege client surface | Current Storefront snapshot includes provider operations that frontend SDK should not expose. | Generating frontend clients from all Storefront-scoped routes blindly. |
| 5 | Client | Generate C# client before decoupling V2 | Auto-decided | Migration safety | V2 needs a replacement transport before removing Application/Web.SharedV2 coupling. | Cutting project references first. |
| 6 | Browser | Keep WASM behind same-origin BFF | Auto-decided | Security boundary | Current BFF model protects tokens, store resolution, and antiforgery. | Direct browser calls to Commerce Node protected APIs. |
| 7 | Runtime | Defer broad `Storefront.Features.*` packaging until proven | Auto-decided | Avoid premature platform complexity | Components are already backend-independent; package modules should follow generated client and V2 decoupling. | Creating many packages before a second consumer exists. |
| 8 | Starter | Build Starter only after Foundation gate | Auto-decided | Sequence by dependency | Starter needs clean OpenAPI, generated client, capability projection, and decoupled V2 proof. | Building Starter in parallel with contract cleanup. |

## Final Recommendation

Approve this foundation as a staged platform cleanup before any Starter or AI generator work.

The safest MVP foundation is:

1. lock roles and dependency rules;
2. audit current Storefront V2 coupling;
3. harden Storefront OpenAPI and remove provider/webhook operations from frontend SDK;
4. create generated C# and strict TypeScript client proof;
5. preserve and harden same-origin BFF;
6. migrate Storefront V2 off backend/core references by capability;
7. prove independent package/build/runtime isolation;
8. only then begin `BlazorShop.Storefront.Starter`.

This keeps the current storefront stable while turning the backend into a reusable ecommerce API platform for future Blazor, React, Next.js, or AI-generated storefronts.

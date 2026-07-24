# Storefront Starter Foundation Todo

Status: In progress
Source: autoplan review on 2026-07-24 after Headless Storefront Foundation completion
Purpose: build a neutral, package-based Storefront Starter that proves a new storefront can be created from the Storefront API platform without copying Storefront V2 internals or rewriting ecommerce business logic.

## Current Verified Codebase Context

- [x] Headless Storefront Platform Foundation is complete and recorded in `Headless Storefront Platform Foundation.completion.md`.
- [x] `CommerceNode.API` is the authoritative Storefront API platform.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client` exists as the generated Storefront API client.
- [x] `Storefront.Client` has package metadata and package consumer proof from Foundation F7.
- [x] Storefront OpenAPI is generator-safe and provider callback/webhook routes are excluded from the frontend Storefront client document.
- [x] `Storefront.V2` no longer references `Application`, `Domain`, `Infrastructure`, Commerce Node API, or Control Plane API projects.
- [x] `Storefront.V2` still uses `Web.SharedV2` for shared runtime/browser utilities.
- [x] `Storefront.V2` still has a hybrid generated/manual client state:
  - [x] generated client adapters are used for store/configuration/currency/catalog/content/navigation/SEO.
  - [x] manual `StorefrontApiClient` remains for address/cart/checkout/consent/customer/payment flows.
- [x] `Storefront.Runtime`, `Storefront.Features.*`, `Storefront.Starter`, and `Storefront.Sample` do not exist yet.
- [x] Architecture docs explicitly say Storefront V2 is a real storefront and must not be copied into Starter.

## Goal

Create a neutral Starter that can be used by humans, deterministic scaffolding, and later AI generator workflows to create a real ecommerce storefront that:

- consumes `BlazorShop.Storefront.Client` as a package;
- uses Commerce Node Storefront OpenAPI/generated client as the frontend-readable contract;
- demonstrates SSR, Hybrid, and WASM-hosted patterns;
- keeps browser-protected flows behind same-origin BFF endpoints;
- uses backend capability projection to decide feature availability;
- provides neutral layout and loading/error/empty states;
- can generate a `Storefront.Sample` project that builds and passes browser QA;
- does not copy Storefront V2 source, design, CSS, or manual transport internals.

## Non-goals

- [ ] Do not build the AI Generator in this phase.
- [ ] Do not build React/Next/Nuxt storefronts in this phase.
- [ ] Do not turn Storefront V2 into a Starter.
- [ ] Do not copy Storefront V2 page/component source as the Starter baseline.
- [ ] Do not require Storefront V2 to finish all manual-client migration before Starter work begins.
- [ ] Do not move pricing, sellability, variant validation, cart validation, checkout state, order placement, payment rules, or authorization into Starter.
- [ ] Do not let browser/WASM call Commerce Node protected APIs directly.
- [ ] Do not introduce a full marketplace/module system before the Starter and Sample prove the need.
- [ ] Do not publish public NuGet/npm packages beyond local/private feed proof unless a separate release plan approves it.

## Roadmap

```text
S0  Starter architecture lock
S1  Package-based generated client consumption
S2  Minimal Storefront.Runtime extraction
S3  Starter SSR/BFF contract foundation
S4  SSR / Hybrid / WASM skeleton
S5  Generated client adoption policy and exception registry
S6  Feature module and capability activation
S7  Neutral layout, loading, error, empty states
S8  Starter independent build/package/repository proof
S9  Deterministic Storefront.Sample generation
S10 Sample QA release gate
S11 AI Generator planning
```

## Cross-cutting Rules

- [ ] Starter uses `PackageReference` to `BlazorShop.Storefront.Client`; it must not use `ProjectReference` to the generated client in the independent proof.
- [ ] Starter must not reference `Storefront.V2`.
- [ ] Starter must not reference `Domain`, `Application`, `Infrastructure`, Commerce Node API, Control Plane API, or Control Plane Web.
- [ ] Starter must not import `Web.SharedV2.Models` business contracts.
- [ ] Starter may use or trigger extraction of neutral `Storefront.Runtime` primitives only when they are needed by both V2 and Starter.
- [ ] Starter must use generated client contracts by default.
- [ ] Manual HTTP transport exceptions require documented reason, owner, test, and revisit plan.
- [ ] Browser code must call same-origin `/api/*` only.
- [ ] SSR/BFF code may call Commerce Node through generated Storefront client and configured base URL/store key.
- [ ] Error UI must branch by error `code`/status, not by localized `message`.
- [ ] Feature visibility requires installed package + backend supported + store enabled + presentation placed.

## S0 - Starter Architecture Lock

Goal: lock project roles, ownership rules, and protected areas before any Starter source is created.

### Decisions to record

- [x] `Storefront.Starter` is the neutral skeleton source for deterministic generated storefronts.
- [x] `Storefront.Sample` is the first deterministic generated project used to prove the Starter.
- [x] `Storefront.V2` remains the real storefront implementation and behavior reference, not a copy source.
- [x] `Storefront.Client` remains generated OpenAPI transport/contracts.
- [x] `Storefront.Runtime` may be introduced only for neutral duplicated runtime primitives.
- [x] `Storefront.Components/Features/*` remains presentation-only reusable Blazor components.

### Ownership table

| Owner | Owns | Does not own |
| --- | --- | --- |
| Backend | pricing, sellability, cart validation, checkout, orders, authorization, provider rules, public config/capability projection | frontend layout/composition |
| Storefront.Client | generated contracts and HTTP transport | UI state, BFF logic, business rules |
| Storefront.Runtime | neutral store/client/error/auth/BFF primitives when proven | V2 design, CSS, route composition, backend rules |
| Storefront.Starter | neutral skeleton, examples, conventions, generation manifest | V2-specific design/source, backend source |
| Storefront.Sample | generated project proof | platform contract ownership |
| Storefront.V2 | real storefront design, BFF, deployment, behavior reference | Starter source template |

### Tasks

- [x] Add ADR for `Storefront.Starter` architecture.
- [x] Update `docs/architecture/01-system-map.md`.
- [x] Update `docs/architecture/05-project-and-folder-guide.md`.
- [x] Update `docs/architecture/10-v2-contract-ownership.md` with Starter rules.
- [x] Define protected directories for future AI/scaffolding:
  - [x] generated client source.
  - [x] runtime security primitives.
  - [x] BFF transport/security code.
  - [x] package/version manifest.
  - [x] generated storefront manifest.
- [x] Add architecture tests:
  - [x] Starter must not reference V2.
  - [x] Starter must not reference backend/core/API projects.
  - [x] Starter must not use `Web.SharedV2.Models` business DTOs.
  - [x] Starter docs must say V2 is behavior reference only.
- [x] Lock policy now: Starter must not copy manual `StorefrontApiClient`.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Starter|FullyQualifiedName~Architecture"
```

### Done when

- [x] Starter role and ownership are documented.
- [x] Starter dependency guardrails exist.
- [x] Manual client copy is explicitly forbidden before implementation begins.

## S1 - Package-based Generated Client Consumption

Goal: make Starter consume `BlazorShop.Storefront.Client` like an external storefront would.

### Tasks

- [x] Decide Starter project location for monorepo development:
  - [x] recommended: `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter`.
  - [x] independent proof still must build outside the monorepo.
- [x] Add local/private NuGet feed configuration for Starter development.
- [x] Add package version pin for `BlazorShop.Storefront.Client`.
- [x] Add `PackageReference` to `BlazorShop.Storefront.Client`.
- [x] Do not use `ProjectReference` to `Storefront.Client` in Starter.
- [x] Add package compatibility matrix:

| Storefront API | Client package | Compatibility |
| --- | --- | --- |
| v1 | 1.x | compatible |
| v2 | 2.x | breaking API changes |

- [x] Add package changelog placeholder for `Storefront.Client`.
- [x] Add restore/build test proving Starter consumes local package.
- [x] Add guard that Starter does not copy `Generated/StorefrontClient.g.cs`.
- [x] Add guard that Starter does not define handwritten duplicate API DTOs for generated schemas.

### Verification

```powershell
dotnet pack BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj --configuration Release
dotnet restore BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore
```

### Done when

- [x] Starter restores generated client from package.
- [x] Starter builds without backend source or generated source copy.
- [x] Package compatibility policy is documented.

## S2 - Minimal Storefront.Runtime Extraction

Goal: extract only neutral runtime primitives proven by V2 + Starter reuse.

### Candidate project

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime
```

Final location should match architecture docs and package strategy.

### Allowed first extraction candidates

- [x] Storefront API generated client registration helpers.
- [x] store context abstraction:
  - [x] configured store key.
  - [x] configured Commerce Node base URL.
  - [x] public URL base.
- [x] capability reader:
  - [x] `IsSupported(key)`.
  - [x] `IsEnabled(key)`.
  - [x] reason handling.
- [x] Commerce API error normalization:
  - [x] status.
  - [x] code.
  - [x] message.
  - [x] traceId.
  - [x] field errors.
- [x] BFF result primitives:
  - [x] success envelope.
  - [x] safe frontend error.
  - [n/a] retryable marker if needed.
- [n/a] auth/session contracts only:
  - [n/a] authenticated customer snapshot.
  - [n/a] unauthenticated result.
  - [n/a] sign-in return URL contract.
- [n/a] antiforgery conventions:
  - [n/a] header name.
  - [n/a] token projection contract.

### Not allowed in Runtime

- [x] Storefront V2 layout/design/CSS/assets.
- [x] V2 route composition.
- [x] V2 SEO composition specifics.
- [x] V2 media proxy if only V2 needs it.
- [x] cart/checkout/order business rules.
- [x] provider secrets.
- [x] backend/core/API project references.
- [x] `Web.SharedV2.Models` business DTOs.

### Extraction rules

A class/function can move to Runtime only when:

- [x] V2 and Starter both need it.
- [x] it is not tied to V2 route/design.
- [x] it contains no backend business truth.
- [x] it can be tested independently.
- [x] it does not need editing when a generated storefront changes layout.

### Tasks

- [x] Create Runtime project only after S1 proves Starter package consumption.
- [x] Start with registration/error/capability primitives, not page code.
- [x] Migrate V2 to consume Runtime only where it reduces duplication.
- [x] Add architecture dependency tests.
- [x] Add package metadata if Runtime is created.
- [x] Add local package consumer proof for Runtime if packaged.

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Runtime|FullyQualifiedName~Architecture"
```

### Done when

- [x] Runtime exists only if justified by V2 + Starter reuse.
- [x] Runtime has no backend/core/V2 design dependencies.
- [x] Starter and V2 share neutral primitives without coupling presentation.

## S3 - Starter SSR/BFF Contract Foundation

Goal: define the runtime contracts before building pages and visual skeletons.

### SSR contract

```text
Starter SSR page/service
    -> Storefront.Runtime adapter
        -> generated Storefront.Client
            -> CommerceNode API
```

SSR owns:

- [x] store bootstrap.
- [x] public configuration/capabilities.
- [x] catalog/category/product/content/navigation/SEO initial reads.
- [n/a] cart initial snapshot if needed.
- [n/a] checkout initial state if needed.
- [x] status codes/noindex behavior for public pages.

### Browser/BFF contract

```text
Starter browser/WASM component
    -> same-origin /api/*
        -> Starter BFF endpoint
            -> Runtime/generated client
                -> CommerceNode API
```

Browser must not know:

- [x] Commerce Node base URL.
- [x] access token.
- [x] refresh token.
- [x] raw cart token.
- [x] store secret.
- [x] provider credentials.

### Error mapping contract

- [x] `401` -> unauthenticated/session expired.
- [x] `403` -> forbidden/policy blocked.
- [x] `409` -> stale cart/checkout conflict.
- [x] `422` -> validation/field errors.
- [x] `5xx` -> service error/retry option.
- [x] UI branches by `code` and status, not by `message`.

### Cookie/security contract

- [n/a] HttpOnly auth/refresh cookie convention.
- [x] cart token cookie convention.
- [x] antiforgery token meta/header convention.
- [x] Secure/SameSite policy.
- [n/a] return URL validation.
- [x] same-origin enforcement.
- [x] no token leakage in SSR HTML or WASM config.

### Required tracer bullets

- [x] One SSR request through generated client:
  - [x] recommended: current store/configuration.
- [x] One public content/catalog request through generated client:
  - [x] recommended: category/product/listing.
- [x] One protected browser command through BFF:
  - [x] recommended: cart add/update or account profile read.
- [x] Error mapping tests for 401/403/409/422.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Starter|FullyQualifiedName~Bff|FullyQualifiedName~Runtime"
```

### Done when

- [x] Starter has SSR generated-client path.
- [x] Starter has protected BFF path.
- [x] Browser output contains no Commerce URL/tokens.
- [x] BFF contains no ecommerce business logic.

## S4 - SSR / Hybrid / WASM Skeleton

Goal: create route/render ownership skeleton after data and BFF contracts are clear.

### Target folder shape

```text
BlazorShop.Storefront.Starter/
  Pages/
    Ssr/
      Content/
      System/
      Auth/
    Hybrid/
      Catalog/
      Commerce/
    WasmHost/
      Account/
  Features/
    Server/
    Browser/
  Components/
    Layout/
    States/
    Navigation/
  Composition/
  wwwroot/
```

### SSR-only pages

- [x] home or landing page if using SSR-only baseline.
- [x] content page.
- [x] auth form page shell where server-owned.
- [x] maintenance.
- [x] not found.

### Hybrid pages

- [x] product detail.
- [x] category.
- [x] search.
- [x] cart.
- [x] checkout.
- [x] payment result.
- [x] recommendations/deals placement.

### WASM-hosted features

- [x] account host.
- [x] profile.
- [x] addresses.
- [x] orders.
- [x] password management.

### Tasks

- [x] Add route skeletons with stable render ownership.
- [x] Add page-level SEO/noindex conventions.
- [x] Add hydration mode convention:
  - [x] `InitialSnapshot`.
  - [x] `BrowserFetch`.
  - [x] `RefreshAfterHydration`.
- [x] Prevent duplicate first-load fetch when SSR snapshot is supplied.
- [x] Add placeholder states, not final visual polish.
- [x] Keep UI neutral and replaceable.
- [x] Avoid importing Storefront V2 components or CSS.

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Starter"
```

### Done when

- [x] Render ownership is visible from folders.
- [x] Routes compile and render placeholder states.
- [x] Data flow and hydration rules are clear.

## S5 - Generated Client Adoption Policy And Exception Registry

Goal: enforce generated client usage for Starter while allowing documented exceptions.

### Policy

- [x] New Starter code uses generated `Storefront.Client` by default.
- [x] Manual `HttpClient` is forbidden unless listed in exception registry.
- [x] Manual request/response DTOs are forbidden when generated DTOs exist.
- [x] Presentation view models are allowed only when they transform API data for rendering/composition.

### Allowed exception candidates

- [x] auth flow needing `Set-Cookie`/refresh-cookie behavior.
- [x] streaming or file download.
- [x] media proxy.
- [x] multipart upload.
- [x] provider redirect callback handling.
- [x] generator limitation with explicit tracking issue.

### Exception registry fields

| Capability | Exception | Reason | Owner | Test | Revisit trigger |
| --- | --- | --- | --- | --- | --- |

### V2 migration backlog

- [x] Record V2 manual client capabilities:
  - [x] address.
  - [x] cart.
  - [x] checkout.
  - [x] consent.
  - [x] customer/account.
  - [x] payment.
- [x] Decide which V2 migrations are required before Sample QA.
- [x] Keep V2 migration as Track B; do not block Starter unless contract gaps break Starter.

### Tests

- [x] Starter source has no `StorefrontApiClient`.
- [x] Starter source has no duplicated generated API contract clones.
- [x] Starter exceptions are listed and tested.
- [x] generated OpenAPI/client compatibility tests still pass.

### Done when

- [x] Starter cannot silently introduce a second contract system.
- [x] Exceptions are explicit and reviewable.
- [x] V2 manual-client migration backlog exists but does not block Starter MVP.

## S6 - Feature Module And Capability Activation

Goal: define how features are installed, enabled, and placed without building a full marketplace.

### Availability rule

```text
Feature visible/usable =
  package/component installed
  + backend capability supported
  + store feature enabled
  + presentation placed
```

### Feature manifest

Starter should include a simple manifest:

```json
{
  "features": {
    "cart": { "installed": true, "required": true },
    "checkout": { "installed": true, "required": true },
    "account": { "installed": true, "required": false },
    "recommendations": { "installed": true, "required": false },
    "reviews": { "installed": false, "required": false }
  }
}
```

### Tasks

- [x] Define feature keys aligned with backend capability projection:
  - [x] `customerAccounts`.
  - [x] `registration`.
  - [x] `cart`.
  - [x] `checkout`.
  - [x] `payments`.
  - [x] `newsletter`.
  - [x] `recommendations`.
  - [x] `contactForm`.
- [x] Add manifest parser/validator.
- [x] Add capability reader that combines manifest + backend `features`.
- [x] Add presentation placement model:
  - [x] home.
  - [x] product detail.
  - [x] category.
  - [x] cart.
  - [x] checkout.
  - [x] account.
- [x] Do not package visual components as feature modules unless reused independently.
- [x] Keep feature UI neutral and overrideable.
- [x] Add tests for missing/disabled/unsupported features.

### Done when

- [x] Starter can hide unsupported/disabled features without code edits.
- [x] Feature placement is explicit.
- [x] Capability projection drives behavior safely.

## S7 - Neutral Layout, Loading, Error, Empty States

Goal: make Starter usable but not visually opinionated.

### Layout baseline

- [ ] neutral root layout.
- [ ] header.
- [ ] footer.
- [ ] main navigation.
- [ ] breadcrumb region.
- [ ] cart/account entry points.
- [ ] notification/toast region.
- [ ] responsive baseline.

### State components

- [ ] `LoadingState`.
- [ ] `SkeletonBlock`.
- [ ] `EmptyState`.
- [ ] `ErrorState`.
- [ ] `ValidationSummary`.
- [ ] `RetryAction`.
- [ ] `UnavailableFeatureState`.

### Feature baseline views

- [ ] product summary card.
- [ ] product grid.
- [ ] product detail shell.
- [ ] product gallery placeholder.
- [ ] purchase panel placeholder.
- [ ] cart line list.
- [ ] checkout step shell.
- [ ] account shell.

### UI rules

- [ ] neutral visual baseline only.
- [ ] no strong brand identity.
- [ ] accessible HTML semantics.
- [ ] responsive without layout overlap.
- [ ] easy to override CSS.
- [ ] no V2 asset/CSS dependency.

### Done when

- [ ] Starter looks usable enough for QA.
- [ ] Store-specific design can replace layout/CSS without touching runtime/security.
- [ ] Loading/error/empty states exist for generated storefronts.

## S8 - Starter Independent Build, Package, And Repository Proof

Goal: prove Starter can live outside the monorepo and use only packages/config.

### Temporary repository proof

Create an isolated working directory such as:

```text
obj/storefront-starter-isolation/
  Storefront.Sample/
    Storefront.Sample.csproj
    nuget.config
    appsettings.json
    source/
```

### Tasks

- [ ] Pack `Storefront.Client`.
- [ ] Pack `Storefront.Runtime` if created.
- [ ] Restore Starter/Sample from local package feed.
- [ ] Build without backend source project references.
- [ ] Publish without backend source project references.
- [ ] Docker build if Starter includes Dockerfile.
- [ ] Fail if relative source path points to V2, backend, or monorepo-only source.
- [ ] Add script:

```text
scripts/qa/run-storefront-starter-isolation-gate.ps1
```

- [ ] Add CI option:
  - [ ] full gate on workflow dispatch/nightly; or
  - [ ] describe/static gate on PR and full gate before release.

### Verification

```powershell
.\scripts\qa\run-storefront-starter-isolation-gate.ps1
```

### Done when

- [ ] Starter/Sample can build independently.
- [ ] Package boundaries are real.
- [ ] No hidden monorepo dependency is required.

## S9 - Deterministic Storefront.Sample Generation

Goal: generate a sample storefront from Starter without AI.

### Generation mechanism

Choose one deterministic mechanism:

- [ ] `dotnet new` template.
- [ ] PowerShell scaffolding script.
- [ ] small CLI command.

Recommended first version:

```powershell
.\scripts\generate-storefront-sample.ps1 -Name BlazorShop.Storefront.Sample -StoreKey sample
```

### Generated output

- [ ] project file.
- [ ] namespace/project name.
- [ ] package references.
- [ ] `nuget.config`.
- [ ] `appsettings.json`.
- [ ] store key/base URL config.
- [ ] feature manifest.
- [ ] neutral design files.
- [ ] test project or QA script.
- [ ] README.

### Rules

- [ ] Generator copies Starter template only.
- [ ] Generator does not copy V2 source.
- [ ] Generator does not edit generated client source.
- [ ] Generator does not create AI-designed UI.
- [ ] Generator output is deterministic.
- [ ] Generated output can be diffed and reviewed.

### Done when

- [ ] `Storefront.Sample` is produced from Starter.
- [ ] Generated output builds.
- [ ] Generated output can run against Commerce Node Storefront API.

## S10 - Sample QA Release Gate

Goal: prove generated `Storefront.Sample` is usable as an ecommerce storefront.

### Contract QA

- [ ] package restore.
- [ ] OpenAPI compatibility.
- [ ] no backend/core references.
- [ ] no V2 reference.
- [ ] no duplicated generated DTOs.
- [ ] no provider endpoint in frontend client.

### Functional QA

- [ ] store bootstrap.
- [ ] home.
- [ ] category.
- [ ] product.
- [ ] variant/product selection preview.
- [ ] add to cart.
- [ ] cart update/remove.
- [ ] checkout COD.
- [ ] order result.
- [ ] login/register/logout.
- [ ] profile/address.
- [ ] order history/detail.
- [ ] consent.
- [ ] maintenance.
- [ ] not found.

### Rendering/SEO QA

- [ ] product title/meta/canonical.
- [ ] category metadata.
- [ ] JSON-LD where supported.
- [ ] sitemap.
- [ ] robots.
- [ ] account noindex.
- [ ] SSR HTML contains product/category content before WASM.
- [ ] hybrid hydration does not duplicate initial request.

### Security QA

- [ ] CSRF rejection.
- [ ] invalid return URL rejection.
- [ ] 401 session handling.
- [ ] 403 policy handling.
- [ ] 409 stale cart/checkout.
- [ ] 422 validation.
- [ ] no tokens in WASM output.
- [ ] browser never calls Commerce Node protected URL directly.

### Performance baseline

- [ ] product page HTML response does not wait for WASM.
- [ ] add-to-cart hydrates early enough for usability.
- [ ] product is not fetched twice on first load.
- [ ] account assemblies are not loaded on public pages unless required by Blazor packaging constraints.

### Verification

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
.\scripts\qa\run-storefront-sample-release-gate.ps1
```

### Done when

- [ ] Generated Sample passes package, contract, functional, rendering, SEO, security, and browser QA.
- [ ] Starter is considered usable.

## S11 - AI Generator Planning

Goal: only begin AI Generator planning after a deterministic generated Sample passes QA.

### Inputs required before AI Generator

- [ ] Starter architecture docs.
- [ ] generated Sample implementation.
- [ ] feature map.
- [ ] capability map.
- [ ] route map.
- [ ] protected file rules.
- [ ] package/version manifest.
- [ ] QA checklist.
- [ ] deterministic generation workflow.

### AI allowed edit areas

- [ ] design.
- [ ] composition.
- [ ] presentation components.
- [ ] CSS.
- [ ] assets.
- [ ] store-specific public pages.
- [ ] feature placement.

### AI protected areas

- [ ] generated client.
- [ ] Runtime security primitives.
- [ ] auth/session.
- [ ] BFF transport.
- [ ] cart commands.
- [ ] checkout commands.
- [ ] error contract.
- [ ] package/version manifest unless explicitly requested.

### Done when

- [ ] AI Generator has a real Starter and Sample to learn from.
- [ ] AI scope is constrained to storefront presentation/composition.
- [ ] AI cannot silently break commerce/security contracts.

## Parallel Workstreams

After S3, work may split:

```text
Track A - Starter
  S4 -> S6 -> S7 -> S8 -> S9 -> S10

Track B - Platform hardening
  V2 manual client migration
  Runtime hardening
  package release pipeline
  CI isolation/full Playwright gate
```

Rules:

- [ ] Track A uses only stable platform pieces.
- [ ] Track B does not block Starter unless it reveals contract gaps.
- [ ] V2 migration to generated client is valuable but not a blocker for Starter MVP.

## CI Recommendations

Add CI in stages:

- [ ] PR static gate:
  - [ ] architecture tests.
  - [ ] generated client deterministic check.
  - [ ] Starter build.
  - [ ] no forbidden references.
- [ ] PR package proof:
  - [ ] pack client/runtime.
  - [ ] restore isolated Starter/Sample.
  - [ ] build isolated Starter/Sample.
- [ ] nightly/full release gate:
  - [ ] run local dependencies.
  - [ ] run Starter/Sample browser QA.
  - [ ] run Playwright release gate.
  - [ ] collect screenshots/network evidence.

## Implementation Order And Commit Plan

- [x] Commit 1: S0 ADR/docs/architecture tests.
- [x] Commit 2: S1 Starter project consuming packaged `Storefront.Client`.
- [x] Commit 3: S2 minimal Runtime extraction if justified by V2 + Starter duplication.
- [x] Commit 4: S3 SSR generated-client tracer and BFF protected-command tracer.
- [x] Commit 5: S4 route/render skeleton.
- [x] Commit 6: S5 generated-client policy and exception registry.
- [x] Commit 7: S6 feature manifest/capability activation.
- [ ] Commit 8: S7 neutral layout and state components.
- [ ] Commit 9: S8 isolation gate script.
- [ ] Commit 10: S9 deterministic Sample generation.
- [ ] Commit 11: S10 Sample QA release gate evidence.
- [ ] Commit 12: S11 AI Generator planning docs.

Each commit must be buildable. Keep V2 behavior changes separate from Starter scaffolding unless a shared Runtime extraction requires touching both.

## Risk Controls

- [ ] Do not copy V2 internals.
- [ ] Do not add backend/core references to Starter/Runtime/Sample.
- [ ] Do not introduce a second handwritten API contract system.
- [ ] Do not expose Commerce Node URL/tokens to browser.
- [ ] Do not move ecommerce business truth into Starter.
- [ ] Do not package visual components prematurely.
- [ ] Do not let Sample QA pass without real checkout/order flow.
- [ ] Do not begin AI Generator before deterministic Sample passes QA.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Start Starter after Headless Foundation completion | Auto-decided | Build on proven boundary | Foundation evidence shows generated client, package proof, V2 decoupling, and BFF boundary are ready. | Reopening Foundation before Starter planning. |
| 2 | S0 | Lock policy that Starter does not copy manual `StorefrontApiClient` | Auto-decided | Avoid second contract system | V2 still has manual/generated clients in parallel; Starter must start clean. | Waiting until S5 to define the rule. |
| 3 | S1 | Use PackageReference to generated client | Auto-decided | Prove external consumer model | Starter must behave like a future separate Git repo/storefront. | ProjectReference to generated client in Starter proof. |
| 4 | S2 | Extract Runtime minimally only when V2 + Starter both need it | Auto-decided | Avoid fake abstraction | Foundation deferred Runtime because there was one consumer; Starter creates the second consumer but still requires evidence before extraction. | Moving V2 services wholesale into Runtime. |
| 5 | S3 | Define SSR/BFF/security contract before page skeleton | Auto-decided | Security and data flow before UI | Starter must teach correct backend/frontend boundaries before visuals. | Building pages/CSS first. |
| 6 | S5 | Treat V2 full generated-client migration as Track B | Auto-decided | Avoid blocking Starter MVP | V2 is already compile-time decoupled; remaining manual adapters should not indefinitely block Starter. | Requiring every V2 capability to migrate before Starter. |
| 7 | S9/S10 | Separate generation from QA gate | Auto-decided | Generated does not mean usable | Sample must prove ecommerce, SEO, security, and browser flows before AI. | Calling Starter done after scaffold output builds. |
| 8 | S11 | AI Generator starts only after deterministic Sample passes QA | User direction preserved | Prevent fake storefront generation | AI needs a tested Starter/Sample contract to avoid generating only visual pages. | Starting AI generator directly from API docs or V2. |

## Final Recommendation

Approve this as the Storefront Starter Foundation roadmap.

The highest-value early implementation path is:

1. S0 architecture lock and protected rules.
2. S1 package-based generated client consumption.
3. S3 SSR/BFF contract tracer bullets.
4. S4 render skeleton.
5. S8/S9/S10 independent Sample proof and QA.

S2 Runtime extraction should happen only for neutral code used by both V2 and Starter. S5 V2 generated-client adoption should be tracked, but it should not block a Starter MVP unless it reveals a real contract gap.

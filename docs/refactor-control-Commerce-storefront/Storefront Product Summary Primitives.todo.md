# Storefront Product Summary Primitives

Status: planned  
Track: Phase 3 - V2 Component Extraction  
Phase: 3.1  
Predecessor: H3 - Hybrid Hardening / Closure  
Successor: choose only after Phase 3.1 closure review  
Scope: plan only; implementation happens in later execution turns

## Decision

Product Summary rendering is duplicated today between:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryCard.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Catalog/StorefrontDiscountedProductRailSection.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Catalog/ProductImage.razor`

The extraction target is correct:

- `StorefrontProductSummaryImage`
- `StorefrontProductSummaryPurchaseActions`
- `StorefrontProductSummaryCard`

The target project is not `BlazorShop.Storefront.Components.Ssr`, because `Components.Ssr` may reference `BlazorShop.Storefront.Presentation` and therefore must not enter the `V2.WASM` browser graph.

Create one narrow browser-safe Razor primitive package:

```text
BlazorShop.Storefront.Components.Primitives
    -> BlazorShop.Storefront.Components
```

`Components.Primitives` is a physical render-only project, not a new component mode. Do not add `StorefrontComponentMode.Primitive`.

## Current Project Facts

Current reusable component mode graph:

```text
BlazorShop.Storefront.Components.Ssr
    -> BlazorShop.Storefront.Components
    -> BlazorShop.Storefront.Presentation

BlazorShop.Storefront.Components.WasmHost
    -> BlazorShop.Storefront.Components
    -> BlazorShop.Storefront.Browser
```

Current `V2.WASM` graph:

```text
BlazorShop.Storefront.V2.WASM
    -> BlazorShop.Storefront.Browser
    -> BlazorShop.Storefront.Components
    -> BlazorShop.Storefront.Components.WasmHost
```

Required final direct graph:

```text
BlazorShop.Storefront.V2
    -> BlazorShop.Storefront.Components.Primitives

BlazorShop.Storefront.V2.WASM
    -> BlazorShop.Storefront.Components.Primitives

BlazorShop.Storefront.Components.Primitives
    -> BlazorShop.Storefront.Components
```

Forbidden final graph:

```text
BlazorShop.Storefront.V2.WASM
    -> BlazorShop.Storefront.Components.Ssr

BlazorShop.Storefront.V2.WASM
    -> BlazorShop.Storefront.Presentation

BlazorShop.Storefront.Components.Primitives
    -> BlazorShop.Storefront.Presentation
    -> BlazorShop.Storefront.Browser
    -> BlazorShop.Storefront.Runtime
    -> BlazorShop.Storefront.Client
    -> backend/core/API projects
```

## Ownership Rules

`BlazorShop.Storefront.Components` owns:

- render/input contracts such as `ProductSummaryItem`
- existing `ProductSummaryLabels`
- headless/browser-safe state only
- no Razor UI
- no final copy/design/layout ownership

`BlazorShop.Storefront.Components.Primitives` owns:

- render-only Razor primitives
- semantic DOM hooks
- accessibility markup
- host-supplied labels/classes
- no runtime behavior
- no API/BFF calls
- no route descriptors
- no visual theme ownership

`BlazorShop.Storefront.V2` owns:

- final Product Summary classes
- final Product Summary copy values
- SSR/catalog page composition
- grid/layout placement
- host assets and CSS

`BlazorShop.Storefront.V2.WASM` owns:

- WASM rail wrapper placement
- final Product Summary classes/copy for the WASM rail context
- rail labels/classes/action descriptor
- no duplicated Product Summary card branching after this phase

`BlazorShop.Storefront.Components.WasmHost` owns:

- `StorefrontDiscountedProductRail`
- loading/success/empty/error/retry state
- browser controller usage
- no final V2 card markup

`BlazorShop.Storefront.Presentation` still owns:

- same-origin BFF/local endpoint behavior
- product purchase/add-to-cart browser binders
- semantic command execution from `data-storefront-*` descriptors
- no V2 visual markup

## Non-Goals

Do not extract or refactor in Phase 3.1:

- `StorefrontProductSummaryGrid`
- `StorefrontDealsSection`
- product detail gallery
- product detail purchase panel
- variant selector
- quantity controls
- cart
- checkout
- account
- Starter
- StorefrontBuilder
- backend APIs
- Commerce Node services
- Control Plane services
- generated storefront projects

Do not introduce:

- `Components.Common`
- `Components.Shared`
- `Components.Product`
- `Components.Catalog`
- a design system
- a component registry
- reflection discovery
- theme schema framework
- a new runtime/component mode enum value

## Phase 3.1.0 - Baseline Audit

Goal: prove the current duplication and dependency graph before adding a project.

Tasks:

- [x] Confirm H3 is closed in code, docs, tests, and solution.
- [x] Run and record `git status --short`.
- [x] Read current boundary docs:
  - [x] `AGENTS.md`
  - [x] `docs/architecture/README.md`
  - [x] `docs/architecture/03-runtime-boundaries.md`
  - [x] `docs/architecture/05-project-and-folder-guide.md`
  - [x] `docs/architecture/08-agent-decision-rules.md`
  - [x] `docs/architecture/10-v2-contract-ownership.md`
  - [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [x] Inspect current project references:
  - [x] `BlazorShop.Storefront.Components`
  - [x] `BlazorShop.Storefront.Components.Ssr`
  - [x] `BlazorShop.Storefront.Components.WasmHost`
  - [x] `BlazorShop.Storefront.V2`
  - [x] `BlazorShop.Storefront.V2.WASM`
- [x] Confirm current facts:
  - [x] `Components.Ssr` references `Components` and `Presentation`.
  - [x] `Components.WasmHost` references `Components` and `Browser`.
  - [x] `Components` has no project references.
  - [x] `V2.WASM` does not reference `Presentation`.
  - [x] `V2.WASM` does not reference `Components.Ssr`.
- [x] Inspect current Product Summary files:
  - [x] `V2/Components/Catalog/StorefrontProductSummaryCard.razor`
  - [x] `V2/Components/Catalog/StorefrontProductSummaryGrid.razor`
  - [x] `V2/Components/Catalog/ProductCard.razor`
  - [x] `V2.WASM/Components/Catalog/StorefrontDiscountedProductRailSection.razor`
  - [x] `V2.WASM/Components/Catalog/ProductImage.razor`
  - [x] `Components.WasmHost/Catalog/StorefrontDiscountedProductRail.razor`
  - [x] `Components/Contracts/Catalog/ProductSummaryItem.cs`
  - [x] `Components/Contracts/Catalog/ProductSummaryLabels.cs`
- [x] Inventory exact behavior to preserve:
  - [x] category link/text behavior
  - [x] product title/link behavior
  - [x] badges: new, variants, out
  - [x] price and compare price
  - [x] image render
  - [x] missing-image fallback
  - [x] broken-image fallback script
  - [x] description
  - [x] direct add-to-cart button
  - [x] variant purchase link
  - [x] view product link
  - [x] purchase availability message priority
  - [x] semantic purchase data attributes
- [x] Search all current occurrences:

```powershell
rg -n "StorefrontProductSummaryCard|ProductImage|StorefrontDiscountedProductRailSection|data-storefront-product-summary-card|data-storefront-product-purchase-submit|BrokenImageFallbackScript|Select variant on the product page before adding|Currently out of stock" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs -g "!*bin*" -g "!*obj*"
```

Exit criteria:

- [x] Duplication is documented.
- [x] Current dependency graph is documented.
- [x] No implementation starts until the `V2.WASM` browser-safe graph requirement is confirmed.

Implementation notes:

- 2026-08-10: H3 closure is confirmed at `47af7221 docs(storefront): close hybrid h3 final report`; `Storefront Hybrid Closure H3.todo.md` records H3.17, required final checks, retired `Components.Hybrid`, and closed hybrid architecture.
- 2026-08-10: Baseline `git status --short` before implementation showed only this plan file as untracked: `?? "docs/refactor-control-Commerce-storefront/Storefront Product Summary Primitives.todo.md"`.
- 2026-08-10: Boundary docs listed above were read before implementation.
- 2026-08-10: Current graph is confirmed: `Components` has no project references; `Components.Ssr` references `Components` and `Presentation`; `Components.WasmHost` references `Components` and `Browser`; `V2.WASM` references `Browser`, `Components`, and `Components.WasmHost`, with no `Presentation` or `Components.Ssr` reference.
- 2026-08-10: Product Summary duplication is confirmed: V2 `StorefrontProductSummaryCard.razor` owns the full card/image/purchase markup; V2.WASM `StorefrontDiscountedProductRailSection.razor` embeds a duplicated full product card and uses V2.WASM `ProductImage.razor`; both paths carry the broken image fallback behavior and purchase submit selector.
- 2026-08-10: Behavior to preserve includes category/title link fallbacks, new/variant/out badges, price/compare price, image unavailable fallback, broken-image fallback script, description, direct add-to-cart descriptors, variant purchase link, view product link, and purchase availability priority.
- 2026-08-10: The required `rg` search found active markup/test/docs occurrences in `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.V2.WASM`, `BlazorShop.Storefront.Presentation` browser binders, and existing V2 tests. This confirms the extraction must keep `data-storefront-product-summary-card` and `data-storefront-product-purchase-submit` stable.

## Phase 3.1.1 - Create Components.Primitives Project

Goal: add the narrow browser-safe Razor primitive project.

Project path:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/
```

Required files:

```text
BlazorShop.Storefront.Components.Primitives.csproj
_Imports.razor
README.md
Catalog/
```

Project settings:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>BlazorShop.Storefront.Components.Primitives</RootNamespace>
    <PackageId>BlazorShop.Storefront.Components.Primitives</PackageId>
  </PropertyGroup>
</Project>
```

Allowed direct project reference:

```text
../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
```

Forbidden direct project references:

- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Browser`
- `BlazorShop.Storefront.Runtime`
- `BlazorShop.Storefront.Client`
- `BlazorShop.Storefront.Components.Ssr`
- `BlazorShop.Storefront.Components.WasmHost`
- `BlazorShop.Storefront.V2`
- `BlazorShop.Storefront.V2.WASM`
- `BlazorShop.Storefront.Starter`
- `BlazorShop.Storefront.Starter.WASM`
- `BlazorShop.Application`
- `BlazorShop.Domain`
- `BlazorShop.Infrastructure`
- `BlazorShop.CommerceNode.API`
- `BlazorShop.ControlPlane.*`
- `BlazorShop.Web.SharedV2`

Tasks:

- [x] Create `BlazorShop.Storefront.Components.Primitives`.
- [x] Add `_Imports.razor` with only required component namespaces.
- [x] Add README stating:
  - [x] browser-safe render-only Razor primitives
  - [x] `Components` dependency only
  - [x] no Presentation/Browser/Runtime/Client/backend/host dependency
  - [x] no final CSS/copy/design ownership
  - [x] not a component mode
- [x] Add project to `BlazorShop.sln`.
- [x] Keep solution placement consistent with existing Storefront component projects.

Exit criteria:

- [x] Project exists.
- [x] Project directly references only `Components`.
- [x] Project builds.
- [x] No consumer references are added yet except when the adoption phase starts.

Implementation notes:

- 2026-08-10: Added `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives` with Razor SDK, `net10.0`, package id/root namespace, and a single direct `ProjectReference` to `BlazorShop.Storefront.Components`.
- 2026-08-10: Added `_Imports.razor` with only `Microsoft.AspNetCore.Components` and catalog contract namespaces.
- 2026-08-10: Added README documenting browser-safe render-only scope, dependency boundary, host-owned CSS/copy/design, and no new component mode.
- 2026-08-10: Added the project to `BlazorShop.sln` adjacent to existing Storefront component projects.
- 2026-08-10: Verification passed: `dotnet build "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj"`.

## Phase 3.1.2 - Add Primitive Dependency Guardrails

Goal: make the new boundary enforceable before moving duplicated markup.

Add focused tests, likely under:

```text
BlazorShop.Tests.V2/PresentationV2/Storefront/
```

Candidate test class:

```text
StorefrontPrimitiveDependencyTests
```

Required assertions:

- [x] `Components.Primitives` references exactly `Components`.
- [x] `Components.Primitives` does not reference forbidden Storefront packages.
- [x] `Components.Primitives` does not reference backend/core/API projects.
- [x] `Components.Primitives` does not reference `Web.SharedV2`.
- [x] `Components.Primitives` source does not contain:
  - [x] `@rendermode`
  - [x] `InteractiveServer`
  - [x] `InteractiveAuto`
  - [x] `InteractiveWebAssembly`
  - [x] `HttpClient`
  - [x] `HttpContext`
  - [x] `IHttpContextAccessor`
  - [x] `IJSRuntime`
  - [x] `HubConnection`
  - [x] `ClientWebSocket`
  - [x] `api/storefront/stores`
  - [x] `api/commerce`
  - [x] `api/control-plane`
  - [x] `CommerceNode`
  - [x] `ControlPlane`
  - [x] localhost backend URLs
- [x] Add or extend graph traversal test proving `V2.WASM` cannot reach:
  - [x] `BlazorShop.Storefront.Presentation`
  - [x] `BlazorShop.Storefront.Components.Ssr`
  - [x] `BlazorShop.Storefront.Runtime`
  - [x] `BlazorShop.Storefront.Client`
  - [x] backend/core/API projects
- [x] Add negative fixture checks proving scanners reject at least:
  - [x] `Presentation` reference
  - [x] `Browser` reference
  - [x] `@rendermode`
  - [x] `HttpClient`
  - [x] `IJSRuntime`
  - [x] literal V2 classes

Important correction from review:

- [x] Existing `V2WasmDoesNotReferenceRuntimeClientConsumersBackendCoreOrApiProjects` is not enough; it must also block `Presentation` and `Components.Ssr`, directly and transitively.

Exit criteria:

- [x] Positive repository scan passes.
- [x] Negative fixture checks pass.
- [x] A future accidental `V2.WASM -> Components.Ssr -> Presentation` path fails tests.

Implementation notes:

- 2026-08-10: Added `StorefrontPrimitiveDependencyTests` to assert exact `Components.Primitives -> Components` references, forbid Storefront host/runtime/backend/core/API/Web.SharedV2 references, and scan primitive source for render-mode/API/runtime/browser tokens.
- 2026-08-10: Added negative fixtures for forbidden `Presentation`, `Browser`, `@rendermode`, `HttpClient`, `IJSRuntime`, and literal V2 class usage.
- 2026-08-10: Extended `StorefrontComponentModeDependencyTests` so V2.WASM blocks `Presentation` and `Components.Ssr` directly and through transitive project-reference traversal.
- 2026-08-10: Verification passed: `dotnet test "BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj" --filter "FullyQualifiedName~StorefrontPrimitiveDependencyTests|FullyQualifiedName~StorefrontComponentModeDependencyTests"`.

## Phase 3.1.3 - Extend Visual Neutrality To Primitives

Goal: ensure `Components.Primitives` does not become a shared V2 visual template layer.

Tasks:

- [x] Extend `StorefrontComponentVisualNeutralityTests` reusable scan roots to include:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives
```

- [x] Keep literal class scanner active for `.razor` and `.cshtml`.
- [x] Allow dynamic class slots:
  - [x] `class="@Classes.Root"`
  - [x] `class="@Classes.Image"`
  - [x] `class="@(BuildClass(...))"`
- [x] Keep forbidden mixed class values:
  - [x] `class="@Classes.Root mt-4"`
  - [x] `class="rounded-xl @Classes.Root"`
  - [x] `class="group relative ..."`
- [x] Keep forbidden final visual tokens:
  - [x] `bs-storefront-`
  - [x] `storefront.css`
  - [x] `css/site.css`
  - [x] `css/wasm-site.css`
  - [x] `wwwroot/`
  - [x] V2 `_content` paths
- [x] Update scan naming from only "ModeProjects" if needed, because primitives are not a component mode.
- [x] Add explicit test text/remediation:

```text
Reusable render primitives must expose semantic hooks and fully dynamic host class slots; host projects own final visual classes.
```

Exit criteria:

- [x] `Components.Primitives` has no final Tailwind/V2 class literals.
- [x] Existing `Components.Ssr` and `Components.WasmHost` neutrality tests still pass.
- [x] Test naming no longer incorrectly implies primitives are runtime mode projects.

Implementation notes:

- 2026-08-10: Extended `StorefrontComponentVisualNeutralityTests` scan roots to include `BlazorShop.Storefront.Components.Primitives`.
- 2026-08-10: Renamed test/helper language from mode-only wording to reusable render project wording where primitives are included.
- 2026-08-10: Added dynamic class slot fixtures for `Classes.Root`, `Classes.Image`, and `BuildClass(...)`; added mixed/literal class rejections for `@Classes.Root mt-4`, `rounded-xl @Classes.Root`, and `group relative`.
- 2026-08-10: Updated remediation text to: `Reusable render primitives must expose semantic hooks and fully dynamic host class slots; host projects own final visual classes.`
- 2026-08-10: Verification passed: `dotnet test "BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj" --filter "FullyQualifiedName~StorefrontComponentVisualNeutralityTests"`.

## Phase 3.1.4 - Reuse Existing Product Summary Contracts

Goal: avoid duplicate label/model contracts.

Existing contract:

```text
BlazorShop.Storefront.Components/Contracts/Catalog/ProductSummaryItem.cs
BlazorShop.Storefront.Components/Contracts/Catalog/ProductSummaryLabels.cs
```

Rules:

- [x] Do not create another Product Summary item model.
- [x] Prefer reuse/extension of `ProductSummaryLabels`.
- [x] Do not create `ProductSummaryCardLabels` unless implementation proves `ProductSummaryLabels` cannot cleanly cover status messages.
- [x] If extending `ProductSummaryLabels`, keep it browser-safe and presentation-only.
- [x] If a new label record is unavoidable, document why `ProductSummaryLabels` was insufficient and keep it in `Components/Contracts/Catalog`.
- [x] Do not put labels in `Components.Primitives` unless they are primitive-specific and cannot be shared as component contracts.

Minimum labels/copy to support:

- [x] `FromPrefix`
- [x] `PricePrefix`
- [x] `ImageUnavailableText`
- [x] `ImageUnavailableAltFormat`
- [x] `NewBadge`
- [x] `VariantsBadge`
- [x] `OutOfStockBadge`
- [x] `AddToCart`
- [x] `AddedToCart`
- [x] `ViewProduct`
- [x] `SelectVariant`
- [x] `CurrentlyOutOfStock`
- [x] `CurrentlyUnavailable`

Class contract:

- [x] Create `ProductSummaryCardClasses` only if needed.
- [x] Keep it small.
- [x] Candidate slots:
  - [x] `Root`
  - [x] `Body`
  - [x] `Header`
  - [x] `Category`
  - [x] `Title`
  - [x] `BadgeGroup`
  - [x] `Badge`
  - [x] `Price`
  - [x] `ComparePrice`
  - [x] `ImageLink`
  - [x] `ImageFrame`
  - [x] `Image`
  - [x] `ImageFallback`
  - [x] `Description`
  - [x] `Footer`
  - [x] `ActionGroup`
  - [x] `PrimaryAction`
  - [x] `SecondaryAction`
  - [x] `Status`
- [x] Reduce slots if a region does not need independent host styling.
- [x] Do not create one class property for every nested span without evidence.

Exit criteria:

- [x] `ProductSummaryItem` remains the only Product Summary data item.
- [x] Existing `ProductSummaryLabels` is reused or deliberately extended.
- [x] No duplicate label contract is introduced by default.
- [x] Class slots remain minimal and semantic.

Implementation notes:

- 2026-08-10: Extended existing `ProductSummaryLabels` with `SelectVariant`, `CurrentlyOutOfStock`, and `CurrentlyUnavailable`; no `ProductSummaryCardLabels` contract was introduced.
- 2026-08-10: Added `ProductSummaryCardClasses` in `Components/Contracts/Catalog` because upcoming render primitives need host-supplied class slots while staying visually neutral.
- 2026-08-10: `ProductSummaryCardClasses` uses semantic region slots for card, image, body, status, and actions; it does not add nested span-level slots without current markup evidence.
- 2026-08-10: Updated contract inventory tests to include `ProductSummaryCardClasses` and assert no duplicate `ProductSummaryCardLabels` exists.
- 2026-08-10: Verification passed: `dotnet build "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj"` and `dotnet test "BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj" --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"`.

## Phase 3.1.5 - Extract StorefrontProductSummaryImage

Target:

```text
BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryImage.razor
```

Responsibility:

- render product image and fallback only
- no business state
- no API calls
- no browser controller
- no `IJSRuntime`

Inputs:

- [ ] `ProductSummaryItem Item`
- [ ] `ProductSummaryLabels Labels`
- [ ] image-related classes from `ProductSummaryCardClasses` or a smaller internal parameter set

Behavior to preserve:

- [ ] If `Item.ImageUrl` exists, render `<img src alt>`.
- [ ] `alt` uses `Item.Name`.
- [ ] If image URL is missing, render fallback immediately.
- [ ] If image load fails, hide image and reveal fallback.
- [ ] Fallback has `role="img"`.
- [ ] Fallback has useful `aria-label`.
- [ ] Fallback text comes from host-supplied labels.
- [ ] Existing simple inline `onerror` behavior may be preserved.
- [ ] Do not replace inline fallback with `IJSRuntime` or Browser controller.
- [ ] Do not introduce static assets or SVG files.

Exit criteria:

- [ ] Image behavior exists once for Product Summary.
- [ ] Component compiles in browser-safe primitive graph.
- [ ] No runtime-specific dependency.

## Phase 3.1.6 - Extract StorefrontProductSummaryPurchaseActions

Target:

```text
BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryPurchaseActions.razor
```

Responsibility:

- render Product Summary purchase action area
- render availability/status message
- emit semantic command descriptors
- do not execute commands

Preserve exact direct-add semantic hooks:

```text
data-storefront-product-purchase
data-storefront-command="cart.add-line"
data-storefront-product-purchase-submit
data-default-label
data-success-label
data-product-id
data-product-name
data-currency-code
```

State priority to preserve:

```text
HasVariants
    -> select variant message

PurchasePaused
    -> PurchaseBlockMessage

!InStock
    -> out-of-stock message

!Purchasable
    -> PurchaseBlockMessage or unavailable fallback
```

Navigation to preserve:

- [ ] Direct-add button when `Item.CanAddDirectly`.
- [ ] Variant/purchase link when `Item.HasVariants` and `Item.PurchaseUrl` exists.
- [ ] View Product link when `Item.ProductUrl` exists.

Forbidden:

- [ ] `HttpClient`
- [ ] `StorefrontLocalApiClient`
- [ ] Browser controller injection
- [ ] direct BFF/API calls
- [ ] constructing cart request DTOs
- [ ] reading raw product-selection preview business fields
- [ ] `IJSRuntime`

Exit criteria:

- [ ] Purchase branching exists once for Product Summary.
- [ ] Semantic attributes remain byte-for-byte equivalent where browser binders depend on them.
- [ ] Command execution remains Presentation/browser binder owned.

## Phase 3.1.7 - Extract StorefrontProductSummaryCard

Target:

```text
BlazorShop.Storefront.Components.Primitives/Catalog/StorefrontProductSummaryCard.razor
```

Responsibility:

```text
StorefrontProductSummaryCard
    -> category/name
    -> badges
    -> price/compare price
    -> StorefrontProductSummaryImage
    -> description
    -> StorefrontProductSummaryPurchaseActions
```

Inputs:

- [ ] `ProductSummaryItem Item`
- [ ] `ProductSummaryLabels Labels`
- [ ] minimal class contract

Preserve:

- [ ] `data-storefront-product-summary-card`
- [ ] category link/text behavior
- [ ] product link/title behavior
- [ ] `New` badge semantics
- [ ] `Variants` badge semantics
- [ ] `Out` badge semantics
- [ ] `From` vs `Price` label behavior
- [ ] `PriceDisplay`
- [ ] `ComparePriceDisplay`
- [ ] image wrapped in product link when product URL exists
- [ ] description
- [ ] purchase action composition
- [ ] accessibility labels
- [ ] all semantic `data-storefront-*` hooks

Do not create in Phase 3.1:

- `ProductPrice`
- `ProductBadge`
- `ProductTitle`
- `ProductCategoryLink`
- `ProductDescription`

Exit criteria:

- [ ] One reusable Product Summary card renders in both V2 and V2.WASM.
- [ ] Card has no final V2 classes.
- [ ] Card has no runtime-specific dependency.

## Phase 3.1.8 - V2 Adoption

Goal: replace V2 Product Summary card implementation with primitive card.

Project reference:

```text
BlazorShop.Storefront.V2
    -> BlazorShop.Storefront.Components.Primitives
```

Tasks:

- [ ] Add project reference from `V2` to `Components.Primitives`.
- [ ] Add required namespace import to V2 `_Imports.razor` or local component files.
- [ ] Replace `V2/Components/Catalog/StorefrontProductSummaryCard.razor` with direct primitive usage.
- [ ] Update `V2/Components/Catalog/StorefrontProductSummaryGrid.razor` to render primitive card.
- [ ] Update `V2/Components/Catalog/ProductCard.razor` if it still wraps the old local card.
- [ ] Move V2 final class values into a V2-owned static property/helper/component-local config.
- [ ] Move V2 final label/copy values into V2-owned config, reusing `ProductSummaryLabels`.
- [ ] Delete the old V2 `StorefrontProductSummaryCard.razor` if it is a pure pass-through.
- [ ] Do not keep a wrapper unless it has a real V2-specific composition responsibility.

Exit criteria:

- [ ] V2 renders primitive card.
- [ ] V2 owns final classes and labels.
- [ ] V2 pages remain SSR/prerender-capable.
- [ ] No V2-specific classes moved into the primitive project.

## Phase 3.1.9 - V2.WASM Adoption

Goal: replace duplicated rail item card markup with primitive card.

Project reference:

```text
BlazorShop.Storefront.V2.WASM
    -> BlazorShop.Storefront.Components.Primitives
```

Required final direct graph:

```text
V2.WASM
    -> Browser
    -> Components
    -> Components.Primitives
    -> Components.WasmHost
```

Tasks:

- [ ] Add project reference from `V2.WASM` to `Components.Primitives`.
- [ ] Add required namespace import to `V2.WASM` `_Imports.razor` or local wrapper.
- [ ] Replace `StorefrontDiscountedProductRailSection` `ItemTemplate` duplicated card markup with:

```razor
<StorefrontProductSummaryCard
    Item="product"
    Classes="..."
    Labels="..." />
```

- [ ] Keep rail labels/classes/action descriptor in `V2.WASM`.
- [ ] Keep `StorefrontDiscountedProductRail` in `Components.WasmHost`.
- [ ] Delete `V2.WASM/Components/Catalog/ProductImage.razor` if unused.
- [ ] Remove duplicate Product Summary image fallback script from `V2.WASM`.
- [ ] Remove duplicate Product Summary purchase-state branching from `V2.WASM`.

Exit criteria:

- [ ] Rail still owns loading/success/empty/error/retry behavior.
- [ ] Rail item rendering uses primitive card.
- [ ] `V2.WASM` does not reference `Presentation`.
- [ ] `V2.WASM` does not reference `Components.Ssr`.
- [ ] Add-to-cart semantic hooks remain present in rendered rail items.

## Phase 3.1.10 - Update Existing Architecture Tests

Goal: change the expected ownership model from "V2 owns all Product Summary markup" to "Primitives owns reusable semantic rendering; V2/V2.WASM own final visual values."

Review and update at minimum:

- [ ] `StorefrontBrandingMarkupTests`
- [ ] `StorefrontComponentsHeadlessPresentationRefactorTests`
- [ ] `StorefrontVisualOnlyBoundaryTests`
- [ ] `StorefrontComponentVisualNeutralityTests`
- [ ] `StorefrontComponentModeDependencyTests`
- [ ] `StorefrontRenderModeOwnershipTests`
- [ ] `StorefrontComponentModeBoundaryValidatorTests`
- [ ] `StorefrontIndependenceBoundaryTests`

Required new assertions:

- [ ] `Components.Primitives` exists and is included in relevant reusable component scans.
- [ ] `Components.Primitives` is not treated as `Ssr`, `Hybrid`, or `WasmHost`.
- [ ] `Components.Primitives` has no descriptors unless a real inventory use case is approved.
- [ ] `Components.Primitives` has no `@rendermode`.
- [ ] `Components.Primitives` has no final literal classes.
- [ ] `Components.Primitives` owns `data-storefront-product-summary-card`.
- [ ] `Components.Primitives` owns Product Summary image fallback.
- [ ] `Components.Primitives` owns Product Summary purchase semantic markup.
- [ ] V2 supplies final Product Summary classes/copy.
- [ ] V2.WASM supplies final Product Summary classes/copy for the rail.
- [ ] Old duplicate Product Summary card implementations are absent.
- [ ] `V2.WASM` cannot reach `Presentation` or `Components.Ssr` directly or transitively.

Avoid:

- [ ] broad allowlist exceptions
- [ ] string-only tests that pass while dependency graph is broken
- [ ] deleting old tests without replacing the architecture intent

Exit criteria:

- [ ] Tests describe the new architecture clearly.
- [ ] No stale "V2 owns all product card markup" assumption remains.

## Phase 3.1.11 - Focused Primitive Component Tests

Goal: cover semantic behavior without brittle full markup snapshots.

Image tests:

- [ ] image URL renders `<img>`
- [ ] missing image renders fallback
- [ ] fallback uses `role="img"`
- [ ] fallback aria label uses product name
- [ ] fallback text uses labels
- [ ] broken-image inline fallback is emitted
- [ ] image class slot is dynamic

Purchase action tests:

- [ ] direct-add item renders button
- [ ] direct-add item emits `data-storefront-product-purchase`
- [ ] direct-add item emits `data-storefront-command="cart.add-line"`
- [ ] direct-add item emits `data-storefront-product-purchase-submit`
- [ ] direct-add item emits product id/name/currency attributes
- [ ] variant item renders purchase link
- [ ] non-direct item does not emit command submit hook
- [ ] purchase paused message wins before out-of-stock
- [ ] out-of-stock message renders when appropriate
- [ ] not-purchasable fallback renders when appropriate
- [ ] view product link renders when available

Card tests:

- [ ] category text/link
- [ ] product title/link
- [ ] badges
- [ ] from/price label behavior
- [ ] price and compare price
- [ ] description
- [ ] image component composition
- [ ] purchase component composition
- [ ] root `data-storefront-product-summary-card`
- [ ] dynamic class slots
- [ ] no hardcoded V2 class tokens

Exit criteria:

- [ ] Focused tests protect behavior without full whitespace snapshots.
- [ ] Tests run without server, browser, or Commerce Node setup.

## Phase 3.1.12 - Focused Build Gate

Run:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Then inspect references:

```powershell
rg -n "BlazorShop.Storefront.Presentation|BlazorShop.Storefront.Components.Ssr|BlazorShop.Storefront.Runtime|BlazorShop.Storefront.Client" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives -g "*.csproj" -g "*.cs" -g "*.razor" -g "!*bin*" -g "!*obj*"
```

Exit criteria:

- [ ] Focused builds pass.
- [ ] `Components.Primitives` direct dependency is still only `Components`.
- [ ] `V2.WASM` remains browser-safe.

## Phase 3.1.13 - Focused Test Gate

Run focused tests by name/filter after implementation:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Primitive|FullyQualifiedName~ProductSummary|FullyQualifiedName~ProductRail|FullyQualifiedName~ComponentVisualNeutrality|FullyQualifiedName~VisualOnlyBoundary|FullyQualifiedName~HeadlessPresentationRefactor|FullyQualifiedName~ComponentModeDependency|FullyQualifiedName~RenderModeOwnership"
```

Also run affected catalog/home/search tests if they are not covered by the filter.

Exit criteria:

- [ ] Primitive dependency tests pass.
- [ ] Product Summary semantic tests pass.
- [ ] Product rail tests pass.
- [ ] Visual neutrality tests pass.
- [ ] Render mode ownership tests pass.
- [ ] No architecture exception hides a dependency regression.

## Phase 3.1.14 - Browser QA

Goal: catch real browser regressions caused by moving semantic Product Summary markup.

Use Playwright/browser QA against Storefront V2 after local runtime is available.

Preferred local runner:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

SSR/V2 surfaces to verify:

- [ ] home latest products
- [ ] category listing
- [ ] search result
- [ ] one alternate Product Summary context if present, such as deals/new releases/related products

V2 visual assertions:

- [ ] product title visible
- [ ] category visible when data exists
- [ ] image visible
- [ ] fallback visible for missing/broken image fixture if available
- [ ] badges visible when data exists
- [ ] price visible
- [ ] compare price visible when data exists
- [ ] description visible when data exists
- [ ] direct add/purchase action visible when applicable
- [ ] unavailable state visible when applicable

V2.WASM discounted rail assertions:

- [ ] loading state
- [ ] success state
- [ ] empty state where fixture supports it
- [ ] error/retry state where controllable
- [ ] product rail items render through primitive Product Summary card

Add-to-cart regression:

- [ ] Find a direct-add item.
- [ ] Click `Add to Cart`.
- [ ] Assert exactly one command execution.
- [ ] Assert success UI/label/event still occurs.
- [ ] Assert no duplicate cart line mutation.
- [ ] Assert no console errors.
- [ ] Assert no page errors.
- [ ] Assert no direct Commerce browser request.
- [ ] Assert no node credentials/access tokens appear in browser traffic.
- [ ] Assert no `/_blazor` server UI circuit is used.

Exit criteria:

- [ ] Same primitive works in SSR and WASM contexts.
- [ ] Add-to-cart behavior is unchanged.
- [ ] Browser evidence is recorded in the phase closure notes.

## Phase 3.1.15 - Duplication Removal Audit

Goal: prove Product Summary implementation is not still duplicated.

Run:

```powershell
rg -n "data-storefront-product-summary-card|BrokenImageFallbackScript|data-storefront-product-purchase-submit|Select variant on the product page before adding|Currently out of stock|View Product|Image unavailable" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs -g "!*bin*" -g "!*obj*"
```

Allowed matches:

- [ ] primitive implementation
- [ ] tests
- [ ] V2/V2.WASM label/config values
- [ ] historical docs
- [ ] product detail gallery fallback where unrelated
- [ ] Presentation/browser binders that consume semantic hooks

Not allowed:

- [ ] second Product Summary full card implementation
- [ ] second Product Summary image fallback component
- [ ] second Product Summary purchase-state branching implementation
- [ ] full card markup embedded inside `StorefrontDiscountedProductRailSection`
- [ ] unused pass-through V2 card wrapper

Exit criteria:

- [ ] One Product Summary rendering implementation remains.
- [ ] Old V2 card is deleted if redundant.
- [ ] Old V2.WASM `ProductImage.razor` is deleted if unused.
- [ ] Rail ItemTemplate no longer duplicates card markup.

## Phase 3.1.16 - Scope Drift Audit

Expected changed areas:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Catalog/**` only if minimal label/class contract change is needed
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Catalog/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/*.csproj`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/*.csproj`
- [ ] `BlazorShop.sln`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/**`
- [ ] architecture docs
- [ ] Storefront V2 QA docs

Unexpected unless separately justified:

- [ ] `BlazorShop.Storefront.Presentation`
- [ ] `BlazorShop.Storefront.Runtime`
- [ ] `BlazorShop.Storefront.Client`
- [ ] `BlazorShop.Storefront.Browser`
- [ ] `BlazorShop.Storefront.Starter`
- [ ] StorefrontBuilder tooling
- [ ] Commerce Node
- [ ] Control Plane
- [ ] Application/Domain/Infrastructure
- [ ] database/migrations
- [ ] cart/checkout/account/product detail surfaces

Exit criteria:

- [ ] Any unexpected file is explained or removed from the phase.
- [ ] No Phase 3.2 work enters Phase 3.1.

## Phase 3.1.17 - Documentation Update

Goal: keep source-of-truth docs aligned with the new package.

Update at minimum:

- [ ] `AGENTS.md`
- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/03-runtime-boundaries.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/08-agent-decision-rules.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

Document:

- [ ] `Components.Primitives` is browser-safe render-only Razor.
- [ ] It references `Components` only.
- [ ] It is not a new semantic mode.
- [ ] It must not own final V2 CSS/copy/layout.
- [ ] It must not reference Presentation/Browser/Runtime/Client/backend/host projects.
- [ ] Hybrid remains server/prerendered HTML plus WebAssembly interactivity after hydration.
- [ ] `Components.Ssr` and `Components.WasmHost` remain runtime-specific reusable packages.

QA checklist update:

- [ ] Add Product Summary primitive extraction entry.
- [ ] Record browser QA expectations.
- [ ] Record dependency guardrail expectations.
- [ ] Record add-to-cart browser regression expectation.

Exit criteria:

- [ ] Docs and code graph agree.
- [ ] Future agents know where neutral Razor primitives belong.

## Phase 3.1.18 - Full Verification Gate

Run before closure:

```powershell
dotnet build BlazorShop.sln --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

If existing unrelated warnings/failures appear:

- [ ] record exact command
- [ ] record exact failing test/build output summary
- [ ] prove no Phase 3.1 regression caused it
- [ ] do not hide it behind broad exclusions

Exit criteria:

- [ ] Solution build passes, or unrelated known failure is documented with evidence.
- [ ] Relevant/full tests pass, or unrelated known failure is documented with evidence.
- [ ] Focused browser QA has evidence.

## Phase 3.1.19 - Closure Review

Answer before marking complete:

- [ ] Did `Components.Primitives` stay narrow?
- [ ] Does it reference only `Components`?
- [ ] Did `V2.WASM` avoid `Presentation`?
- [ ] Did `V2.WASM` avoid `Components.Ssr`?
- [ ] Did Product Summary duplication disappear?
- [ ] Did V2 keep final visual/copy ownership?
- [ ] Did V2.WASM keep final visual/copy ownership for rail context?
- [ ] Did WasmHost keep rail runtime/loading/error/retry ownership?
- [ ] Did Presentation keep add-to-cart command execution ownership?
- [ ] Did Hybrid semantics remain unchanged?
- [ ] Did any primitive gain runtime behavior?
- [ ] Did any new component mode or registry appear accidentally?
- [ ] Did Product Summary labels reuse/extend `ProductSummaryLabels` instead of creating duplicate contracts?

Record in closure notes:

- [ ] final project graph
- [ ] new files
- [ ] deleted files
- [ ] changed tests
- [ ] changed docs
- [ ] browser QA evidence
- [ ] focused build/test command results
- [ ] remaining Product Summary duplication if any
- [ ] known debt
- [ ] next candidate phase only after review

Exit criteria:

- [ ] Phase 3.1 can be marked closed.
- [ ] Phase 3.2 is not selected until this review is complete.

## Definition Of Done

Boundary:

- [ ] `BlazorShop.Storefront.Components.Primitives` exists.
- [ ] It uses Razor SDK.
- [ ] It directly references only `BlazorShop.Storefront.Components`.
- [ ] Dependency guardrails enforce this.
- [ ] It is documented as render-only browser-safe primitives.
- [ ] It is not documented as a runtime/component mode.

Product Summary primitives:

- [ ] `StorefrontProductSummaryImage` exists.
- [ ] `StorefrontProductSummaryPurchaseActions` exists.
- [ ] `StorefrontProductSummaryCard` exists.
- [ ] `ProductSummaryItem` remains the single Product Summary item model.
- [ ] Existing `ProductSummaryLabels` is reused or explicitly extended.
- [ ] No duplicate Product Summary label contract is created without evidence.
- [ ] No descriptor is added without a real public inventory use case.

V2:

- [ ] V2 references `Components.Primitives`.
- [ ] V2 Product Summary consumers use primitive card.
- [ ] V2 owns final Product Summary classes.
- [ ] V2 owns final Product Summary copy values.
- [ ] Old V2 Product Summary card implementation is removed if redundant.

V2.WASM:

- [ ] V2.WASM references `Components.Primitives`.
- [ ] V2.WASM does not directly or transitively reference `Presentation`.
- [ ] V2.WASM does not directly or transitively reference `Components.Ssr`.
- [ ] Discounted rail uses primitive Product Summary card.
- [ ] Old `ProductImage.razor` is removed if unused.
- [ ] Duplicate rail ItemTemplate card markup is removed.

WasmHost:

- [ ] Discounted rail remains in `Components.WasmHost`.
- [ ] Browser controller remains in the WasmHost/browser path.
- [ ] Loading/success/empty/error/retry ownership remains unchanged.

Hybrid:

- [ ] Hybrid remains server/prerendered HTML plus InteractiveWebAssembly hydration.
- [ ] No `InteractiveServer` or `InteractiveAuto` is introduced.
- [ ] No server UI circuit is introduced.
- [ ] No physical `Components.Hybrid` project is recreated.

Visual ownership:

- [ ] Primitives contain no final V2 class literals.
- [ ] Primitives contain no CSS/static assets.
- [ ] Class slots are fully dynamic.
- [ ] No oversized visual schema is introduced without evidence.
- [ ] V2/V2.WASM own final visual values.

Browser boundary:

- [ ] Primitives do not call API/BFF.
- [ ] Primitives do not use Browser controller.
- [ ] Primitives do not use `HttpClient`.
- [ ] Primitives do not use `IJSRuntime` in Phase 3.1.
- [ ] Semantic command hooks are preserved.
- [ ] Add-to-cart executes exactly once in browser QA.
- [ ] No direct Commerce browser request appears.
- [ ] No credential leak appears.

Tests and QA:

- [ ] Primitive dependency tests pass.
- [ ] Product Summary component tests pass.
- [ ] Visual neutrality tests pass.
- [ ] Render mode ownership tests pass.
- [ ] V2/V2.WASM dependency graph tests pass.
- [ ] Browser QA passes for SSR Product Summary.
- [ ] Browser QA passes for V2.WASM discounted rail.
- [ ] Browser QA passes for add-to-cart regression.

Scope:

- [ ] No product detail extraction.
- [ ] No cart/checkout/account refactor.
- [ ] No backend/API change.
- [ ] No StorefrontBuilder change.
- [ ] No Starter/generated storefront change.
- [ ] No database/migration change.

## Expected Final Graph

```text
                       Components
                    contracts/headless
                            ^
                            |
                 Components.Primitives
              browser-safe render-only Razor
                    ^               ^
                    |               |
                   V2            V2.WASM
                    |               |
                    |               +-- Browser
                    |               +-- Components.WasmHost
                    |
                    +-- Components.Ssr
                    +-- Presentation
```

Important:

```text
V2.WASM
does not traverse
Components.Primitives -> Presentation
```

because:

```text
Components.Primitives -> Components only
```

## Phase 3 Iteration Rule

After Phase 3.1:

- [ ] Review V2.
- [ ] Review V2.WASM.
- [ ] Review `Components.Primitives`.
- [ ] Identify remaining meaningful reuse boundaries.
- [ ] Select the next batch only after evidence.
- [ ] Prefer 3-5 small components, or 1-3 components if runtime-heavy.
- [ ] Stop Phase 3 when no meaningful reusable boundary remains.

V2 is expected to retain:

- theme layout
- page composition
- CSS
- copy
- store identity
- visual-only sections

The goal is not to empty V2. The goal is to move only reusable rendering/runtime capability into the correct shared boundary.

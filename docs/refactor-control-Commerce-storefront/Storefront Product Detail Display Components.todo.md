# Storefront Product Detail Display Components

Status: planned  
Track: Phase 3 - V2 Component Extraction  
Phase: 3.2  
Predecessor: Phase 3.1 - Product Summary Browser-Safe Primitives  
Successor: choose only after Phase 3.2 closure review  
Scope: plan only; implementation happens in later execution turns

## Decision

Phase 3.2 extracts only Product Detail display regions that have stable prepared input and do not own purchase runtime behavior.

Approved extraction:

- `StorefrontProductGallery` -> `BlazorShop.Storefront.Components.Primitives/Product`
- `StorefrontProductPricing` -> `BlazorShop.Storefront.Components.Ssr/Product`
- `StorefrontProductAvailability` -> `BlazorShop.Storefront.Components.Ssr/Product`
- `StorefrontProductVariantList` -> `BlazorShop.Storefront.Components.Ssr/Product`

Keep in Storefront V2:

- `V2ProductPageView`
- `StorefrontProductPurchasePanel`
- product page layout and section ordering
- Product Detail final classes/copy values
- navigation links
- support callout
- SEO content and related products composition
- `wwwroot/js/storefrontCommerce.js` gallery and purchase progressive enhancement behavior

Important correction from review:

`StorefrontProductGallery` may move to `Components.Primitives` only as a render-only semantic primitive. It must not carry final V2 class literals, store-specific copy, Presentation references, Browser references, Runtime/Client references, API calls, `@rendermode`, or Blazor C# event interaction.

Current gallery behavior is enhanced by V2 script through stable DOM hooks. Phase 3.2 must not move that JS behavior into `Components.Primitives`.

## Current Codebase Facts

Current product page:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor
```

It currently owns inline Product Detail display markup for:

- pricing;
- compare price;
- availability/variant summary;
- SKU;
- GTIN;
- stock badge;
- informational variant list.

It already consumes:

```razor
<StorefrontProductGallery Items="_galleryItems" ProductName="@_product.Name" />
<StorefrontProductPurchasePanel Model="_purchasePanel" Actions="Context.PurchaseActions" />
```

Current gallery:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor
```

It already uses browser-safe `ProductGalleryState` and `ProductGalleryItem`, but it also contains:

- V2/Tailwind class literals;
- `bs-product-gallery*` class names;
- hardcoded `Image unavailable` copy;
- inline broken image fallback behavior;
- `data-storefront-gallery-*` hooks consumed by V2 JS.

Current prepared Product Detail contracts:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageContext.cs
```

Relevant records:

- `StorefrontProductPricingView`
- `StorefrontProductAvailabilityView`
- `StorefrontProductPurchaseView`
- `StorefrontProductVariantView`

Current component project graph:

```text
BlazorShop.Storefront.Components.Primitives
    -> BlazorShop.Storefront.Components

BlazorShop.Storefront.Components.Ssr
    -> BlazorShop.Storefront.Components
    -> BlazorShop.Storefront.Presentation

BlazorShop.Storefront.V2
    -> BlazorShop.Storefront.Components.Primitives
    -> BlazorShop.Storefront.Components.Ssr
    -> BlazorShop.Storefront.Presentation
    -> BlazorShop.Storefront.Browser
    -> BlazorShop.Storefront.V2.WASM
```

Current test facts:

- Existing tests intentionally assert that old `Components/Features/Product/ProductGallery.razor` is gone.
- Existing tests also assert `StorefrontProductGallery` is currently a V2-owned visual template.
- Phase 3.2 must update those tests to the new ownership model instead of adding contradictory guardrails.

## Ownership Rules

`BlazorShop.Storefront.Components` owns:

- browser-safe component contracts;
- `ProductGalleryItem`;
- `ProductGalleryLabels`;
- `ProductGalleryState`;
- future small Product Detail display class/label contracts if needed.

`BlazorShop.Storefront.Components.Primitives` owns:

- narrow render-only Razor primitives;
- semantic DOM hooks;
- accessibility markup;
- host-supplied labels/classes;
- no final CSS/copy/layout ownership;
- no API/BFF/Runtime/Client/Browser behavior.

`BlazorShop.Storefront.Components.Ssr` owns:

- reusable SSR display components over prepared Presentation views;
- semantic hooks;
- accessibility markup;
- host-supplied classes/labels;
- no Browser controller;
- no HTTP/API calls;
- no render-mode ownership.

`BlazorShop.Storefront.V2` owns:

- Product Detail page composition;
- final visual classes;
- final copy values;
- JS progressive enhancement;
- ProductPurchasePanel;
- selection-preview/add-to-cart markup placement.

`BlazorShop.Storefront.Presentation` owns:

- route/page context construction;
- product page mapper;
- same-origin BFF/action descriptors;
- SEO and structured data composition.

## Non-Goals

Do not extract or redesign:

- `StorefrontProductPurchasePanel`
- variant selector/input controls
- quantity selector/input controls
- selection preview behavior
- add-to-cart behavior
- cart
- checkout
- account
- breadcrumb composition
- SEO components
- related product grid
- page shell
- Browser controllers
- Presentation services
- Runtime/Client generated transport
- Commerce Node APIs
- Control Plane APIs
- database/migrations
- Starter
- generated storefronts
- StorefrontBuilder

Do not introduce:

- another component project;
- `Components.Product`;
- `Components.Common`;
- a design system;
- a component registry;
- reflection discovery;
- new component mode enum values;
- route descriptors in reusable display components;
- direct `/api/*` paths in reusable display components.

## Phase 3.2.0 - Baseline Audit

Goal: prove current Product Detail ownership before moving code.

Tasks:

- [x] Confirm Phase 3.1 is closed in code, docs, tests, and QA notes.
- [x] Run and record `git status --short`.
- [x] Read current boundary docs:
  - [x] `AGENTS.md`
  - [x] `docs/architecture/README.md`
  - [x] `docs/architecture/03-runtime-boundaries.md`
  - [x] `docs/architecture/05-project-and-folder-guide.md`
  - [x] `docs/architecture/08-agent-decision-rules.md`
  - [x] `docs/architecture/10-v2-contract-ownership.md`
  - [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
  - [x] `docs/refactor-control-Commerce-storefront/Storefront Product Summary Primitives.todo.md`
  - [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] Inspect:
  - [x] `BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor`
  - [x] `BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor`
  - [x] `BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor`
  - [x] `BlazorShop.Storefront.Presentation/Services/Product/StorefrontProductPageContext.cs`
  - [x] `BlazorShop.Storefront.Components/Contracts/Product/ProductGalleryLabels.cs`
  - [x] `BlazorShop.Storefront.Components/Headless/Product/ProductGalleryState.cs`
  - [x] `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
- [x] Record current semantic hooks:
  - [x] `data-storefront-product-gallery`
  - [x] `data-storefront-gallery-main`
  - [x] `data-storefront-gallery-main-image`
  - [x] `data-storefront-gallery-placeholder`
  - [x] `data-storefront-gallery-controls`
  - [x] `data-storefront-gallery-prev`
  - [x] `data-storefront-gallery-next`
  - [x] `data-storefront-gallery-thumb-viewport`
  - [x] `data-storefront-gallery-thumbnail`
  - [x] `data-storefront-gallery-thumb-fallback`
  - [x] `data-gallery-index`
  - [x] `data-image-url`
  - [x] `data-alt`
  - [x] `data-storefront-selection-price`
  - [x] `data-storefront-selection-compare`
  - [x] `data-storefront-selection-sku`
  - [x] `data-storefront-selection-gtin`
  - [x] `data-storefront-selection-stock`
- [x] Search all current usages:

```powershell
rg -n "StorefrontProductGallery|StorefrontProductPricingView|StorefrontProductAvailabilityView|StorefrontProductVariantView|data-storefront-product-gallery|data-storefront-gallery-|data-storefront-selection-price|data-storefront-selection-compare|data-storefront-selection-sku|data-storefront-selection-gtin|data-storefront-selection-stock" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs -g "!*bin*" -g "!*obj*"
```

- [x] Identify current tests that must change:
  - [x] `StorefrontBrandingMarkupTests.ProductPage_RendersProductImageGalleryComponent`
  - [x] `StorefrontComponentsHeadlessPresentationRefactorTests.ProductGallery_UsesHeadlessStateAndV2VisualTemplateAfterHpr5Migration`
  - [x] `StorefrontPresentationFoundationBoundaryTests.ProductPageVerticalSlice_IsPresentationRouteWithV2ViewOnly`
  - [x] any Product Detail markup tests that expect inline pricing/availability/variant markup directly in V2 page.

Exit criteria:

- [x] Current behavior and ownership are recorded.
- [x] Contradictory legacy guardrails are listed.
- [x] No runtime/purchase behavior enters the extraction scope.


Implementation notes:

- 2026-08-10: Phase 3.1 closure confirmed in `Storefront Product Summary Primitives.todo.md`; no unchecked checklist remains there and closure states Phase 3.1 can be marked closed.
- 2026-08-10: Baseline `git status --short` before Phase 3.2 showed only this plan file as untracked.
- 2026-08-10: Boundary docs and Product Summary closure/QA notes were read before implementation.
- 2026-08-10: Product Detail source audit confirmed `V2ProductPageView.razor` owns inline pricing, availability metadata, and informational variant list markup; V2 also currently owns `StorefrontProductGallery.razor` visual markup while `StorefrontProductPurchasePanel.razor` owns purchase input behavior.
- 2026-08-10: Semantic hooks recorded from source and JS include gallery hooks, thumbnail metadata, and selection preview price/compare/SKU/GTIN/stock hooks. `storefrontCommerce.js` scopes selection updates through `rootElement.closest("main") || document`, so hooks do not need to be direct children of the V2 page component.
- 2026-08-10: Required `rg` search found active Product Detail display ownership in V2 page/gallery, Presentation product page context/mapper, V2 JS selectors, existing guardrail tests, generated client/API contract names, and historical docs. Contradictory tests to update are `StorefrontBrandingMarkupTests.ProductPage_RendersProductImageGalleryComponent`, `StorefrontComponentsHeadlessPresentationRefactorTests.ProductGallery_UsesHeadlessStateAndV2VisualTemplateAfterHpr5Migration`, and `StorefrontPresentationFoundationBoundaryTests.ProductPageVerticalSlice_IsPresentationRouteWithV2ViewOnly`.
- 2026-08-10: No runtime/purchase behavior is entering extraction scope; `StorefrontProductPurchasePanel` remains V2-owned.

## Phase 3.2.1 - Add Product Gallery Visual Contracts

Goal: make `StorefrontProductGallery` portable without carrying V2 final classes/copy.

Existing contract:

```text
BlazorShop.Storefront.Components/Contracts/Product/ProductGalleryLabels.cs
```

Tasks:

- [ ] Reuse `ProductGalleryLabels`; do not create duplicate gallery label models.
- [ ] Confirm existing fields cover:
  - [ ] unavailable image text;
  - [ ] unavailable image alt format;
  - [ ] previous image aria label;
  - [ ] next image aria label;
  - [ ] product image region label;
  - [ ] image button aria format.
- [ ] Add `ProductGalleryClasses` under:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductGalleryClasses.cs
```

- [ ] Keep class slots minimal and semantic. Candidate slots:
  - [ ] `Root`
  - [ ] `Main`
  - [ ] `MainImage`
  - [ ] `Placeholder`
  - [ ] `Controls`
  - [ ] `PreviousButton`
  - [ ] `NextButton`
  - [ ] `Icon`
  - [ ] `ThumbnailViewport`
  - [ ] `Thumbnail`
  - [ ] `ThumbnailImage`
  - [ ] `ThumbnailFallback`
- [ ] Reduce slots if implementation can reuse fewer host classes.
- [ ] Do not add V2 class values to `Components`.
- [ ] Do not add route/action/API descriptors.

Exit criteria:

- [ ] `ProductGalleryLabels` is reused.
- [ ] `ProductGalleryClasses` is browser-safe and presentation-only.
- [ ] No duplicate `ProductGallery*Label` contract is created.

## Phase 3.2.2 - Extract StorefrontProductGallery Primitive

Goal: move gallery render markup to `Components.Primitives` while V2 remains visual/behavior host.

Move target:

```text
from:
BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor

to:
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Product/StorefrontProductGallery.razor
```

Namespace:

```text
BlazorShop.Storefront.Components.Primitives.Product
```

Inputs:

- [ ] `IReadOnlyList<ProductGalleryItem> Items`
- [ ] `string? ProductName`
- [ ] `ProductGalleryLabels Labels`
- [ ] `ProductGalleryClasses Classes`

Preserve behavior/markup semantics:

- [ ] main image;
- [ ] missing image fallback;
- [ ] broken image fallback behavior consistent with existing Product Summary primitive fallback policy;
- [ ] previous/next controls;
- [ ] thumbnails;
- [ ] selected thumbnail semantics;
- [ ] `aria-label`;
- [ ] `aria-current`;
- [ ] `aria-selected`;
- [ ] `aria-disabled`;
- [ ] `data-selected`;
- [ ] all gallery data hooks listed in Phase 3.2.0.

Must remove from primitive:

- [ ] V2/Tailwind class literals;
- [ ] `bs-product-gallery*` class ownership unless supplied through `ProductGalleryClasses`;
- [ ] hardcoded `Image unavailable`;
- [ ] host-specific copy;
- [ ] any `@rendermode`;
- [ ] any `@onclick`/Blazor C# event interaction;
- [ ] any `IJSRuntime`;
- [ ] any `HttpClient`;
- [ ] any Browser/Presentation/Runtime/Client/backend reference.

Allowed:

- [ ] `ProductGalleryState.Create(Items, ProductName)`;
- [ ] semantic `data-storefront-*` hooks;
- [ ] host-supplied class slots;
- [ ] host-supplied labels;
- [ ] the same narrow inline image fallback pattern already allowed by Product Summary primitive, unless architecture tests are deliberately tightened in the same phase.

Important:

- [ ] Do not move gallery click/keyboard JS into `Components.Primitives`.
- [ ] Do not add a JS file to `Components.Primitives`.
- [ ] V2 `storefrontCommerce.js` remains responsible for progressive enhancement through existing hooks.
- [ ] If the primitive cannot satisfy visual-neutrality tests, stop and split the work:
  - [ ] move only static image/fallback/thumbnail primitive now;
  - [ ] keep interactive gallery shell in V2 until a separate decision.

Exit criteria:

- [ ] Old V2 gallery implementation is removed.
- [ ] Primitive gallery builds.
- [ ] V2 consumes the primitive gallery.
- [ ] Primitive dependency and visual-neutrality tests pass.

## Phase 3.2.3 - Add V2 Product Gallery Visuals

Goal: keep final Product Detail gallery classes/copy in V2 after primitive extraction.

Add V2 visual configuration, likely:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/ProductGalleryVisuals.cs
```

Tasks:

- [ ] Provide `ProductGalleryLabels`.
- [ ] Provide `ProductGalleryClasses`.
- [ ] Preserve current 1:1 visual intent:
  - [ ] main image aspect square;
  - [ ] thumbnail square cells;
  - [ ] no-image placeholder square frame.
- [ ] Preserve current gallery class semantics required by CSS/JS:
  - [ ] if `storefront.css` targets `bs-product-gallery*`, keep those values in V2 class config;
  - [ ] do not bake those values into the primitive.
- [ ] Update V2 `_Imports.razor`:
  - [ ] add `BlazorShop.Storefront.Components.Primitives.Product`;
  - [ ] keep `BlazorShop.Storefront.V2.Components.Product` only for V2-owned Product components.
- [ ] Update `V2ProductPageView.razor` gallery call:

```razor
<StorefrontProductGallery
    Items="_galleryItems"
    ProductName="@_product.Name"
    Labels="ProductGalleryVisuals.Labels"
    Classes="ProductGalleryVisuals.Classes" />
```

Exit criteria:

- [ ] V2 owns final gallery labels/classes.
- [ ] Primitive owns only semantic render markup.
- [ ] Product gallery visual behavior remains unchanged.

## Phase 3.2.4 - Add Product Detail Display Visual Contracts

Goal: provide minimal class/label contracts for SSR display components without creating a design system.

Add only if needed:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductPricingClasses.cs
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductAvailabilityClasses.cs
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductVariantListClasses.cs
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductVariantListLabels.cs
```

Preferred simplification:

- [ ] If one small record is enough, use `ProductDetailDisplayClasses`.
- [ ] If separate records reduce confusion, keep each record small.
- [ ] Do not add a single giant class bag for the full Product Detail page.
- [ ] Do not create one class slot for every nested span without real styling need.

Required copy handling:

- [ ] `Available Variants` must be host supplied.
- [ ] No broad localization framework.
- [ ] No Storefront V2 copy in reusable component projects.

Exit criteria:

- [ ] SSR components have enough class/label input to avoid literal class/copy ownership.
- [ ] Contracts remain small and render-facing only.

## Phase 3.2.5 - Extract StorefrontProductPricing

Goal: remove inline pricing block from `V2ProductPageView.razor`.

Target:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Product/StorefrontProductPricing.razor
```

Namespace:

```text
BlazorShop.Storefront.Components.Ssr.Product
```

Input:

```text
StorefrontProductPricingView Model
```

Responsibilities:

- [ ] render `PrimaryPriceLabel`;
- [ ] render `PriceDisplay`;
- [ ] render `ComparePriceDisplay` when present;
- [ ] hide compare-price region when absent;
- [ ] preserve:
  - [ ] `data-storefront-selection-price`
  - [ ] `data-storefront-selection-compare`

Forbidden:

- [ ] Browser dependency;
- [ ] API calls;
- [ ] generated client usage;
- [ ] V2 references;
- [ ] hardcoded final V2 classes;
- [ ] final customer copy outside the prepared view.

Exit criteria:

- [ ] `V2ProductPageView.razor` no longer owns pricing markup.
- [ ] selection-preview JS can still update price and compare-price hooks.

## Phase 3.2.6 - Extract StorefrontProductAvailability

Goal: remove inline variant summary/SKU/GTIN/stock block from `V2ProductPageView.razor`.

Target:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Product/StorefrontProductAvailability.razor
```

Inputs:

```text
StorefrontProductAvailabilityView Availability
StorefrontProductPurchaseView Purchase
```

Responsibilities:

- [ ] render variant summary;
- [ ] render SKU when present;
- [ ] render GTIN when present;
- [ ] render stock label;
- [ ] derive semantic availability state only for class selection;
- [ ] preserve:
  - [ ] `data-storefront-selection-sku`
  - [ ] `data-storefront-selection-gtin`
  - [ ] `data-storefront-selection-stock`

Forbidden:

- [ ] purchase option selection;
- [ ] quantity selection;
- [ ] add-to-cart state;
- [ ] selection-preview request payload construction;
- [ ] hardcoded final V2 colors/classes.

Exit criteria:

- [ ] `V2ProductPageView.razor` no longer owns availability metadata markup.
- [ ] selection-preview JS can still update SKU/GTIN/stock hooks.

## Phase 3.2.7 - Extract StorefrontProductVariantList

Goal: extract informational variant list only, not purchase selection.

Target:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Product/StorefrontProductVariantList.razor
```

Input:

```text
IReadOnlyList<StorefrontProductVariantView> Items
```

Responsibilities:

- [ ] render nothing or an empty fragment when no variants exist;
- [ ] render section heading from host-supplied labels;
- [ ] render variant display name;
- [ ] render attribute text;
- [ ] render price display;
- [ ] render stock label;
- [ ] use semantic state only to choose host-supplied class slots.

Must not become:

- [ ] variant selector;
- [ ] radio/select/color input owner;
- [ ] selected variant state owner;
- [ ] purchase validation owner;
- [ ] add-to-cart eligibility owner.

Exit criteria:

- [ ] `V2ProductPageView.razor` no longer owns informational variant-list markup.
- [ ] `StorefrontProductPurchasePanel` remains the only Product Detail purchase input surface in this phase.

## Phase 3.2.8 - Adopt Display Components In V2ProductPageView

Goal: make Product Detail page thinner while keeping V2 composition ownership.

Expected shape:

```razor
<StorefrontPageShell ...>
    <Breadcrumb>
        <BreadcrumbNav Items="_breadcrumbs" />
    </Breadcrumb>

    <ChildContent>
        <div class="... product layout ...">
            <div class="... gallery shell ...">
                <StorefrontProductGallery ... />
            </div>

            <div class="... details shell ...">
                category/fresh/name
                <StorefrontProductPricing ... />
                <StorefrontProductAvailability ... />
                description
                <StorefrontProductPurchasePanel ... />
                <StorefrontProductVariantList ... />
                navigation
                support callout
            </div>
        </div>
    </ChildContent>
</StorefrontPageShell>
```

V2 page must still own:

- [ ] `StorefrontPageShell`;
- [ ] breadcrumb slot;
- [ ] product layout grid;
- [ ] gallery/details cards;
- [ ] category link/title/new badge;
- [ ] product description placement;
- [ ] `StorefrontProductPurchasePanel`;
- [ ] navigation buttons;
- [ ] support callout;
- [ ] SEO content section;
- [ ] related products section.

Exit criteria:

- [ ] all four extracted components are consumed.
- [ ] page remains a composition view, not an empty pass-through shell.
- [ ] Product Detail route/page services are unchanged.

## Phase 3.2.9 - Update Existing Guardrail Tests

Goal: remove contradictions between old V2-owned-gallery guardrails and new primitive ownership.

Update tests that currently read:

```text
BlazorShop.Storefront.V2/Components/Product/StorefrontProductGallery.razor
```

Expected changes:

- [ ] `StorefrontBrandingMarkupTests.ProductPage_RendersProductImageGalleryComponent`
  - [ ] read primitive gallery file instead of V2 gallery file;
  - [ ] assert V2 page consumes primitive gallery with labels/classes;
  - [ ] assert V2 visual config contains V2 class/copy values;
  - [ ] keep CSS/1:1 expectations against V2 stylesheet/config where appropriate.
- [ ] `StorefrontComponentsHeadlessPresentationRefactorTests.ProductGallery_UsesHeadlessStateAndV2VisualTemplateAfterHpr5Migration`
  - [ ] rename test to reflect new primitive ownership;
  - [ ] keep assertion that old `Components/Features/Product/ProductGallery.razor` is absent;
  - [ ] assert `ProductGalleryState` remains in `Components.Headless.Product`;
  - [ ] assert primitive consumes `ProductGalleryState`;
  - [ ] assert V2 owns visual config, not gallery implementation.
- [ ] `StorefrontPresentationFoundationBoundaryTests.ProductPageVerticalSlice_IsPresentationRouteWithV2ViewOnly`
  - [ ] keep route/service/mapper assertions;
  - [ ] keep V2 page as view-only composition;
  - [ ] allow V2 page to consume reusable primitive/SSR display components.
- [ ] Update tests expecting selection hooks directly in V2 page:
  - [ ] new expectation: hooks appear in extracted SSR component files;
  - [ ] V2 page composes those components.

Exit criteria:

- [ ] No test still requires Product Detail gallery implementation to live in V2.
- [ ] Tests still prove Product Detail remains a Presentation route with V2 visual composition.

## Phase 3.2.10 - Add Product Detail Component Tests

Goal: cover render semantics without snapshot brittleness.

Add focused tests under:

```text
BlazorShop.Tests.V2/PresentationV2/Storefront/
```

Candidate test classes:

```text
StorefrontProductGalleryPrimitiveTests
StorefrontProductDetailDisplayComponentTests
```

Gallery tests:

- [ ] no images renders fallback;
- [ ] one image renders main image and no controls/thumbnails;
- [ ] multiple images render controls and thumbnails;
- [ ] selected thumbnail state is present on index 0 initially;
- [ ] labels are host supplied;
- [ ] classes are host supplied;
- [ ] all semantic hooks are present;
- [ ] no V2 class literal is required;
- [ ] broken-image fallback marker exists according to approved primitive policy.

Pricing tests:

- [ ] label and price render;
- [ ] compare price renders when present;
- [ ] compare price is hidden/empty when absent;
- [ ] price/compare semantic hooks are present.

Availability tests:

- [ ] variant summary renders;
- [ ] SKU hidden when blank;
- [ ] SKU shown when present;
- [ ] GTIN hidden when blank;
- [ ] GTIN shown when present;
- [ ] stock label renders;
- [ ] stock hook is present;
- [ ] availability state only affects host class selection.

Variant list tests:

- [ ] empty list renders no section;
- [ ] one variant renders;
- [ ] multiple variants render;
- [ ] attribute text renders;
- [ ] price display renders;
- [ ] stock label renders;
- [ ] host-supplied section label renders.

Avoid:

- [ ] pixel snapshots;
- [ ] whitespace snapshots;
- [ ] tests that enforce exact Tailwind order;
- [ ] tests that require purchase behavior setup.

Exit criteria:

- [ ] component render semantics are covered.
- [ ] purchase runtime behavior is not coupled to component tests.

## Phase 3.2.11 - Architecture Guardrail Updates

Goal: keep the extraction from opening forbidden dependency paths.

Primitive guardrails:

- [ ] `Components.Primitives` still references exactly `Components`.
- [ ] `Components.Primitives` source still forbids:
  - [ ] `BlazorShop.Storefront.Presentation`
  - [ ] `BlazorShop.Storefront.Browser`
  - [ ] `BlazorShop.Storefront.Runtime`
  - [ ] `BlazorShop.Storefront.Client`
  - [ ] `BlazorShop.Storefront.Components.Ssr`
  - [ ] `BlazorShop.Storefront.Components.WasmHost`
  - [ ] V2/V2.WASM/Starter/generated storefronts
  - [ ] backend/core/API projects
  - [ ] `Web.SharedV2`
  - [ ] `HttpClient`
  - [ ] `IJSRuntime`
  - [ ] `@rendermode`
  - [ ] `InteractiveServer`
  - [ ] `InteractiveAuto`
  - [ ] `InteractiveWebAssembly`
  - [ ] direct API path strings

SSR guardrails:

- [ ] `Components.Ssr` remains allowed to reference `Components` and `Presentation`.
- [ ] `Components.Ssr` still forbids:
  - [ ] Browser;
  - [ ] Runtime;
  - [ ] Client;
  - [ ] V2;
  - [ ] V2.WASM;
  - [ ] Starter;
  - [ ] backend/core/API projects;
  - [ ] `Web.SharedV2`;
  - [ ] `HttpClient`;
  - [ ] `IJSRuntime`;
  - [ ] `@rendermode`;
  - [ ] direct API path strings.

Visual neutrality:

- [ ] Include new `Components.Primitives/Product/*.razor`.
- [ ] Include new `Components.Ssr/Product/*.razor`.
- [ ] No literal class attributes in reusable render projects except fully dynamic class slots.
- [ ] No final copy strings such as `Available Variants` or `Image unavailable` in reusable component project files.

Render-mode ownership:

- [ ] All extracted components contain no `@rendermode`.
- [ ] Host/composition remains the only render-mode owner.

Exit criteria:

- [ ] Existing guardrails pass after updating expected ownership.
- [ ] No broad allowlist exception is added to make tests pass.

## Phase 3.2.12 - Product Detail JS Hook Regression Tests

Goal: prove extraction did not break existing V2 JS behavior.

Existing JS:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js
```

It reads:

- gallery hooks;
- selection price hook;
- compare price hook;
- stock hook;
- SKU hook;
- GTIN hook;
- purchase panel hooks.

Tasks:

- [ ] Keep existing JS selectors unchanged.
- [ ] Add/update static JS tests to prove:
  - [ ] gallery selector strings still exist;
  - [ ] selection selector strings still exist;
  - [ ] JS does not assume hooks are direct children of `V2ProductPageView`;
  - [ ] JS scopes selection updates to the correct purchase/product root where currently intended.
- [ ] If extraction changes DOM nesting, verify selectors still resolve at runtime.

Exit criteria:

- [ ] Gallery next/previous/thumbnail behavior still works.
- [ ] Product selection preview still updates price/compare/SKU/GTIN/stock.
- [ ] Add-to-cart still posts the selected variant/attributes/quantity.

## Phase 3.2.13 - Focused Build Gate

Run after implementation:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Also build V2.WASM if a contract touched by browser graph changes:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
```

Exit criteria:

- [ ] focused builds pass;
- [ ] no new warning is introduced by Phase 3.2;
- [ ] any unrelated existing warning is recorded with exact command and summary.

## Phase 3.2.14 - Focused Test Gate

Run relevant tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontProductGalleryPrimitiveTests|FullyQualifiedName~StorefrontProductDetailDisplayComponentTests|FullyQualifiedName~StorefrontPrimitiveDependencyTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontRenderModeOwnershipTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests"
```

If filter syntax is too long for the shell, split by test class.

Exit criteria:

- [ ] focused tests pass;
- [ ] updated old guardrails no longer contradict new ownership;
- [ ] no purchase behavior regression is hidden.

## Phase 3.2.15 - Browser QA

Goal: catch real browser regressions on Product Detail after display extraction.

Start local runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Primary product route:

- [ ] choose a fixture product with multiple gallery images and variants;
- [ ] choose a fixture product with compare price when available;
- [ ] choose an out-of-stock or purchase-disabled product if fixture exists.

Product Detail visual/semantic assertions:

- [ ] route returns HTTP 200;
- [ ] product name visible;
- [ ] category label/link visible when available;
- [ ] main gallery frame is 1:1;
- [ ] thumbnail cells are 1:1;
- [ ] gallery has no visible counter regression;
- [ ] gallery placeholder remains hidden when image loads;
- [ ] price visible;
- [ ] compare price visible where applicable;
- [ ] SKU/GTIN/stock visible where applicable;
- [ ] variant list renders when variants exist;
- [ ] ProductPurchasePanel still renders;
- [ ] related products still render or empty state remains acceptable.

Gallery interaction assertions:

- [ ] next button changes main image;
- [ ] previous button changes main image;
- [ ] boundary disabled states are correct;
- [ ] thumbnail click changes main image;
- [ ] selected thumbnail stays in viewport;
- [ ] ArrowRight/ArrowLeft work on thumbnails;
- [ ] broken main image fallback can still appear;
- [ ] broken thumbnail fallback can still appear.

Selection-preview assertions:

- [ ] changing variant/attribute triggers same-origin selection preview request;
- [ ] `data-storefront-selection-price` updates;
- [ ] `data-storefront-selection-compare` updates/hides correctly;
- [ ] `data-storefront-selection-sku` updates/hides correctly;
- [ ] `data-storefront-selection-gtin` updates/hides correctly;
- [ ] `data-storefront-selection-stock` updates;
- [ ] main gallery image updates when preview returns a new image URL.

Add-to-cart assertions:

- [ ] selected variant/attributes are posted;
- [ ] quantity is posted;
- [ ] add-to-cart succeeds for valid item;
- [ ] disabled/purchase-blocked item cannot be submitted;
- [ ] exactly one cart mutation is sent per click;
- [ ] no duplicate event handling;
- [ ] button/feedback state still updates;
- [ ] cart badge/summary behavior remains unchanged if visible.

Browser security/network assertions:

- [ ] no console errors;
- [ ] no page errors;
- [ ] no direct browser request to `api/storefront/stores`;
- [ ] no direct browser request to `api/commerce`;
- [ ] no direct browser request to `api/control-plane`;
- [ ] no node credentials/access tokens in browser traffic;
- [ ] no public `/_blazor` server circuit request.

Stop runtime:

```powershell
.\scripts\stop-v2-local.ps1
```

Exit criteria:

- [ ] Product Detail remains functionally usable.
- [ ] Gallery and purchase enhancement still work.
- [ ] Browser evidence path is recorded in closure notes.

## Phase 3.2.16 - Duplication And Markup Reduction Audit

Goal: prove Product Detail display implementation is not still duplicated.

Run:

```powershell
rg -n "StorefrontProductGallery|StorefrontProductPricing|StorefrontProductAvailability|StorefrontProductVariantList|data-storefront-selection-price|data-storefront-selection-compare|data-storefront-selection-sku|data-storefront-selection-gtin|data-storefront-selection-stock|data-storefront-product-gallery|data-storefront-gallery-" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs -g "!*bin*" -g "!*obj*"
```

Allowed matches:

- [ ] primitive gallery implementation;
- [ ] SSR display component implementations;
- [ ] V2 page component usage;
- [ ] V2 visual config;
- [ ] V2 JS selectors;
- [ ] tests;
- [ ] docs/QA notes.

Not allowed:

- [ ] old V2 gallery implementation file still present;
- [ ] inline pricing implementation still in `V2ProductPageView.razor`;
- [ ] inline availability/SKU/GTIN/stock implementation still in `V2ProductPageView.razor`;
- [ ] inline informational variant list implementation still in `V2ProductPageView.razor`;
- [ ] duplicated Product Detail display contracts;
- [ ] reusable component files with final V2 class literals.

Exit criteria:

- [ ] Product Detail page is materially thinner.
- [ ] No duplicate extracted implementation remains.

## Phase 3.2.17 - Scope Drift Audit

Expected changed areas:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Product/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Product/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/**`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/**`
- [ ] `BlazorShop.Tests.V2/Architecture/**` only for ownership assertion updates
- [ ] architecture docs
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [ ] this plan file

Unexpected unless separately justified:

- [ ] `BlazorShop.Storefront.Presentation` behavior/service code;
- [ ] `BlazorShop.Storefront.Runtime`;
- [ ] `BlazorShop.Storefront.Client`;
- [ ] `BlazorShop.Storefront.Browser`;
- [ ] `BlazorShop.Storefront.V2.WASM`;
- [ ] `BlazorShop.Storefront.Starter`;
- [ ] StorefrontBuilder tooling;
- [ ] Commerce Node;
- [ ] Control Plane;
- [ ] Application/Domain/Infrastructure;
- [ ] database/migrations;
- [ ] cart/checkout/account source;
- [ ] product purchase behavior source.

Exit criteria:

- [ ] any unexpected file is explained or removed;
- [ ] Phase remains Product Detail display extraction only.

## Phase 3.2.18 - Documentation Update

Goal: keep source-of-truth docs aligned with the new component locations.

Review and update as needed:

- [ ] `AGENTS.md`
- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [ ] this plan file

Document:

- [ ] `StorefrontProductGallery` is now a `Components.Primitives/Product` render-only primitive.
- [ ] V2 owns Product Gallery final classes/copy and JS progressive enhancement.
- [ ] `StorefrontProductPricing`, `StorefrontProductAvailability`, and `StorefrontProductVariantList` are `Components.Ssr/Product` display components over prepared Presentation views.
- [ ] ProductPurchasePanel remains V2-owned and out of Phase 3.2.
- [ ] Existing historical note that gallery returned to V2 after `Features` removal is superseded only because `Components.Primitives` now exists and is render-only, not a shared visual wrapper.

QA checklist update:

- [ ] add Product Detail display component extraction item;
- [ ] record Product Detail browser QA expectations;
- [ ] record gallery primitive dependency/visual-neutrality expectations;
- [ ] record selection-preview and add-to-cart regression expectations.

Exit criteria:

- [ ] docs and code graph agree;
- [ ] future agents know why this does not reintroduce retired `Features` visual wrappers.

## Phase 3.2.19 - Full Verification Gate

Run before closure:

```powershell
dotnet build BlazorShop.sln --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

If existing unrelated warnings/failures appear:

- [ ] record exact command;
- [ ] record exact warning/failure summary;
- [ ] prove no Phase 3.2 regression caused it;
- [ ] do not hide failures behind broad exclusions.

Exit criteria:

- [ ] full solution build passes, or unrelated known issue is documented with evidence;
- [ ] full V2 tests pass, or unrelated known issue is documented with evidence;
- [ ] browser QA evidence is recorded.

## Phase 3.2.20 - Closure Review

Answer before marking complete:

- [ ] Did `StorefrontProductGallery` move to `Components.Primitives/Product`?
- [ ] Did gallery stay `Components`-only?
- [ ] Did gallery avoid Presentation/Browser/Runtime/Client/backend references?
- [ ] Did gallery avoid final V2 class/copy ownership?
- [ ] Did V2 keep gallery final labels/classes?
- [ ] Did V2 keep gallery JS progressive enhancement?
- [ ] Did `StorefrontProductPricing` move to `Components.Ssr/Product`?
- [ ] Did `StorefrontProductAvailability` move to `Components.Ssr/Product`?
- [ ] Did `StorefrontProductVariantList` move to `Components.Ssr/Product`?
- [ ] Did SSR display components consume prepared Presentation views only?
- [ ] Did ProductPurchasePanel remain untouched except import/call-site compatibility if needed?
- [ ] Did V2ProductPageView remain the page composition owner?
- [ ] Did semantic hooks remain stable?
- [ ] Did selection-preview still update price/compare/SKU/GTIN/stock?
- [ ] Did add-to-cart still work?
- [ ] Did browser network guardrails pass?
- [ ] Did no new component mode/project/registry appear?

Record in closure notes:

- [ ] final project graph;
- [ ] new files;
- [ ] moved/deleted files;
- [ ] changed tests;
- [ ] changed docs;
- [ ] focused build/test command results;
- [ ] full build/test command results;
- [ ] browser QA evidence;
- [ ] remaining Product Detail inline markup;
- [ ] known visual debt;
- [ ] next candidate phase only after fresh review.

Exit criteria:

- [ ] Phase 3.2 can be marked closed.
- [ ] Phase 3.3 is not selected until closure review is complete.

## Definition Of Done

Gallery:

- [ ] `StorefrontProductGallery` lives in `Components.Primitives/Product`.
- [ ] It depends only on `BlazorShop.Storefront.Components`.
- [ ] It uses `ProductGalleryItem`, `ProductGalleryLabels`, `ProductGalleryClasses`, and `ProductGalleryState`.
- [ ] It has no Presentation/Browser/Runtime/Client/backend dependency.
- [ ] It has no `@rendermode`.
- [ ] It has no `IJSRuntime`.
- [ ] It has no `HttpClient`.
- [ ] It has no final V2 class literals.
- [ ] It has no hardcoded V2 copy.
- [ ] V2 supplies labels/classes.
- [ ] V2 JS still enhances gallery through stable hooks.
- [ ] Old V2 gallery implementation is removed.

Pricing:

- [ ] `StorefrontProductPricing` lives in `Components.Ssr/Product`.
- [ ] It consumes `StorefrontProductPricingView`.
- [ ] It preserves price/compare semantic hooks.
- [ ] It has no Browser/Runtime/Client/V2/backend dependency.
- [ ] V2 supplies final classes.

Availability:

- [ ] `StorefrontProductAvailability` lives in `Components.Ssr/Product`.
- [ ] It consumes prepared Presentation views.
- [ ] It preserves SKU/GTIN/stock semantic hooks.
- [ ] It does not own purchase selection.
- [ ] V2 supplies final classes.

Variant list:

- [ ] `StorefrontProductVariantList` lives in `Components.Ssr/Product`.
- [ ] It is informational only.
- [ ] It does not render selection inputs.
- [ ] It does not affect purchase payload.
- [ ] V2 supplies final classes/copy.

V2 page:

- [ ] `V2ProductPageView` consumes all four extracted components.
- [ ] `V2ProductPageView` still owns outer layout and section order.
- [ ] `StorefrontProductPurchasePanel` remains V2-owned.
- [ ] navigation/support/SEO/related products remain V2-owned.

Architecture:

- [ ] `Components.Primitives` remains `Components`-only.
- [ ] `Components.Ssr` remains Browser-free.
- [ ] no extracted component owns render mode.
- [ ] no reusable component owns final V2 CSS/copy/layout.
- [ ] no new component mode/project/registry is introduced.

Tests and QA:

- [ ] primitive dependency tests pass.
- [ ] component mode dependency tests pass.
- [ ] visual neutrality tests pass.
- [ ] render-mode ownership tests pass.
- [ ] Product Detail display component tests pass.
- [ ] old contradictory gallery ownership tests are updated.
- [ ] focused browser QA passes.
- [ ] add-to-cart regression passes.
- [ ] no direct Commerce browser request appears.

Scope:

- [ ] no ProductPurchasePanel extraction.
- [ ] no cart/checkout/account work.
- [ ] no backend/API change.
- [ ] no database/migration change.
- [ ] no StorefrontBuilder/Starter/generated-storefront change.

## Expected Final Graph

```text
Components
  Contracts/Product
    ProductGalleryItem
    ProductGalleryLabels
    ProductGalleryClasses
  Headless/Product
    ProductGalleryState
        ^
        |
Components.Primitives
  Product/StorefrontProductGallery
        ^
        |
Storefront.V2
  ProductGalleryVisuals
  V2ProductPageView
  storefrontCommerce.js

Presentation
  StorefrontProductPageContext
    StorefrontProductPricingView
    StorefrontProductAvailabilityView
    StorefrontProductPurchaseView
    StorefrontProductVariantView
        ^
        |
Components.Ssr
  Product/StorefrontProductPricing
  Product/StorefrontProductAvailability
  Product/StorefrontProductVariantList
        ^
        |
Storefront.V2
  V2ProductPageView
```

Forbidden graph:

```text
Components.Primitives -> Presentation
Components.Primitives -> Browser
Components.Primitives -> Runtime
Components.Primitives -> Client
Components.Primitives -> V2
Components.Primitives -> backend/core/API

Components.Ssr -> Browser
Components.Ssr -> Runtime
Components.Ssr -> Client
Components.Ssr -> V2
Components.Ssr -> backend/core/API
```

## Suggested Commit Breakdown

```text
docs(storefront): plan product detail display extraction

refactor(storefront): add product gallery primitive contracts

refactor(storefront): extract product gallery primitive

refactor(storefront): add v2 product gallery visuals

refactor(storefront): extract product pricing display

refactor(storefront): extract product availability display

refactor(storefront): extract product variant list

refactor(storefront): adopt product detail display components

test(storefront): update product detail ownership guardrails

test(storefront): cover product detail display components

docs(storefront): record product detail display extraction qa
```

## Phase 3 Continuation Rule

Phase 3 remains extraction-first.

Before selecting Phase 3.3:

- [ ] close Phase 3.2 completely;
- [ ] review V2 and V2.WASM fresh;
- [ ] identify remaining meaningful reuse boundaries;
- [ ] avoid visual polish until extraction batches are complete;
- [ ] keep each batch small enough for one implementation/review/QA loop.

Deferred until final V2 visual sweep:

- spacing normalization;
- class naming cleanup;
- responsive tuning;
- pixel-level visual parity;
- product page visual redesign;
- broad CSS consolidation.

The goal is not to empty V2. The goal is to move only reusable render/display capability into the correct shared boundary while V2 remains the final visual host.

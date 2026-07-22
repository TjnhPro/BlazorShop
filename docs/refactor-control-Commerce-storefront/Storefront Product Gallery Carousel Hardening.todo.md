# Storefront Product Gallery Carousel Hardening Todo

## Scope

This plan hardens `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` product image gallery behavior after the Product Image Gallery phase.

User-observed problem:

- Adding more images makes the gallery thumbnail boxes smaller.
- The gallery can visually degrade into tiny dots instead of fixed image cells.
- The product detail experience needs two navigation buttons to move images backward/forward.

Primary target:

- Keep the main product image box stable regardless of gallery item count.
- Keep thumbnail cells fixed and scrollable.
- Add previous/next product-image navigation controls.
- Follow Smartstore's product gallery principles without copying Smartstore code or introducing Slick/jQuery into Storefront V2.

Non-goals:

- Do not change Commerce Node media storage, media import, imgproxy, or public media route semantics.
- Do not add Smartstore runtime references.
- Do not introduce a full third-party carousel library for MVP.
- Do not change product API contracts unless current DTOs prove insufficient during implementation.
- Do not redesign the full product detail page beyond the gallery surface.

## Current Findings

- Storefront V2 product detail uses `ProductImageGallery.razor`.
- `storefrontCommerce.js` already has thumbnail click behavior through `selectGalleryThumbnail`.
- `storefront.css` now owns gallery frame dimensions after the dot/collapse investigation.
- The current gallery does not have first-class carousel state, prev/next controls, disabled states, or thumbnail scroll synchronization.
- Tailwind utility output can drift from Razor class usage because Storefront V2 `BlazorShop.Storefront.V2.csproj` does not run the Storefront Tailwind build automatically.
- Smartstore's gallery is split into a stable main media viewport and a separate thumbnail navigation surface.

## Smartstore Reference Summary

Files inspected:

- `Smartstore/src/Smartstore.Web/Views/Shared/Partials/MediaGallery.cshtml`
- `Smartstore/src/Smartstore.Web/Themes/Flex/wwwroot/_gallery.scss`
- `Smartstore/src/Smartstore.Web/wwwroot/skinning/_slick.scss`
- `Smartstore/src/Smartstore.Web/wwwroot/js/smartstore.gallery.js`
- `Smartstore/src/Smartstore.Web/wwwroot/js/public.product.js`

Smartstore behavior to adapt:

- Main gallery renders one selected media item at a time.
- Main media viewport remains square and stable.
- Thumbnail navigation is structurally separate from the main image.
- Thumbnail navigation has a fixed visible region and scrolls/slides when item count exceeds the visible capacity.
- Navigation controls are disabled at boundaries.
- Selecting a thumbnail synchronizes the main image and selected thumbnail.
- The gallery keeps accessibility roles and selected state.

Smartstore behavior not to copy:

- Slick carousel dependency.
- jQuery plugin architecture.
- PhotoSwipe/zoom integration for this MVP.
- Table-cell layout mechanics.

## Recommended Direction

Build a lightweight app-owned Storefront V2 gallery controller:

- Razor owns semantic markup and data attributes.
- CSS owns stable dimensions and responsive layout.
- `storefrontCommerce.js` owns progressive enhancement for click, previous, next, keyboard, disabled state, and scroll synchronization.
- Server-rendered fallback still shows the first image and thumbnail list before JavaScript loads.

Reasoning:

- This matches the current Storefront V2 architecture: SSR-first Razor with small app-owned JavaScript.
- It avoids adding Slick/jQuery to a V2 storefront that has already moved away from legacy presentation dependencies.
- It fixes the actual failure mode: layout collapse and missing navigation state, not media delivery.

## Phase 0 - Baseline And Guardrails

Status: Complete - 2026-07-22.

Goal: Capture current behavior and lock the regression target before implementation.

Checklist:

- [x] Confirm current product detail URL still renders gallery SSR markup.
  - 2026-07-22: `GET http://localhost:18598/product/best-friends-greg-and-rowley-diary-of-a-wimpy-kid-meme-shirt-personalized-casual-t-shirt` returned `200` and included `data-storefront-product-gallery` plus `data-storefront-gallery-thumbnail`.
- [x] Confirm product media URLs return `200 image/*` for the selected fixture/product.
  - 2026-07-22: `GET /media/products/afeb14a9-a845-4a3d-89ff-eaee6aaaa136?w=1000&h=1000&fit=contain&format=webp&v=1` returned `200 image/webp`.
- [x] Measure current main image frame dimensions on desktop and mobile.
  - 2026-07-22: Playwright measured desktop main frame `542x542`; mobile viewport `390x844` measured `308x308`.
- [x] Measure current thumbnail cell dimensions with 1, 2, 6, and more than 6 images where fixture data allows.
  - 2026-07-22: current fixture exposed 14 images. Desktop thumbnails collapsed to about `28x28`; mobile thumbnails remained `80x80`.
- [x] Confirm current thumbnail click still changes the main image.
  - 2026-07-22: Playwright clicked the second thumbnail and confirmed the main image `src` changed and the clicked thumbnail set `data-selected="true"`.
- [x] Confirm current failure: thumbnail layout must not depend on item count.
  - 2026-07-22: desktop grid compressed 14 thumbnails into 28px cells, confirming the count-driven shrink regression.
- [x] Capture before screenshots under `output/playwright/`.
  - 2026-07-22: captured `output/playwright/storefront-product-gallery-phase0-baseline.png` and `output/playwright/storefront-product-gallery-phase0-mobile-baseline.png`.
- [x] Record baseline findings in `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.

Suggested verification:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrandingMarkupTests.ProductPage_RendersProductImageGalleryComponent"
```

Commit rule:

- Commit only if this phase creates intentional documentation or test baseline changes.

## Phase 1 - Markup Contract

Status: Complete - 2026-07-22.

Goal: Give the gallery explicit carousel controls and stable data hooks.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductImageGallery.razor`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs`

Checklist:

- [x] Keep first image SSR-visible without JavaScript.
- [x] Add previous button with `data-storefront-gallery-prev`.
- [x] Add next button with `data-storefront-gallery-next`.
- [x] Add a stable current index label or counter only if it improves accessibility without visual clutter.
  - 2026-07-22: added an `sr-only` polite status label so visual layout stays unchanged.
- [x] Add `data-gallery-index` to each thumbnail button.
- [x] Keep `data-image-url` and `data-alt` on thumbnail buttons.
- [x] Preserve `aria-current` or `aria-selected` on the selected thumbnail.
- [x] Disable previous on the first item during SSR.
- [x] Disable next when only one gallery item exists.
  - 2026-07-22: carousel controls render only when `GalleryItems.Count > 1`.
- [x] Do not render navigation buttons when `GalleryItems.Count <= 1`, unless a consistent disabled UI is explicitly preferred.
- [x] Keep no-image fallback behavior unchanged.
- [x] Add static markup tests for prev/next controls and gallery index data.
  - 2026-07-22: `StorefrontBrandingMarkupTests.ProductPage_RendersProductImageGalleryComponent` passed.

Acceptance criteria:

- Product detail has predictable data hooks for JS.
- HTML remains useful before JS loads.
- No Commerce Node or API contract changes.

Commit:

```powershell
git add BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductImageGallery.razor BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs
git commit -m "feat(storefront): add product gallery navigation markup"
```

## Phase 2 - Fixed Gallery Layout

Status: Complete - 2026-07-22.

Goal: Make gallery dimensions independent from image count.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs`

Checklist:

- [x] Keep `.bs-product-gallery__main` square and width-stable.
- [x] Add a thumbnail viewport/strip wrapper that does not resize main gallery.
- [x] Set thumbnail item dimensions explicitly, for example `5rem x 5rem`.
- [x] Use `flex: 0 0 5rem` for horizontal scrolling.
- [x] Keep thumbnail images `object-fit: contain`.
- [x] Add responsive constraints for mobile, tablet, and desktop.
  - 2026-07-22: layout uses a fixed-size horizontal strip across breakpoints instead of desktop grid shrinking.
- [x] Avoid grid column counts that shrink thumbnails as images increase.
- [x] Ensure `[hidden]` fallback placeholders remain hidden even when display utility classes exist.
- [x] Add CSS guard tests for fixed thumbnail dimensions and no count-driven grid shrink behavior.
  - 2026-07-22: focused `ProductPage_RendersProductImageGalleryComponent|ProductGalleryCss_EnforcesSquareImageFrames` run passed.

Acceptance criteria:

- Main image frame dimensions are stable with 1, 2, 6, and more than 6 images.
- Thumbnail cells do not collapse below the expected minimum.
- Adding images increases scrollable content, not layout shrink.

Commit:

```powershell
git add BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs
git commit -m "fix(storefront): stabilize product gallery layout"
```

## Phase 3 - Client Interaction

Goal: Add previous/next behavior while preserving existing thumbnail click behavior.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs`

Checklist:

- [ ] Refactor current `selectGalleryThumbnail` into index-based gallery selection.
- [ ] Add `selectGalleryIndex(gallery, index)` helper.
- [ ] Add previous button click handling.
- [ ] Add next button click handling.
- [ ] Update main image `src` and `alt`.
- [ ] Update thumbnail selected state.
- [ ] Update `aria-current` or `aria-selected`.
- [ ] Disable previous at index `0`.
- [ ] Disable next at the last index.
- [ ] Scroll selected thumbnail into view with `scrollIntoView({ block: "nearest", inline: "nearest" })`.
- [ ] Support keyboard navigation on thumbnails: ArrowLeft/ArrowRight at minimum.
- [ ] Keep behavior no-op when markup is incomplete.
- [ ] Avoid global state shared across galleries; derive state per gallery root.
- [ ] Add static JS guard tests for prev/next selectors, index selection, disabled state, and scroll synchronization.

Acceptance criteria:

- Clicking next changes the main image to the next gallery item.
- Clicking previous changes it back.
- Clicking a thumbnail still changes the main image.
- Boundary buttons disable correctly.
- Multiple galleries on one page would not share state accidentally.

Commit:

```powershell
git add BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs
git commit -m "feat(storefront): add product gallery carousel controls"
```

## Phase 4 - Accessibility And Error States

Goal: Make the gallery usable with keyboard, screen readers, slow images, and broken images.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductImageGallery.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs`

Checklist:

- [ ] Buttons have clear accessible names.
- [ ] Disabled navigation uses both `disabled` and `aria-disabled` where appropriate.
- [ ] Selected thumbnail is announced with a stable selected/current attribute.
- [ ] Keyboard focus ring remains visible.
- [ ] Main image fallback does not cover valid loaded images.
- [ ] Thumbnail fallback does not collapse the thumbnail cell.
- [ ] Broken thumbnail still keeps cell dimensions stable.
- [ ] No in-app instructional copy is added.
- [ ] Reduced-motion users do not get forced animation.
- [ ] Static tests cover accessibility hooks.

Acceptance criteria:

- Gallery remains operable by mouse and keyboard.
- Broken images show fallback without altering layout dimensions.
- Accessibility attributes do not drift from visual selected state.

Commit:

```powershell
git add BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductImageGallery.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs
git commit -m "test(storefront): harden product gallery accessibility states"
```

## Phase 5 - Browser QA

Goal: Verify the exact user-facing behavior in Chromium.

Checklist:

- [ ] Start local V2 runtime if not already running.
- [ ] Open the reported product detail URL.
- [ ] Verify main image renders and is not covered by placeholder.
- [ ] Verify thumbnail cells stay fixed with all images present.
- [ ] Click next until the last image.
- [ ] Verify next disables at the last image.
- [ ] Click previous until the first image.
- [ ] Verify previous disables at the first image.
- [ ] Click a middle thumbnail and confirm main image changes.
- [ ] Verify selected thumbnail scrolls into view when the strip overflows.
- [ ] Run desktop viewport check.
- [ ] Run mobile viewport check.
- [ ] Capture screenshots under `output/playwright/`.
- [ ] Update `QA-StorefrontV2.todo.md` with exact evidence.

Suggested browser measurements:

- Main frame width/height before and after adding/scrolling images.
- First thumbnail width/height.
- Last thumbnail width/height.
- Current main image `src`.
- Previous/next disabled state.

Commit:

```powershell
git add docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md
git commit -m "docs(storefront): record product gallery carousel QA"
```

## Phase 6 - Release Gate

Goal: Prove the change is ready to merge without breaking Storefront V2.

Checklist:

- [ ] Run focused Storefront branding/static tests.
- [ ] Run focused Storefront host tests if local test runtime is stable.
- [ ] Run full `BlazorShop.Tests.V2`.
- [ ] Run browser QA from Phase 5 after tests pass.
- [ ] Confirm `git status --short` only has expected files before committing.
- [ ] Commit any remaining intentional changes.
- [ ] Document known warnings separately from regressions.

Commands:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~PresentationV2.Storefront"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore
```

Known warning treatment:

- Existing `MessagePack 2.5.192` NU1902/NU1903 warnings are not product gallery regressions.
- New warnings introduced by gallery changes must block the phase.

Commit:

```powershell
git add BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductImageGallery.razor BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md
git commit -m "fix(storefront): harden product image gallery carousel"
```

## Implementation Notes

Preferred DOM contract:

```text
data-storefront-product-gallery
  data-storefront-gallery-main
    data-storefront-gallery-main-image
    data-storefront-gallery-placeholder
  data-storefront-gallery-controls
    data-storefront-gallery-prev
    data-storefront-gallery-next
  data-storefront-gallery-thumb-viewport
    data-storefront-gallery-thumb-track
      data-storefront-gallery-thumbnail
```

Suggested interaction flow:

```text
thumbnail click / prev / next / keyboard
  -> resolve target index
  -> clamp index to [0, itemCount - 1]
  -> update main image src + alt
  -> update selected thumbnail attributes
  -> update prev/next disabled states
  -> scroll selected thumbnail into view
```

CSS constraints:

```text
main image box: stable square
thumbnail viewport: overflow-x auto
thumbnail item: fixed flex basis
navigation buttons: stable icon-sized buttons
fallback placeholder: hidden must display none
```

## Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| Tailwind utilities missing from `site.css` | Razor class changes do not affect layout | Put critical gallery dimensions in `storefront.css` and guard with tests. |
| JS fails before initialization | Gallery navigation buttons may not work | SSR first image and thumbnail list remain usable; no-image fallback remains server-rendered. |
| More than one gallery on a page | Shared JS state can corrupt selection | Scope all queries to the nearest `data-storefront-product-gallery`. |
| Broken images | Placeholder can cover loaded images or collapse cells | Preserve `[hidden]` override and fixed cell dimensions. |
| Mobile overflow | Thumbnail strip can shift page width | Use contained overflow and stable flex basis. |
| Accessibility drift | Screen reader selected state can mismatch visual state | Update ARIA state in the same JS function that changes main image. |

## Test Matrix

| Case | Expected Result | Automated | Browser |
|---|---|---:|---:|
| One image | Main image visible, no active carousel controls required | [ ] | [ ] |
| Two images | Prev disabled at first, next enabled, next changes image | [ ] | [ ] |
| Six images | Thumbnail cells fixed, no collapse | [ ] | [ ] |
| More than six images | Strip scrolls, selected thumbnail scrolls into view | [ ] | [ ] |
| Broken main image | Main fallback visible, layout stable | [ ] | [ ] |
| Broken thumbnail | Thumbnail fallback visible, cell stable | [ ] | [ ] |
| Keyboard ArrowRight | Moves to next image when focus is in gallery/thumbs | [ ] | [ ] |
| Keyboard ArrowLeft | Moves to previous image when focus is in gallery/thumbs | [ ] | [ ] |
| Mobile viewport | Main image stable, controls tappable, strip scrolls | [ ] | [ ] |
| Desktop viewport | Main image stable, controls positioned cleanly | [ ] | [ ] |

## Autoplan Review Summary

### CEO Review

Mode: HOLD SCOPE.

Premise challenge:

- The user is not asking for a larger merchandising redesign; the product pain is specific: image gallery stability and navigation.
- The most direct outcome is confidence that product images remain inspectable regardless of media count.
- Doing nothing keeps a high-visibility ecommerce regression in product detail pages.

What already exists:

- Product media DTOs and URLs already work.
- Product gallery component exists.
- Storefront JavaScript already handles thumbnail clicks.
- Storefront CSS already owns some gallery structure.
- QA checklist already tracks product media regressions.

Rejected expansions:

- Full zoom/lightbox.
- Drag/swipe physics.
- Variant-specific media filtering.
- Smartstore Slick/PhotoSwipe port.

### Design Review

Design score target: 9/10 for this component scope.

Design decisions:

- Main image remains the visual anchor.
- Navigation controls must be icon-sized and familiar.
- Thumbnail strip must not compete with product title or purchase panel.
- Thumbnail cells should be image cells, not dots.
- No visible instructional copy should be added.

Design risks to check:

- Controls overlapping product imagery on small viewports.
- Thumbnail strip making the product column taller than expected.
- Disabled buttons looking broken instead of intentionally unavailable.

### Engineering Review

Architecture:

```text
Storefront ProductPage
  -> ProductImageGallery.razor
      -> SSR first image + thumbnail markup
      -> data attributes for JS
  -> storefront.css
      -> stable dimensions
  -> storefrontCommerce.js
      -> progressive gallery behavior
```

Engineering score target: 9/10.

Key engineering decision:

- Use app-owned JS and CSS rather than adding a carousel dependency.

Rationale:

- The required behavior is small and deterministic.
- Existing Storefront V2 already uses app-owned JavaScript for browser interactions.
- Dependency cost is not justified for MVP gallery navigation.

### DX Review

DX scope: internal contributor/developer experience only.

DX target:

- A future agent or developer should know exactly which files to edit, which tests to run, and what browser evidence to collect.
- The plan should avoid hidden prerequisites like "remember to rebuild Tailwind" by keeping critical layout in `storefront.css`.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | CEO | Hold scope to product gallery carousel hardening | Auto-decided | Fix the real user-visible regression | User asked for fixed image box and prev/next, not a full product page redesign | Full gallery/lightbox/zoom expansion |
| 2 | Design | Keep main image as stable square visual anchor | Auto-decided | Visual stability over decorative motion | Ecommerce product pages must let users inspect product media reliably | Count-dependent thumbnail grid |
| 3 | Engineering | Use app-owned Razor/CSS/JS instead of Slick/jQuery | Auto-decided | Boring, local, low blast radius | Storefront V2 already has SSR + small JS pattern and no legacy dependency target | Smartstore runtime dependency |
| 4 | Engineering | Put critical gallery dimensions in `storefront.css` | Auto-decided | Explicit over generated-class drift | Current Tailwind output did not contain new utility classes used by Razor | Relying only on Tailwind utilities |
| 5 | QA | Require browser measurement evidence | Auto-decided | Verify the exact failure mode | The previous bug reproduced only visually despite media responses being `200` | Static tests only |

## Phase Checklist

- [x] Phase 0 - Baseline And Guardrails
- [x] Phase 1 - Markup Contract
- [x] Phase 2 - Fixed Gallery Layout
- [ ] Phase 3 - Client Interaction
- [ ] Phase 4 - Accessibility And Error States
- [ ] Phase 5 - Browser QA
- [ ] Phase 6 - Release Gate

## GSTACK REVIEW REPORT

| Run | Status | Findings |
|---|---|---|
| CEO | Clean with scoped recommendation | Hold scope; no business need for lightbox/zoom in this fix. |
| Design | Issues captured in plan | Fixed-size image cells, familiar prev/next controls, no instructional copy. |
| Engineering | Issues captured in plan | Avoid Slick/jQuery; keep dimensions in app CSS; scope JS per gallery root. |
| DX | Clean with checklist additions | Plan includes file list, commands, test matrix, and QA evidence requirements. |

VERDICT: APPROVED FOR IMPLEMENTATION PLAN

NO UNRESOLVED DECISIONS

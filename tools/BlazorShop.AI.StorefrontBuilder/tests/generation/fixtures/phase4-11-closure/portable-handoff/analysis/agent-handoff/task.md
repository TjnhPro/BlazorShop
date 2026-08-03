# Storefront Visual Handoff Task

## Objective
Implement generated storefront visual files for `phase-3d-shared-baseline` from reviewed handoff artifacts only. Readiness passed: `True`.

## Inputs
- `analysis/agent-handoff/manifest.json`
- `analysis/agent-handoff/task.md`
- `analysis/agent-handoff/allowed-files.json`
- `analysis/agent-handoff/protected-files.json`
- `analysis/agent-handoff/page-compositions.json`
- `analysis/agent-handoff/visual-style.json`
- `analysis/agent-handoff/design-tokens.json`
- `analysis/agent-handoff/storefront-pattern.json`
- `analysis/agent-handoff/visual-blueprint.json`
- `analysis/agent-handoff/presentation-catalog.json`
- `analysis/agent-handoff/presentation-mappings.json`
- `analysis/agent-handoff/component-candidates.json`
- `analysis/agent-handoff/component-instances.json`
- `analysis/agent-handoff/responsive-behavior.json`
- `analysis/agent-handoff/interaction-models.json`
- `analysis/agent-handoff/originality-restrictions.json`
- `analysis/agent-handoff/confidence.json`
- `analysis/agent-handoff/review-resolution.json`
- `analysis/agent-handoff/unresolved-regions.json`
- `analysis/agent-handoff/generation-readiness.json`
- `analysis/agent-handoff/handoff-readiness.json`
- `analysis/agent-handoff/evidence-manifest.json`
- `analysis/agent-handoff/screenshots/`
- `analysis/agent-handoff/section-screenshots/`

## Source of Truth Priority
1. `handoff-readiness.json`
2. `visual-blueprint.json`
3. `storefront-pattern.json`
4. `page-compositions.json`
5. `allowed-files.json` / `protected-files.json`
6. `design-tokens.json` / `visual-style.json`
7. `screenshots/` / `section-screenshots/`

## Allowed File Operations
- `Components/Catalog/ProductDetailShell.razor`
- `Components/Catalog/ProductGalleryPlaceholder.razor`
- `Components/Catalog/ProductSummaryCard.razor`
- `Components/Catalog/PurchasePanelPlaceholder.razor`
- `Components/Layout/MainLayout.razor`
- `Pages/Hybrid/Account/AccountPage.razor`
- `Pages/Hybrid/Cart/CartPage.razor`
- `Pages/Hybrid/Checkout/CheckoutPage.razor`
- `Pages/Ssr/Home/HomePage.razor`
- `Pages/Ssr/System/MaintenancePage.razor`

## Protected Files
- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Runtime`
- `BlazorShop.Storefront.Client`
- `BlazorShop.Storefront.V2`
- `BlazorShop.CommerceNode.API`
- `BlazorShop.ControlPlane.API`
- `StorefrontPackageVersions.props`
- `starter-generation.contract.yaml`
- `docs/storefront-analysis/generated-files.yaml`

## Required Page Slots
- `home`: `layout.header`, `home.sections`, `layout.footer`
- `category-listing`: `layout.header`, `catalog.product-card`, `layout.footer`
- `search-results`: `layout.header`, `catalog.product-card`, `layout.footer`
- `product-detail`: `layout.header`, `product.gallery`, `product.information`, `product.purchase`, `layout.footer`
- `cart-shell`: `layout.header`, `cart.page`, `layout.footer`
- `checkout-shell`: `layout.header`, `checkout.page`, `layout.footer`
- `account-auth-shell`: `layout.header`, `account.shell`, `layout.footer`
- `content-page`: `layout.header`, `home.sections`, `layout.footer`
- `maintenance`: `layout.header`, `system.error`, `layout.footer`
- `not-found`: `layout.header`, `system.error`, `layout.footer`
- `service-unavailable`: `layout.header`, `system.error`, `layout.footer`
- `error-state`: `layout.header`, `system.error`, `layout.footer`

## Optional Page Slots
- `home`: `layout.main-navigation`, `layout.mobile-navigation`, `layout.cart-badge`, `layout.account-menu`
- `category-listing`: `catalog.filters`, `catalog.sorting`, `catalog.pagination`, `layout.main-navigation`, `layout.mobile-navigation`, `layout.cart-badge`, `layout.account-menu`
- `search-results`: `catalog.filters`, `catalog.sorting`, `catalog.pagination`
- `product-detail`: `product.reviews`, `product.related-products`, `layout.main-navigation`, `layout.mobile-navigation`, `layout.cart-badge`, `layout.account-menu`
- `cart-shell`: 
- `checkout-shell`: 
- `account-auth-shell`: 
- `content-page`: `layout.main-navigation`, `layout.mobile-navigation`
- `maintenance`: 
- `not-found`: 
- `service-unavailable`: 
- `error-state`: 

## Section Order
- `account`: section-01 (header) -> section-02 (featured hero) -> section-03 (footer)
- `cart`: section-01 (header) -> section-02 (featured hero) -> section-03 (footer)
- `category`: section-01 (header) -> section-02 (featured hero) -> section-03 (footer)
- `checkout`: section-01 (header) -> section-02 (featured hero) -> section-03 (footer)
- `home`: section-01 (header) -> section-02 (featured hero) -> section-03 (footer)
- `maintenance`: section-01 (header) -> section-02 (featured hero) -> section-03 (footer)
- `product`: section-01 (header) -> section-02 (featured hero) -> section-03 (product information) -> section-04 (purchase actions) -> section-05 (footer)

## Responsive Evidence
- `account`: no reviewed responsive override
- `cart`: no reviewed responsive override
- `category`: no reviewed responsive override
- `checkout`: no reviewed responsive override
- `home`: no reviewed responsive override
- `maintenance`: no reviewed responsive override
- `product`: no reviewed responsive override

## Interaction Evidence
- Preserve action descriptors. Generated visual files may restyle or reposition but must not implement functional JavaScript, routes, BFF calls, SEO/media behavior, cart/checkout/account/auth logic, or payment logic.

## Originality Restrictions
- Screenshots, section crops, and reference assets are evidence-only and reference-only. Do not copy them into production-safe asset folders without explicit human review metadata.

## Forbidden Behavior
- No `@page` route declarations.
- No direct Commerce Node Storefront API browser calls.
- No BFF, SEO, media, cart, checkout, account/auth, payment, or Runtime transport reimplementation.

## Unsupported Handling
- No blocking unsupported regions in generation readiness.

## Validation Commands
- `dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore`
- `powershell -ExecutionPolicy Bypass -File scripts/qa/run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1`

## Stop Conditions
- Stop if handoff readiness is false.
- Stop if a required page slot is missing.
- Stop if visual evidence is missing for a required major section.
- Stop if a target path is missing, outside allowed zones, or protected.
- Stop if unsupported critical pattern remains.
- Stop if implementation would require routes, BFF, SEO/media, cart/checkout/account/auth logic, payment logic, or functional JavaScript.
- StorefrontBuilder must consume this package only through the approved Phase 4 preflight and generation plan; do not read raw captures, source analysis, Storefront V2 source, or reports as fallback input.

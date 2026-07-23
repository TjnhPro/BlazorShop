# Storefront Page Composition Strategy Todo

Status: In progress
Source: investigate review of Storefront page strategy on 2026-07-22
Purpose: clarify which Storefront V2 pages stay as SSR route pages, which pages are database-driven content, and how to add template-aware SEO/rendering without breaking the current V2 storefront.

## Current Verified Evidence

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/StorefrontPage.razor` owns route `/pages/{Slug}` and loads a published page through `IStorefrontContentClient.GetPublishedPageBySlugAsync`.
- [x] `StorefrontPage.razor` currently renders `BodyHtml` with `MarkupString` and composes SEO with `IStorefrontSeoComposer.ComposeStorefrontPageAsync`.
- [x] `StorefrontRoutes` maps `About`, `Faq`, `Privacy`, `Terms`, and `CustomerService` to `/pages/...`; there are no dedicated `About.razor`, `FAQ.razor`, `Privacy.razor`, `Terms.razor`, or `CustomerService.razor` files under Storefront V2 `Pages`.
- [x] `Home.razor` keeps route `/`, renders catalog/category/product content SSR, and already reads the DB page slug `home` through `StorefrontRoutes.HomeMetadataSlug`.
- [x] `CategoryPage.razor`, `ProductPage.razor`, `NewReleases.razor`, and `TodaysDeals.razor` have route-specific SSR catalog queries, SEO metadata, and structured data.
- [x] `SearchPage.razor` is SSR GET with query parameters and applies search noindex metadata through `StorefrontIndexingPolicy.ApplySearchMetadata`.
- [x] `CartPage.razor` is an SSR route shell for `/my-cart` and renders `StorefrontCartView` with `InteractiveWebAssembly`.
- [x] Account pages render WASM components (`AccountProfileEditor`, `AccountAddressBook`, `AccountOrderList`, `AccountOrderDetail`, `AccountChangePasswordForm`) and set `noindex,nofollow`.
- [x] `CheckoutPage.razor` renders `StorefrontCheckoutShell` with `InteractiveWebAssembly`, while server endpoints still own antiforgery, cart version validation, idempotency, checkout review, place order, and payment redirect.
- [x] `StorefrontStructuredDataComposer` already has `ComposeFaqPageAsync`, but `StorefrontPage.razor` does not currently pass structured data to `SeoHead`.
- [x] `StorefrontPagePublicDto` does not expose `PageKey`, so the public page renderer cannot currently choose FAQ/policy/customer-service behavior from the DB page identity.
- [x] `StorefrontPageContentRules.PageKeys` and `StorefrontPageTemplateCatalog` do not include `faq` or `customer_service`.
- [x] `MaintenancePage.razor` does not emit a direct `<meta name="robots">`, but `StorefrontResponseHeaders.ApplyServiceUnavailable` sets `X-Robots-Tag: noindex, nofollow` for 503 responses.
- [x] Product detail already uses `ProductImageGallery`; product media is not part of this phase.

## Goal

Make the Storefront page model explicit and maintainable:

- Content pages are DB-driven through one renderer.
- Commerce/catalog pages stay route-specific SSR pages.
- Application pages stay server route shells with WASM interaction where already migrated.
- Storefront page rendering becomes template-aware enough for SEO, especially FAQ.
- Account route pages stop duplicating layout/navigation.
- The change remains mechanical and incremental, without rewriting routing, checkout, cart, account, or catalog behavior.

## Non-goals

- [ ] Do not delete `Home.razor`, `CategoryPage.razor`, `ProductPage.razor`, `NewReleases.razor`, `TodaysDeals.razor`, or `SearchPage.razor`.
- [ ] Do not convert cart, checkout, account, auth, payment-result, maintenance, or not-found pages into DB content pages.
- [ ] Do not move checkout place-order logic into pure WASM.
- [ ] Do not add a contact form in this phase. Contact/support content can be DB content, but contact form behavior remains a future WASM component phase.
- [ ] Do not introduce a large CMS/page-builder system.
- [ ] Do not add Smartstore runtime references or copy Smartstore implementation code.
- [ ] Do not change Commerce Node route ownership or Storefront API route shape except for explicit DTO/contract additions needed by this plan.
- [ ] Do not reorganize folders in the same commit as behavior changes unless the phase explicitly says it is a mechanical move.

## Page Classification Decision

| Page group | Current/codebase decision | Reason |
|---|---|---|
| Home | Keep SSR route page | Catalog landing page with category/product SSR and DB metadata support. |
| Product | Keep SSR route page | SEO-critical product detail, structured data, media gallery, variants, price, sellability. |
| Category | Keep SSR route page | SEO-critical product listing, filters, paging, breadcrumb, category structured data. |
| New releases | Keep SSR route page for now | Catalog collection landing page, not raw content HTML. |
| Today's deals | Keep SSR route page for now | Catalog collection landing page, not raw content HTML. |
| Search | Keep SSR GET, noindex | Query URL, filters, pagination, browser back/forward, no sitemap. |
| Content pages | DB-driven `/pages/{slug}` | About, FAQ, policies, customer service, shipping/payment/returns/cookie content. |
| Cart | SSR route shell + WASM | Current architecture already matches this direction. |
| Checkout | Hybrid SSR route shell + WASM + server POST | Server must keep antiforgery, validation, idempotency, payment redirect. |
| Account | SSR route shell + WASM | Current architecture works but duplicated shell should be refactored. |
| Auth/payment/system | Server route pages, noindex | Cookie, antiforgery, redirects, status codes, and crawler behavior are server-owned. |

## Phase Dependency Map

```text
Phase 0: Baseline and guardrails
  -> Phase 1: Public page contract exposes template identity
      -> Phase 2: Complete content page template catalog
          -> Phase 3: Template-aware StorefrontPage renderer and structured data
              -> Phase 4: Account route shell deduplication
                  -> Phase 5: Optional page folder reorganization
                      -> Phase 6: QA, SEO, and release verification
```

## Phase 0 - Baseline And Guardrails

Goal: freeze current behavior before adding template-aware rendering.

### Tasks

- [x] Record current Storefront V2 page inventory:
  - [x] `Pages/Home.razor`
  - [x] `Pages/CategoryPage.razor`
  - [x] `Pages/ProductPage.razor`
  - [x] `Pages/NewReleases.razor`
  - [x] `Pages/TodaysDeals.razor`
  - [x] `Pages/SearchPage.razor`
  - [x] `Pages/StorefrontPage.razor`
  - [x] cart/account/checkout/auth/payment/system pages.
- [x] Add or update architecture guardrail tests proving content pages do not reappear as dedicated Razor route files:
  - [x] No `About.razor`.
  - [x] No `Faq.razor` or `FAQ.razor`.
  - [x] No `Privacy.razor`.
  - [x] No `Terms.razor`.
  - [x] No `CustomerService.razor`.
- [x] Add or update tests proving commerce route pages still exist:
  - [x] `Home.razor` has `@page "/"`.
  - [x] `ProductPage.razor` has `@page "/product/{Slug}"`.
  - [x] `CategoryPage.razor` has `@page "/category/{Slug}"`.
  - [x] `SearchPage.razor` has `@page "/search"`.
- [x] Add a guardrail that `StorefrontRoutes.About`, `Faq`, `Privacy`, `Terms`, and `CustomerService` still resolve to `/pages/...`.
- [x] Add a guardrail that private/application routes are excluded from sitemap by `StorefrontIndexingPolicy`/`StorefrontSitemapService`.
- [x] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` with a page-classification QA section.

### Files likely touched

- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`
- `BlazorShop.Tests.V2/Architecture/*`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
```

### Done when

- [x] Tests document the page classification boundary.
- [x] No runtime behavior changes have been made.
- [x] Future agents have a failing test if they recreate dedicated static content pages.

## Phase 1 - Public Page Contract Exposes Template Identity

Goal: let Storefront V2 know what kind of DB page it is rendering without introducing a new CMS abstraction.

### Design decision

Use existing `PageKey` as the template identity for this phase. Do not add a separate `PageType` column unless implementation proves `PageKey` is insufficient.

Reason:

- `StorefrontPage` already has `PageKey`.
- Admin/template catalog already uses `PageKey`.
- The current need is rendering/SEO behavior, not a full page-builder.

### Tasks

- [x] Extend `StorefrontPagePublicDto` to include:
  - [x] `string? PageKey`
  - [x] `int DisplayOrder` only if the public renderer or navigation needs it directly; otherwise keep display ordering in menu/sitemap/admin flows.
  - [x] `bool IncludeInNavigation` only if the public renderer needs it directly; otherwise do not expose extra fields.
- [x] Update Commerce Node Storefront page mapping in `StorefrontPageService.GetPublishedBySlugAsync`.
- [x] Update Storefront V2 local/page contract models if they mirror `StorefrontPagePublicDto`.
- [x] Update `IStorefrontContentClient.GetPublishedPageBySlugAsync` deserialization model.
- [x] Update Swagger/OpenAPI tests for the public page response schema.
- [x] Keep backward-compatible JSON shape for existing fields.
- [x] Do not expose admin-only fields, archived state, internal IDs, or unpublished preview data through the public endpoint.

### Files likely touched

- `BlazorShop.Application/CommerceNode/StorefrontPages/StorefrontPageDtos.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontPageService.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/*Storefront*Pages*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/*`
- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/*OpenApi*`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPage|FullyQualifiedName~OpenApi"
```

### Done when

- [x] Published page API returns `PageKey`.
- [x] Storefront renderer can branch by `PageKey`.
- [x] Public contract tests prove no private/admin fields leak.

## Phase 2 - Complete Content Page Template Catalog

Goal: make Control Plane/admin page-template completeness match the Storefront routes that already exist.

### Tasks

- [x] Add missing page keys to `StorefrontPageContentRules.PageKeys`:
  - [x] `faq`
  - [x] `customer_service`
- [x] Add matching definitions to `StorefrontPageTemplateCatalog`:
  - [x] `faq` with slug `faq`, title `FAQ`, include in sitemap true, navigation location likely `footer_support`.
  - [x] `customer_service` with slug `customer-service`, title `Customer service`, include in sitemap true, navigation location likely `footer_support`.
- [x] Review current template slugs for consistency with `StorefrontRoutes`:
  - [x] `about` currently template slug `about-us` and route `/pages/about-us` match.
  - [x] `terms_conditions` currently slug `terms` and route `/pages/terms` match.
  - [x] `privacy_policy` currently slug `privacy` and route `/pages/privacy` match.
  - [x] `cookie_information` currently slug `cookies`; consent default uses `/pages/cookies`.
- [x] Decide whether existing `shipping_information`, `payment_information`, and `return_refund_policy` slugs should remain short (`shipping`, `payment`, `returns`) or change to longer route names.
  - [x] Recommended: keep existing short slugs to avoid unnecessary data churn unless public nav already promised longer URLs.
- [x] Do not add a `generic` template. Pages without known `PageKey` already behave as standard content.
- [x] Do not add a `contact_form` page key in this phase.
- [x] Add/update tests for:
  - [x] known page key validation.
  - [x] template catalog contains every required content template.
  - [x] template definitions do not point to dedicated Razor routes for content pages.
- [x] Update Development seeding only to create missing QA fixtures if needed; do not overwrite store-edited page content on restart.

### Files likely touched

- `BlazorShop.Application/CommerceNode/StorefrontPages/StorefrontPageContentRules.cs`
- `BlazorShop.Application/CommerceNode/StorefrontPages/StorefrontPageTemplateCatalog.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.ContentNavigationSeed.cs`
- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/*StorefrontPage*`
- `BlazorShop.Tests.V2/Application/*StorefrontPage*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPageTemplate|FullyQualifiedName~StorefrontPageContentRules"
```

### Done when

- [x] Admin/template completeness can detect missing FAQ and customer-service pages.
- [x] Public Storefront routes and template catalog are aligned.
- [x] No contact form behavior was introduced.

## Phase 3 - Template-aware StorefrontPage Rendering And Structured Data

Goal: keep one DB content renderer while making its SEO/rendering aware of known page keys.

### Strategy

Add a small renderer/composer layer around `StorefrontPage.razor`; avoid turning pages into a CMS framework.

Suggested shape:

```text
StorefrontPage.razor
  -> load GetStorefrontPage
  -> StorefrontPagePresentationResolver.Resolve(page)
      -> layout variant
      -> structured data kind
      -> optional FAQ entries
  -> <SeoHead Metadata="..." StructuredData="..." />
  -> render known content template or standard content fallback
```

### Tasks

- [x] Add a Storefront-side page presentation resolver:
  - [x] Input: public page DTO with `Slug`, `Title`, `Intro`, `BodyHtml`, `Seo`, `PageKey`.
  - [x] Output: content layout kind, structured data kind, optional structured data entries.
  - [x] Keep it in Storefront V2, not in Domain/Infrastructure.
- [x] Supported MVP layout behavior:
  - [x] Unknown/null page key: standard content fallback.
  - [x] `about`: standard content.
  - [x] `shipping_information`: policy/help content styling.
  - [x] `payment_information`: policy/help content styling.
  - [x] `return_refund_policy`: policy/help content styling.
  - [x] `terms_conditions`: policy content styling.
  - [x] `privacy_policy`: policy content styling.
  - [x] `cookie_information`: policy content styling.
  - [x] `faq`: FAQ content styling and structured data support.
  - [x] `customer_service`: support content styling without contact-form behavior.
- [x] Decide FAQ data source for this phase:
  - [x] Recommended MVP: parse a controlled JSON payload only if a field already exists or can be added safely.
  - [x] If no structured field exists, render HTML normally and add only `WebPage` structured data, then create a follow-up ticket for structured FAQ storage.
  - [x] Do not scrape arbitrary Q/A pairs from `BodyHtml` with regex.
- [x] Wire `StorefrontPage.razor` to pass `StructuredData` into `SeoHead`.
- [x] Use `StorefrontStructuredDataComposer.ComposeWebPageAsync` for standard/policy/customer-service pages.
- [x] Use `StorefrontStructuredDataComposer.ComposeFaqPageAsync` only when structured FAQ entries are available.
- [x] Keep service-unavailable and not-found behavior unchanged:
  - [x] 503 uses `ComposeServiceUnavailablePageAsync`.
  - [x] 404 uses `ComposeNotFoundPageAsync`.
  - [x] `StorefrontResponseHeaders.ApplyServiceUnavailable` and `ApplyNotFound` remain in place.
- [x] Avoid adding heavy template components unless the Razor page becomes hard to read.
- [x] Add bUnit/static markup tests:
  - [x] standard page renders HTML body.
  - [x] policy page keeps SEO metadata.
  - [x] FAQ page emits FAQ JSON-LD only when structured FAQ entries exist.
  - [x] unknown page key falls back safely.
  - [x] unpublished/missing page still returns not-found behavior.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/StorefrontPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/*StorefrontPagePresentation*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/*Pages*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/*`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPage"
```

### Done when

- [x] `/pages/{slug}` remains the only route renderer for content pages.
- [x] Known DB page keys can affect SEO/structured data without dedicated route pages.
- [x] FAQ structured data behavior is explicit and tested.

## Phase 4 - Account Route Shell Deduplication

Goal: reduce account page duplication without changing customer/account behavior.

### Current problem

Account pages already follow the desired SSR shell + WASM component model, but page navigation/layout is duplicated across:

- `AccountProfilePage.razor`
- `AccountChangePasswordPage.razor`
- `AccountAddressesPage.razor`
- `AccountOrdersPage.razor`
- `AccountOrderDetailPage.razor`

### Tasks

- [ ] Add `Components/Account/AccountPageShell.razor` or `Components/Account/AccountRouteShell.razor`.
- [ ] Shell responsibilities:
  - [ ] consistent account layout grid.
  - [ ] account navigation.
  - [ ] active item selection.
  - [ ] title/eyebrow/header area.
  - [ ] child content slot.
- [ ] Keep each page responsible for:
  - [ ] route declaration.
  - [ ] `noindex,nofollow`.
  - [ ] session guard.
  - [ ] antiforgery token creation where needed.
  - [ ] initial API data load.
  - [ ] rendering the correct WASM component.
- [ ] Do not move API calls into the shell in this phase.
- [ ] Do not change account endpoint routes.
- [ ] Do not change account WASM component contracts except if markup tests require passing active route/title.
- [ ] Add tests:
  - [ ] account pages still include noindex.
  - [ ] account pages still render WASM component markers.
  - [ ] account navigation includes profile, orders, addresses, password where appropriate.
  - [ ] unauthenticated users still redirect to sign-in with safe return URL.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/AccountPageShell.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountProfilePage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountChangePasswordPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountAddressesPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrdersPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrderDetailPage.razor`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Account"
```

### Done when

- [ ] Account pages have the same behavior and route shape.
- [ ] Navigation/layout duplication is removed.
- [ ] WASM account components remain the interaction surface.

## Phase 5 - Optional Mechanical Page Folder Reorganization

Goal: make Storefront page ownership easier to scan after behavior is tested.

### Timing rule

Only run this phase after Phases 1-4 pass. This phase should be a mostly mechanical move and should not be mixed with behavior changes.

### Proposed folder structure

```text
Pages/
  Catalog/
    Home.razor
    CategoryPage.razor
    ProductPage.razor
    SearchPage.razor
    NewReleases.razor
    TodaysDeals.razor
  Content/
    StorefrontPage.razor
  Commerce/
    CartPage.razor
    CheckoutPage.razor
    PaymentSuccessPage.razor
    PaymentCancelPage.razor
  Auth/
    SignInPage.razor
    RegisterPage.razor
    ForgotPasswordPage.razor
    ResetPasswordPage.razor
  Account/
    AccountProfilePage.razor
    AccountChangePasswordPage.razor
    AccountAddressesPage.razor
    AccountOrdersPage.razor
    AccountOrderDetailPage.razor
  System/
    MaintenancePage.razor
    NotFoundPage.razor
```

### Tasks

- [ ] Move files without changing route declarations.
- [ ] Update namespaces/partial class namespaces where needed.
- [ ] Update tests/source references affected by file moves.
- [ ] Avoid broad namespace rewrites outside Storefront V2.
- [ ] Confirm Razor routing still discovers all pages.
- [ ] Update architecture folder guide if this structure becomes canonical.

### Files likely touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/**`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/**/*.razor.cs`
- `docs/architecture/05-project-and-folder-guide.md`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
```

### Done when

- [ ] Page folder structure communicates intent.
- [ ] Route URLs are unchanged.
- [ ] No behavior changes are mixed into the move commit.

## Phase 6 - QA, SEO, And Release Verification

Goal: prove the page model works in browser and API-level tests before production.

### Automated test checklist

- [ ] Published DB page:
  - [ ] `/pages/qa-legal` returns 200.
  - [ ] page title/body render.
  - [ ] SEO meta title/description render.
  - [ ] sitemap includes the page when `IncludeInSitemap=true`.
- [ ] Unpublished DB page:
  - [ ] `/pages/qa-unpublished-page` returns 404 behavior.
  - [ ] response has noindex behavior.
  - [ ] sitemap excludes it.
- [ ] Missing DB page:
  - [ ] `/pages/not-a-real-page` returns 404 behavior.
  - [ ] no alternate store fallback occurs.
- [ ] FAQ page:
  - [ ] `/pages/faq` resolves when seeded/created.
  - [ ] HTML content renders.
  - [ ] structured FAQ JSON-LD appears only when structured FAQ entries exist.
- [ ] Customer service page:
  - [ ] `/pages/customer-service` resolves when seeded/created.
  - [ ] it does not require/contact-form-check any WASM contact component.
- [ ] Commerce SSR pages:
  - [ ] `/` renders categories/products before WASM.
  - [ ] `/category/{slug}` renders product list, SEO, and structured data.
  - [ ] `/product/{slug}` renders product details, gallery, SEO, and structured data.
  - [ ] `/new-releases` renders product grid and collection structured data.
  - [ ] `/todays-deals` renders product grid and collection structured data.
- [ ] Search:
  - [ ] `/search?q=...` renders GET results.
  - [ ] search page has `noindex,follow`.
  - [ ] search URL is excluded from sitemap.
- [ ] WASM route shells:
  - [ ] `/my-cart` renders cart WASM root and has noindex.
  - [ ] `/checkout` renders checkout WASM root and has noindex.
  - [ ] `/account/profile`, `/account/addresses`, `/account/orders`, `/account/change-password` render expected account WASM components and have noindex.
- [ ] System pages:
  - [ ] `/maintenance?reason=maintenance` returns 503 and `X-Robots-Tag: noindex, nofollow`.
  - [ ] unknown route returns 404 and noindex.
  - [ ] auth/payment result pages remain noindex.

### Browser QA with Playwright

- [ ] Run Playwright against the local V2 runner, not static HTML.
- [ ] Validate rendered browser DOM, not only HTTP smoke.
- [ ] Capture desktop and mobile screenshots for:
  - [ ] home.
  - [ ] product detail.
  - [ ] category listing.
  - [ ] generic content page.
  - [ ] FAQ content page if present.
  - [ ] cart.
  - [ ] checkout.
  - [ ] account profile after login.
  - [ ] maintenance.
  - [ ] not found.
- [ ] Confirm no visible layout overlap after account shell refactor.
- [ ] Confirm content pages are usable without WASM.
- [ ] Confirm cart/account/checkout interaction still hydrates under WASM.

### Suggested commands

```powershell
.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
npx playwright test --config .\playwright.config.ts
```

Use the repository's actual Playwright command/config if it differs from the generic command above.

### Done when

- [ ] Focused Storefront tests pass.
- [ ] Browser QA confirms SSR content pages, commerce pages, and WASM route shells.
- [ ] `QA-StorefrontV2.todo.md` is updated with pass/fail evidence.
- [ ] No dedicated Razor static content pages were reintroduced.

## Implementation Order And Commit Plan

- [x] Commit 1: baseline guardrails and QA checklist updates.
- [x] Commit 2: public page contract exposes `PageKey` and tests.
- [x] Commit 3: template catalog/rules add `faq` and `customer_service`.
- [x] Commit 4: StorefrontPage template-aware SEO/structured data rendering.
- [ ] Commit 5: account page shell deduplication.
- [ ] Commit 6: optional mechanical folder reorganization.
- [ ] Commit 7: QA evidence/docs updates.

Each commit should be independently buildable. If Phase 5 folder reorganization is skipped, do not block Phases 1-4.

## Risk Controls

- [ ] Public DTO changes must not expose internal/admin fields.
- [ ] Storefront page body remains controlled HTML; do not parse arbitrary scripts or add public script/style injection.
- [ ] Do not rely on WASM for SEO pages.
- [ ] Do not remove server POST fallback for checkout.
- [ ] Do not make search indexable.
- [ ] Do not overwrite store-edited page content from Development seeding.
- [ ] Keep all route URLs stable unless a separate redirect/slug migration phase is approved.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Keep commerce/catalog pages as SSR Razor pages | Auto-decided | Preserve SEO and existing runtime boundaries | Product/category/home/search/collection pages have route-specific API calls, SEO, structured data, and product grid behavior. | Replacing them with generic DB content pages. |
| 2 | Scope | Use existing `PageKey` as template identity | Auto-decided | Smallest compatible extension | Domain entity and admin template catalog already have `PageKey`; this avoids a new page type system. | Adding a broad CMS/page-builder model in this phase. |
| 3 | Scope | Add `faq` and `customer_service` to template catalog/rules | Auto-decided | Align code with public routes | Routes already expose `/pages/faq` and `/pages/customer-service`, but catalog/rules do not list matching keys. | Leaving admin completeness checks unable to detect missing pages. |
| 4 | Rendering | Add structured data through StorefrontPage renderer, not dedicated FAQ route | Auto-decided | Keep one content renderer | `StorefrontStructuredDataComposer` already supports FAQ; the missing piece is renderer wiring/template identity. | Creating `FAQ.razor`. |
| 5 | Account | Refactor account shell, keep account routes | Auto-decided | Reduce duplication without changing behavior | Account pages already use WASM components and server session/antiforgery handling; the pain is repeated layout. | Converting account pages to DB pages or pure WASM routes. |
| 6 | Checkout | Keep checkout hybrid | Auto-decided | Server owns mutation safety | Current checkout POST path handles antiforgery, cart version, review, idempotency, place order, and payment redirect. | Moving final place-order entirely to browser WASM. |
| 7 | Contact | Do not add contact form/page type in this phase | User preference preserved | Avoid future WASM migration friction | User has stated contact form should become a WASM component later and should not be part of page template validation. | Mapping contact form as a DB page type. |

## Final Recommendation

Approve this as a staged hardening/refactor plan. The highest-value implementation path is Phases 1-4:

1. Expose `PageKey` safely.
2. Complete required content templates.
3. Add template-aware SEO/structured data to `StorefrontPage`.
4. Deduplicate account route shell.

Phase 5 is useful but optional. It should be run only as a mechanical move after behavior is protected by tests.

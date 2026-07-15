# BlazorShop Basic Page Content Core Autoplan

Generated: 2026-07-15

Scope:

- 7.1 Page model.
- 7.2 Basic content page type template catalog.
- 7.3 Publishing behavior for content pages.
- 7.4 Contact page decision update: contact form is out of this phase and will be handled as an independent component/WASM surface later.

Implementation status:

- Phase 0 committed as `04ed8dd docs: plan basic page content core`.
- Phase 1 committed as `da5111e feat(commerce-node): add storefront page content metadata`.
- Phase 2 committed as `446f91e feat(commerce-node): add page template readiness service`.
- Phase 3 committed as `d589657 feat(control-plane): expose page template readiness APIs`.
- Phase 4 committed as `206215f feat(control-plane-web): add page content readiness manager`.
- Phase 5 committed as `c9e2517 feat(storefront): render content page navigation links`.
- Phase 8 cache hardening committed as `29bcd4e test(storefront): cover page navigation provider cache`; Commerce Node projections are not persisted/cached, and Storefront uses request-scoped provider caching only.
- Phase 9 QA/docs recorded on 2026-07-15 with focused builds/tests and QA checklist updates.

Autoplan note: external dual-voice subagents are not available in this Codex runtime. This plan records the CEO, Design, Eng, and DX passes as an internal autoplan audit using the same decision principles: keep the business outcome clear, prefer the narrow path that works, reuse existing code, keep boundaries explicit, avoid speculative framework work, and make each phase verifiable.

## 1. Premise Challenge

The requested feature is valid, but it should not become a CMS, page builder, menu framework, or functional component registry.

The codebase already has a working `StorefrontPage` foundation:

- Store-scoped pages live in `CommerceNodeDbContext`.
- Control Plane already manages pages through the gateway path.
- Storefront V2 already renders published pages at `/pages/{slug}`.
- Draft/unpublished pages are already hidden from public slug lookup.

The missing part is not "create pages from scratch". The missing part is content governance:

1. Admin needs to know whether a store has the required legal/support content pages.
2. Existing pages need a stable `PageKey`/system name so they can be mapped to known content slots without relying on mutable slug.
3. Storefront navigation needs ordered, published content links without hard-coding every legal/support URL.
4. The plan must not bind functional areas such as contact form, cart, checkout, and account to page type mapping because those will move toward independent WASM components later.

## 2. Current Code Facts

| Area | Current fact | Decision |
| --- | --- | --- |
| Page entity | `StorefrontPage` has `Id`, `PublicId`, `StoreId`, `Slug`, `Title`, `Intro`, `BodyHtml`, `IsPublished`, `IncludeInSitemap`, SEO fields, timestamps, and `ArchivedAt`. | Extend this entity additively. Do not replace it. |
| Store ownership | `StorefrontPage.StoreId` is required and service methods resolve current store through `ICommerceStoreContext`. | Keep store-owned page rows. Do not add many-to-many store mapping for pages. |
| Public route | Storefront V2 route `/pages/{Slug}` calls `GetPublishedPageBySlugAsync`. | Keep `/pages/{slug}` stable. |
| Draft behavior | Public slug lookup filters `IsPublished` and `ArchivedAt == null`. | Reuse this for public protection. |
| Admin UI | Control Plane Web has `/commerce-admin/pages` with list, search, status filter, create/edit/archive. | Add readiness/template controls into this page instead of creating a second manager. |
| Gateway | Control Plane Web calls Control Plane API; Control Plane API forwards to Commerce Node API with `storeKey`. | Preserve boundary. |
| Permissions | `commerce.pages.read` and `commerce.pages.write` exist. | Reuse for page template/readiness operations unless a later phase proves more granularity is needed. |
| Navigation | Storefront header/footer still have hard-coded links such as About, Customer Service, Privacy, Terms. | Move content links to page-driven navigation in a later phase, with a safe fallback. |
| Store mapping tests | Store mapping phase added tests proving page operations stay store-scoped. | Keep those tests and add tests only for new fields/workflows. |
| Contact | Footer can display support contact data, but no Commerce Node contact-message/contact-form domain exists. | Do not include contact form in page system. |

## 3. Approved Scope

### In Scope

- Add `PageKey`/system name to map a store page to a known content template.
- Add display ordering for manager/template/navigation use.
- Add minimal navigation attachment fields for content links.
- Add static template catalog map for required/recommended content pages.
- Add manager readiness/checklist UI so admin knows which content templates are missing.
- Allow mapping an existing page to a template.
- Allow creating a draft shell from a template.
- Keep custom content pages possible with `PageKey = null`.
- Keep public rendering at `/pages/{slug}`.
- Keep draft/unpublished hidden from public route.
- Add admin preview for unpublished page only through protected admin/gateway flow if included in a later phase.
- Add cache invalidation only for new page readiness/navigation projections.

### Out Of Scope

- Contact form.
- Cart.
- Checkout.
- Account/customer dashboard.
- Functional component mapping on page type.
- Generic page type `generic` in the template catalog.
- Page builder/block editor.
- WYSIWYG editor.
- Markdown parser.
- Password-protected pages.
- Language-specific content.
- ACL/customer-role restricted pages.
- Dynamic functional route registry.
- Generic menu framework.
- Error/not-found page managed by DB.
- Replacing the maintenance fallback page.
- Moving page data to `ControlPlaneDbContext` or legacy `AppDbContext`.

## 4. Template Catalog Decision

The template catalog is a code-owned map, not a content table and not a page type framework.

It answers:

- Which content pages should a quality store normally have?
- Which page key maps to which existing or draft page?
- Which pages are missing?
- Which draft shell can the admin create quickly?

It does not answer:

- How contact/cart/checkout/account components work.
- Which component should render inside a page.
- How a page builder composes blocks.
- How arbitrary custom pages are classified.

### Catalog Entries

Required for store content readiness:

| PageKey | Default slug | Default title | Default menu location | Default order |
| --- | --- | --- | --- | --- |
| `about` | `about-us` | `About us` | `footer_company` | 100 |
| `shipping_information` | `shipping` | `Shipping information` | `footer_support` | 200 |
| `payment_information` | `payment` | `Payment information` | `footer_support` | 210 |
| `terms_conditions` | `terms` | `Terms and conditions` | `footer_legal` | 300 |
| `privacy_policy` | `privacy` | `Privacy policy` | `footer_legal` | 310 |
| `return_refund_policy` | `returns` | `Return and refund policy` | `footer_support` | 220 |
| `cookie_information` | `cookies` | `Cookie information` | `footer_legal` | 320 |

Optional content slots:

| PageKey | Default slug | Default title | Reason |
| --- | --- | --- | --- |
| `home_content` | `home` | `Home content` | Existing architecture already reserves published page slug `home` for home metadata/content usage. `/` remains canonical. |
| `store_closed_content` | `store-closed` | `Store closed` | Optional future override for maintenance copy. The hard-coded maintenance fallback remains required. |

Explicitly excluded:

| Candidate | Decision |
| --- | --- |
| `generic` | Do not put in catalog. Custom pages are just `PageKey = null`. |
| `contact` | Excluded from content readiness. Contact form will be independent component/WASM surface later. |
| `cart` | Functional route/component, not page content. |
| `checkout` | Functional route/component, not page content. |
| `account` | Functional route/component, not page content. |
| `not_found` | Keep code-level fallback so 404 works even when DB/store/content is unavailable. |

## 5. Target Architecture

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/admin/pages/*?storeKey={storeKey}
          -> StorefrontPageTemplateService
          -> StorefrontPageService
              -> CommerceNodeDbContext.storefront_page

Storefront.V2
  -> CommerceNode.API api/storefront/stores/{storeKey}/pages/{slug}
  -> CommerceNode.API api/storefront/stores/{storeKey}/pages/navigation

StorefrontPage
  StoreId
  PageKey?              // stable known content slot, nullable for custom pages
  Slug                  // active public slug
  Title
  Intro
  BodyHtml
  IsPublished
  IncludeInSitemap
  IncludeInNavigation
  NavigationLocation?
  DisplayOrder
  SEO fields
```

Forbidden:

```text
ControlPlane.Web -> CommerceNode.API
Storefront.V2 -> ControlPlane.API
StorefrontPage -> AppDbContext
StorefrontPage.PageKey -> contact/cart/checkout/account component registry
```

## 6. Model Changes

Additive fields on `StorefrontPage`:

| Field | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `PageKey` | `string` max 80 | yes | Stable template/system name. Null means custom page. |
| `DisplayOrder` | `int` | no | Default `0`; used by manager and navigation projections. |
| `IncludeInNavigation` | `bool` | no | Default `false`. Separate from sitemap. |
| `NavigationLocation` | `string` max 50 | yes | Allowlisted values such as `header`, `footer_company`, `footer_support`, `footer_legal`. |

Indexes:

- Keep existing unique `(store_id, slug)` behavior.
- Add filtered unique index on `(store_id, page_key)` where `page_key IS NOT NULL AND archived_at IS NULL`.
- Add index `(store_id, page_key, archived_at)`.
- Add index `(store_id, include_in_navigation, is_published, archived_at, display_order)`.

Validation:

- `PageKey` must be null or one of the code catalog keys.
- A page key can be mapped to at most one active page per store.
- `NavigationLocation` must be null or one of the allowlisted locations.
- `IncludeInNavigation = true` should require `NavigationLocation`.
- Public navigation returns only published, non-archived pages.
- Draft shell creation creates `IsPublished = false`, `IncludeInSitemap = false`, `IncludeInNavigation = false`.

## 7. Application Contracts

Add or extend DTOs under:

```text
BlazorShop.Application/CommerceNode/StorefrontPages
```

Suggested new DTOs:

```csharp
public sealed record StorefrontPageTemplateDefinitionDto(
    string PageKey,
    string DisplayName,
    string DefaultSlug,
    string DefaultTitle,
    bool RequiredForReadiness,
    string? DefaultNavigationLocation,
    int DisplayOrder);

public sealed record StorefrontPageTemplateStatusDto(
    string PageKey,
    string DisplayName,
    bool RequiredForReadiness,
    string DefaultSlug,
    string DefaultTitle,
    string Status,
    StorefrontPageSummaryDto? MappedPage,
    IReadOnlyList<StorefrontPageSummaryDto> SuggestedExistingPages);

public sealed record CreatePageFromTemplateRequest(
    string? Slug = null,
    string? Title = null);

public sealed record MapPageTemplateRequest(
    string PageKey);

public sealed record UpdatePageNavigationRequest(
    int DisplayOrder,
    bool IncludeInNavigation,
    string? NavigationLocation);

public sealed record StorefrontPageNavigationLinkDto(
    string PageKey,
    string Slug,
    string Title,
    string? NavigationLocation,
    int DisplayOrder);
```

Status values:

- `missing`
- `mapped_draft`
- `mapped_published`
- `mapped_archived` should not normally appear for active mapping because active mapping excludes archived rows.
- `candidate_unmapped` for suggested pages with matching slug/title but no `PageKey`.

Service additions:

```csharp
public interface IStorefrontPageTemplateService
{
    IReadOnlyList<StorefrontPageTemplateDefinitionDto> ListDefinitions();

    Task<ServiceResponse<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStatusAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> CreateDraftFromTemplateAsync(
        string pageKey,
        CreatePageFromTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> MapExistingPageAsync(
        Guid pagePublicId,
        MapPageTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> ClearPageKeyAsync(
        Guid pagePublicId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<IReadOnlyList<StorefrontPageNavigationLinkDto>>> ListNavigationLinksAsync(
        CancellationToken cancellationToken = default);
}
```

Implementation rule:

- Template service can internally reuse `StorefrontPageService` where it fits, but it must enforce page-key allowlist and store scope itself.
- Do not let public/client requests set `StoreId`.
- Do not let public storefront APIs expose draft/unpublished pages.

## 8. Phase Plan

### Phase 0 - Baseline And Contract Inventory

Goal: freeze existing behavior before adding template governance.

Tasks:

- Confirm current `StorefrontPage` entity fields and migrations.
- Confirm `StorefrontPageService` public slug lookup only returns published pages.
- Confirm Control Plane page manager still calls Control Plane API only.
- Confirm existing store-scope tests remain passing.
- Inventory current header/footer hard-coded content links.
- Add or update tests for current page CRUD if a focused coverage gap blocks safe extension.
- Record that contact/cart/checkout/account are functional component surfaces and not page templates.

Exit criteria:

- Existing dynamic page route `/pages/{slug}` remains the baseline.
- No schema change in this phase.
- Existing page tests pass.

Suggested commit:

```text
docs: plan basic page content core
```

### Phase 1 - Page Metadata Schema

Goal: add the minimum metadata required for template mapping and ordered content navigation.

Tasks:

- Add nullable `PageKey` to `StorefrontPage`.
- Add `DisplayOrder`.
- Add `IncludeInNavigation`.
- Add nullable `NavigationLocation`.
- Configure columns in `CommerceNodeDbContext`.
- Add filtered unique index for active `PageKey` per store.
- Add navigation lookup index.
- Add Commerce Node migration only.
- Update page DTOs to include new fields where admin needs them.
- Keep public page DTO minimal; only expose new fields publicly if Storefront needs them.
- Update create/update validation so page key and navigation location are allowlisted when present.

Exit criteria:

- Migration touches `CommerceNodeDbContext` only.
- Existing create/update requests remain compatible.
- Custom pages with `PageKey = null` still work.
- A store cannot have two active pages mapped to the same `PageKey`.

Suggested commit:

```text
feat(commerce-node): add storefront page content metadata
```

### Phase 2 - Template Catalog And Status Service

Goal: give admin a reliable completeness map without putting functional components into page model.

Tasks:

- Add code-owned template catalog definitions for required and optional content slots.
- Exclude `generic`, `contact`, `cart`, `checkout`, `account`, and `not_found`.
- Add `IStorefrontPageTemplateService`.
- Implement template status calculation:
  - mapped published.
  - mapped draft.
  - missing.
  - suggested existing pages by default slug/title.
- Implement create draft from template.
- Implement map existing page to template.
- Implement clear mapping.
- Ensure create draft uses placeholder shell content only:
  - draft status.
  - default title/slug.
  - non-final placeholder body.
  - sitemap/navigation off.
- Add service tests for:
  - missing template detection.
  - mapping existing page.
  - duplicate page key conflict.
  - create draft from template.
  - custom page remains outside readiness.
  - contact is not required and not in catalog.

Exit criteria:

- Required content readiness can be computed per store.
- Existing pages can be mapped without rewriting content.
- No functional component is represented by `PageKey`.

Suggested commit:

```text
feat(commerce-node): add page template readiness service
```

### Phase 3 - Commerce Node And Control Plane API Surface

Goal: expose template readiness through the existing V2 API boundaries.

Commerce Node admin endpoints:

```text
GET    api/commerce/admin/pages/templates
GET    api/commerce/admin/pages/template-status
POST   api/commerce/admin/pages/templates/{pageKey}/draft
PUT    api/commerce/admin/pages/{pagePublicId}/template
DELETE api/commerce/admin/pages/{pagePublicId}/template
PUT    api/commerce/admin/pages/{pagePublicId}/navigation
```

Storefront endpoint:

```text
GET api/storefront/stores/{storeKey}/pages/navigation
```

Control Plane gateway endpoints:

```text
GET    api/controlplane/commerce/stores/{storePublicId}/pages/templates
GET    api/controlplane/commerce/stores/{storePublicId}/pages/template-status
POST   api/controlplane/commerce/stores/{storePublicId}/pages/templates/{pageKey}/draft
PUT    api/controlplane/commerce/stores/{storePublicId}/pages/{pagePublicId}/template
DELETE api/controlplane/commerce/stores/{storePublicId}/pages/{pagePublicId}/template
PUT    api/controlplane/commerce/stores/{storePublicId}/pages/{pagePublicId}/navigation
```

Tasks:

- Add explicit request/response DTOs.
- Add stable operation IDs and short summaries.
- Add expected error schemas.
- Add required body metadata for write endpoints.
- Add security metadata for protected endpoints.
- Keep Storefront navigation endpoint anonymous/public but store-route scoped.
- Reuse `commerce.pages.read` for template/status read.
- Reuse `commerce.pages.write` for draft/create/map/navigation write.
- Add contract tests for OpenAPI operation metadata and schemas.
- Add gateway tests proving Control Plane API forwards with `storeKey` and no node credentials leak to Web.

Exit criteria:

- Control Plane Web still has no direct Commerce Node calls.
- Public Storefront endpoint returns only published navigation links.
- API contracts satisfy `docs/architecture/09-api-contract-standards.md`.

Suggested commit:

```text
feat(control-plane): expose page template readiness APIs
```

### Phase 4 - Control Plane Page Manager UI

Goal: make content completeness visible and actionable to admin.

Tasks:

- Extend `/commerce-admin/pages`.
- Add a "Content readiness" panel above or beside the existing page list.
- Show required and optional template rows:
  - display name.
  - mapped page title/slug.
  - status badge: missing, draft, published.
  - default slug.
  - actions.
- Actions:
  - create draft shell for missing template.
  - map existing page for suggested candidates.
  - open edit drawer for mapped page.
  - clear mapping when needed.
- Extend page drawer with:
  - page key display/mapping state.
  - display order.
  - include in navigation.
  - navigation location selector.
- Do not add contact form controls.
- Do not add page component selector.
- Do not add WYSIWYG/page builder.
- Preserve existing search/status/paging behavior.
- Show API response messages directly.
- Hide/disable write actions when user lacks write permission if current UI permission plumbing supports it; otherwise rely on API 403 and show message.

Exit criteria:

- Admin can see which content templates are missing for the selected store.
- Admin can create draft shells without final content being auto-filled.
- Admin can map old pages into template slots.
- Browser network shows only Control Plane API calls.

Suggested commit:

```text
feat(control-plane-web): add page content readiness manager
```

### Phase 5 - Storefront Content Navigation

Goal: replace hard-coded content/legal/support page links with published page-driven links without creating a full menu framework.

Tasks:

- Add Storefront API client method for published page navigation links.
- Add a small Storefront navigation provider/cache if needed.
- Update footer content sections to render published links by `NavigationLocation`:
  - `footer_company`.
  - `footer_support`.
  - `footer_legal`.
- Consider header only for pages explicitly marked `header`; do not auto-promote required legal pages into header.
- Keep product/category/deal links owned by existing route constants.
- If navigation endpoint is unavailable, render a safe minimal fallback that does not create broken legal/support links.
- Do not include contact/cart/checkout/account in this projection.
- Add tests for published-only navigation.

Exit criteria:

- Draft pages are not rendered in header/footer links.
- Published pages with `IncludeInNavigation=true` appear in the selected location.
- Existing functional/product navigation is not rewritten.
- Hard-coded content links no longer point to missing pages when templates are not created.

Suggested commit:

```text
feat(storefront): render content page navigation links
```

### Phase 6 - Optional Home And Store Closed Content Hooks

Goal: support optional content overrides without risking core storefront availability.

Tasks:

- For `home_content`:
  - Keep `/` as canonical home route.
  - If a published page with `PageKey = home_content` or reserved slug `home` exists, allow home SEO/content sections to consume safe fields where current architecture already expects this.
  - Do not replace the whole home page with arbitrary `BodyHtml` unless a separate design approves it.
- For `store_closed_content`:
  - Keep the current maintenance page as fallback.
  - Optionally read a published mapped page to override maintenance copy/body.
  - If lookup fails, show existing maintenance message.
- Add tests proving missing optional content does not break home or maintenance.

Exit criteria:

- Home and maintenance still work with zero mapped pages.
- Optional mapped content can enhance those pages without becoming a hard dependency.

Suggested commit:

```text
feat(storefront): support optional page content hooks
```

### Phase 7 - Admin Preview For Draft Content

Goal: let managers preview unpublished content without exposing drafts publicly.

Tasks:

- Add protected admin preview endpoint through Control Plane gateway.
- Prefer preview by `pagePublicId`, not by public slug.
- Require `commerce.pages.read`.
- Return preview DTO with body/SEO but never make public slug return drafts.
- Storefront preview rendering should be clearly separated from public route, for example:
  - Control Plane preview panel, or
  - signed/admin-only preview URL if later approved.
- Do not add preview tokens unless needed for browser isolation.
- Add tests:
  - public `/pages/{slug}` still 404 for draft.
  - admin preview can read draft through protected path.
  - other-store draft cannot be previewed.

Exit criteria:

- Manager can preview draft content.
- Public storefront cannot access draft content.
- Preview does not require Storefront V2 to call Control Plane directly.

Suggested commit:

```text
feat(control-plane): add protected page draft preview
```

### Phase 8 - Cache, Invalidation, And Sitemap Alignment

Goal: keep readiness/navigation/sitemap behavior deterministic after page changes.

Tasks:

- If template status or navigation is cached, use explicit per-store cache keys:
  - `store-pages:{storeId}:template-status`.
  - `store-pages:{storeId}:navigation`.
  - `store-pages:{storeId}:sitemap`.
- Invalidate affected keys on:
  - create page.
  - update page.
  - archive page.
  - map/clear page key.
  - navigation update.
  - publish/unpublish.
- Ensure sitemap still includes only published pages with `IncludeInSitemap=true`.
- Ensure navigation only includes published pages with `IncludeInNavigation=true`.
- Add update-then-read tests for cache invalidation.

Exit criteria:

- Admin changes are visible on next readiness/navigation read.
- Cache is scoped to one store.
- No cross-store invalidation or leakage.

Suggested commit:

```text
perf(commerce-node): cache page readiness navigation projections
```

### Phase 9 - QA, Docs, And Release Gate

Goal: finish with focused verification and clear QA checklist coverage.

Tasks:

- Update `QA-CommerceNode.todo.md`.
- Update `QA-ControlPlane.todo.md`.
- Update `QA-StorefrontV2.todo.md`.
- Add Commerce Node tests for:
  - page key allowlist.
  - duplicate active page key per store.
  - same page key allowed across different stores.
  - template status.
  - draft shell creation.
  - mapping existing page.
  - public navigation published-only filter.
- Add Control Plane gateway tests for:
  - `storeKey` forwarding.
  - permission enforcement.
  - no direct browser exposure of node credentials.
- Add Storefront tests or Playwright QA for:
  - footer/header content links.
  - missing templates do not break layout.
  - published mapped page renders at `/pages/{slug}`.
- Run focused builds:
  - CommerceNode API.
  - ControlPlane API.
  - ControlPlane Web.
  - Storefront V2.
- Run focused tests before commit.

Exit criteria:

- QA checklist entries are updated with evidence.
- Required content readiness is visible in manager.
- Public page route remains backward compatible.
- Contact/cart/checkout/account remain out of page content core.

Suggested commit:

```text
test(v2): add basic page content core qa coverage
```

## 9. QA Checklist To Add

### CommerceNode

- [ ] Template catalog returns required content keys and excludes `generic`.
- [ ] Template catalog excludes `contact`, `cart`, `checkout`, and `account`.
- [ ] Template status marks missing required pages.
- [ ] Template status marks mapped draft pages.
- [ ] Template status marks mapped published pages.
- [ ] Template status suggests existing page by default slug/title.
- [ ] Creating draft from template creates unpublished page.
- [ ] Creating draft from template does not enable sitemap/navigation.
- [ ] Mapping existing page sets `PageKey`.
- [ ] Clearing mapping returns page to custom/unmapped state.
- [ ] Duplicate active `PageKey` in same store is rejected.
- [ ] Same `PageKey` in different stores is allowed.
- [ ] Archived mapped page does not block new active mapping if filtered unique index is implemented as planned.
- [ ] Public navigation returns only published pages.
- [ ] Public navigation excludes draft pages.
- [ ] Public navigation excludes pages not marked `IncludeInNavigation`.
- [ ] Sitemap behavior remains based on `IncludeInSitemap`, not navigation.

### ControlPlane

- [ ] Pages readiness panel loads for selected store.
- [ ] Missing required templates are visible.
- [ ] Create draft shell works from readiness panel.
- [ ] Map existing page works.
- [ ] Clear mapping works.
- [ ] Display order save works.
- [ ] Navigation location save works.
- [ ] Pages list still uses `pageNumber/pageSize`.
- [ ] Pages manager calls Control Plane API only.
- [ ] User without `commerce.pages.write` cannot create/map/update navigation.
- [ ] Contact form is not shown as required page readiness item.

### StorefrontV2

- [ ] Published mapped page renders at `/pages/{slug}`.
- [ ] Draft mapped page returns 404 at `/pages/{slug}`.
- [ ] Footer company/support/legal links use published content page navigation when available.
- [ ] Draft content pages do not appear in navigation.
- [ ] Missing template pages do not create broken footer/header links.
- [ ] Contact/cart/checkout/account links are not sourced from page template catalog.
- [ ] Store closed page still works when no `store_closed_content` page exists.
- [ ] Home page still works when no `home_content` page exists.

## 10. Failure Modes Registry

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Page system becomes component registry | Later WASM migration becomes expensive. | Exclude contact/cart/checkout/account and remove `ComponentKind` from page model. |
| Admin cannot tell missing legal pages | Store quality suffers. | Template readiness map with required content keys. |
| Existing pages need recreation | Wastes admin work and can break slugs. | Support mapping existing pages to `PageKey`. |
| Duplicate page key per store | Readiness and navigation become ambiguous. | Filtered unique index and service validation. |
| Draft leaks publicly | Legal/support draft content may be visible. | Public route and navigation filter `IsPublished`. Admin preview protected only. |
| Hard-coded links 404 | Footer/header can point to pages that do not exist. | Move content/legal/support links to published navigation projection. |
| Optional home/maintenance content breaks fallback | Storefront availability suffers. | Optional hooks only; keep code fallback required. |
| Store A reads Store B readiness/navigation | Multi-store data leak. | Use current store context and existing store-scope tests. |
| API contract drift | Control Plane/Storefront clients become brittle. | Operation IDs, DTOs, validation metadata, contract tests. |
| Legacy schema churn | Migration risk and boundary violation. | CommerceNodeDbContext only. |

## 11. Alternatives Considered

### Alternative 1: Page Type Enum On Every Page

Rejected.

An enum suggests all pages must fit a type. The approved model needs only stable known content slots plus unrestricted custom pages. A nullable `PageKey` with an allowlisted catalog is more flexible and less coupled.

### Alternative 2: Include `generic` In Catalog

Rejected.

Generic pages are not readiness requirements. Putting `generic` in the catalog adds noise and does not help admin know whether the store has required content.

### Alternative 3: Contact Page Template With ComponentKind

Rejected.

The user intends contact form, cart, checkout, and account to move toward independent WASM components. Binding them to page type mapping would create coupling and make migration harder.

### Alternative 4: Database Table For Template Definitions

Rejected for MVP.

The catalog should be code-owned and versioned with the application because it is a quality/readiness rule, not store-authored content.

### Alternative 5: Full Menu Framework

Rejected.

This phase only needs ordered content links. A generic menu builder can wait until product/category/custom link/menu needs are real.

## 12. Scorecards

### CEO Review

Score: 8/10.

The plan improves store launch quality by making required content gaps visible without blocking stores on a large CMS build.

### Design Review

Score: 7/10.

The Control Plane readiness panel is operational and useful. The main UX risk is crowding the existing pages screen; use compact status rows and drawer actions instead of a new page builder UI.

### Engineering Review

Score: 8/10.

The plan reuses existing page service, store scope, gateway, permissions, and public route. Main risk is API surface growth, controlled by contract tests and small endpoints.

### DX Review

Score: 7/10.

Explicit DTOs and template definitions make the feature easy to reason about. Keep names plain: `PageKey`, `TemplateStatus`, `NavigationLocation`, `DisplayOrder`.

## 13. Decision Audit Trail

| Decision | Status | Reason |
| --- | --- | --- |
| Extend `StorefrontPage` instead of replacing it | Approved | Existing implementation is store-scoped and already used by admin/public flows. |
| Use nullable `PageKey` | Approved | Supports known content templates while leaving custom pages free. |
| Keep catalog in code | Approved | Template requirements are product rules, not store content. |
| Exclude `generic` from catalog | Approved | Generic pages do not indicate readiness. |
| Exclude contact/cart/checkout/account | Approved | They are functional component/WASM surfaces, not page content. |
| Add display/navigation metadata | Approved | Needed for manager ordering and published content links. |
| Keep `/pages/{slug}` | Approved | Avoids breaking current public route and SEO path. |
| Keep error/not-found code fallback | Approved | Error pages must work even if DB/page content fails. |
| Keep maintenance fallback | Approved | Store closed behavior must not depend on optional content page. |
| Defer language-specific content | Approved | No localization content framework exists yet. |
| Defer password protection | Approved | Not needed for required store content and adds auth/cache complexity. |
| Use existing page permissions | Approved | Current read/write permissions match this phase; split later only if necessary. |

## 14. Recommended Implementation Order

1. Phase 0 - baseline and contract inventory.
2. Phase 1 - page metadata schema.
3. Phase 2 - template catalog and status service.
4. Phase 3 - Commerce Node and Control Plane APIs.
5. Phase 4 - Control Plane page manager UI.
6. Phase 5 - Storefront content navigation.
7. Phase 8 - cache/invalidation if projections are cached.
8. Phase 9 - QA/docs/release gate.

Phase 6 and Phase 7 are optional follow-up phases:

- Phase 6 only if home/maintenance content hooks are needed.
- Phase 7 only if manager draft preview is still required after the editor flow is usable.

## 15. Definition Of Done

This phase is done when:

- Each store can be evaluated against the required content template catalog.
- Admin can see missing required content pages.
- Admin can create draft shells from templates.
- Admin can map existing pages to template keys.
- Custom pages remain supported without catalog classification.
- Public `/pages/{slug}` still renders only published pages.
- Storefront navigation can render published content links by location/order.
- Contact form, cart, checkout, and account are not coupled to page templates.
- Commerce Node, Control Plane, and Storefront V2 boundaries remain intact.
- OpenAPI contracts and focused tests cover the new API surface.
- QA checklist files are updated with evidence after implementation.

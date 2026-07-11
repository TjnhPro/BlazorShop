# BlazorShop CommerceNode StorefrontPage Todo

Status: draft
Created: 2026-07-11
Scope: Store-scoped dynamic storefront informational pages rendered from CommerceNode data at `/pages/{slug}`.

## Goal

Add a StorefrontPage foundation so stores can create and render informational pages from data instead of maintaining one Razor page per static content route.

Examples:

- `/pages/privacy`
- `/pages/terms`
- `/pages/faq`
- `/pages/customer-service`
- `/pages/about-us`

This is a CommerceNode/StorefrontV2 feature with ControlPlane admin management.
It is not a CMS/page-builder project and it must not revive or extend legacy Presentation pages.

## Smartstore Reference Summary

Smartstore has a similar concept named `Topic`.

Useful concepts to learn from:

- Store-scoped public content.
- SEO slug based rendering.
- Published/draft behavior.
- Sitemap inclusion flag.
- Admin CRUD.
- HTML body rendering inside a storefront layout.

Not adopted in this MVP:

- Topic password protection.
- Widget zones.
- ACL/customer role restriction.
- Localization.
- Cookie consent type.
- Menu link resolver.
- PageBuilder/block architecture.
- Smartstore implementation code.

## Locked Decisions

| Area | Decision |
|---|---|
| Entity name | `StorefrontPage`. |
| Ownership | Each page belongs to one CommerceNode store. |
| Database | `CommerceNodeDbContext` only. |
| Public URL | `/pages/{slug}`. |
| Slug shape | One segment only. No slash/nested path. |
| Static old routes | Old routes such as `/privacy`, `/faq`, `/terms`, `/customer-service`, `/about-us` are removed and return 404. No redirect. |
| Body format | Raw HTML textarea in MVP. |
| Dangerous HTML | Reject the entire create/update request when unsafe HTML is detected. Do not auto-strip. |
| Images in HTML | `<img src>` must use local paths only. No external image URLs. |
| Links in HTML | `<a href>` may use local paths or `https://`. Reject dangerous protocols. |
| Media upload | No page media upload in this phase. Page media can be designed later. |
| Cache | No app-layer cache for pages in MVP. Updates should be visible on the next request. |
| Version field | No `version` in MVP. Use `updated_at`. |
| System name | No `system_name` in MVP. |
| Page type | No `page_type` in MVP. FAQ is rendered as plain HTML page. |
| Display order | No `display_order` in MVP. |
| Sorting | Admin list and sitemap pages sort by `updated_at desc`. |
| Default status | New page defaults to draft/unpublished. |
| Sitemap default | `include_in_sitemap=false` by default. |
| Robots default | `robots_index=true`, `robots_follow=true`, independent from sitemap. |
| Archive | Archive only. No hard delete. |
| Restore | No restore UI/API in MVP. |
| Slug uniqueness | `unique(store_id, slug)` across all pages, including archived pages. Archived slugs remain reserved. |
| Slug input | Slug is required. API normalizes before saving. No special message when normalized. |
| Slug change | Do not auto-create SEO redirects. Return normal update response. |
| Revision history | No revision history in MVP. Use timestamps and audit. |
| Preview draft | No public/admin draft preview API in MVP. |
| Menu/footer | Do not implement dynamic footer/header/menu links in this phase. Do not refactor navigation placeholders unless required for build. |
| Seed/demo data | No runtime auto seed. No SQL seed file. QA creates pages manually through API/UI. |
| Permissions | Add Storefront Pages permissions, separate from catalog/product permissions. |
| Audit | Reuse existing CommerceNode/admin audit pattern for create/update/archive where available. |
| UI framework | ControlPlane Web UI uses existing Tailwind + FontAwesome style. |
| Boundary | ControlPlane Web calls ControlPlane API only. ControlPlane API proxies to CommerceNode API. StorefrontV2 calls CommerceNode internal API only. |

## Non-Goals

- No page builder.
- No widget system.
- No dynamic menu/footer management.
- No page media library or upload UI.
- No page blocks/components.
- No Markdown parser.
- No WYSIWYG/rich text editor.
- No FAQ structured data in MVP.
- No password-protected pages.
- No localization.
- No ACL/customer role restrictions.
- No automatic redirects from old URLs.
- No automatic redirect on slug changes.
- No seed/demo data logic in runtime code.
- No ControlPlane direct database access to CommerceNode data.
- No `AppDbContext` changes.
- No legacy `BlazorShop.Presentation` changes.

## Current Code Facts

- Storefront V2 currently has placeholder Razor pages:
  - `Pages/About.razor`
  - `Pages/FAQ.razor`
  - `Pages/Privacy.razor`
  - `Pages/Terms.razor`
  - `Pages/CustomerService.razor`
- Those placeholder pages are not real business content and should be removed in this phase.
- Storefront V2 already has `NotFoundPage.razor` for catch-all 404 behavior.
- Storefront V2 already has `StorefrontApiClient`.
- Storefront V2 already has SEO and structured data composers.
- `StorefrontSitemapService` currently uses static route entries and catalog sitemap entries.
- CommerceNode internal Storefront APIs use `api/internal/*`.
- CommerceNode admin/control APIs use `api/commerce/*`.
- ControlPlane commerce admin gateway routes currently use canonical `api/controlplane/commerce/*` style for V2 admin UX.
- `SeoFieldsDto`, `SeoMetadataBuilder`, and product/category SEO patterns already exist and should be reused.
- Admin list APIs must use `pageNumber/pageSize` paging.

## Target Architecture

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/stores/{storePublicId}/pages
          -> StorefrontPageService
              -> CommerceNodeDbContext.storefront_page

StorefrontV2
  -> /pages/{slug}
      -> StorefrontApiClient
          -> CommerceNode.API api/internal/pages/slug/{slug}
              -> StorefrontPageService
                  -> CommerceNodeDbContext.storefront_page
```

Forbidden:

```text
ControlPlane.Web -> CommerceNode.API
StorefrontV2 -> ControlPlane.API
StorefrontPage -> AppDbContext
```

## Database Design

### New entity: `StorefrontPage`

Namespace:

```text
BlazorShop.Domain.Entities.CommerceNode
```

### New table: `storefront_page`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `id` | `uuid` | no | Primary key. Internal DB id. |
| `public_id` | `uuid` | no | Public/admin id used by APIs. |
| `store_id` | `uuid` | no | Required CommerceNode store id. |
| `slug` | `varchar(160)` | no | Required, normalized one-segment URL slug. |
| `title` | `varchar(200)` | no | Required page title. |
| `intro` | `varchar(1000)` | yes | Optional intro/lead text. |
| `body_html` | `text` | no | Required HTML body, max 100 KB by service validation. |
| `is_published` | `boolean` | no | Default false. |
| `include_in_sitemap` | `boolean` | no | Default false. |
| `meta_title` | `varchar(400)` | yes | Reuse SEO constraints where practical. |
| `meta_description` | `varchar(4000)` | yes | Reuse SEO constraints where practical. |
| `canonical_url` | `varchar(2048)` | yes | Optional canonical override. |
| `og_title` | `varchar(400)` | yes | Optional OG title. |
| `og_description` | `varchar(4000)` | yes | Optional OG description. |
| `og_image` | `varchar(2048)` | yes | Optional OG image URL. |
| `robots_index` | `boolean` | no | Default true. |
| `robots_follow` | `boolean` | no | Default true. |
| `created_at` | `timestamp with time zone` | no | Created timestamp. |
| `updated_at` | `timestamp with time zone` | no | Updated timestamp. |
| `archived_at` | `timestamp with time zone` | yes | Soft archive timestamp. |

### Indexes

- unique `public_id`
- unique `(store_id, slug)`
- index `(store_id, is_published, archived_at)`
- index `(store_id, include_in_sitemap, is_published, archived_at)`
- index `(store_id, updated_at)`

### Constraints and validation

Database:

- `slug` required, max length 160.
- `title` required, max length 200.
- `body_html` required.

Service/API validation:

- `slug` must normalize to non-empty.
- `slug` must not contain `/`.
- `title` max 200.
- `intro` max 1000.
- `body_html` max 100 KB.
- Reject duplicate `slug` for the store, including archived rows.
- Reject unsafe HTML.
- Reject external image URLs in `<img src>`.

### Slug rules

Input is required and normalized through existing `ISlugService`.

Examples:

| Input | Stored |
|---|---|
| `Privacy Policy` | `privacy-policy` |
| `privacy` | `privacy` |
| `customer-service` | `customer-service` |

Reject:

- empty after normalization
- `customer-service/shipping`
- duplicate within the same store

## HTML Safety Rules

Reject the full request if `body_html` contains unsafe content.

Block at minimum:

- `<script`
- `<iframe`
- `<object`
- `<embed`
- `javascript:`
- `data:`
- inline event attributes such as `onerror=`, `onclick=`, `onload=`
- external image URLs in `<img src>`, including `http://`, `https://`, protocol-relative `//`

Allow:

- local image paths in `<img src="/...">`
- local links in `<a href="/...">`
- external HTTPS links in `<a href="https://...">`

Do not auto-strip or rewrite unsafe HTML in MVP.

## Application Contracts

Folder:

```text
BlazorShop.Application/CommerceNode/StorefrontPages
```

DTOs:

```csharp
public sealed record StorefrontPageQuery(
    int PageNumber = 1,
    int PageSize = 25,
    string? Search = null,
    string Status = "all");

public sealed record StorefrontPageListItemDto(
    Guid PublicId,
    string Slug,
    string Title,
    bool IsPublished,
    bool IncludeInSitemap,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record StorefrontPageDetailDto(
    Guid PublicId,
    Guid StoreId,
    string Slug,
    string Title,
    string? Intro,
    string BodyHtml,
    bool IsPublished,
    bool IncludeInSitemap,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImage,
    bool RobotsIndex,
    bool RobotsFollow,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateStorefrontPageRequest(
    string Slug,
    string Title,
    string? Intro,
    string BodyHtml,
    bool IsPublished = false,
    bool IncludeInSitemap = false,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    string? OgTitle = null,
    string? OgDescription = null,
    string? OgImage = null,
    bool RobotsIndex = true,
    bool RobotsFollow = true);

public sealed record UpdateStorefrontPageRequest(
    string Slug,
    string Title,
    string? Intro,
    string BodyHtml,
    bool IsPublished,
    bool IncludeInSitemap,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImage,
    bool RobotsIndex,
    bool RobotsFollow);

public sealed record GetStorefrontPageSitemapEntry(
    string Slug,
    DateTimeOffset? LastModifiedUtc);
```

Service:

```csharp
public interface IStorefrontPageService
{
    Task<PagedResult<StorefrontPageListItemDto>> QueryAsync(
        Guid storeId,
        StorefrontPageQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> GetAsync(
        Guid storeId,
        Guid pagePublicId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> GetPublishedBySlugAsync(
        Guid storeId,
        string slug,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> CreateAsync(
        Guid storeId,
        CreateStorefrontPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StorefrontPageDetailDto>> UpdateAsync(
        Guid storeId,
        Guid pagePublicId,
        UpdateStorefrontPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> ArchiveAsync(
        Guid storeId,
        Guid pagePublicId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GetStorefrontPageSitemapEntry>> GetSitemapEntriesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);
}
```

Implementation:

```text
BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontPageService.cs
```

Rules:

- Service methods receive internal `Guid storeId`, not store public id.
- Controllers resolve current/admin store before calling the service.
- All queries are store-scoped.
- Public queries require `is_published=true` and `archived_at is null`.
- Admin list excludes archived pages.
- Status filter values: `all`, `published`, `draft`.
- Search matches `title` and `slug`.
- List sort: `updated_at desc`.
- Sitemap sort: `updated_at desc`.

## CommerceNode API Design

### Admin/control controller

Controller:

```text
CommerceStorefrontPagesController
```

Route:

```text
api/commerce/stores/{storePublicId:guid}/pages
```

Endpoints:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `api/commerce/stores/{storePublicId}/pages?pageNumber=&pageSize=&search=&status=` | Paged admin list. |
| `GET` | `api/commerce/stores/{storePublicId}/pages/{pagePublicId}` | Admin detail. |
| `POST` | `api/commerce/stores/{storePublicId}/pages` | Create draft/published page. |
| `PUT` | `api/commerce/stores/{storePublicId}/pages/{pagePublicId}` | Update page. |
| `DELETE` | `api/commerce/stores/{storePublicId}/pages/{pagePublicId}` | Archive page. |

Security:

- Reuse existing CommerceNode `api/commerce/*` node key/secret/IP behavior.
- Resolve `storePublicId` to internal `CommerceStore.Id`.
- Reject requests where store does not exist or is archived.

Audit:

- `storefront_page.created`
- `storefront_page.updated`
- `storefront_page.archived`

### Internal Storefront controller

Controller:

```text
StorefrontPagesController
```

Route:

```text
api/internal/pages
```

Endpoints:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `api/internal/pages/slug/{slug}` | Public Storefront page detail. |

Behavior:

- Resolve current store through existing Storefront store context.
- Normalize slug.
- Return not found for missing, draft, or archived pages.
- API unavailable is handled by Storefront client as service unavailable, not 404.

## Sitemap Design

Extend existing sitemap endpoint/pattern:

```text
GET api/internal/catalog/sitemap
```

Do not rename `GetPublicCatalogSitemap` in this phase.

Add property:

```csharp
public IReadOnlyList<GetStorefrontPageSitemapEntry> Pages { get; set; } = Array.Empty<GetStorefrontPageSitemapEntry>();
```

Sitemap inclusion rules:

- `store_id = current store`
- `is_published = true`
- `include_in_sitemap = true`
- `archived_at is null`
- sort by `updated_at desc`

Storefront sitemap output:

```text
/pages/{slug}
```

Remove old static sitemap entries:

- `/privacy`
- `/faq`
- `/terms`
- `/customer-service`
- `/about-us`

Keep product/category sitemap behavior unchanged.

## ControlPlane API Gateway Design

Canonical gateway routes:

```text
api/controlplane/commerce/stores/{storePublicId:guid}/pages
api/controlplane/commerce/stores/{storePublicId:guid}/pages/{pagePublicId:guid}
```

Endpoints:

| Method | Route |
|---|---|
| `GET` | `api/controlplane/commerce/stores/{storePublicId}/pages?pageNumber=&pageSize=&search=&status=` |
| `GET` | `api/controlplane/commerce/stores/{storePublicId}/pages/{pagePublicId}` |
| `POST` | `api/controlplane/commerce/stores/{storePublicId}/pages` |
| `PUT` | `api/controlplane/commerce/stores/{storePublicId}/pages/{pagePublicId}` |
| `DELETE` | `api/controlplane/commerce/stores/{storePublicId}/pages/{pagePublicId}` |

Responsibilities:

- Check ControlPlane user permissions.
- Resolve ControlPlane store/node mapping.
- Forward to CommerceNode with node credentials and store scope.
- Preserve API response envelope.
- Return CommerceNode response message to Web.
- Do not expose CommerceNode base URL/key/secret to Web.

Permissions:

- Add `commerce.pages.read`.
- Add `commerce.pages.write`.
- Map permissions to existing platform/admin permission seed pattern.

## ControlPlane Web UI Design

Add a Commerce Admin Pages surface.

Suggested route:

```text
/commerce-admin/pages
```

Navigation:

- Place under existing `Commerce Admin` group.
- Do not refactor header/footer/menu placeholders in this phase.

List:

- Store selector.
- Search box: title + slug.
- Status filter: `all`, `published`, `draft`.
- Paged table using `pageNumber/pageSize`.
- Columns:
  - Title.
  - Slug/Public URL: `/pages/{slug}`.
  - Published status.
  - Sitemap flag.
  - Updated timestamp.
  - Actions.
- Published pages show an `Open public page` link.
- Draft pages show `/pages/{slug}` as text only or disabled link.

Create/Edit drawer:

- Use existing right drawer pattern.
- No rich text editor.
- Field groups:
  1. SEO
  2. Basic info
  3. Content
  4. Publish controls

Fields:

- Slug, required.
- Title, required.
- Intro.
- Body HTML textarea, required.
- Meta title.
- Meta description.
- Canonical URL.
- OG title.
- OG description.
- OG image.
- Robots index.
- Robots follow.
- Published checkbox.
- Include in sitemap checkbox.

Actions:

- Create.
- Save.
- Archive.
- Open public page when published.

UI behavior:

- Display API response `message`.
- Rely on API validation. Do not duplicate complex HTML validation in UI.
- Keep textarea simple.
- Do not add preview draft.
- Do not add upload media.
- Do not add dynamic menu/footer link management.

## Storefront V2 Design

### Route

Add:

```text
Pages/StorefrontPage.razor
@page "/pages/{Slug}"
```

Behavior:

- Load page from `StorefrontApiClient.GetPublishedPageBySlugAsync`.
- If not found: apply 404 response headers and render `NotFoundState`.
- If service unavailable: render service unavailable state and noindex metadata.
- If found: render title, intro, and body HTML.

### Renderer component

Create reusable content component:

```text
Components/Pages/StorefrontPageContent.razor
```

Parameters:

- `Title`
- `Intro`
- `BodyHtml`

Markup:

- Use the existing storefront visual style.
- Include wrapper content styling.
- Render body through `MarkupString`.

### SEO

Add a page-specific SEO composer method:

```csharp
ComposeStorefrontPageAsync(GetStorefrontPage page)
```

Map:

- `MetaTitle`
- `MetaDescription`, fallback to `Intro`
- `CanonicalUrl`
- `OgTitle`
- `OgDescription`
- `OgImage`
- `RobotsIndex`
- `RobotsFollow`
- relative path `/pages/{slug}`

Structured data:

- Use existing `ComposeWebPageAsync`.
- Do not add FAQ structured data in MVP.

### Remove static placeholder pages

Delete:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/About.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/FAQ.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Privacy.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Terms.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CustomerService.razor`

Route behavior after deletion:

- `/privacy` returns 404.
- `/faq` returns 404.
- `/terms` returns 404.
- `/customer-service` returns 404.
- `/about-us` returns 404.

No redirects.

Update route constants:

- Remove unused old static constants if references are gone.
- Add `StorefrontRoutes.Page(slug)` helper.
- Do not add new hard-coded footer/header links to `/pages/{slug}` in this phase.

## Phase Plan

### Phase 0 - Baseline Audit

- [ ] Confirm no existing `StorefrontPage` entity/service exists.
- [ ] Confirm current placeholder static pages and route constants.
- [ ] Confirm current `StorefrontSitemapService` static route entries.
- [ ] Confirm current ControlPlane commerce admin client/gateway pattern.
- [ ] Confirm current CommerceNode service response and audit patterns.
- [ ] Confirm existing permission seed pattern for ControlPlane permissions.

Exit gate:

- [ ] No legacy project needs to be edited.
- [ ] Boundaries are still `ControlPlane.Web -> ControlPlane.API -> CommerceNode.API` and `StorefrontV2 -> CommerceNode.API`.

Suggested commit:

```text
docs: plan storefront page foundation
```

### Phase 1 - Domain, DbContext, Migration

- [ ] Add `StorefrontPage` entity under `BlazorShop.Domain.Entities.CommerceNode`.
- [ ] Add `DbSet<StorefrontPage>` to `CommerceNodeDbContext`.
- [ ] Configure `storefront_page` table with snake_case columns.
- [ ] Add indexes and constraints from Database Design.
- [ ] Add CommerceNode EF migration.
- [ ] Verify migration targets `CommerceNodeDbContext` only.
- [ ] Run build for Domain/Infrastructure if practical.

Exit gate:

- [ ] Migration does not touch `ControlPlaneDbContext`.
- [ ] Migration does not touch `AppDbContext`.
- [ ] `unique(store_id, slug)` reserves archived slugs too.

Suggested commit:

```text
feat(commerce-node): add storefront page schema
```

### Phase 2 - Application DTOs, Validation, Service

- [ ] Add StorefrontPage DTOs and query contracts.
- [ ] Add `IStorefrontPageService`.
- [ ] Implement `StorefrontPageService`.
- [ ] Reuse `ISlugService` for normalization.
- [ ] Add HTML safety validation.
- [ ] Enforce body max 100 KB.
- [ ] Enforce local-only `<img src>`.
- [ ] Allow local/HTTPS `<a href>`.
- [ ] Implement paged admin query.
- [ ] Implement published-by-slug query.
- [ ] Implement create/update/archive.
- [ ] Implement sitemap entries.
- [ ] Add audit hooks/events where existing pattern makes this practical.
- [ ] Add focused tests for slug, paging, archive, sitemap, and HTML validation.

Exit gate:

- [ ] Draft page is not returned by public query.
- [ ] Archived page is not returned by public/admin list.
- [ ] Dangerous HTML rejects the whole request.
- [ ] External image URL rejects the whole request.
- [ ] Local image URL is accepted.

Suggested commit:

```text
feat(commerce-node): add storefront page service
```

### Phase 3 - CommerceNode APIs

- [ ] Add `CommerceStorefrontPagesController` under `api/commerce/stores/{storePublicId}/pages`.
- [ ] Resolve `storePublicId` to internal store id before calling service.
- [ ] Add paged list endpoint.
- [ ] Add detail endpoint.
- [ ] Add create endpoint.
- [ ] Add update endpoint.
- [ ] Add archive endpoint.
- [ ] Add `StorefrontPagesController` under `api/internal/pages`.
- [ ] Add `GET api/internal/pages/slug/{slug}`.
- [ ] Return not found for missing/draft/archived public pages.
- [ ] Ensure response envelope/message pattern matches nearby controllers.
- [ ] Add controller tests where existing test style supports it.

Exit gate:

- [ ] `api/commerce/*` endpoint is admin/control scoped.
- [ ] `api/internal/*` endpoint is Storefront scoped.
- [ ] Store A cannot read Store B page by slug.

Suggested commit:

```text
feat(commerce-node): expose storefront page APIs
```

### Phase 4 - Sitemap Integration

- [ ] Extend `GetPublicCatalogSitemap` with `Pages`.
- [ ] Add `GetStorefrontPageSitemapEntry`.
- [ ] Extend `PublicCatalogService.GetPublishedSitemapAsync` or equivalent sitemap service to include StorefrontPage entries.
- [ ] Sort page sitemap entries by `updated_at desc`.
- [ ] Update CommerceNode internal sitemap endpoint response.
- [ ] Update Storefront `StorefrontSitemapService` to render `/pages/{slug}` entries.
- [ ] Remove old static sitemap entries for placeholder pages.
- [ ] Keep product/category sitemap behavior unchanged.

Exit gate:

- [ ] Published page with `include_in_sitemap=true` appears as `/pages/{slug}`.
- [ ] Draft page does not appear.
- [ ] Published page with `include_in_sitemap=false` does not appear.
- [ ] Old `/privacy`, `/faq`, `/terms`, `/customer-service`, `/about-us` sitemap entries are gone.

Suggested commit:

```text
feat(storefront): include dynamic pages in sitemap
```

### Phase 5 - ControlPlane API Gateway

- [ ] Add ControlPlane gateway service methods for pages.
- [ ] Add ControlPlane API routes under `api/controlplane/commerce/stores/{storePublicId}/pages`.
- [ ] Add read/write permission checks.
- [ ] Add `commerce.pages.read` and `commerce.pages.write` permissions using existing seed/pattern.
- [ ] Forward requests to CommerceNode using node credentials.
- [ ] Preserve API response envelope.
- [ ] Preserve CommerceNode response message.
- [ ] Add focused tests for route/gateway behavior where current tests allow.

Exit gate:

- [ ] ControlPlane Web does not need CommerceNode URL/key/secret.
- [ ] Read routes require read permission.
- [ ] Write/archive routes require write permission.

Suggested commit:

```text
feat(control-plane): add storefront page gateway
```

### Phase 6 - ControlPlane Web UI

- [ ] Add typed page methods to the ControlPlane commerce/catalog client.
- [ ] Add `/commerce-admin/pages` route.
- [ ] Add nav link under `Commerce Admin`.
- [ ] Add store selector.
- [ ] Add search by title/slug.
- [ ] Add status filter: all/published/draft.
- [ ] Add paged table.
- [ ] Show `/pages/{slug}` in table.
- [ ] Show public open link only when published.
- [ ] Add create/edit drawer.
- [ ] Add SEO group.
- [ ] Add basic info group.
- [ ] Add body HTML textarea.
- [ ] Add publish and sitemap toggles.
- [ ] Add archive action.
- [ ] Display API messages.
- [ ] Do not add preview draft.
- [ ] Do not add media upload.
- [ ] Do not refactor footer/header/menu placeholders.

Exit gate:

- [ ] List uses pageNumber/pageSize.
- [ ] Web calls only ControlPlane API.
- [ ] UI follows existing Tailwind + FontAwesome style.
- [ ] Missing permissions surface API message cleanly.

Suggested commit:

```text
feat(control-plane-web): add storefront page admin UI
```

### Phase 7 - Storefront V2 Rendering

- [ ] Add Storefront API client method for published page by slug.
- [ ] Add shared model under Web.SharedV2 if needed by Storefront client.
- [ ] Add `StorefrontPage.razor` route `/pages/{slug}`.
- [ ] Add `StorefrontPageContent.razor` content component.
- [ ] Add `StorefrontSeoComposer.ComposeStorefrontPageAsync`.
- [ ] Use `ComposeWebPageAsync` for structured data.
- [ ] Render service unavailable state when CommerceNode API is unavailable.
- [ ] Render 404 state for missing/draft/archived pages.
- [ ] Apply noindex metadata for service unavailable.
- [ ] Render body via `MarkupString`.
- [ ] Verify wrapper CSS makes normal HTML content readable.

Exit gate:

- [ ] `/pages/{slug}` renders title, intro, and body HTML.
- [ ] Missing page returns real 404.
- [ ] Draft page returns real 404.
- [ ] API unavailable does not become 404.

Suggested commit:

```text
feat(storefront): render dynamic storefront pages
```

### Phase 8 - Remove Static Placeholder Pages and Cleanup Docs

- [ ] Delete placeholder static Razor pages:
  - [ ] `About.razor`
  - [ ] `FAQ.razor`
  - [ ] `Privacy.razor`
  - [ ] `Terms.razor`
  - [ ] `CustomerService.razor`
- [ ] Remove unused old route constants if build allows.
- [ ] Add `StorefrontRoutes.Page(slug)` helper.
- [ ] Do not add redirects for old routes.
- [ ] Do not add new hard-coded footer/header links.
- [ ] Update architecture docs if route ownership documentation needs clarification.
- [ ] Update `AGENTS.md` only if a new durable rule is introduced.

Exit gate:

- [ ] Old routes return 404.
- [ ] Build has no stale references to removed pages/constants.
- [ ] Docs state dynamic informational pages now use `/pages/{slug}`.

Suggested commit:

```text
refactor(storefront): remove static placeholder pages
```

### Phase 9 - QA Checklist Updates and Verification

- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-ControlPlane.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md`.
- [ ] Add QA cases listed below.
- [ ] Run focused API tests for CommerceNode pages.
- [ ] Run ControlPlane Web visible Playwright QA if UI changed and operator observation is requested.
- [ ] Run Storefront V2 visible Playwright QA for `/pages/{slug}` if requested.
- [ ] Fix discovered bugs in the same phase.
- [ ] Mark checklist items only after verification.

Exit gate:

- [ ] QA files contain StorefrontPage coverage.
- [ ] Verified items are marked with evidence/date.
- [ ] No known regression in product/category/search/auth/cart routes.

Suggested commit:

```text
test(v2): document storefront page qa coverage
```

## QA Checklist To Add

### CommerceNode

- [ ] `GET api/commerce/stores/{storePublicId}/pages` returns paged response.
- [ ] Admin page list search matches title.
- [ ] Admin page list search matches slug.
- [ ] Status filter `all` includes draft and published non-archived pages.
- [ ] Status filter `published` includes published only.
- [ ] Status filter `draft` includes draft only.
- [ ] Create page defaults to draft when `is_published` is omitted/false.
- [ ] Create page requires slug.
- [ ] Slug is normalized before save.
- [ ] Duplicate slug in same store is rejected.
- [ ] Duplicate slug is rejected even if old page is archived.
- [ ] Dangerous HTML `<script>` is rejected.
- [ ] Dangerous HTML `javascript:` is rejected.
- [ ] Dangerous inline event such as `onerror=` is rejected.
- [ ] External image URL in `<img src>` is rejected.
- [ ] Local image URL in `<img src="/media/...">` is accepted.
- [ ] External HTTPS link in `<a href>` is accepted.
- [ ] Body above 100 KB is rejected.
- [ ] Draft page is not returned from internal public slug endpoint.
- [ ] Archived page is not returned from internal public slug endpoint.
- [ ] Published page is returned from internal public slug endpoint.
- [ ] Archive hides page from admin list.
- [ ] Archive reserves slug.
- [ ] Sitemap entries include only published pages with `include_in_sitemap=true`.
- [ ] Store A cannot read Store B page by slug.

### ControlPlane

- [ ] Pages nav item renders under Commerce Admin.
- [ ] Pages list loads after selecting a store.
- [ ] Pages list calls ControlPlane API only.
- [ ] Browser network has no direct CommerceNode page API calls.
- [ ] Page list uses pageNumber/pageSize.
- [ ] Search title works.
- [ ] Search slug works.
- [ ] Status filter works.
- [ ] Create draft page works.
- [ ] Edit page works.
- [ ] Publish page works.
- [ ] Include in sitemap toggle works.
- [ ] Archive page works.
- [ ] Published page shows `Open public page` link.
- [ ] Draft page does not expose an active public open link.
- [ ] API validation messages display from response message.
- [ ] User without write permission cannot create/update/archive pages.

### StorefrontV2

- [ ] `/pages/{slug}` renders published page title.
- [ ] `/pages/{slug}` renders intro.
- [ ] `/pages/{slug}` renders body HTML.
- [ ] Local image path in body renders.
- [ ] HTTPS anchor link renders.
- [ ] Missing page returns 404.
- [ ] Draft page returns 404.
- [ ] Archived page returns 404.
- [ ] CommerceNode API unavailable renders service unavailable, not 404.
- [ ] Page SEO uses page meta fields.
- [ ] Page structured data uses WebPage.
- [ ] Sitemap includes `/pages/{slug}` when published and included.
- [ ] Sitemap excludes draft page.
- [ ] Sitemap excludes published page with `include_in_sitemap=false`.
- [ ] Old `/privacy`, `/faq`, `/terms`, `/customer-service`, `/about-us` routes return 404.

## Review and Fix Checklist

Use after each phase:

- [ ] Does this phase preserve Layered Architecture?
- [ ] Did it avoid legacy `BlazorShop.Presentation`?
- [ ] Did it avoid `AppDbContext`?
- [ ] Did it keep ControlPlane Web behind ControlPlane API?
- [ ] Did it keep Storefront V2 behind CommerceNode internal API?
- [ ] Are all page queries store-scoped?
- [ ] Are list endpoints paged with `pageNumber/pageSize`?
- [ ] Are unsafe HTML and unsafe URLs rejected by API/service?
- [ ] Are public missing/draft/archived pages treated as 404?
- [ ] Are service unavailable cases not mislabeled as 404?
- [ ] Are old static placeholder routes removed from sitemap?
- [ ] Are QA checklist updates included before marking the phase complete?

## Open Questions Before Implementation

None currently. The MVP scope is locked by the decisions above.

If implementation reveals that an existing shared helper makes one of these decisions expensive, stop and update this plan before widening scope.

## Suggested Commit Order

1. `docs: plan storefront page foundation`
2. `feat(commerce-node): add storefront page schema`
3. `feat(commerce-node): add storefront page service`
4. `feat(commerce-node): expose storefront page APIs`
5. `feat(storefront): include dynamic pages in sitemap`
6. `feat(control-plane): add storefront page gateway`
7. `feat(control-plane-web): add storefront page admin UI`
8. `feat(storefront): render dynamic storefront pages`
9. `refactor(storefront): remove static placeholder pages`
10. `test(v2): document storefront page qa coverage`

Do not start ControlPlane Web UI before CommerceNode admin/internal APIs are testable directly.

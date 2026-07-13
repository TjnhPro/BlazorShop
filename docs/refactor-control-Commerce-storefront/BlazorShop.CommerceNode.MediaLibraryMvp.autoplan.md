# BlazorShop Commerce Node Media Library MVP Autoplan

Status: planned
Date: 2026-07-13
Source: Smartstore investigation + product discussion decisions
Scope: Commerce Node Media Library plus page local links. No implementation has been started from this plan.

## Plan Summary

Build a store-scoped generic Media Library in Commerce Node for public page assets such as banners, logos, favicon-like files, and other reusable local images. The MVP does not replace existing `ProductMedia`; it adds a new asset surface for admin upload, metadata, public local links, and imgproxy-backed transforms.

The core user workflow is:

1. Admin uploads one image file.
2. System creates a new asset with a new `publicId`.
3. System auto-generates `displayName`, `altText`, and `titleText` from the uploaded filename.
4. Admin selects transform options or a preset.
5. Admin copies a URL or `<img>` snippet into Storefront Page body HTML.

## Architecture Boundary

Owner: `BlazorShop.CommerceNode.API`

Data owner: `CommerceNodeDbContext`

Admin API route:

```text
/api/commerce/admin/media/assets
```

Public route:

```text
/media/assets/{assetPublicId}/{canonicalFileName}
```

Store scope:

- Production resolves store scope from host/domain/Nginx request context.
- Local direct API QA may send `X-Store-Key`.
- Store A must never list, mutate, or render Store B media assets.

Out of scope:

- Control Plane Web direct Commerce Node calls.
- Legacy presentation projects.
- `AppDbContext`.
- Replacing existing product media behavior.
- Reference tracking.
- Page editor media picker.
- Audit log, quota, recycle bin, folders, tags, localization.

## MVP Decisions

| Area | Decision |
| --- | --- |
| Asset scope | Store-scoped |
| Data model | Generic `CommerceMediaAsset`, not ProductMedia replacement |
| Upload | Single file upload only |
| Input types | `jpg`, `jpeg`, `png`, `webp`, `gif`, `ico` |
| File size limit | `10 MB` per file |
| Duplicate policy | Every upload creates a new asset and new `publicId`; filename can repeat because id disambiguates |
| Canonical URL | `/media/assets/{publicId}/{canonicalFileName}` |
| Filename source | Sanitized uploaded filename |
| Metadata defaults | `displayName`, `altText`, `titleText` auto-generated from filename |
| Metadata editing | Manual save in admin drawer |
| Metadata version | Metadata update bumps `updatedAt/version` |
| Delete | Hard delete: DB row, original file, old public URL becomes `404` |
| Transform | Real transform through imgproxy |
| Transform cache | Nginx/imgproxy layer, not BlazorShop disk cache |
| imgproxy source | Local filesystem path to original file |
| imgproxy signing | Not signed in MVP; imgproxy must not be public |
| Public auth | No auth; still store-scoped |
| Wrong filename | Correct `publicId` but wrong filename returns `301` to canonical URL |
| Cache headers | Long cache with `v`, shorter cache without `v` |
| ETag/Last-Modified | Deferred |
| Admin UI | Thumbnail grid + right-side drawer |
| Page integration | Copy URL/snippet into page body; no picker |

## Entity Plan

Add `CommerceMediaAsset` under the Commerce Node domain/data model.

Required fields:

```text
Id
PublicId
StoreId
CanonicalFileName
OriginalFileName
DisplayName
AltText
TitleText
Extension
MimeType
FileSizeBytes
Width
Height
StoragePath
CreatedAtUtc
UpdatedAtUtc
```

Recommended constraints:

- `PublicId` unique.
- Index `(StoreId, PublicId)`.
- Index `(StoreId, CreatedAtUtc)`.
- `CanonicalFileName`, `OriginalFileName`, `DisplayName`, `AltText`, `TitleText` max `200` unless an existing local convention requires otherwise.
- `DisplayName` required.
- `AltText` stored as required after normalization.
- `TitleText` optional.

Deferred fields:

- Folder id.
- Tags.
- Reference tracking.
- Usage kind.
- Localization rows.
- Audit/history rows.
- Storage provider abstraction.

## Filename And Metadata Rules

Canonical filename sanitization:

1. Lowercase.
2. Remove path separators.
3. Keep extension.
4. Spaces become `-`.
5. Allow only `a-z`, `0-9`, `-`, `_`, `.`.
6. Collapse repeated hyphens.
7. Fallback to `asset-{shortPublicId}.{extension}` if empty.

Metadata generation from filename:

1. Remove extension.
2. Decode URL-style escapes if present.
3. Replace `-`, `_`, and repeated whitespace with spaces.
4. Lightly split camelCase.
5. Trim.
6. Title-case the result.
7. Fallback to `Asset {shortPublicId}`.

Example:

```text
summer-sale-banner.webp
```

becomes:

```text
canonicalFileName = summer-sale-banner.webp
displayName       = Summer Sale Banner
altText           = Summer Sale Banner
titleText         = Summer Sale Banner
```

Update validation:

- Blank `displayName` returns `400 Bad Request`.
- Blank `altText` normalizes to current `displayName`.
- Blank `titleText` stores null/empty; snippet omits `title`.

## Storage Plan

Original file path:

```text
runtime/media/assets/stores/{storeId:N}/{assetPublicId:N}/original.{extension}
```

Do not pre-compress and replace the original. Keep original upload as source-of-truth.

Delivery optimization:

- Original/no transform can serve the original.
- Transform request is handled by imgproxy using the local filesystem source path.
- Nginx/imgproxy owns transform output caching.

## Public URL And Transform Contract

Canonical public URL:

```text
/media/assets/{assetPublicId}/{canonicalFileName}
```

Supported transform query params:

```text
w or width
h or height
fit
format
v
```

Supported `fit` values:

- `cover`
- `contain`
- `inside`

Supported output `format` values:

- omitted or `original`
- `webp`
- `jpg`
- `png`

Rules:

- No upscale in MVP.
- Max `w` or `h`: `4096`.
- Max output pixels: about `16 MP`.
- `gif` and `ico` return original when no transform query is present.
- `gif` and `ico` with transform query return `400 Bad Request`.
- Invalid query returns `400 Bad Request`.
- Asset not found in current store returns `404`.
- Correct `publicId` but wrong filename returns `301 Moved Permanently` to canonical filename while preserving query string.
- Path extension stays as canonical filename even when `format=webp`; response content type comes from output format.

Version:

- DTO `version` is Unix milliseconds from `UpdatedAtUtc`.
- Link generator auto-adds `v`.
- Admin does not enter `v`.

Cache headers:

- URL with `v`: `Cache-Control: public, max-age=31536000, immutable`
- URL without `v`: `Cache-Control: public, max-age=3600`

## API Plan

Admin routes:

```text
GET    /api/commerce/admin/media/assets?pageNumber=1&pageSize=25&search=&sortBy=createdAt&sortDirection=desc
POST   /api/commerce/admin/media/assets/upload
GET    /api/commerce/admin/media/assets/{assetPublicId}
PUT    /api/commerce/admin/media/assets/{assetPublicId}
POST   /api/commerce/admin/media/assets/{assetPublicId}/replace
DELETE /api/commerce/admin/media/assets/{assetPublicId}
```

List contract:

- `pageNumber`
- `pageSize`
- `search`
- `sortBy`
- `sortDirection`

Search matches:

- `canonicalFileName`
- `originalFileName`
- `displayName`
- `altText`
- `mimeType`

Sort allowlist:

- `createdAt`
- `updatedAt`
- `fileName`
- `fileSizeBytes`
- `width`
- `height`

Default sort:

```text
createdAt desc
```

List response shape:

```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 25,
  "totalPages": 0
}
```

Upload response:

Return full asset DTO:

```text
publicId
canonicalFileName
originalFileName
displayName
altText
titleText
mimeType
extension
fileSizeBytes
width
height
originalUrl
version
createdAt
updatedAt
```

## Admin UI Plan

Page shape:

- Search and sort controls at top.
- Thumbnail grid.
- Click asset opens right-side drawer.
- Mobile can use full-screen drawer.

Drawer content:

- Preview.
- Metadata form: `displayName`, `altText`, `titleText`.
- Save button.
- Original URL.
- Transform generator.
- Copy URL.
- Copy `<img>` snippet.
- Replace file.
- Delete asset.

Generator controls:

- Width.
- Height.
- Fit.
- Format.
- Version is automatic and read-only/implicit.

Presets:

| Preset | Values |
| --- | --- |
| Banner | `w=1920`, `h=600`, `fit=cover`, `format=webp` |
| Card | `w=800`, `h=600`, `fit=cover`, `format=webp` |
| Logo | `w=320`, `fit=inside`, `format=png` or `original` |
| Favicon | no transform; copy original `.ico` |

Snippet:

```html
<img src="/media/assets/{publicId}/{fileName}?w=1920&h=600&fit=cover&format=webp&v=..." alt="Summer Sale Banner" title="Summer Sale Banner" width="1920" height="600" loading="lazy">
```

Rules:

- Always include `alt`.
- Include `title` only when `titleText` has value.
- Include `loading="lazy"` by default.
- Include `width`/`height` when the generator knows them.
- Do not generate `<picture>` in MVP.
- If requested transform is larger than original, show a small warning because no upscale occurs.

## Phase Breakdown

### Phase 0: Alignment And Cleanup

Goal: convert this plan into implementation-ready tickets and correct stale investigation notes.

Tasks:

- Update the Smartstore investigation note so its MVP section no longer claims remote URL import, folders/tags/reference tracking, or product attach as first-slice scope.
- Confirm existing ProductMedia remains untouched.
- Confirm imgproxy/Nginx local setup path from `docs/architecture/07-deployment-and-local-run.md`.
- Identify existing response/paging helpers in Commerce Node APIs.

Exit gate:

- Plan and investigation docs agree on MVP scope.
- No code changes yet.

### Phase 1: Domain, Persistence, And Options

Goal: add the asset model and persistence foundation.

Tasks:

- Add `CommerceMediaAsset` entity.
- Add `DbSet` and EF configuration in `CommerceNodeDbContext`.
- Add migration for Commerce Node only.
- Add media options for root storage path, max file size, allowed extensions, transform limits, and imgproxy/Nginx behavior.
- Add helper for canonical filename sanitization.
- Add helper for filename-derived metadata generation.

Exit gate:

- Migration compiles.
- Unit-level validation for filename/metadata helpers.
- No public/admin API behavior yet.

### Phase 2: Application Service And Admin API

Goal: support upload/list/detail/update/replace/delete through Commerce Node admin API.

Tasks:

- Define DTOs and request contracts in `BlazorShop.Application/CommerceNode`.
- Add service contract for media assets.
- Implement service in Infrastructure using `CommerceNodeDbContext`.
- Implement single-file multipart upload.
- Read image metadata: MIME, extension, file size, dimensions.
- Validate signature and extension.
- Store original under the decided runtime path.
- Implement paged list/search/sort.
- Implement metadata update normalization.
- Implement replace with same `publicId` and same `canonicalFileName`.
- Implement hard delete.
- Add controller routes under `/api/commerce/admin/media/assets`.

Exit gate:

- Admin API returns full asset DTO after upload.
- List is paged.
- Store scoping is enforced.
- Delete removes DB and original file.

### Phase 3: Public Rendering And Imgproxy Integration

Goal: make copied URLs work publicly with real transforms.

Tasks:

- Add public route `/media/assets/{assetPublicId}/{fileName}`.
- Resolve current store from host/domain or local `X-Store-Key`.
- Validate asset belongs to current store.
- Implement canonical filename redirect.
- Implement transform query normalization and validation.
- Return original for no-transform requests.
- For transform requests, integrate with Nginx/internal proxy to imgproxy using local filesystem source path.
- Ensure imgproxy is internal-only for MVP.
- Apply cache headers based on presence of `v`.

Exit gate:

- Original URL loads.
- Transformed URL returns resized/converted image via imgproxy.
- Wrong filename redirects with `301`.
- Cross-store media access returns `404`.
- Invalid transform returns `400`.

### Phase 4: Admin UI Media Library

Goal: give admin the real workflow for page local links.

Tasks:

- Add Media Library page in the active admin UI path.
- Implement search/sort/paged grid thumbnails.
- Implement right-side drawer.
- Implement upload flow.
- Implement metadata edit with manual Save.
- Implement replace and delete actions.
- Implement transform generator form.
- Implement presets.
- Implement copy URL and copy `<img>` snippet.
- Add user-facing errors for unsupported files and invalid transforms.

Exit gate:

- Admin can upload `summer-sale-banner.webp`.
- UI shows `Summer Sale Banner` metadata without manual typing.
- Admin can copy a working WebP banner snippet and paste it into page body.

### Phase 5: QA And Hardening

Goal: verify Commerce Node behavior and public page workflow.

Tasks:

- Update `QA-CommerceNode.todo.md` with API and public media cases.
- Update `QA-StorefrontV2.todo.md` with page HTML image rendering cases if page rendering is tested.
- Run focused API tests for upload/list/update/delete/public route.
- Run browser QA for Media Library UI if implemented in this phase.
- Verify imgproxy/Nginx local runtime path.
- Verify store isolation with at least two stores.
- Verify page body can render copied `<img>` snippet.

Exit gate:

- Focused tests pass or known gaps are documented.
- Browser screenshot confirms grid/drawer/snippet workflow if UI was changed.

## Deferred Phases

### Phase 6: Page Editor Integration

- Media picker inside page editor.
- Insert image/snippet without copy/paste.
- Possible page-specific fallback title/alt behavior.

### Phase 7: ProductMedia Integration

- Optional `MediaAssetId` on `ProductMedia`.
- Attach existing asset to product.
- Backfill current product media into generic assets.
- Keep `/media/products/{mediaPublicId}` backward compatible until migration is complete.

### Phase 8: Library Management Depth

- Folders.
- Tags.
- Reference tracking.
- Usage view.
- Orphan detection.
- Soft delete/recycle bin if product direction changes.

### Phase 9: Operations And Storage

- Store quota and storage usage.
- Audit history.
- Signed imgproxy URLs.
- Remote object storage/provider abstraction.
- Optimized derivative storage if disk pressure becomes real.

## Risk Register

| Risk | Impact | MVP Handling |
| --- | --- | --- |
| Store scope bug exposes another store asset | Critical | Resolve store before asset lookup; query by `(StoreId, PublicId)` |
| imgproxy exposed publicly | High | Keep browser URL on `/media/assets`; imgproxy internal-only |
| Transform query causes heavy CPU/cache abuse | Medium | Enforce max `4096`, `16 MP`, no upscale |
| Admin assumes GIF/ICO transform works | Low | Return clear `400` for transform query on `gif/ico` |
| Metadata update busts image cache unnecessarily | Low | Accepted for MVP simplicity |
| Hard delete breaks page links | Medium | Accepted product decision; no reference tracking in MVP |
| Original overwritten by optimized file | Medium | Do not pre-compress original; serve optimized derivatives through imgproxy |
| Page body HTML sanitization blocks snippet | Low | Current page does not block `<img>`; no MVP change |

## QA Checklist Additions

Add to Commerce Node QA:

- Upload accepts valid `jpg`, `png`, `webp`, `gif`, `ico`.
- Upload rejects unsupported extension, invalid signature, and over `10 MB`.
- Upload auto-generates metadata from filename.
- List endpoint is paged with `pageNumber/pageSize`.
- Search matches filename, original filename, display name, alt text, MIME type.
- Sort allowlist works and rejects unknown sort fields.
- Metadata update rejects blank display name.
- Blank alt text saves as display name.
- Replace keeps `publicId` and canonical filename, bumps version.
- Delete hard-deletes asset and original file.
- Public original route is store-scoped.
- Wrong filename redirects with `301`.
- Transform route validates params.
- `gif/ico` transform request returns `400`.
- `format=webp` returns WebP content through imgproxy/Nginx.

Add to Storefront V2 QA when page rendering is exercised:

- Page body renders copied `<img>` snippet.
- Public media URL resolves from store host.
- Page image does not resolve from another store host/key.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
| --- | --- | --- | --- | --- |
| 1 | Store-scoped generic media asset | Architecture | Commerce Node owns store-local ecommerce runtime and public media must isolate stores | Global media library |
| 2 | Do not replace ProductMedia in MVP | Scope | ProductMedia already supports product-specific behavior and compatibility | ProductMedia rewrite |
| 3 | Single file upload | MVP scope | Keeps validation, metadata generation, and UI focused | Batch upload |
| 4 | Auto-generate metadata from filename | UX | Admin should not type display/alt/title for every image | Blank/manual-only metadata |
| 5 | Keep original file | Quality | Allows future transforms without generation loss | Pre-compress and overwrite original |
| 6 | Use imgproxy for transforms | Architecture | Existing runtime includes imgproxy and avoids .NET image processing scope | ImageSharp processing in app |
| 7 | Nginx/imgproxy cache transforms | Performance | Avoid duplicate app-level transform cache | BlazorShop disk transform cache |
| 8 | Public URL remains `/media/assets/...` | SEO/UX | Browser sees stable store-scoped local link | Browser redirect to imgproxy URL |
| 9 | Hard delete | Product choice | User explicitly chose delete means delete asset | Soft delete/recycle bin |
| 10 | No reference tracking in MVP | Scope | User explicitly deferred it | Smartstore-style tracking |
| 11 | Copy URL and `<img>` snippet | UX | Supports page local links without page editor integration | URL-only |
| 12 | No page editor picker in MVP | Scope | Copy/paste local links are enough for this phase | Integrated picker |

## Final Gate

This plan is ready for implementation when the user confirms:

- Phase 0 and Phase 1 should start.
- No additional scope is added to MVP.
- Existing ProductMedia behavior remains untouched except shared helper reuse if it is clearly local and safe.

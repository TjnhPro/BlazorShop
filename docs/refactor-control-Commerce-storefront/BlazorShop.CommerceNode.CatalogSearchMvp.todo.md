# BlazorShop CommerceNode CatalogSearchMvp Todo

## Goal

Build the StorefrontV2 catalog search MVP with an Amazon-style header search:

- A global category combobox in `StorefrontHeader`.
- A search text box beside it.
- Submit only on Enter or search button click.
- Results render on `/search`.
- Reuse `GET /api/internal/catalog/products`.
- Keep ControlPlane out of this feature.

This phase is Storefront-facing only. It does not add ControlPlane admin catalog search, attribute filters, facets, autocomplete, analytics, or sitemap entries.

## Locked Decisions

| Area | Decision |
|---|---|
| Runtime boundary | `BlazorShop.Storefront.V2` calls only `BlazorShop.CommerceNode.API` internal catalog endpoints. |
| API route | Reuse `GET /api/internal/catalog/products`. |
| Search page | Use `/search`. |
| SEO | `/search` is `noindex` and is not included in sitemap. |
| Category query | Use `categorySlug`, not `categoryId`, in Storefront URL/API query. |
| Invalid category slug | Return an empty listing with HTTP 200/API success, not 404. |
| Empty search text | Show all products for the selected category scope, or all published products when no category is selected. |
| Parent category | Parent category search includes child categories. |
| Text search target | Search title only: `Product.Name`. Do not search SKU, description, attributes, or variant values in this MVP. |
| Search engine | PostgreSQL full-text search using `simple` config. |
| Index | Add a GIN expression index for product title FTS. |
| Search ordering | When text exists: rank desc, then `DisplayOrder` asc, then `CreatedOn` desc, then `Id`. |
| Browse ordering | When text is empty: existing catalog ordering may remain available. Header search does not expose a sort dropdown. |
| Pagination | Backend caps results to max 10 pages. |
| Total count | `TotalCount` is capped to `PageSize * 10`; do not expose real counts beyond the cap. |
| Cache | Add a cache abstraction skeleton now, backed by memory cache, so Redis can replace it later. |
| Cache invalidation | Store-level invalidation after product, category, variant, inventory, or primary media changes. |
| UI behavior | Category change does not auto-submit. Only Enter/click submits. |
| Empty result text | Show short copy: `No products found`. |

## Current Code Facts

| Concern | Current state |
|---|---|
| Internal catalog API | `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontCatalogController.cs` exposes `GET api/internal/catalog/products`. |
| Storefront API client | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs` already builds catalog product query routes. |
| Query contract | `BlazorShop.Domain/Contracts/ProductCatalogQuery.cs` has `PageNumber`, `PageSize`, `CategoryId`, `SearchTerm`, price, stock, sort, and created filters. |
| Shared V2 query model | `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Models/Product/ProductCatalogQuery.cs` mirrors the domain query. |
| Product repository | `BlazorShop.Infrastructure/Data/CommerceNode/Repositories/CommerceNodeProductReadRepository.cs` currently searches SKU, name, and description with `Contains`. This must change for Storefront title search. |
| Public service | `BlazorShop.Application/Services/PublicCatalogService.cs` maps repository `PagedResult` to Storefront DTOs. |
| Header UI | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor` has brand/nav/account/cart, but no search form yet. |
| Category tree | `PublicCatalogService.GetPublishedCategoryTreeAsync` already builds a published category tree that can be flattened for the combobox. |

## Architecture

```text
StorefrontHeader
  -> StorefrontApiClient.GetPublishedCategoriesAsync()
  -> /api/internal/catalog/categories

SearchPage (/search)
  -> StorefrontApiClient.GetPublishedCatalogPageAsync(ProductCatalogQuery)
  -> /api/internal/catalog/products?categorySlug=...&searchTerm=...&pageNumber=...

StorefrontCatalogController
  -> IPublicCatalogService
  -> IProductReadRepository
  -> CommerceNodeDbContext
  -> PostgreSQL FTS over Products.Name
```

## Database Plan

No new table is required.

Add a CommerceNode EF migration that creates a PostgreSQL GIN expression index for Storefront title search.

Candidate SQL:

```sql
CREATE INDEX IF NOT EXISTS ix_products_name_fts_simple
ON "Products"
USING GIN (to_tsvector('simple', coalesce("Name", '')))
WHERE "ArchivedAt" IS NULL
  AND "IsPublished" = TRUE
  AND "PublishedOn" IS NOT NULL;
```

Down migration:

```sql
DROP INDEX IF EXISTS ix_products_name_fts_simple;
```

Implementation notes:

- Use raw SQL in the migration for the expression index.
- Keep the index scoped to published Storefront products.
- Continue filtering by `StoreId` through `ICommerceStoreContext`.
- Keep category slug uniqueness scoped by store as already established in catalog/store expansion.

## API Plan

### Query Contract

Extend both query models:

- `BlazorShop.Domain.Contracts.ProductCatalogQuery`
- `BlazorShop.Web.SharedV2.Models.Product.ProductCatalogQuery`

Add:

```csharp
public string? CategorySlug { get; init; }
```

Shared V2 model uses `set` instead of `init`, following the current pattern.

Add helper behavior:

- Normalize `CategorySlug` by trim/lower slug normalization through existing slug service where service layer is available.
- Normalize `SearchTerm` by trim.
- Backend enforces max page count of 10.
- Backend clamps requested page into `[1, maxPage]`.

### Route Building

Update `StorefrontApiClient.BuildCatalogRoute`:

- Send `categorySlug` when present.
- Keep `categoryId` support for existing category pages until they are moved.
- Send `searchTerm` only when non-empty.
- Keep page number and page size.

### Controller

Keep:

```http
GET /api/internal/catalog/products
```

No new route is required for MVP.

### Service

`PublicCatalogService.GetPublishedCatalogPageAsync` remains the Storefront entry point.

Add service-level handling for:

- Invalid `CategorySlug` returns an empty `PagedResult<GetCatalogProduct>`.
- Parent category includes all published child category ids.
- Cache lookup and cache set around published catalog page requests.

### Repository

Change only the published Storefront catalog path:

- Resolve store-scoped category slug before applying category filter.
- Build category id set with parent + published descendants.
- If `SearchTerm` is empty, do not call PostgreSQL FTS.
- If `SearchTerm` has text, use PostgreSQL FTS on `Product.Name`.
- Order text search by rank desc, then `DisplayOrder`, then `CreatedOn` desc, then `Id`.
- Apply pagination after filtering and ordering.
- Cap `TotalCount` to `PageSize * 10`.

Do not change admin catalog behavior unless shared code forces a small refactor. If shared code must change, keep admin behavior equivalent.

## PostgreSQL FTS Plan

Use Npgsql EF Core PostgreSQL full-text search support where it keeps the query readable.

Target behavior:

```sql
to_tsvector('simple', coalesce("Name", '')) @@ plainto_tsquery('simple', @searchTerm)
```

Rank:

```sql
ts_rank(
  to_tsvector('simple', coalesce("Name", '')),
  plainto_tsquery('simple', @searchTerm)
)
```

Implementation options:

1. Prefer Npgsql EF Core translated full-text APIs if the current package version supports the needed rank expression cleanly.
2. Use raw SQL only for the smallest query fragment if rank translation becomes awkward.
3. Do not fall back to `Contains` for the Storefront search path.

## Cache Plan

Add a small abstraction in the CommerceNode/Application boundary:

```csharp
public interface ICatalogQueryCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task InvalidateStoreCatalogAsync(Guid storeId, CancellationToken cancellationToken = default);
}
```

MVP implementation:

- `MemoryCatalogQueryCache` backed by `IMemoryCache`.
- Cache keys include:
  - store id
  - normalized category slug or `all`
  - normalized search text or `empty`
  - page number
  - page size
  - endpoint mode/version
- TTL:
  - category tree: 60 seconds.
  - catalog results: 30 to 60 seconds.

Invalidation:

- Add a single store-level invalidation method.
- Call it after product/category/variant/inventory/media primary changes.
- If some write paths are hard to wire during MVP, document the missing invalidation in QA and keep TTL short.

Future Redis:

- Keep the interface small.
- Do not introduce Redis in this phase.
- Do not serialize domain entities directly. Cache DTO/read result shapes.

## Storefront UI Plan

### Header Search

Update `StorefrontHeader.razor`:

- Load published categories.
- Flatten category tree/list into combobox options.
- Include an `All` option.
- Add search input.
- Add search button.
- Submit on Enter or button click.
- Category change alone does not navigate.

URL shape:

```text
/search?q=shirt&category=men
/search?category=men
/search?q=shirt
/search
```

Use `category` in the browser URL. Map it to `ProductCatalogQuery.CategorySlug` before API call.

### Search Page

Add `Search.razor` or `SearchPage.razor` under StorefrontV2 pages/routes:

- Route: `/search`.
- Query params:
  - `q`
  - `category`
  - `page`
- Page size default: 24 unless existing Storefront listing pattern uses another value.
- Render existing product card/list components if available.
- No sort dropdown.
- No facets.
- No attribute filters.
- No autocomplete.
- Empty state text: `No products found`.
- Loading and API unavailable states follow existing StorefrontV2 patterns.

### SEO

- Add page metadata `noindex, follow` if the existing SEO system supports it.
- Ensure search route is not added to sitemap generation.
- Do not generate canonical search URLs in sitemap.

## QA Plan

Update `QA-StorefrontV2.todo.md` with:

- Header search renders on desktop.
- Header search renders on mobile without overlapping account/cart/menu controls.
- Category combobox loads published categories.
- Parent category returns products from child categories.
- Empty input + All returns published products.
- Empty input + category returns category scoped products.
- Text search targets product title.
- Text search does not match SKU-only or description-only terms.
- Invalid category slug shows `No products found`.
- Page greater than max page is clamped by backend.
- Total count never exposes more than 10 pages.
- `/search` has noindex and is not in sitemap.
- Category change alone does not submit.
- Enter key submits.
- Search button submits.
- Cache hit does not change visible results.
- Product/category update invalidates or expires cached search result.

Update `QA-CommerceNode.todo.md` with API-level checks:

- `GET /api/internal/catalog/products?searchTerm=shirt`
- `GET /api/internal/catalog/products?categorySlug=men`
- `GET /api/internal/catalog/products?categorySlug=men&searchTerm=shirt`
- invalid `categorySlug`
- empty `searchTerm`
- page cap and total count cap
- FTS does not search description/SKU

## Phase Plan

### Phase 0 - Baseline Verification

- [ ] Verify current StorefrontV2 category and product listing behavior still works.
- [ ] Verify current CommerceNode `api/internal/catalog/products` response shape.
- [ ] Confirm current CommerceNode migration state before adding FTS index.
- [ ] Confirm Npgsql EF Core version supports needed FTS query APIs.

Exit gate:

- [ ] Baseline catalog API works before changes.

### Phase 1 - Query Contract and Database Index

- [ ] Add `CategorySlug` to domain `ProductCatalogQuery`.
- [ ] Add `CategorySlug` to SharedV2 `ProductCatalogQuery`.
- [ ] Update route builder to emit `categorySlug`.
- [ ] Add CommerceNode migration for `ix_products_name_fts_simple`.
- [ ] Run migration against CommerceNode database.

Exit gate:

- [ ] API still returns products without category/search params.
- [ ] Migration applies cleanly.

### Phase 2 - API Search Semantics

- [ ] Resolve `CategorySlug` store-scoped.
- [ ] Parent category includes published descendants.
- [ ] Invalid category slug returns empty `PagedResult`.
- [ ] Replace Storefront published search `Contains` with PostgreSQL FTS over `Product.Name`.
- [ ] Apply rank ordering for text search.
- [ ] Preserve non-search browse behavior.
- [ ] Enforce max 10 pages.
- [ ] Cap `TotalCount` to `PageSize * 10`.

Exit gate:

- [ ] API search by title works.
- [ ] SKU-only and description-only terms do not match.
- [ ] Category slug works without an extra Storefront category lookup.

### Phase 3 - Catalog Cache Skeleton

- [ ] Add `ICatalogQueryCache`.
- [ ] Add `MemoryCatalogQueryCache`.
- [ ] Cache category tree/list result.
- [ ] Cache published catalog page result.
- [ ] Add store-level invalidation method.
- [ ] Wire invalidation into product/category/variant/inventory/media-primary write paths where practical.
- [ ] Document any write path still relying on TTL.

Exit gate:

- [ ] Repeated search returns same result through cache.
- [ ] Store-level invalidation clears affected cached search/listing data.

### Phase 4 - StorefrontV2 UI

- [ ] Add header category combobox.
- [ ] Add header search input.
- [ ] Add header search button.
- [ ] Add `/search` page.
- [ ] Bind `q`, `category`, and `page` query params.
- [ ] Render existing product listing/card components.
- [ ] Render `No products found`.
- [ ] Add noindex metadata.
- [ ] Confirm sitemap excludes `/search`.
- [ ] Verify desktop and mobile layout with Playwright headless=false.

Exit gate:

- [ ] Header search works from desktop and mobile.
- [ ] `/search` works with category only, text only, both, or empty query.

### Phase 5 - QA and Fixes

- [ ] Update `QA-StorefrontV2.todo.md`.
- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Run CommerceNode API tests manually against seeded data.
- [ ] Run StorefrontV2 browser QA with Playwright headless=false.
- [ ] Fix any discovered regression.
- [ ] Mark checklist items verified with evidence.

Exit gate:

- [ ] QA files show verified status for CatalogSearchMvp.
- [ ] No known regression in category/product detail/cart/login/register flows.

## Not In Scope

- Attribute filters.
- Facets.
- Autocomplete.
- Search suggestions.
- Search analytics/logging.
- Admin/ControlPlane catalog search.
- Redis.
- External search engines.
- Sitemap entries for `/search`.
- Product description/SKU/variant/attribute full-text search.
- Sort dropdown on `/search`.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| FTS EF translation is awkward | Query becomes hard to maintain | Use Npgsql APIs first, raw SQL only for the smallest ranking fragment if needed. |
| Cache returns stale products | Storefront shows old catalog briefly | Store-level invalidation plus short TTL in MVP. |
| Header becomes crowded on mobile | Storefront navigation becomes hard to use | Use responsive layout and Playwright visual QA. |
| Category slug collision across stores | Wrong category scope | Always resolve slug under current store context. |
| Search endpoint gets abused with high page values | Expensive scans | Backend clamps max page to 10 and caps count. |

## Review Notes

- This plan intentionally keeps the MVP narrow.
- The implementation should reuse existing catalog DTOs, services, repository, StorefrontApiClient, and Storefront layout.
- If implementation requires a wider refactor, stop and update this plan before coding further.

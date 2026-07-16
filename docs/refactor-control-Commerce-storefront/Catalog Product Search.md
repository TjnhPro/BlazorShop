# Catalog Product Search

Generated: 2026-07-16

Status: Proposed implementation plan.

Scope: Product listing, category product listing, storefront search, instant search suggestions, and a practical filter metadata contract. This phase moves catalog discovery from MVP behavior to real storefront usage without introducing a full search engine or unsupported facet domains.

Autoplan note: this plan applies the autoplan review lens directly in this Codex runtime: CEO scope review, design review, engineering review, DX/API review, decision audit, failure-mode registry, and test map. External dual-voice review agents are not run here. The plan is based on active V2 code evidence and the prior investigation, not feature guessing.

## 1. Approved Scope

This phase should include:

- Category product listing with real paging.
- Search result product listing with real paging.
- Page-size options for Storefront listing pages.
- Existing sorting contract hardening.
- Product summary read model reuse.
- Empty-result state consistency.
- Search term normalization and minimum length policy.
- Public search by product name, short description, description, and SKU.
- Store filtering through existing current-store scope.
- Category scope through category slug and descendant categories.
- Include/exclude unavailable products using current `InStock` behavior.
- Instant search suggestion endpoint.
- Suggestion minimum characters and max result limit.
- Optional product image in suggestions.
- Keyboard/mobile-friendly suggestion DTO shape.
- Search/suggestion routes excluded from indexing.
- Basic filter metadata contract:
  - category choices
  - price range
  - availability option
  - new-arrival option
  - sort options
  - page-size options
  - display order and max choices metadata.

This phase should not include:

- Brand/manufacturer facet.
- Rating facet.
- Delivery-time facet.
- Specification facet.
- GTIN/MPN search until those fields are implemented in Product Identity.
- Full text-search platform such as Elasticsearch, Meilisearch, or OpenSearch.
- Search analytics, typo tolerance, synonyms, spell correction, personalization, or boosted campaigns.
- Full grid/list view mode unless a later UI phase explicitly needs it.
- Featured product placement hook; keep that for a merchandising phase.
- Replacing existing catalog endpoints wholesale.

## 2. What Already Exists

Current codebase evidence:

- `ProductCatalogQuery` already has:
  - `PageNumber`
  - `PageSize`
  - `CategoryId`
  - `CategorySlug`
  - `SearchTerm`
  - `MinPrice`
  - `MaxPrice`
  - `InStock`
  - `IsPublished`
  - `SortBy`
  - `CreatedAfterUtc`
  - normalization helpers for page, page size, search term, and category slug.
- `StorefrontProductCatalogQuery` already publishes validation metadata:
  - default page size 24
  - max page size 100
  - search/category max length
  - named sort values.
- `StorefrontPagedResponse<T>` already returns:
  - `Items`
  - `PageNumber`
  - `PageSize`
  - `TotalCount`
  - `TotalPages`.
- `CatalogProductReadModel` and `StorefrontCatalogProductResponse` already act as product summary read models.
- `CommerceNodeProductReadRepository.GetPublishedCatalogPageAsync` already:
  - resolves current store
  - filters `Product.StoreId == storeId`
  - filters published/non-archived product
  - requires published category in same store
  - supports category slug scope
  - includes descendant categories for category slug search
  - filters min/max price
  - filters `InStock`
  - sorts by newest, oldest, price, name, display order, and updated
  - caps storefront result count to 10 pages.
- `PublicCatalogService.GetPublishedCatalogPageAsync` already caches catalog page results by store, page, size, sort, category, search, price, stock, and created-after filters.
- `SearchPage.razor` already uses the paged catalog API, renders pagination, applies `noindex`, and shows empty result copy.
- `CategoryPage.razor` already uses the paged catalog API for filtered category results, but it hardcodes `PageNumber = 1` and `PageSize = 48`.
- `CatalogFilterPanel.razor` already supports category, search, price range, sort, and in-stock controls.
- `ProductGrid.razor` already renders grid cards and empty states.
- `ProductCard.razor` already shows in-stock/out-of-stock, variant, and new-arrival badges.
- `StorefrontIndexingPolicy` already makes search pages `noindex, follow`.

Meaning:

- The project does not need a new catalog listing abstraction.
- The project does need to make the current public listing/query behavior more complete and consistent.
- The safest path is additive: extend current query/DTO/service/repository and storefront pages.

## 3. Current Gaps

| Area | Gap | Decision |
| --- | --- | --- |
| Category listing | Category page does not expose real paging or page-size query. | Add paging/page-size handling to category page using existing `products` endpoint. |
| Page-size options | API supports page size but UI hardcodes values. | Add supported page sizes: 12, 24, 48. |
| Public text search | Published storefront search uses PostgreSQL FTS on `Product.Name` only. | Search `Name`, `ShortDescription`, `Description`, and `Sku` in public path. |
| Search minimum length | No minimum length policy exists. | Add minimum searchable term length; default 2 characters. |
| Instant search | No suggestion endpoint or DTO exists. | Add narrow suggestion endpoint under Storefront catalog routes. |
| Facets | No facet/filter metadata response exists. | Add filter metadata only for fields backed by current data. |
| Brand/rating/delivery/spec facets | Underlying model/data is missing or deferred. | Keep out of this phase. |
| Grid/list mode | `ProductGrid` is grid-only. | Defer unless later UX requires it. |
| Featured placement | No product placement model exists. | Defer to merchandising phase. |

## 4. Core Product Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Keep `ProductCatalogQuery` as the core listing/search query object. | It already flows from Storefront V2 to Commerce Node and repository. |
| D2 | Keep `GET products` as the canonical paged listing/search endpoint. | It already returns `StorefrontPagedResponse<StorefrontCatalogProductResponse>`. |
| D3 | Use category slug for public URLs and API category scope. | Storefront URLs already use slugs and category slug includes descendants. |
| D4 | Keep page-size max 100 in API, expose only 12/24/48 in UI. | API stays flexible while Storefront UX remains simple. |
| D5 | Treat empty search as browse. | Current SearchPage already supports browse products without a term. |
| D6 | Reject too-short non-empty search terms before expensive search work. | Prevents noisy queries and improves UX clarity. |
| D7 | Search SKU as optional current-field behavior; do not add GTIN/MPN now. | `Product.Sku` exists; GTIN/MPN are not in the current product model. |
| D8 | Add suggestion endpoint as read-only Storefront API. | It supports future WASM/header typeahead without coupling to page rendering. |
| D9 | Add basic filter metadata, not full facet counts. | Gives UI a stable contract without expensive unsupported facet domains. |
| D10 | Preserve `InStock` as the phase availability filter. | Availability Quantity will later introduce `Purchasable`; this phase must not depend on unimplemented fields. |

## 5. Target Boundary

All runtime catalog/search data belongs to `CommerceNodeDbContext`.

Active paths remain:

```text
Storefront.V2
  -> CommerceNode.API api/storefront/stores/{storeKey}/catalog/*
      -> PublicCatalogService
          -> IProductReadRepository
              -> CommerceNodeDbContext

ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/*?storeKey={storeKey}
```

Do not add active routes under:

- `api/internal/*`
- legacy `api/public/*`
- legacy `api/admin/*`
- legacy controller routes.

Do not add V2 persistence to `AppDbContext`.

## 6. Target API Surface

### Existing Endpoint To Keep

```http
GET api/storefront/stores/{storeKey}/catalog/products
```

Keep current query shape and extend behavior behind it:

```text
pageNumber
pageSize
categoryId
categorySlug
searchTerm
minPrice
maxPrice
inStock
sortBy
createdAfterUtc
currencyCode
```

Compatibility rule:

- Existing clients that call this endpoint without new parameters must receive the same shape.
- Existing response fields must not be removed or renamed.
- `TotalCount` may remain capped by the existing Storefront max-page policy.

### New Suggestion Endpoint

Add:

```http
GET api/storefront/stores/{storeKey}/catalog/search-suggestions
```

Query:

```text
term string required, max 128
categorySlug string optional, max 256
limit int optional, default 6, min 1, max 10
includeImages bool optional, default true
currencyCode string optional, length 3
```

Response:

```text
StorefrontSearchSuggestionResponse
  Items IReadOnlyList<StorefrontSearchSuggestionItemResponse>
  Term string
  MinimumLength int
  Limit int
```

Item:

```text
Id Guid
Slug string?
Name string?
Sku string?
Image string?
PrimaryMediaPublicId Guid?
HasPrimaryMedia bool
Price decimal
DisplayPrice decimal?
DisplayCurrencyCode string?
CategoryName string?
CategorySlug string?
InStock bool
Url string
```

Rules:

- Return empty `Items` when normalized term length is below minimum.
- Do not return unpublished, archived, wrong-store, wrong-category, or un-slugged products.
- Use same published catalog visibility rules as product listing.
- Suggestions are read-only `GET`.
- Do not create a public page route for suggestions; the API endpoint itself should not be linked in sitemap.

### New Filter Metadata Endpoint

Add:

```http
GET api/storefront/stores/{storeKey}/catalog/product-filter-metadata
```

Query:

```text
categorySlug string optional
searchTerm string optional
currencyCode string optional
```

Response:

```text
StorefrontProductFilterMetadataResponse
  PageSizes IReadOnlyList<int>
  SortOptions IReadOnlyList<StorefrontProductSortOptionResponse>
  CategoryFacet StorefrontFilterFacetResponse
  PriceFacet StorefrontPriceFacetResponse
  AvailabilityFacet StorefrontFilterFacetResponse
  NewArrivalFacet StorefrontFilterFacetResponse
```

Facet:

```text
Key string
Label string
DisplayOrder int
MaxChoices int
MinimumHitCount int
Choices IReadOnlyList<StorefrontFilterChoiceResponse>
```

Choice:

```text
Value string
Label string
DisplayOrder int
HitCount int?
Selected bool
Disabled bool
```

Price facet:

```text
Min decimal?
Max decimal?
CurrencyCode string?
```

Initial count policy:

- Category choices may omit hit counts or return null until count queries are optimized.
- Availability/new-arrival choices may omit hit counts in the first implementation.
- Do not add brand/rating/delivery/specification facets with empty placeholder data.

## 7. Search Rules

### Normalization

Add a shared search normalization helper near the catalog application layer:

```text
NormalizeSearchTerm(input)
  trim
  collapse repeated whitespace
  return null when empty
```

Do not lowercase aggressively before PostgreSQL FTS unless the query path requires it. Keep Unicode text intact.

### Minimum Length

Recommended default:

```text
MinimumSearchTermLength = 2
```

Rules:

- Empty search term means browse current scope.
- Non-empty term shorter than minimum returns empty result with a clear UI message.
- Suggestion endpoint returns empty suggestions when term is too short.
- API should not throw validation errors for term length below minimum; storefront search forms commonly submit short terms accidentally.

### Search Fields

Search public published products by:

```text
Product.Name
Product.ShortDescription
Product.Description
Product.Sku
```

Do not search:

- `FullDescription` in this phase unless performance is acceptable after measurement.
- GTIN, MPN, barcode before those fields exist.
- Variant attributes.
- Private/internal fields.

### Ordering

When `SearchTerm` is present and valid:

```text
rank desc
DisplayOrder asc
CreatedOn desc
Id asc
```

When `SearchTerm` is empty:

- Preserve existing `SortBy` behavior.

### Index Direction

Add or update Commerce Node migration for PostgreSQL FTS expression index if current indexes are insufficient.

Candidate direction:

```sql
to_tsvector(
  'simple',
  coalesce("Name", '') || ' ' ||
  coalesce("ShortDescription", '') || ' ' ||
  coalesce("Description", '') || ' ' ||
  coalesce("Sku", '')
)
```

Keep this in `CommerceNodeDbContext` migration only.

## 8. Storefront UX Plan

### Search Page

Update `SearchPage.razor`:

- Add page-size query parameter.
- Add sort query parameter.
- Preserve category/search/page/pageSize/sort in generated URLs.
- Show message when term is shorter than minimum.
- Keep `noindex, follow`.
- Keep empty result copy short and specific.

Suggested URL shape:

```text
/search?q=shirt&category=men&page=2&pageSize=24&sortBy=displayOrder
/search?category=men&pageSize=48
/search
```

### Category Page

Update `CategoryPage.razor`:

- Add `page` query parameter.
- Add `pageSize` query parameter.
- Use `CategorySlug = Slug` instead of only `CategoryId` where possible.
- Render pagination controls.
- Preserve min/max price, in-stock, sort, page size in links.
- Keep category not-found and service-unavailable behavior unchanged.

### Listing Controls

Extend `CatalogFilterPanel.razor` or add a small listing toolbar component:

- Page-size select: 12, 24, 48.
- Sort select uses existing named API values.
- Keep price/in-stock controls.
- Do not add grid/list switch in this phase unless there is real UI demand.

### Instant Search UI Hook

Add a reusable component or service hook, not a mandatory page dependency:

```text
StorefrontSearchSuggestClient
  -> StorefrontApiClient.GetSearchSuggestionsAsync(...)
```

UI behavior:

- Minimum characters before request.
- Debounce in browser UI.
- Arrow up/down and Enter selection data supported by item order and URL.
- Mobile can render suggestions as a simple stacked list.
- No server-rendered page route for suggestions.

If the current header search is not ready for typeahead, implement the endpoint and client first, then wire header typeahead in a follow-up UI commit.

## 9. Data Model Direction

No new table is required for this phase.

Potential migration:

- Add PostgreSQL FTS expression index over the selected public search fields.

Do not add:

- Search index table.
- Search analytics table.
- Product featured placement table.
- Brand/manufacturer tables.
- Rating summary tables.
- Specification tables.

Those are valid future features but not necessary for this phase.

## 10. Implementation Phases

### Phase 0 - Baseline And Contract Guard

Goal: protect current behavior before changing search/listing.

Tasks:

- Add/verify tests for `ProductCatalogQuery` normalization and page-size clamp.
- Add/verify Storefront catalog contract tests for:
  - `GET products`
  - paging metadata
  - sort validation metadata
  - response schema.
- Add repository/service tests for:
  - store scope
  - published-only products
  - category slug includes descendants
  - invalid category slug returns empty page.
- Record current SearchPage and CategoryPage browser behavior in QA checklist.

Acceptance:

- Existing Storefront product listing/search still passes without new parameters.
- No legacy `api/internal/*` routes are added.

### Phase 1 - Search Query Hardening

Goal: define stable search normalization/minimum behavior and broaden current public search fields.

Tasks:

- Add catalog search options/constants:
  - minimum term length 2
  - suggestion default/max limit
  - supported page sizes 12/24/48.
- Add search normalization helper in application/catalog area.
- Update `CommerceNodeProductReadRepository.BuildPublishedCatalogQuery`:
  - keep current store/published/category filters
  - apply minimum length policy
  - search `Name`, `ShortDescription`, `Description`, and `Sku`
  - keep rank ordering for valid search.
- Add FTS migration/index if query plan needs it.
- Update catalog cache key version from `v1` to `v2` if search semantics change.

Acceptance:

- Search by name still works.
- Search by SKU works.
- Search by short/description works.
- One-character search returns empty result or clear non-search behavior, not an expensive broad query.
- Empty search still browses products.

### Phase 2 - Category And Search Paging UX

Goal: make listing pages usable for real catalogs.

Tasks:

- Add `pageSize` and `sortBy` query support to `SearchPage.razor`.
- Add `page` and `pageSize` query support to `CategoryPage.razor`.
- Update category page to use paged result metadata instead of only `_displayProducts.Count`.
- Add pagination links for category page.
- Preserve filter query values when navigating pages.
- Add page-size control to listing UI.
- Keep page number clamped server-side and UI-safe.

Acceptance:

- Category pages can move past the first 48 products.
- Search results preserve query/category/sort/page-size between pages.
- Empty result states remain clear.
- Service unavailable and not-found paths remain unchanged.

### Phase 3 - Filter Metadata Contract

Goal: provide a stable backend contract for filters without pretending unsupported facets exist.

Tasks:

- Add DTOs:
  - `StorefrontProductFilterMetadataResponse`
  - `StorefrontFilterFacetResponse`
  - `StorefrontFilterChoiceResponse`
  - `StorefrontPriceFacetResponse`
  - `StorefrontProductSortOptionResponse`.
- Add Storefront catalog endpoint:
  - `GET product-filter-metadata`.
- Populate:
  - page sizes 12/24/48
  - sort options from existing sort values
  - category choices from published category tree
  - price min/max from published current-store products in scope
  - availability choices based on current `InStock` filter
  - new-arrival choice using `CreatedAfterUtc` policy.
- Include `DisplayOrder`, `MaxChoices`, and `MinimumHitCount` fields.
- Keep hit counts nullable unless cheap and tested.

Acceptance:

- Storefront can render filters from API metadata.
- No empty brand/rating/delivery/specification facets are returned.
- Contract tests prove response schema and validation metadata.

### Phase 4 - Instant Search Suggestions

Goal: add low-risk autocomplete support that future header/WASM components can consume.

Tasks:

- Add Storefront query DTO:
  - `StorefrontSearchSuggestionQuery`.
- Add response DTOs:
  - `StorefrontSearchSuggestionResponse`
  - `StorefrontSearchSuggestionItemResponse`.
- Add service/repository method using the same published catalog visibility rules.
- Apply:
  - minimum term length
  - max result limit 10
  - optional category scope
  - optional image projection
  - currency display mapping.
- Add `StorefrontApiClient.GetSearchSuggestionsAsync`.
- Add no sitemap/robots link for suggestion API.

Acceptance:

- Suggestion endpoint returns empty for too-short terms.
- Suggestion endpoint returns at most 10 items.
- Suggestions never expose unpublished/wrong-store products.
- Suggestion items include URL and safe display fields.

### Phase 5 - Storefront Integration

Goal: wire the improved API into Storefront UI without forcing a large redesign.

Tasks:

- Update `CatalogFilterPanel` or add `CatalogListingToolbar` for page-size/sort.
- Optionally add suggestion behavior to header/search input if the current layout can support it cleanly.
- Keep ProductGrid as grid-only.
- Ensure search/category pages are responsive and query-state preserving.
- Keep `SearchPage` noindex and exclude suggestion behavior from SEO discovery.

Acceptance:

- Storefront listing UX works on desktop and mobile.
- Search and category pages preserve filters across navigation.
- No text overlap/regression in listing toolbar.
- Search route remains noindex.

### Phase 6 - QA, API Contracts, And Cache Invalidation

Goal: finish with reliable contracts and practical runtime behavior.

Tasks:

- Add/update Commerce Node API contract tests:
  - operation IDs
  - summaries
  - schemas
  - error responses
  - validation metadata
  - generator-safe names.
- Add service/repository tests for:
  - search fields
  - minimum term length
  - category scope
  - page-size clamp
  - filter metadata
  - suggestions.
- Add Storefront rendering tests or Playwright QA for:
  - search results
  - category pagination
  - page-size selector
  - empty result state
  - suggestion keyboard/mobile behavior if UI is implemented.
- Confirm catalog cache invalidation still fires after:
  - product changes
  - category changes
  - inventory changes
  - primary media changes.
- Update:
  - `QA-CommerceNode.todo.md`
  - `QA-StorefrontV2.todo.md`.

Acceptance:

- Focused tests pass.
- Browser QA passes for changed Storefront flows.
- No active V2 route is added under `api/internal/*`.

## 11. Failure Modes Registry

| Failure mode | Risk | Mitigation |
| --- | --- | --- |
| Search becomes too broad or slow | High | Minimum term length, page cap, FTS index, result limit. |
| Category page silently hides products after first page | High | Implement real paging and page metadata. |
| Facet contract implies unsupported data | Medium | Return only supported facets; defer brand/rating/delivery/spec. |
| Suggestion endpoint leaks unpublished products | High | Reuse published catalog visibility query. |
| Store A can see Store B products | Critical | Keep current-store `StoreId` filter in repository. |
| API contract breaks generated clients | High | Add explicit DTOs and contract tests. |
| Search page gets indexed as duplicate content | Medium | Keep `StorefrontIndexingPolicy.ApplySearchMetadata`. |
| Cache returns stale search/filter results | Medium | Cache key versioning and store-level invalidation. |
| UI query parameters drop filters on paging | Medium | Central URL builder preserving page/category/search/sort/pageSize/filter. |

## 12. Test Map

| Layer | Coverage |
| --- | --- |
| Domain/Application | Query normalization, search options, supported page sizes. |
| Infrastructure | Published catalog query, store scope, category descendants, search fields, stock filter, price filter, sort order. |
| Commerce Node API | Product listing contract, filter metadata contract, suggestion contract, validation metadata. |
| Storefront API client | Route building for page/pageSize/search/category/filter/suggestions. |
| Storefront UI | Search page, category page, pagination, page-size selector, empty state, noindex metadata. |
| Regression | No legacy route additions, no `AppDbContext` migration, no public domain entity schemas. |

## 13. NOT In Scope

- Control Plane product search redesign.
- Admin catalog list refactor.
- Brand/manufacturer domain.
- Review/rating domain.
- Delivery promise domain.
- Specification attributes/facets.
- Search analytics.
- Search spelling/synonym engine.
- Merchandising boosts/featured placements.
- Customer-role-specific search visibility.
- Localization/language-specific search.
- Full grid/list display mode.

## 14. Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Scope | Keep this phase focused on product discovery, not merchandising/search platform | Auto-decided | MVP-to-real without feature stuffing | The codebase already has listing/search foundations; the highest value is consistency and practical UX | Search engine, analytics, featured placement |
| 2 | API | Keep existing `products` endpoint as canonical listing/search endpoint | Auto-decided | Preserve working contracts | Existing Storefront API/client/pages already depend on it | Replacing route surface |
| 3 | Search | Search current fields only: name, short description, description, SKU | Auto-decided | Match implemented data model | Product has these fields today; GTIN/MPN do not exist yet | GTIN/MPN/variant/spec search |
| 4 | Facets | Return only supported basic metadata | Auto-decided | Do not imply unsupported behavior | Brand/rating/delivery/spec data is missing or deferred | Placeholder facets |
| 5 | UI | Defer grid/list mode and featured placement | Auto-decided | Avoid low-value UI churn | Grid already works; placement belongs to merchandising | Adding switches/placement flags now |

## 15. Recommended Implementation Order

1. Phase 0 baseline and tests.
2. Phase 1 search query hardening.
3. Phase 2 category/search paging UX.
4. Phase 3 filter metadata contract.
5. Phase 4 instant search suggestions.
6. Phase 5 Storefront integration polish.
7. Phase 6 QA, contract hardening, and cache invalidation verification.

Each phase should be small enough to commit separately when implementation starts.

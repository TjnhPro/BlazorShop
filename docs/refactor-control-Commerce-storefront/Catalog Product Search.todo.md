# BlazorShop Catalog Product Search Todo

Generated: 2026-07-16

Source plan:

- `docs/refactor-control-Commerce-storefront/Catalog Product Search.md`

Scope:

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
  - category choices.
  - price range.
  - availability option.
  - new-arrival option.
  - sort options.
  - page-size options.
  - display order and max choices metadata.

Explicitly out of scope:

- Brand/manufacturer facet.
- Rating facet.
- Delivery-time facet.
- Specification facet.
- GTIN/MPN search beyond fields already present and contract-approved in current product identity.
- Full text-search platform such as Elasticsearch, Meilisearch, or OpenSearch.
- Search analytics.
- Typo tolerance, synonyms, spell correction, personalization, or boosted campaigns.
- Full grid/list view mode.
- Featured product placement hook.
- Replacing existing catalog endpoints wholesale.
- Control Plane product search redesign.
- Admin catalog list refactor.
- V2 feature work in legacy `BlazorShop.Presentation` or `AppDbContext`.

Boundary checklist:

- [x] Keep runtime catalog/search data in `CommerceNodeDbContext`. Phase 0 source review confirmed active Storefront catalog uses `CommerceNodeProductReadRepository` and current store context.
- [x] Keep canonical Storefront listing/search under `api/storefront/stores/{storeKey}/catalog/products`. Phase 0 source review confirmed `StorefrontScopedCatalogController.GetProducts`.
- [ ] Add any new Storefront read endpoints under `api/storefront/stores/{storeKey}/catalog/*`.
- [x] Keep Storefront V2 calling Commerce Node Storefront APIs through configured store key route scope. Phase 0 source review confirmed `StorefrontApiClient` route construction.
- [x] Keep Storefront V2 current-store guard before catalog/search reads. Phase 0 source review confirmed existing Storefront V2 current-store middleware remains unchanged.
- [x] Do not add `api/internal/*`, legacy `api/public/*`, legacy `api/admin/*`, or legacy controller routes. Phase 0 makes no route changes.
- [x] Do not add V2 persistence to `AppDbContext`. Phase 0 makes no persistence changes.
- [x] Do not add a search engine or search-index table in this phase. Phase 0 makes no schema changes.
- [x] Keep ControlPlane Web out of scope unless QA docs need boundary evidence. Phase 0 touches no ControlPlane Web code.
- [x] Every new/changed active V2 API must satisfy `docs/architecture/09-api-contract-standards.md`. Phase 0 adds no API surface; existing Storefront OpenAPI tests remain the guard.

Current code facts to preserve:

- [x] `ProductCatalogQuery` already contains page, page size, category, search, price, stock, published, sort, and created-after fields.
- [x] `StorefrontProductCatalogQuery` already publishes validation metadata, named sort values, default page size 24, and max page size 100.
- [x] `StorefrontPagedResponse<T>` already returns `Items`, `PageNumber`, `PageSize`, `TotalCount`, and `TotalPages`.
- [x] `CatalogProductReadModel` and `StorefrontCatalogProductResponse` already provide the public product summary projection.
- [x] Commerce Node published catalog query already filters by current store, published/non-archived product, published category, category slug, descendant category scope, price, stock, and sort.
- [x] `PublicCatalogService.GetPublishedCatalogPageAsync` already caches catalog page results by store and query values.
- [x] `SearchPage.razor` already uses the paged catalog API, renders pagination, applies noindex, and shows empty result copy.
- [x] `CategoryPage.razor` already uses the paged catalog API for filtered category results, but currently needs real paging/page-size behavior.
- [x] `CatalogFilterPanel.razor` already supports category, search, price range, sort, and in-stock controls.
- [x] `ProductGrid.razor` already renders grid cards and empty states.
- [x] `StorefrontIndexingPolicy` already keeps search pages `noindex, follow`.

## Phase 0 - Baseline And Contract Guard

Goal: protect current listing/search behavior before changing query semantics or UI.

Implementation checklist:

- [x] Re-read active V2 files before implementation:
  - [x] `BlazorShop.Web.SharedV2/Models/Product/ProductCatalogQuery.cs`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontApiContracts.cs`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontContractMappings.cs`.
  - [x] `BlazorShop.Infrastructure/Data/CommerceNode/Repositories/CommerceNodeProductReadRepository.cs`.
  - [x] `BlazorShop.Application/Services/PublicCatalogService.cs`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/SearchPage.razor`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CategoryPage.razor`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor`.
- [x] Add or verify tests for `ProductCatalogQuery` normalization and page-size clamp. Added `ProductCatalogQueryTests`.
- [x] Add or verify Storefront catalog contract tests for:
  - [x] `GET /api/storefront/stores/{storeKey}/catalog/products`.
  - [x] paging metadata.
  - [x] sort validation metadata.
  - [x] response schema.
- [x] Add or verify repository/service tests for:
  - [x] store scope.
  - [x] published-only products.
  - [x] category slug includes descendants.
  - [x] invalid category slug returns empty page.
- [x] Record current SearchPage behavior in `QA-StorefrontV2.todo.md`.
- [x] Record current CategoryPage behavior in `QA-StorefrontV2.todo.md`.
- [x] Make no schema or endpoint change in this phase.

Verification checklist:

- [ ] Focused catalog repository/service tests pass.
- [ ] Storefront catalog OpenAPI contract tests pass.
- [ ] Storefront API client route-building tests pass.
- [ ] Storefront search/category markup guardrails pass.
- [ ] No route ownership changes.

Exit criteria:

- [ ] Existing Storefront product listing/search still passes without new parameters.
- [ ] No legacy `api/internal/*` route is added.
- [ ] Known coverage gaps are written down before implementation phases.

Suggested commit:

```text
docs: plan catalog product search hardening
```

## Phase 1 - Search Query Hardening

Goal: define stable public search normalization/minimum behavior and broaden current search fields.

Implementation checklist:

- [x] Add catalog search options/constants in application/catalog area:
  - [x] minimum term length `2`.
  - [x] suggestion default limit `6`.
  - [x] suggestion max limit `10`.
  - [x] supported Storefront page sizes `12`, `24`, `48`.
- [x] Add shared search normalization helper:
  - [x] trim.
  - [x] collapse repeated whitespace.
  - [x] return null when empty.
  - [x] preserve Unicode text.
- [x] Update `CommerceNodeProductReadRepository` published catalog query:
  - [x] keep current store filter.
  - [x] keep published/non-archived visibility filters.
  - [x] keep category and descendant category filters.
  - [x] apply minimum length policy for non-empty search terms.
  - [x] search `Name`.
  - [x] search `ShortDescription`.
  - [x] search `Description`.
  - [x] search `Sku`.
  - [x] keep rank ordering for valid search.
  - [x] preserve existing sort behavior when search term is empty.
- [x] Add Commerce Node migration/index only if current query plan requires it:
  - [x] PostgreSQL FTS expression index over public search fields, if needed.
  - [x] `CommerceNodeDbContext` migration only.
- [x] Update catalog cache key version if search semantics change.

Verification checklist:

- [x] Search by name still works.
- [x] Search by SKU works.
- [x] Search by short description works.
- [x] Search by description works.
- [x] One-character non-empty search avoids expensive broad query and returns empty result/clear policy result.
- [x] Empty search still browses products.
- [x] Store scope still blocks cross-store products.
- [x] Category scope still includes descendants.

Exit criteria:

- [x] Public search behavior is normalized and deterministic.
- [x] Search does not become broader or slower for accidental short queries.
- [x] No unsupported field such as brand/rating/spec is searched.

Suggested commit:

```text
feat(commerce-node): harden catalog search query
```

## Phase 2 - Category And Search Paging UX

Goal: make Storefront listing pages usable for real catalogs.

Implementation checklist:

- [x] Update `SearchPage.razor`:
  - [x] add `pageSize` query support.
  - [x] add `sortBy` query support if not already complete.
  - [x] preserve `q`, category, page, pageSize, sort, price, and stock query values.
  - [x] show a clear message for too-short search terms.
  - [x] keep `noindex, follow`.
- [x] Update `CategoryPage.razor`:
  - [x] add `page` query support.
  - [x] add `pageSize` query support.
  - [x] use paged result metadata instead of `_displayProducts.Count`.
  - [x] render pagination controls.
  - [x] preserve min/max price, in-stock, sort, page, and pageSize in links.
  - [x] keep category not-found and service-unavailable behavior unchanged.
- [x] Update `CatalogFilterPanel` or add a small listing toolbar:
  - [x] page-size select with `12`, `24`, `48`.
  - [x] sort select uses named API values.
  - [x] keep price controls.
  - [x] keep in-stock control.
  - [x] no grid/list switch.
- [x] Update Storefront API client route builder if query parameters are missing. Existing client route builder already emitted page/pageSize/sort/filter query values.
- [x] Keep generated URLs stable and readable.

Verification checklist:

- [x] Category pages can move past the first page.
- [x] Category page page-size selector changes result size.
- [x] Search results preserve query/category/sort/page-size between pages.
- [x] Empty result states remain clear.
- [x] Service unavailable and not-found paths remain unchanged.
- [x] Storefront markup/static tests pass.
- [x] Storefront host smoke tests pass.

Exit criteria:

- [x] Search and category listing UX are consistently paged.
- [x] Query-state preserving navigation works without UI regressions.
- [x] Search route remains noindex.

Suggested commit:

```text
feat(storefront): add catalog search paging controls
```

## Phase 3 - Filter Metadata Contract

Goal: provide a stable backend filter contract without pretending unsupported facets exist.

Implementation checklist:

- [x] Add Storefront response DTOs:
  - [x] `StorefrontProductFilterMetadataResponse`.
  - [x] `StorefrontFilterFacetResponse`.
  - [x] `StorefrontFilterChoiceResponse`.
  - [x] `StorefrontPriceFacetResponse`.
  - [x] `StorefrontProductSortOptionResponse`.
- [x] Add Storefront catalog endpoint:
  - [x] `GET /api/storefront/stores/{storeKey}/catalog/product-filter-metadata`.
- [x] Add query support:
  - [x] `categorySlug`.
  - [x] `searchTerm`.
  - [x] `currencyCode`.
- [x] Populate metadata:
  - [x] page sizes `12`, `24`, `48`.
  - [x] sort options from existing named sort values.
  - [x] category choices from published category tree.
  - [x] price min/max from published current-store products in scope.
  - [x] availability choices based on current `InStock` filter.
  - [x] new-arrival choice using `CreatedAfterUtc` policy.
  - [x] `DisplayOrder`.
  - [x] `MaxChoices`.
  - [x] `MinimumHitCount`.
- [x] Keep hit counts nullable unless cheap and tested.
- [x] Do not return brand/rating/delivery/specification facets.
- [x] Add `StorefrontApiClient.GetProductFilterMetadataAsync`.
- [x] Update OpenAPI metadata:
  - [x] stable operationId.
  - [x] summary.
  - [x] response schema.
  - [x] error responses.
  - [x] validation metadata.

Verification checklist:

- [x] Storefront API contract tests pass.
- [x] OpenAPI snapshot updated.
- [x] Generator-safe schema tests pass.
- [x] Filter metadata service/repository tests pass.
- [x] Metadata is store-scoped.
- [x] Metadata omits unsupported facets.

Exit criteria:

- [x] Storefront can render filter choices from API metadata.
- [x] Public contract is explicit enough for generated clients and AI agents.
- [x] No empty placeholder facets are returned.

Suggested commit:

```text
feat(storefront-api): add product filter metadata
```

## Phase 4 - Instant Search Suggestions

Goal: add low-risk autocomplete support that future header/WASM components can consume.

Implementation checklist:

- [ ] Add Storefront query DTO:
  - [ ] `StorefrontSearchSuggestionQuery`.
- [ ] Add response DTOs:
  - [ ] `StorefrontSearchSuggestionResponse`.
  - [ ] `StorefrontSearchSuggestionItemResponse`.
- [ ] Add Storefront endpoint:
  - [ ] `GET /api/storefront/stores/{storeKey}/catalog/search-suggestions`.
- [ ] Add service/repository method using same published catalog visibility rules.
- [ ] Apply suggestion rules:
  - [ ] normalized term.
  - [ ] minimum term length.
  - [ ] default limit `6`.
  - [ ] max limit `10`.
  - [ ] optional category slug scope.
  - [ ] optional image projection.
  - [ ] optional currency display mapping.
- [ ] Suggestion item includes:
  - [ ] id.
  - [ ] slug.
  - [ ] name.
  - [ ] SKU.
  - [ ] image.
  - [ ] primary media public id.
  - [ ] has primary media.
  - [ ] price.
  - [ ] display price.
  - [ ] display currency code.
  - [ ] category name.
  - [ ] category slug.
  - [ ] in-stock flag.
  - [ ] URL.
- [ ] Add `StorefrontApiClient.GetSearchSuggestionsAsync`.
- [ ] Ensure suggestion API is not linked in sitemap/robots discovery.
- [ ] Update OpenAPI metadata and snapshot.

Verification checklist:

- [ ] Too-short term returns empty suggestions.
- [ ] Suggestions return at most 10 items.
- [ ] Suggestions never expose unpublished products.
- [ ] Suggestions never expose archived products.
- [ ] Suggestions never expose wrong-store products.
- [ ] Suggestions honor category scope.
- [ ] Suggestions include URL and safe display fields.
- [ ] Storefront API client tests pass.
- [ ] Storefront OpenAPI contract tests pass.

Exit criteria:

- [ ] Suggestion endpoint is read-only, store-scoped, and generator-safe.
- [ ] Future header/typeahead UI can consume suggestions without guessing models.

Suggested commit:

```text
feat(storefront-api): add catalog search suggestions
```

## Phase 5 - Storefront Integration

Goal: wire improved search/listing APIs into Storefront UI without a large redesign.

Implementation checklist:

- [ ] Update `CatalogFilterPanel` or listing toolbar to consume supported page sizes.
- [ ] Update Storefront pages to preserve query state across:
  - [ ] page changes.
  - [ ] page-size changes.
  - [ ] sort changes.
  - [ ] category changes.
  - [ ] search term changes.
  - [ ] price filter changes.
  - [ ] stock filter changes.
- [ ] Optionally add suggestion behavior to header/search input only if current layout can support it cleanly.
- [ ] If suggestion UI is implemented:
  - [ ] minimum characters before request.
  - [ ] debounce before request.
  - [ ] arrow up/down selection support.
  - [ ] Enter selection support.
  - [ ] mobile stacked list behavior.
  - [ ] no text overlap.
- [ ] Keep `ProductGrid` grid-only.
- [ ] Keep SearchPage noindex.
- [ ] Keep suggestion API out of SEO discovery.
- [ ] Keep desktop and mobile layout stable.

Verification checklist:

- [ ] Storefront V2 build passes.
- [ ] Storefront static markup tests pass.
- [ ] Storefront host smoke tests pass.
- [ ] Browser QA verifies search results.
- [ ] Browser QA verifies category pagination.
- [ ] Browser QA verifies page-size selector.
- [ ] Browser QA verifies empty result state.
- [ ] Browser QA verifies suggestion keyboard/mobile behavior if UI is implemented.
- [ ] Browser QA finds no unexpected console errors.

Exit criteria:

- [ ] Storefront listing UX works on desktop and mobile.
- [ ] Search/category pages preserve filters across navigation.
- [ ] No layout/text-overlap regression in listing toolbar.
- [ ] Search route remains noindex.

Suggested commit:

```text
feat(storefront): integrate catalog product search
```

## Phase 6 - QA, API Contracts, And Cache Invalidation

Goal: finish with reliable contracts and practical runtime behavior.

Implementation checklist:

- [ ] Add/update Commerce Node API contract tests for:
  - [ ] operation IDs.
  - [ ] summaries.
  - [ ] response schemas.
  - [ ] error responses.
  - [ ] validation metadata.
  - [ ] generator-safe names.
  - [ ] OpenAPI validation.
  - [ ] snapshot coverage.
- [ ] Add/update service/repository tests for:
  - [ ] search fields.
  - [ ] minimum term length.
  - [ ] category scope.
  - [ ] page-size clamp.
  - [ ] filter metadata.
  - [ ] suggestions.
- [ ] Add/update Storefront tests for:
  - [ ] API client route building.
  - [ ] API client model compatibility.
  - [ ] search page query state.
  - [ ] category page query state.
  - [ ] pagination controls.
  - [ ] page-size selector.
  - [ ] empty result state.
  - [ ] noindex metadata.
- [ ] Confirm catalog cache invalidation still fires after:
  - [ ] product changes.
  - [ ] category changes.
  - [ ] inventory changes.
  - [ ] primary media changes.
- [ ] Update QA docs:
  - [ ] `QA-CommerceNode.todo.md`.
  - [ ] `QA-StorefrontV2.todo.md`.
  - [ ] `QA-ControlPlane.todo.md` only if Control Plane boundary evidence is touched.
- [ ] Build active V2 projects touched by the phase.
- [ ] Review diff for:
  - [ ] no legacy `BlazorShop.Presentation` feature changes.
  - [ ] no `AppDbContext` migration.
  - [ ] no new `api/internal/*`.
  - [ ] no search engine/search-index table.
  - [ ] no unsupported facets.

Verification checklist:

- [ ] CommerceNode API build passes.
- [ ] Storefront V2 build passes.
- [ ] Storefront OpenAPI contract tests pass.
- [ ] Focused repository/service tests pass.
- [ ] Focused Storefront API client tests pass.
- [ ] Storefront host/static tests pass.
- [ ] Visible browser QA passes for changed Storefront flows when runtime is available.

Exit criteria:

- [ ] Focused tests pass.
- [ ] Browser QA evidence is recorded or explicitly marked pending with reason.
- [ ] QA checklists contain evidence.
- [ ] No active V2 route is added under `api/internal/*`.
- [ ] Deferred advanced search/facet/merchandising features remain unimplemented.

Suggested commit:

```text
test(catalog-search): complete release gate
```

## QA Checklist Seeds

### Commerce Node

- [ ] Product catalog listing remains store-scoped.
- [ ] Product catalog listing excludes unpublished products.
- [ ] Product catalog listing excludes archived products.
- [ ] Product catalog listing excludes wrong-store products.
- [ ] Category slug includes descendant categories.
- [ ] Invalid category slug returns empty page.
- [ ] Search by product name works.
- [ ] Search by SKU works.
- [ ] Search by short description works.
- [ ] Search by description works.
- [ ] One-character non-empty search follows minimum term policy.
- [ ] Empty search still browses current scope.
- [ ] Page-size clamp still protects API max page size.
- [ ] Sort values remain named strings in Storefront OpenAPI.
- [ ] Product filter metadata endpoint returns only supported facets.
- [ ] Product filter metadata endpoint is store-scoped.
- [ ] Product filter metadata endpoint exposes page sizes and sort options.
- [ ] Product filter metadata endpoint exposes price range.
- [ ] Search suggestions endpoint returns empty for too-short terms.
- [ ] Search suggestions endpoint caps result count at 10.
- [ ] Search suggestions endpoint excludes unpublished/wrong-store products.
- [ ] Search suggestions endpoint includes safe URL/display fields.
- [ ] Storefront OpenAPI validates and snapshot passes.

### Storefront V2

- [ ] Search page supports `page`.
- [ ] Search page supports `pageSize`.
- [ ] Search page supports named `sortBy`.
- [ ] Search page preserves query state across pagination.
- [ ] Search page shows clear empty result state.
- [ ] Search page shows clear too-short search state.
- [ ] Search page remains noindex.
- [ ] Category page supports `page`.
- [ ] Category page supports `pageSize`.
- [ ] Category page uses paged metadata for pagination.
- [ ] Category page preserves filters across pagination.
- [ ] Listing page-size selector supports 12/24/48.
- [ ] Listing sort selector uses named sort values.
- [ ] Product grid remains stable and responsive.
- [ ] Instant search suggestions render keyboard-friendly list if UI is implemented.
- [ ] Instant search suggestions render mobile-friendly list if UI is implemented.
- [ ] Browser QA finds no unexpected console errors.

### Control Plane

- [ ] ControlPlane Web still calls only ControlPlane API.
- [ ] No ControlPlane Web direct call to CommerceNode API is introduced.
- [ ] Control Plane admin product search redesign remains out of scope.

## Final Release Gate

- [ ] Existing `GET catalog/products` contract remains compatible.
- [ ] New Storefront filter metadata contract is explicit and generator-safe.
- [ ] New Storefront search suggestions contract is explicit and generator-safe.
- [ ] Search/category pages have real paging and page-size UX.
- [ ] Search behavior is normalized and minimum-length protected.
- [ ] Store scope is enforced throughout listing, metadata, and suggestions.
- [ ] Search and suggestion APIs expose no unpublished, archived, or wrong-store products.
- [ ] Search page stays noindex and suggestion API is not discoverable as SEO content.
- [ ] Cache invalidation remains correct after catalog/inventory/media changes.
- [ ] QA checklist files contain evidence.
- [ ] No unsupported facets/search engine/analytics/merchandising features were implemented.

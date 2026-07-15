# BlazorShop Menu Navigation Core Autoplan

Generated: 2026-07-15

Scope:

- 8.1 Menu structure
- 8.2 Menu item types
- 8.3 Navigation behavior
- 8.4 Optional navigation extensions

Autoplan note: this plan follows the repo architecture rules and the current V2 code shape. External dual-voice review agents were not used in this session, so the plan includes an internal decision audit and phased risk controls.

## Executive Decision

Build a store-scoped navigation core, not a full CMS/menu engine.

The first version should move hard-coded header/footer links into configurable store menus while preserving the existing Storefront V2 behavior. Functional entries such as cart, account, search, and checkout must be first-class navigation targets so the SSR layout can render the same shell before WASM/component hydration. However, navigation must not own their business logic.

Approved direction:

- Menu owns label, placement, order, enabled state, tree structure, and target resolution.
- Cart/account/search/checkout are reserved system targets or component slots.
- Cart/account/search/checkout logic stays in the existing or future WASM/components.
- Storefront SSR renders stable links/placeholders so layout does not shift after WASM loads.
- Advanced rules, mega menu, brand menu, and customer-role visibility are deferred.

## Current Code Facts

| Area | Current state |
| --- | --- |
| Header navigation | `StorefrontHeader.razor` hard-codes Home, New Releases, Today Deals, About, Customer Service, search, cart, account, and mobile links. |
| Footer navigation | `StorefrontFooter.razor` hard-codes Home, New Releases, Today Deals, About, Customer Service, FAQ, Privacy, and Terms. |
| Route helpers | `StorefrontRoutes.cs` already centralizes routes for cart, checkout, search, auth, product, category, and page slugs. |
| Category tree | Category already has store ownership, parent/child, display order, slug, published state, and cached public tree query. |
| Pages | `StorefrontPage` and `StorefrontPageService` already support store-scoped published page lookup by slug. |
| Product links | Product slug route exists. Product target support is feasible, but admin target selection needs validation. |
| Manufacturer links | No active V2 manufacturer/brand domain model was found. Do not add this target yet. |
| Breadcrumbs | Breadcrumbs are built locally by product/category/page components, not by a navigation service. |
| Cache patterns | Storefront catalog and public configuration already use per-store cache/invalidation patterns. |

## In Scope

- Multiple menus per store by system name, initially: `main`, `footer_company`, `footer_support`, `footer_legal`, `utility`, and optional `mobile`.
- Tree menu structure with parent/child, display order, enabled state, and cycle prevention.
- Store-owned menu rows in Commerce Node.
- Menu item target types:
  - `system`
  - `category`
  - `page`
  - `product`
  - `external_url`
  - `group`
  - `internal_route` only for allowlisted routes
- Reserved system targets:
  - `home`
  - `search`
  - `cart`
  - `checkout`
  - `account`
  - `login`
  - `register`
  - `new_releases`
  - `todays_deals`
- Canonical storefront URL resolution.
- Public storefront menu projection API.
- Commerce admin API through Commerce Node, proxied by Control Plane API.
- Control Plane manager UI for menu management.
- Cache and invalidation for menu tree changes and target slug/publish changes.
- SSR rendering of navigation shell with stable component slots for search/cart/account.

## Out Of Scope For MVP

- Manufacturer/brand target type, because there is no active V2 manufacturer model yet.
- Language-specific labels until the localization model is implemented.
- Mega menu, promotion banner in menu, rule-based visibility, and customer-role-specific menu.
- Lazy loading menu branches. Current menu depth and size do not justify it.
- Menu-driven breadcrumb replacement. Keep existing product/category/page breadcrumbs first.
- Moving cart/account/search/checkout business behavior into menu configuration.
- Arbitrary internal route entry that can bypass route ownership or expose unsupported pages.

## Core Decisions

### Decision 1: Store-Owned Menus

Use direct `StoreId` ownership on menu and menu item entities. Do not build many-to-many store mapping for menus in the first version.

Reason:

- Storefront V2 always resolves one current store before public data access.
- Store-specific navigation is easier to validate, cache, and render when rows are store-owned.
- Cross-store copied menus can be handled later by copy/import tooling instead of shared mutable rows.

### Decision 2: Reserved System Targets

Cart, account, search, and checkout must be menu targets, but only as reserved target keys.

Examples:

| Target key | SSR fallback | Component/WASM responsibility |
| --- | --- | --- |
| `search` | GET form or link to `/search` | Search UX, suggestions, filters, async behavior |
| `cart` | Link to `/my-cart` with stable badge placeholder | Cart count and cart drawer/component behavior |
| `account` | Account menu shell or login/register fallback | Auth state, account dropdown, customer actions |
| `checkout` | Link to `/checkout` | Checkout validation and flow state |

Reason:

- The layout remains consistent before and after WASM hydration.
- Menu configuration can control whether these entries appear in desktop/mobile/footer positions.
- Component code remains isolated and can be moved to WASM later without changing the menu data model.

### Decision 3: Target IDs, Not Stored URLs

For category, page, and product targets, store stable entity identifiers and resolve canonical URLs at read time.

Reason:

- Slug changes should not break menu items.
- Unpublished or archived targets can be hidden from public projection.
- Admin UI can show broken target status instead of serving stale links.

### Decision 4: Public Projection Must Be Safe

The storefront API should return only rendered navigation data:

- label
- href or null
- target type/key
- active matching hints
- children
- icon key only if allowlisted
- `opensInNewTab`

It must not return admin notes, internal entity IDs, permissions, unpublished target metadata, or private configuration.

### Decision 5: Breadcrumb Replacement Is Deferred

Keep local breadcrumbs in existing product/category/page components. Add only lightweight active item hints in the navigation response.

Reason:

- Current breadcrumbs are already close to page context.
- Replacing them with menu-driven breadcrumbs would change behavior for pages that appear in multiple menus.

## Proposed Data Model

### StoreNavigationMenu

Commerce Node table: `store_navigation_menu`

Fields:

- `Id`
- `PublicId`
- `StoreId`
- `SystemName`
- `DisplayName`
- `IsEnabled`
- `CreatedAt`
- `UpdatedAt`
- `ArchivedAt`

Indexes and constraints:

- Unique active menu by `(StoreId, SystemName)`.
- Index `(StoreId, IsEnabled)`.
- Validate `SystemName` against known menu names for MVP.

### StoreNavigationMenuItem

Commerce Node table: `store_navigation_menu_item`

Fields:

- `Id`
- `PublicId`
- `StoreId`
- `MenuId`
- `ParentItemId`
- `Label`
- `TargetType`
- `TargetKey`
- `TargetEntityPublicId`
- `Url`
- `IsEnabled`
- `DisplayOrder`
- `OpensInNewTab`
- `CreatedAt`
- `UpdatedAt`
- `ArchivedAt`

Validation:

- `StoreId` must match the parent menu store.
- `ParentItemId` must belong to the same menu.
- No parent cycles.
- `Label` is required except for system component slots that render their own accessible label.
- `TargetType=system` requires allowlisted `TargetKey`.
- `TargetType=category/page/product` requires `TargetEntityPublicId`.
- `TargetType=external_url` requires `https://` URL.
- `TargetType=group` must not have `Url` or target entity.
- `internal_route` is allowlist-only and should be used sparingly.

Deferred fields:

- `LanguageId`
- `IconKey`
- `ImageAssetId`
- visibility rules
- customer role rules
- mega menu layout metadata

## Target Resolution Rules

| Target type | Resolution behavior |
| --- | --- |
| `system` | Resolve by `StorefrontRoutes` constants or component slot key. |
| `category` | Resolve current-store published category slug. Hide if missing, archived, unpublished, or cross-store. |
| `page` | Resolve current-store published page slug. Hide if missing, archived, unpublished, or cross-store. |
| `product` | Resolve current-store visible product slug. Hide if missing, archived, unpublished, or cross-store. |
| `external_url` | Return sanitized HTTPS URL. |
| `group` | Return no href and render as non-clickable heading/group. |
| `internal_route` | Resolve only allowlisted static storefront route keys. |

Admin APIs should return broken/invalid target status. Public storefront APIs should omit invalid target items and their invalid children unless the parent is a valid group with valid descendants.

## API Plan

All active V2 APIs must follow `docs/architecture/09-api-contract-standards.md`.

### Commerce Node Admin API

Route group: `api/commerce/admin/navigation`

Endpoints:

- `GET /menus?storeKey={storeKey}`
- `POST /menus`
- `GET /menus/{menuPublicId}`
- `PUT /menus/{menuPublicId}`
- `POST /menus/{menuPublicId}/items`
- `PUT /items/{itemPublicId}`
- `DELETE /items/{itemPublicId}` or `POST /items/{itemPublicId}/archive`
- `PUT /menus/{menuPublicId}/items/order`
- `GET /target-options?storeKey={storeKey}&targetType={targetType}&search={search}`
- `GET /system-targets`

Required behavior:

- Protected by commerce admin credential/auth pattern already used by Commerce Node admin endpoints.
- Stable operation IDs.
- Explicit DTOs.
- Validation error schemas.
- No domain entities in API contracts.
- No server-owned fields in request DTOs.

### Control Plane Gateway API

Control Plane Web must call Control Plane API, not Commerce Node API directly.

Suggested route group:

- `api/control-plane/commerce/stores/{storePublicId}/navigation/*`

Behavior:

- Resolve store/node from Control Plane registry.
- Forward to Commerce Node admin navigation API.
- Enforce platform permissions before forwarding.
- Preserve route ownership boundary.

### Storefront Public API

Route group: `api/storefront/stores/{storeKey}/navigation`

Endpoints:

- `GET /{systemName}`
- Optional batch endpoint: `GET ?systemNames=main,footer_company,footer_support,footer_legal`

Response DTO:

- `systemName`
- `items`
- `cacheVersion` or `generatedAt`

Menu item DTO:

- `label`
- `href`
- `targetType`
- `targetKey`
- `opensInNewTab`
- `isCurrentHint`
- `children`

The public response must not include private admin fields or unresolved internal IDs.

## Permission Plan

Add dedicated permissions rather than reusing page/catalog permissions:

- `commerce.navigation.read`
- `commerce.navigation.write`

Reason:

- Navigation can expose or hide major storefront entry points.
- It links across pages, catalog, and system routes.
- Separate permission allows managers to adjust layout/menu without full catalog/page edit rights.

## Control Plane UI Plan

Add a Commerce manager navigation page.

UI capabilities:

- Store selector or current store context.
- Menu selector tabs for `main`, footer menus, `utility`, and optional `mobile`.
- Tree editor with drag/reorder controls.
- Enable/disable menu and item.
- Add/edit item drawer.
- Target type selector.
- Target-specific fields:
  - system target dropdown
  - category picker
  - page picker
  - product picker
  - external URL input
  - group label input
- Broken target badge.
- Preview resolved URL.
- Save/reorder actions.

Do not add:

- Rule builder.
- Mega menu editor.
- Contact/cart/account/search business settings.
- Arbitrary HTML injection.

## Storefront Rendering Plan

Replace hard-coded header/footer link lists with menu projections in small steps.

Header:

- Load `main` and `utility` menus.
- Render normal menu items recursively.
- Render reserved component slots for `search`, `cart`, and `account`.
- Keep existing `StorefrontAccountMenu` behavior for account fallback.
- Keep search form behavior compatible with current `/search` route.
- Keep cart route fallback to `/my-cart`.

Footer:

- Load footer menu groups.
- Render configured page/system/external links.
- Keep store company/contact data from the store display context, not from menu items.

Mobile:

- Use the same public menu tree.
- Keep the current mobile `<details>` structure for MVP unless a separate responsive shell phase changes it.
- Do not require branch lazy loading.

Active item:

- Match current path against resolved href.
- For category/product/page detail pages, use route helper match first.
- Do not replace existing breadcrumbs in this phase.

## Cache And Invalidation Plan

Add `IStorefrontNavigationCache` or extend an existing per-store cache pattern if it fits cleanly.

Cache key shape:

- `store:{storeId}:navigation:{systemName}:v1`
- Optional batch key for common header/footer load.

Invalidate when:

- Menu is created, updated, disabled, archived, or reordered.
- Menu item is created, updated, disabled, archived, moved, or reordered.
- A target category slug/published/archive state changes.
- A target page slug/published/archive state changes.
- A target product slug/published/archive state changes.

Initial implementation can invalidate all navigation cache for the store. Fine-grained invalidation can wait until menu volume proves it is needed.

## Phase Plan

### Phase 0 - Baseline And Guardrails

Goal: confirm exact existing behavior before changing runtime.

Tasks:

- Capture current hard-coded header/footer route list.
- List all static route constants in `StorefrontRoutes`.
- Confirm active V2 API patterns for Commerce Node admin and Storefront public endpoints.
- Confirm current Control Plane gateway pattern for Commerce Node admin calls.
- Add the implementation checklist to the relevant QA todo files before code changes.

Exit criteria:

- No schema or runtime change yet.
- Header/footer links to preserve are explicitly listed.
- Reserved system target list is locked for Phase 1.

### Phase 1 - Domain And Database

Goal: introduce menu persistence without changing storefront rendering.

Tasks:

- Add `StoreNavigationMenu` and `StoreNavigationMenuItem` domain entities.
- Add EF configuration in `CommerceNodeDbContext`.
- Add migration for Commerce Node database only.
- Add enum/string constants for menu system names and target types.
- Add validation for store ownership, parent cycles, target requirements, and display order.
- Seed or backfill default menus per existing dev/demo stores if the repo already has store seed patterns.

Exit criteria:

- Commerce Node database can migrate.
- Existing storefront continues using hard-coded links.
- Unit tests cover target validation and parent cycle prevention.

### Phase 2 - Navigation Service, Resolver, And Cache

Goal: produce safe menu projections from stored data.

Tasks:

- Add navigation application service in Commerce Node boundary.
- Add canonical target resolver using `StorefrontRoutes`.
- Resolve category/page/product targets only within the current store.
- Omit invalid public targets from storefront projection.
- Return broken target state for admin projection.
- Add per-store navigation cache.
- Add store-wide navigation cache invalidation on menu changes.

Exit criteria:

- Service can return admin menu tree and public safe menu tree.
- Public projection contains no private/admin fields.
- Invalid or unpublished targets do not leak to storefront.

### Phase 3 - APIs And Permissions

Goal: expose navigation management and public read APIs through correct boundaries.

Tasks:

- Add Commerce Node admin navigation endpoints.
- Add Commerce Node storefront public navigation endpoints.
- Add Control Plane gateway endpoints.
- Add `commerce.navigation.read` and `commerce.navigation.write`.
- Add request/response DTOs with OpenAPI metadata.
- Add contract tests for operation IDs, security metadata, validation errors, and public response shape.

Exit criteria:

- Control Plane Web can manage navigation only through Control Plane API.
- Storefront V2 can read only `api/storefront/stores/{storeKey}/navigation/*`.
- OpenAPI remains valid and generator-safe.

### Phase 4 - Control Plane Manager UI

Goal: allow managers to configure menus safely.

Tasks:

- Add Navigation manager page under the Commerce/store management area.
- Add menu selector and tree editor.
- Add create/edit item drawer.
- Add target pickers for system, category, page, product, external URL, and group.
- Add reorder and enable/disable actions.
- Show broken target warnings in admin view.
- Gate page/actions by navigation permissions.

Exit criteria:

- Manager can recreate the current header/footer link structure from UI.
- Broken targets are visible before saving or publishing.
- Control Plane Web has no direct Commerce Node calls.

### Phase 5 - Storefront Header/Footer Integration

Goal: replace hard-coded menu links without changing visible behavior.

Tasks:

- Add Storefront V2 navigation client methods.
- Load `main`, `utility`, and footer menus from the public API.
- Render menu tree recursively with stable dimensions.
- Map reserved system targets to existing components/slots:
  - `search` -> search form/component slot
  - `cart` -> cart icon/link slot
  - `account` -> account menu shell slot
  - `checkout` -> checkout link
- Keep route fallbacks if public menu API fails.
- Preserve current mobile navigation behavior.

Exit criteria:

- Header/footer still expose the same default links after migration.
- Cart/account/search/checkout render before WASM enhancement.
- If WASM fails, fallback links remain usable.

### Phase 6 - Target Health And Invalidation Expansion

Goal: make target changes reliably reflected in navigation.

Tasks:

- Invalidate navigation cache from category slug/publish/archive changes.
- Invalidate navigation cache from page slug/publish/archive changes.
- Invalidate navigation cache from product slug/publish/archive changes.
- Add admin target health summary per menu.
- Add tests for changed slug resolving to the new canonical URL.

Exit criteria:

- Menu links follow slug changes without manual edit.
- Public menus hide unpublished targets after cache invalidation.
- Admin can see which menu items need attention.

### Phase 7 - Optional Navigation Enhancements

Goal: add only proven-needed extensions after the core is stable.

Candidate items:

- Language-specific labels after localization foundation exists.
- Icon key support with allowlisted icons.
- Image asset support after asset/page media rules are stable.
- Account menu system name if account dropdown shell needs manager-controlled labels.
- Mega menu layout metadata.
- Promotion banner in menu.
- Customer-role or rule-based visibility.
- Lazy branch loading for large menu trees.
- Manufacturer/brand target after manufacturer/brand domain model exists.

Exit criteria:

- Each optional item has a separate approved mini-plan.
- No optional extension blocks the MVP navigation rollout.

### Phase 8 - QA, Docs, And Release Closeout

Goal: finish the phase without hidden regressions.

Tasks:

- Update `QA-ControlPlane.todo.md`.
- Update `QA-CommerceNode.todo.md`.
- Update `QA-StorefrontV2.todo.md`.
- Add manual browser checks for desktop and mobile navigation.
- Add API contract verification.
- Add migration verification against Commerce Node database.
- Document route/target rules for future agents.

Exit criteria:

- Focused tests pass.
- Browser smoke test confirms header/footer/mobile rendering.
- QA checklist is completed or has explicit unresolved items.
- Code is committed only after verification.

## QA Checklist Draft

### Commerce Node

- Menu system name is unique per store.
- Menu item parent must belong to the same menu.
- Parent cycles are rejected.
- Disabled menus and items are excluded from public projection.
- Public projection hides unpublished category/page/product targets.
- External URL validation rejects non-HTTPS URLs.
- Reserved system target keys resolve to expected `StorefrontRoutes`.
- Store A cannot resolve Store B category/page/product targets.
- Cache invalidates after menu item create/update/reorder/archive.

### Control Plane

- User without `commerce.navigation.read` cannot view navigation manager.
- User without `commerce.navigation.write` cannot save menu changes.
- Control Plane Web calls only Control Plane API.
- Tree editor can create, edit, disable, and reorder items.
- Target picker validates system/category/page/product/external/group targets.
- Broken target badge appears for deleted/unpublished targets.

### Storefront V2

- Header renders `main` menu from API.
- Footer renders footer menus from API.
- Mobile menu uses the same menu tree.
- Active link highlights correctly for static routes.
- Category/page/product links use canonical slugs.
- Cart/account/search/checkout have stable SSR slots before WASM loads.
- Search fallback posts or navigates to `/search`.
- Cart fallback navigates to `/my-cart`.
- Checkout fallback navigates to `/checkout`.
- Account fallback shows login/register or account shell consistent with current behavior.
- Storefront does not render invalid cross-store links.

## Risks And Controls

| Risk | Control |
| --- | --- |
| Menu becomes a CMS/page engine | Keep page content and component logic outside navigation. |
| Header/footer behavior changes during migration | Seed/default menus from the current hard-coded links and keep fallback rendering. |
| WASM component migration becomes harder | Use reserved system targets and stable SSR slots instead of custom menu HTML. |
| Slug changes break links | Store target IDs and resolve canonical URLs at read time. |
| Cross-store data leak | Validate target store ownership in resolver and admin save paths. |
| API boundary violation | Control Plane Web calls Control Plane API only; Storefront calls Storefront public API only. |
| Overbuilt optional features slow MVP | Defer mega menu, rules, localization, lazy loading, and manufacturer target. |

## Definition Of Done

- Navigation entities and Commerce Node migration exist.
- Admin and public navigation services are covered by focused tests.
- Commerce Node admin, Storefront public, and Control Plane gateway APIs satisfy V2 contract standards.
- Control Plane UI can manage menus by store.
- Storefront header/footer render from configured menus.
- Cart/account/search/checkout remain functional reserved targets with stable SSR layout.
- Cache invalidation handles menu and target publish/slug changes.
- QA todo files are updated and completed for the implemented phase.
- No legacy `BlazorShop.Presentation/*` project is extended.


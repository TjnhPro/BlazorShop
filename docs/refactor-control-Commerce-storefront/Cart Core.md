# Cart Core

Generated: 2026-07-16

Status: Proposed implementation plan.

Scope: Move the existing V2 server-side cart from MVP behavior to a practical production-ready baseline without replacing the current Commerce Node and Storefront V2 architecture.

Autoplan note: This plan applies the autoplan review lens against product value, engineering risk, UX fit, and developer experience. It is intentionally conservative: keep the current working cart foundation, add missing behavior in additive phases, and avoid advanced commerce engines that are not needed yet.

## Approved Scope

- Cart belongs to a store.
- Guest cart with opaque cart token.
- Authenticated customer cart attachment and merge after login.
- Cart created/updated/expiration behavior.
- No public browser request may choose an arbitrary customer ID.
- Cart line product, variant/combination, quantity, selected attributes, personalization payload, artwork, fulfillment provider, and server price snapshot.
- Core cart commands: get current cart, add item, update quantity, remove item, clear cart, validate cart, recalculate cart.
- Add-to-cart validation on the server: product exists, published, store-visible, purchasable, variant/attribute selection valid, quantity valid, stock valid when managed.
- Basic totals: item subtotal, order subtotal, order total, currency, rounding, warning/adjustment breakdown.
- Cart UI projection: product display name, selected attributes, image, unit price, line total, quantity constraints, availability warnings, checkout allowed flag, cart badge summary.

## Not Approved For This Phase

- Full discount engine.
- Full tax engine.
- Full shipping estimator engine.
- Partial checkout or active/inactive line selection.
- Wishlist, saved carts, multi-cart per customer, quote cart, abandoned-cart marketing automation.
- Customer-role pricing, store-specific price hooks, or permission pricing beyond existing server-side price calculation.
- Signed JWT cart tokens. The current opaque random token plus hashed persistence is enough.
- Browser-supplied customer ID, order status, line price, discount, tax, or server-owned fields.
- Replacing checkout or redesigning payment/order flow.
- Extending legacy `AppDbContext` or legacy presentation projects.

## What Already Exists

### Domain And Data

- `CartSession` already stores `PublicId`, `StoreId`, `TokenHash`, `CustomerId`, `AppUserId`, `State`, `Version`, `LastActivityAtUtc`, `ExpiresAtUtc`, order conversion, merge target, and timestamps.
- `CartLine` already stores product, variant, line key, selected attributes JSON, personalization, artwork, fulfillment provider, quantity, price snapshots, currency snapshots, exchange-rate snapshots, and timestamps.
- `CommerceNodeDbContext` already owns `cart_sessions` and `cart_lines`.
- Existing indexes cover public ID, token hash, store/state, customer, app user, expiry, product, variant, artwork, and `(CartSessionId, LineKey)`.
- The cart already belongs to Commerce Node data ownership. No new DbContext is needed.

### Application Services

- `StorefrontCartService` already supports create/resume, get, add line, update line, remove line, clear, and validate.
- Add-line already recalculates price on the server and stores unit price/currency snapshots.
- Product validation already checks published storefront availability, store visibility, category state, variant resolution, selected custom attributes, quantity lower bound, and stock.
- `StorefrontCartSessionService` already creates secure random tokens, stores only SHA256 hashes, loads active sessions by store and token, rejects expired carts, merges line keys, and increments cart version.

### API And Storefront

- Commerce Node already exposes cart routes under `api/storefront/stores/{storeKey}/cart`.
- Storefront V2 uses local `/api/cart` endpoints and `StorefrontCartTokenService`, so the browser does not call Commerce Node directly.
- Storefront V2 already stores `bs-cart-token` as HttpOnly, SameSite Lax, secure outside development, and imports the legacy readable `my-cart` cookie into the server cart.
- Checkout already validates cart version and cart validity before order placement.
- Contract tests already cover cart operation IDs, `X-Cart-Token`, request body requirements, validation metadata, schema snapshots, and anonymous cart commands.

## Gaps To Close

- Current create/resume DTO allows `CustomerId` and `AppUserId`, but public browser flows must never be allowed to choose customer identity.
- Authenticated cart merge after login is not complete as a first-class cart behavior.
- `ValidateAsync` is non-mutating and does not refresh stale price snapshots. A separate recalculate command is needed.
- Cart response is too thin for UI. Storefront V2 has to load product details line by line, creating N+1 behavior and duplicating availability display logic.
- Cart totals are basic and not expressed as a stable projection contract with line totals, subtotal, total, warnings, and checkout eligibility.
- Quantity constraints are currently mostly `Quantity >= 1`; future availability rules need a stable output contract for min, max, step, allowed quantities, and purchasability reason.
- Cart item limit and payload limit rules need explicit server enforcement and contract tests.
- Expiration behavior exists on load, but cleanup and QA coverage should be formalized.

## Core Product Decisions

1. Keep the server-side cart as the source of truth.

   Storefront UI may cache and display cart state, but all add/update/recalculate/checkout decisions must be computed by Commerce Node services.

2. Keep opaque cart tokens.

   The existing random token plus hashed database storage is safer and simpler than exposing signed payloads. Token contents should remain meaningless to the browser.

3. Keep cart commands anonymous, but bind authenticated customer identity only from trusted auth context.

   Guest add-to-cart must stay frictionless. When the user logs in, merge/attach cart through an authenticated server flow that derives customer/app user from claims or trusted token result, never from browser JSON.

4. Add projection DTOs additively.

   Do not break existing `StorefrontCartResponse`. Add fields or introduce a dedicated projection endpoint only if schema compatibility becomes risky.

5. Separate validation and recalculation.

   `validate` should remain safe for checking current state. `recalculate` should be an explicit POST command that can update snapshots, totals, warnings, and cart version.

6. Basic totals now, engine hooks later.

   This phase should expose subtotal, total, currency, rounding, and warning breakdown. Discount, shipping, and tax can be represented as zero/default adjustment slots until their real engines exist.

7. Version remains the stale-update guard.

   Continue using `CartSession.Version` for checkout and mutating command concurrency.

## Target Boundary

```text
Storefront V2 browser
  -> Storefront V2 local /api/cart endpoints
      -> StorefrontCartTokenService
          -> Commerce Node Storefront API
              api/storefront/stores/{storeKey}/cart/*
                  -> StorefrontCartService
                      -> StorefrontCartSessionService
                      -> Product/catalog/currency/availability services
                      -> CommerceNodeDbContext
```

Rules:

- Store scope comes from the route/store resolver, not from request body trust.
- Browser requests do not send customer ID, user ID, price, tax, discount, or order-owned fields.
- Storefront V2 does not directly query `CommerceNodeDbContext`.
- Control Plane is not part of this runtime path.
- Legacy `AppDbContext` and legacy cart code remain migration reference only.

## Target API Direction

### Keep Existing Endpoints

- `POST /api/storefront/stores/{storeKey}/cart/session`
- `GET /api/storefront/stores/{storeKey}/cart`
- `POST /api/storefront/stores/{storeKey}/cart/lines`
- `PUT /api/storefront/stores/{storeKey}/cart/lines/{lineId}`
- `DELETE /api/storefront/stores/{storeKey}/cart/lines/{lineId}`
- `DELETE /api/storefront/stores/{storeKey}/cart`
- `POST /api/storefront/stores/{storeKey}/cart/validate`

### Add Or Extend

- `POST /api/storefront/stores/{storeKey}/cart/recalculate`
  - Requires `X-Cart-Token`.
  - Optional expected version.
  - Revalidates each line.
  - Recomputes server-side price snapshots where needed.
  - Returns the same enriched cart projection.
  - Increments version only when persisted values change.

- `POST /api/storefront/stores/{storeKey}/cart/merge-current-customer`
  - Requires `X-Cart-Token`.
  - Requires authenticated user context.
  - Derives customer/app user from auth context.
  - Merges current guest cart into the active customer cart or attaches it if no active customer cart exists.
  - Rejects request body identity fields.

### Enriched Cart Projection

Additive fields for cart response:

- `currencyCode`
- `subtotal`
- `discountTotal`
- `shippingEstimate`
- `taxEstimate`
- `grandTotal`
- `checkoutAllowed`
- `warnings`
- `adjustments`
- `summaryCount`

Additive fields for line response:

- `displayName`
- `productSlug`
- `productUrl`
- `imageUrl`
- `selectedAttributes`
- `unitPrice`
- `lineSubtotal`
- `lineTotal`
- `quantityMinimum`
- `quantityMaximum`
- `quantityStep`
- `allowedQuantities`
- `purchasable`
- `warnings`

If adding these fields to the existing response risks too much churn, add `GET /cart/projection` and migrate Storefront V2 first. The preferred path is additive response fields because the current API already returns cart state.

## Data Model Direction

No new table is required for the first implementation phases.

Use existing fields:

- `cart_sessions.StoreId`
- `cart_sessions.TokenHash`
- `cart_sessions.CustomerId`
- `cart_sessions.AppUserId`
- `cart_sessions.State`
- `cart_sessions.Version`
- `cart_sessions.LastActivityAtUtc`
- `cart_sessions.ExpiresAtUtc`
- `cart_lines.LineKey`
- `cart_lines.SelectedAttributesJson`
- `cart_lines.PersonalizationJson`
- `cart_lines.Quantity`
- `cart_lines.UnitPriceSnapshot`
- `cart_lines.CurrencyCodeSnapshot`

Avoid storing transient warnings and projection-only data unless there is a clear business need. Compute warnings during projection/recalculation so product publication, stock, price, and currency changes are reflected immediately.

Potential later columns, not phase 1:

- `CartLine.IsSelected` only if partial checkout becomes a real requirement.
- `CartLine.ValidationCode` only if persistent invalid cart state needs audit/history.

## Implementation Phases

### Phase 0 - Baseline Guardrails

Goal: Protect existing behavior before changing cart contracts.

Tasks:

- Re-read current cart tests, checkout tests, Storefront V2 smoke tests, and Commerce Node OpenAPI contract tests.
- Add missing baseline tests for current cart behavior if any obvious regression gap exists.
- Confirm all active cart paths use `CommerceNodeDbContext`, not `AppDbContext`.
- Confirm Storefront V2 local `/api/cart` continues to hide Commerce Node internals from the browser.
- Record current response schema snapshots before adding projection fields.

Acceptance:

- Existing cart, checkout, and API contract tests pass.
- No legacy cart path is extended.
- No public request DTO accepts customer identity for new cart behavior.

### Phase 1 - Cart Projection And Basic Totals

Goal: Give Storefront V2 one stable cart shape that is enough to render cart page, mini-cart badge, and checkout readiness.

Tasks:

- Create application-level cart projection DTOs or extend current DTOs additively.
- Compute per-line:
  - display name
  - product URL/slug
  - image URL
  - selected attribute labels
  - unit price
  - line subtotal
  - line total
  - basic quantity constraints
  - line warnings
- Compute cart-level:
  - summary count
  - subtotal
  - zero/default discount, shipping estimate, and tax estimate placeholders
  - grand total
  - currency code
  - checkout allowed flag
  - cart warnings
- Keep rounding aligned with the existing currency/price services from the Currency and Pricing Core plans.
- Keep response additive to avoid breaking existing clients.
- Update OpenAPI metadata and schema tests.

Acceptance:

- `GET cart` returns enough data for Storefront V2 to render without per-line product detail N+1 calls.
- Existing client code still works with old fields.
- Product identity and media fields are storefront-safe.
- No admin-only DTOs or domain entities leak into public schemas.

### Phase 2 - Recalculate Command

Goal: Separate non-mutating validation from explicit snapshot refresh.

Tasks:

- Add `POST cart/recalculate`.
- Accept optional expected cart version.
- Resolve each cart line against current product, variant, availability, price, currency, and rounding rules.
- Update `UnitPriceSnapshot`, currency snapshot, base currency snapshot, and exchange-rate snapshot only when values changed.
- Increment cart version only when persisted line/cart data changes.
- Return enriched cart projection.
- Keep `validate` non-mutating and focused on issues/status.
- Return 409 for stale expected version.
- Return stable warning codes for invalid products, unavailable products, invalid variant, invalid quantity, insufficient stock, and missing price.

Acceptance:

- Stale cart update is rejected by version.
- Recalculate fixes stale snapshots when products/prices changed.
- Validation can still be called without side effects.
- Checkout can rely on recalculated/validated cart state before placing orders.

### Phase 3 - Authenticated Cart Attach And Merge

Goal: Make guest-to-customer cart behavior safe without trusting browser identity.

Tasks:

- Add authenticated merge/attach application method.
- Add authenticated Storefront API endpoint for current customer cart merge.
- Derive `CustomerId`/`AppUserId` from trusted auth context or server-side auth result only.
- If a customer has no active cart, attach the guest cart to the customer.
- If a customer has an active cart, merge guest lines using existing line-key behavior.
- Preserve price snapshot recalculation rules during merge.
- Mark old cart state as `merged` and set `MergedIntoCartId` when a merge occurs.
- Add Storefront V2 login-flow hook to merge the current guest cart after successful login when a cart token exists.
- Do not expose customer ID fields in browser requests.

Acceptance:

- Guest cart survives login.
- Existing customer cart and guest cart line quantities merge deterministically.
- Public browser cannot attach a cart to another customer by sending an ID.
- Merged/ordered/expired carts are not loadable as active carts.

### Phase 4 - Quantity Constraints And Item Limits

Goal: Enforce practical cart limits and prepare for the Availability Quantity plan.

Tasks:

- Add central quantity validation for:
  - minimum quantity
  - maximum quantity
  - quantity step
  - allowed quantities list when available
  - managed stock ceiling when applicable
- Add a cart item limit setting or conservative application option.
- Enforce personalization payload limits consistently.
- Return quantity constraints in line projection.
- Use stable warning/error codes that the UI can display without parsing message text.
- Keep initial defaults simple where catalog fields do not exist yet:
  - minimum quantity `1`
  - step `1`
  - maximum from stock when stock is managed, otherwise configured limit

Acceptance:

- Add/update quantity commands reject invalid quantities consistently.
- UI projection includes enough data to render quantity controls correctly.
- Large carts and oversized payloads are rejected before they create database or checkout risk.

### Phase 5 - Storefront V2 Cart UI Consumption

Goal: Move Storefront V2 from product-by-product lookup to the cart projection contract.

Tasks:

- Update local `/api/cart` endpoints to return the enriched projection.
- Update `CartPage` to render projection fields directly.
- Keep image, URL, selected attributes, unit price, line total, warnings, and checkout button state driven by projection.
- Update mini-cart badge to use `summaryCount`.
- Add checkout button disable/redirect behavior when `checkoutAllowed` is false.
- Keep noindex behavior on cart pages.

Acceptance:

- Cart page no longer needs to fetch each product separately just to render current lines.
- Cart badge count matches server projection.
- Invalid/unavailable cart lines show warnings and block checkout clearly.
- Existing add/update/remove/clear flows still work through Storefront V2 local endpoints.

### Phase 6 - Expiration, Cleanup, QA, And Contract Finish

Goal: Finish the phase with operational behavior and regression coverage.

Tasks:

- Formalize cart expiration policy in application options.
- Add cleanup job/task for expired carts if current task infrastructure is suitable.
- Keep first cleanup implementation simple: expire active sessions older than `ExpiresAtUtc`.
- Avoid hard delete in this phase unless storage pressure requires it.
- Update QA checklists:
  - `QA-CommerceNode.todo.md`
  - `QA-StorefrontV2.todo.md`
- Add or update tests:
  - application service tests
  - session service tests
  - Commerce Node API contract tests
  - Storefront V2 host smoke tests
  - checkout regression tests
- Run focused verification.

Acceptance:

- Expired carts cannot be used.
- Expired cart cleanup does not remove active, merged, or ordered carts incorrectly.
- OpenAPI remains generator-safe.
- Checkout/order tests still pass.

## Failure Modes To Design Against

- Unknown store token loads a cart from another store.
- Browser sends customer ID and attaches cart to another account.
- Cart token is stored in plaintext.
- Recalculate changes cart on `GET`.
- Checkout uses stale price snapshot after product price changed.
- Storefront cart page hides an unavailable item but still allows checkout.
- Product deleted/unpublished after add-to-cart causes null reference or checkout crash.
- Variant selection becomes invalid but the cart still places an order.
- Quantity update bypasses stock or minimum/maximum constraints.
- Merge duplicates lines incorrectly or loses personalization/artwork line-key differences.
- Old `my-cart` cookie remains readable after server cart import.
- Public API leaks provider secrets, internal product/admin fields, or domain entities.
- OpenAPI schema silently breaks generated clients.

## Test Map

Application tests:

- Create/resume guest cart.
- Add line with server price snapshot.
- Add same line merges quantity and increments version.
- Add same product with different variant/attributes/personalization/artwork keeps separate lines.
- Reject unavailable, wrong-store, unpublished, archived, invalid variant, invalid attributes, and invalid quantity.
- Validate is non-mutating.
- Recalculate updates stale snapshots and returns warnings.
- Merge guest cart into customer cart without browser-supplied customer ID.

Session service tests:

- Token hash only, no plaintext token persistence.
- Store-scoped token loading.
- Expired cart marked expired.
- Merged/ordered carts are not active.
- Version increments on mutating commands.

API contract tests:

- Stable operation IDs.
- `X-Cart-Token` documented.
- `recalculate` uses POST.
- Merge endpoint requires auth.
- Request schemas do not include customer ID, app user ID, unit price, discount, tax, or order-owned fields.
- Quantity fields have minimum validation metadata.
- Response schema includes projection fields and warning codes.

Storefront V2 tests:

- `/api/cart/lines` creates HttpOnly `bs-cart-token`.
- Browser request does not send price/customer identity.
- Cart page renders projection without product N+1 dependency.
- Badge uses server summary count.
- Invalid cart blocks checkout.
- Login merge keeps guest cart lines.
- Legacy `my-cart` cookie import still works and deletes legacy cookie.

Checkout regression tests:

- Checkout rejects stale cart version.
- Checkout rejects invalid cart after product/variant availability changes.
- Checkout uses server-side snapshots/totals.
- Ordered cart cannot be loaded as active cart.

## Migration And Compatibility

- No required database migration for phases 1 and 2 if projection/warnings remain computed.
- Phase 3 may use existing `CustomerId`, `AppUserId`, `State`, and `MergedIntoCartId` columns.
- Keep existing response fields and routes to avoid breaking Storefront V2.
- Add new fields as optional/additive response data.
- Keep legacy cookie import until old browser carts naturally expire.
- Do not delete or rewrite existing cart data during this phase.

## Final Phase Definition Of Done

- Storefront V2 can add, view, update, remove, clear, recalculate, and checkout cart lines through the server cart.
- Guest cart is store-scoped and token-secured.
- Authenticated login can attach or merge the current guest cart without trusting browser identity.
- Cart projection is rich enough for cart page, badge, warnings, and checkout eligibility.
- Server validates product, variant, quantity, price, currency, and availability before cart mutation and checkout.
- OpenAPI and tests protect the contract.
- No active V2 code depends on legacy cart storage.

## Decision Audit Trail

- Chosen: server-side cart remains the source of truth.
- Chosen: opaque random cart token with hashed persistence remains the identity model.
- Chosen: add projection and recalculate behavior before advanced engines.
- Chosen: customer identity comes only from authenticated server context.
- Deferred: discount, shipping, tax, saved cart, partial checkout, wishlist, and abandoned-cart marketing.
- Deferred: persistent line validation state until a concrete business workflow needs it.
- Rejected: browser-supplied customer ID or line price.
- Rejected: extending legacy `AppDbContext` or legacy presentation cart flows.

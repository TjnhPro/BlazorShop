# BlazorShop Cart Core Todo

Generated: 2026-07-16

Source plan:

- `docs/refactor-control-Commerce-storefront/Cart Core.md`

Scope:

- Store-scoped server-side cart.
- Guest cart with opaque cart token.
- Authenticated customer cart attachment and merge after login.
- Cart create/resume/update/expiration behavior.
- Server-owned customer identity; browser requests must not choose arbitrary customer or app user ids.
- Cart line product, variant/combination, quantity, selected attributes, personalization payload, artwork, fulfillment provider, and server price snapshot.
- Core cart commands: get current cart, add item, update quantity, remove item, clear cart, validate cart, recalculate cart.
- Server-side add-to-cart validation for product visibility, publication, purchasability, variant/attribute selection, quantity, and managed stock.
- Basic cart totals: item subtotal, order subtotal, order total, currency, rounding, warning/adjustment breakdown.
- Storefront cart projection for line display, selected attributes, images, prices, quantity constraints, availability warnings, checkout eligibility, and badge summary.

Explicitly out of scope:

- Full discount engine.
- Full tax engine.
- Full shipping estimator engine.
- Partial checkout or active/inactive line selection.
- Wishlist, saved carts, multi-cart per customer, quote cart, and abandoned-cart marketing automation.
- Customer-role pricing, store-specific price hooks, or permission pricing beyond existing server-side price calculation.
- Signed JWT cart tokens.
- Browser-supplied customer id, app user id, order status, line price, discount, tax, or server-owned fields.
- Replacing checkout or redesigning payment/order flow.
- Extending legacy `AppDbContext` or legacy presentation projects.

Boundary checklist:

- [x] Keep cart runtime data in `CommerceNodeDbContext`. 2026-07-16 Phase 0: `CommerceNodeDbContext` owns `CartSessions` and `CartLines`; no Phase 0 runtime schema change.
- [x] Keep Storefront cart routes under `api/storefront/stores/{storeKey}/cart/*`. 2026-07-16 Phase 0: controller inventory test asserts scoped route.
- [x] Keep Storefront V2 browser calls behind local `/api/cart` endpoints. 2026-07-16 Phase 0: `Storefront V2` local endpoint inventory confirmed add/update/remove/clear route handlers.
- [x] Keep store scope from route/store resolver and current store context. 2026-07-16 Phase 0: scoped controller resolves store id before cart service calls.
- [x] Keep browser payloads free of customer id, app user id, store id, price, discount, tax, and order-owned fields. 2026-07-16 Phase 0: `CartCorePhase0InventoryTests` guards public request contracts and local browser payload mapping.
- [x] Keep opaque cart token persistence hashed; never persist plaintext tokens. 2026-07-16 Phase 0: `StorefrontCartSessionServiceTests.CreateAsync_ReturnsOpaqueToken_AndStoresOnlyHash` guards this.
- [x] Keep Control Plane out of the cart runtime path unless QA docs need boundary evidence. 2026-07-16 Phase 0: no Control Plane cart runtime changes.
- [x] Do not add or extend `api/internal/*`, legacy `api/public/*`, legacy `api/admin/*`, or legacy controller routes. 2026-07-16 Phase 0: only active V2 docs/tests changed.
- [x] Do not add V2 cart persistence to `AppDbContext`. 2026-07-16 Phase 0: no legacy/default context changes.
- [x] Every new or changed active V2 API must satisfy `docs/architecture/09-api-contract-standards.md`. 2026-07-16 Phase 0: no API contract changes; existing cart OpenAPI tests remain the guard.

Current code facts to verify in Phase 0:

- [x] `CartSession` stores public id, store id, token hash, customer/app user identity, state, version, activity/expiry, order conversion, merge target, and timestamps. 2026-07-16 Phase 0 source review.
- [x] `CartLine` stores product, variant, line key, selected attributes JSON, personalization, artwork, fulfillment provider, quantity, price/currency/exchange-rate snapshots, and timestamps. 2026-07-16 Phase 0 source review.
- [x] `CommerceNodeDbContext` owns `cart_sessions` and `cart_lines`. 2026-07-16 Phase 0 source review.
- [x] Existing cart indexes cover public id, token hash, store/state, customer, app user, expiry, product, variant, artwork, and `(CartSessionId, LineKey)`. 2026-07-16 Phase 0 source review.
- [x] `StorefrontCartService` supports create/resume, get, add line, update line, remove line, clear, and validate. 2026-07-16 Phase 0 source review.
- [x] Add-line recalculates price server-side and stores unit price/currency snapshots. 2026-07-16 Phase 0: existing `StorefrontCartServiceTests` cover server snapshot behavior.
- [x] Product validation checks published/storefront availability, store visibility, category state, variant resolution, selected custom attributes, quantity lower bound, and stock. 2026-07-16 Phase 0: existing cart/selection tests cover these flows.
- [x] `StorefrontCartSessionService` creates secure random tokens, stores only SHA256 hashes, loads active sessions by store and token, rejects expired carts, merges line keys, and increments cart version. 2026-07-16 Phase 0: existing session tests cover token/hash/store/expiry/merge/version behavior.
- [x] Commerce Node exposes cart routes under `api/storefront/stores/{storeKey}/cart`. 2026-07-16 Phase 0: route inventory test added.
- [x] Storefront V2 local `/api/cart` uses `StorefrontCartTokenService` and does not expose Commerce Node internals to the browser. 2026-07-16 Phase 0: inventory test added.
- [x] Storefront V2 stores `bs-cart-token` as HttpOnly, SameSite Lax, secure outside development, and imports the legacy readable `my-cart` cookie. 2026-07-16 Phase 0: inventory test plus existing host coverage.
- [x] Checkout validates cart version and cart validity before order placement. 2026-07-16 Phase 0: existing checkout regression tests remain in focused gate.
- [x] Contract tests cover cart operation ids, `X-Cart-Token`, request body requirements, validation metadata, schema snapshots, and anonymous cart commands. 2026-07-16 Phase 0: existing `CommerceNodeStorefrontOpenApiContractTests`.

## Phase 0 - Baseline Guardrails

Goal: protect existing cart, checkout, Storefront V2, and OpenAPI behavior before changing cart contracts.

Implementation checklist:

- [x] Re-read active V2 cart files before implementation:
  - [x] `BlazorShop.Application/CommerceNode/Carts/StorefrontCartDtos.cs`.
  - [x] `BlazorShop.Application/CommerceNode/Carts/StorefrontCartService.cs`.
  - [x] `BlazorShop.Application/CommerceNode/Carts/IStorefrontCartService.cs`.
  - [x] `BlazorShop.Application/CommerceNode/Carts/IStorefrontCartSessionService.cs`.
  - [x] Commerce Node cart session service implementation.
  - [x] Commerce Node Storefront cart controller/contracts.
  - [x] Storefront V2 local `/api/cart` route handlers.
  - [x] `StorefrontCartTokenService`.
  - [x] `CartPage.razor`.
  - [x] `storefrontCommerce.js` cart integration.
  - [x] Storefront checkout service and checkout page.
- [x] Re-read current cart tests, checkout tests, Storefront V2 host smoke tests, and Commerce Node OpenAPI contract tests.
- [x] Confirm active cart paths use `CommerceNodeDbContext`, not `AppDbContext`.
- [x] Confirm Storefront V2 local `/api/cart` hides Commerce Node internals from browser requests.
- [x] Confirm no public request DTO currently trusts browser-supplied customer identity for new cart behavior.
- [x] Record current cart response schema snapshot before adding projection fields. 2026-07-16 Phase 0: existing Storefront Swagger snapshot remains unchanged and guarded.
- [x] Add missing baseline tests only where an obvious current behavior has no guard. 2026-07-16 Phase 0: added public cart contract/browser-payload guardrails.
- [x] Add QA checklist seeds to:
  - [x] `QA-CommerceNode.todo.md`.
  - [x] `QA-StorefrontV2.todo.md`.
  - [x] `QA-ControlPlane.todo.md` only for boundary evidence.
- [x] Make no schema, route, or behavior change in this phase unless needed to close a baseline test gap.

Verification checklist:

- [x] Existing cart application/session tests pass. 2026-07-16 Phase 0: `StorefrontCartSessionServiceTests|StorefrontCartServiceTests` passed 25/25.
- [x] Existing checkout regression tests pass. 2026-07-16 Phase 0: `StorefrontCheckoutServiceTests` passed inside focused 43/43 run.
- [x] Existing Storefront V2 cart host/static tests pass. 2026-07-16 Phase 0: `StorefrontV2HostSmokeTests|SecurityPrivacyPhase0InventoryTests` passed 42/42.
- [x] Existing Storefront OpenAPI contract tests pass. 2026-07-16 Phase 0: `CommerceNodeStorefrontOpenApiContractTests` passed inside focused 43/43 run.
- [x] No legacy cart path is extended. 2026-07-16 Phase 0: docs/tests only.
- [x] No `api/internal/*` route is added. 2026-07-16 Phase 0: docs/tests only.

Exit criteria:

- [x] Existing cart behavior is protected by focused tests or QA evidence. 2026-07-16 Phase 0: focused cart/session, checkout, OpenAPI, Storefront host/static, and new contract-inventory guardrails passed.
- [x] Known coverage gaps are written down before implementation phases.
- [x] No public request DTO accepts customer/app user identity for new behavior. 2026-07-16 Phase 0: `CartCorePhase0InventoryTests` guards public cart request DTOs.

Suggested commit:

```text
docs: plan cart core hardening
```

## Phase 1 - Cart Projection And Basic Totals

Goal: give Storefront V2 one stable cart shape for cart page, mini-cart badge, warnings, and checkout readiness without product N+1 reads.

Implementation checklist:

- [x] Add or extend application-level cart projection DTOs additively. 2026-07-16 Phase 1: `StorefrontCartSessionDto` and `StorefrontCartLineDto` gained additive projection fields.
- [x] Keep existing response fields/routes compatible. 2026-07-16 Phase 1: no route changes; old fields remain in cart responses.
- [x] Compute per-line projection:
  - [x] display name.
  - [x] product slug.
  - [x] product URL.
  - [x] image URL.
  - [x] selected attribute labels.
  - [x] unit price.
  - [x] line subtotal.
  - [x] line total.
  - [x] quantity minimum.
  - [x] quantity maximum.
  - [x] quantity step.
  - [x] allowed quantities only if already available; otherwise leave null/empty.
  - [x] purchasable flag.
  - [x] line warnings with stable codes.
- [x] Compute cart-level projection:
  - [x] currency code.
  - [x] summary count.
  - [x] subtotal.
  - [x] discount total placeholder.
  - [x] shipping estimate placeholder.
  - [x] tax estimate placeholder.
  - [x] grand total.
  - [x] checkout allowed flag.
  - [x] cart warnings with stable codes.
  - [x] adjustment breakdown with zero/default placeholders where engines do not exist.
- [x] Keep rounding aligned with existing currency/pricing services. 2026-07-16 Phase 1: projection uses `IMoneyRoundingService`.
- [x] Keep projection product/media/identity fields Storefront-safe. 2026-07-16 Phase 1: public response exposes display/product URL/image/warnings only; no admin/domain entity output.
- [x] Update Commerce Node Storefront API response DTOs additively.
- [x] Update Storefront API client models additively.
- [x] Update OpenAPI metadata/snapshots.

Verification checklist:

- [x] `GET cart` returns projection data without requiring Storefront V2 per-line product detail fetches. 2026-07-16 Phase 1: Commerce Node cart response now includes server projection fields; Storefront UI consumption remains Phase 5.
- [x] Existing clients still deserialize old fields. 2026-07-16 Phase 1: existing Storefront host/client tests passed with fallback behavior.
- [x] Product identity and media fields are public-safe. 2026-07-16 Phase 1: projection is limited to display name, slug/path, image URL, selected attributes, quantity metadata, totals, and warning codes.
- [x] No admin-only DTOs or domain entities appear in public schemas. 2026-07-16 Phase 1: Storefront OpenAPI contract tests passed.
- [x] Storefront OpenAPI contract tests pass. 2026-07-16 Phase 1: focused OpenAPI/cart run passed 47/47 after snapshot refresh.
- [x] Storefront API client tests pass. 2026-07-16 Phase 1: `StorefrontV2ApiClientTests|StorefrontV2HostSmokeTests|CartCorePhase0InventoryTests` passed 54/54.

Exit criteria:

- [x] Storefront has one stable cart projection shape.
- [x] Basic totals and checkout eligibility are explicit in the response.
- [x] Projection remains additive and generator-safe.

Suggested commit:

```text
feat(storefront-api): add cart projection totals
```

## Phase 2 - Recalculate Command

Goal: keep `validate` non-mutating and add an explicit command that can refresh stale cart snapshots.

Implementation checklist:

- [x] Add request DTO for cart recalculation. 2026-07-16 Phase 2: added application/API/Storefront V2 typed recalculation request DTOs.
- [x] Add response DTO or reuse enriched cart projection response. 2026-07-16 Phase 2: recalculate reuses the enriched `StorefrontCartResponse`.
- [x] Add `POST /api/storefront/stores/{storeKey}/cart/recalculate`. 2026-07-16 Phase 2: Commerce Node Storefront scoped cart controller exposes the POST command.
- [x] Require `X-Cart-Token`. 2026-07-16 Phase 2: controller and Storefront V2 client keep the cart token header; OpenAPI/client tests guard it.
- [x] Accept optional expected cart version. 2026-07-16 Phase 2: `ExpectedVersion` is optional with `minimum: 1` OpenAPI validation metadata.
- [x] Return `409 Conflict` for stale expected version. 2026-07-16 Phase 2: `StorefrontCartServiceTests.RecalculateAsync_WhenExpectedVersionIsStale_ReturnsConflict` passed.
- [x] Resolve every line against current:
  - [x] product visibility and publication. 2026-07-16 Phase 2: recalculation resolves lines through `ProductSelectionResolver`.
  - [x] variant/attribute validity. 2026-07-16 Phase 2: selection resolver is reused for current line variant/attribute state.
  - [x] availability/sellability. 2026-07-16 Phase 2: selection resolver sellability path is reused.
  - [x] quantity constraints. 2026-07-16 Phase 2: line quantity is passed back through selection resolver.
  - [x] managed stock. 2026-07-16 Phase 2: selection resolver stock checks remain the source of truth.
  - [x] price. 2026-07-16 Phase 2: stale product price snapshot refresh is covered by `StorefrontCartServiceTests`.
  - [x] currency. 2026-07-16 Phase 2: snapshot currency fields are updated through `StorefrontCartLineSnapshotUpdate`.
  - [x] rounding rules. 2026-07-16 Phase 2: recalculated selections and enriched projection use existing money rounding services.
- [x] Update persisted snapshots only when values changed:
  - [x] unit price snapshot.
  - [x] currency snapshot.
  - [x] base currency snapshot.
  - [x] exchange-rate snapshot.
- [x] Increment cart version only when persisted line/cart data changes. 2026-07-16 Phase 2: `StorefrontCartSessionServiceTests.UpdateLineSnapshotsAsync_IncrementsVersionOnlyWhenSnapshotsChange` passed.
- [x] Keep `ValidateAsync` non-mutating. 2026-07-16 Phase 2: `StorefrontCartServiceTests.ValidateAsync_DoesNotMutateCartSnapshots` passed.
- [x] Return stable warning codes for invalid products, unavailable products, invalid variant, invalid quantity, insufficient stock, mixed/unavailable currency, and missing price. 2026-07-16 Phase 2: recalculate leaves invalid lines for existing enriched warning-code projection instead of silently mutating them.
- [x] Update OpenAPI operation metadata:
  - [x] stable `operationId`.
  - [x] short summary.
  - [x] required request body metadata.
  - [x] typed success/error schemas.
  - [x] `X-Cart-Token` metadata.

Verification checklist:

- [x] Recalculate uses POST and is not exposed as GET. 2026-07-16 Phase 2: OpenAPI contract test asserts POST `/cart/recalculate`.
- [x] Stale expected cart version returns 409. 2026-07-16 Phase 2: focused cart service test passed.
- [x] Recalculate refreshes stale snapshots after product/price/currency changes. 2026-07-16 Phase 2: focused cart service/session tests passed for price/currency snapshot updates.
- [x] Recalculate increments version only when persisted values change. 2026-07-16 Phase 2: session service test passed.
- [x] Validate remains non-mutating. 2026-07-16 Phase 2: application service test passed.
- [x] Checkout can rely on recalculated/validated cart state before order placement. 2026-07-16 Phase 2: command/validate semantics are separated and existing checkout validation contract remains unchanged.
- [x] Storefront OpenAPI contract tests pass. 2026-07-16 Phase 2: focused cart/session/OpenAPI/client run passed 69/69 after snapshot refresh.

Exit criteria:

- [x] Cart validation and recalculation have separate semantics. 2026-07-16 Phase 2: `ValidateAsync` remains read-only and `RecalculateAsync` owns snapshot mutation.
- [x] Stale cart data can be refreshed explicitly. 2026-07-16 Phase 2: new POST command refreshes server snapshots.
- [x] The command is safe for generated clients and AI agents. 2026-07-16 Phase 2: OpenAPI operation metadata, request schema, response schema, error responses, and snapshot were refreshed.

Suggested commit:

```text
feat(storefront-api): add cart recalculation command
```

## Phase 3 - Authenticated Cart Attach And Merge

Goal: make guest-to-customer cart behavior safe without trusting browser identity.

Implementation checklist:

- [ ] Remove or stop honoring browser-supplied customer identity from public create/resume and merge flows.
- [ ] Add authenticated application method for current customer cart attach/merge.
- [ ] Add authenticated Storefront API endpoint:
  - [ ] `POST /api/storefront/stores/{storeKey}/cart/merge-current-customer`.
- [ ] Require `X-Cart-Token`.
- [ ] Require authenticated customer context.
- [ ] Derive `CustomerId`/`AppUserId` from trusted auth context or server-side auth result.
- [ ] Reject request body identity fields.
- [ ] If customer has no active cart, attach guest cart to the customer.
- [ ] If customer has an active cart, merge guest lines with existing line-key behavior.
- [ ] Preserve line-key differences for variant, selected attributes, personalization, artwork, and fulfillment provider.
- [ ] Preserve or trigger price snapshot recalculation rules during merge.
- [ ] Mark old cart state as `merged` and set `MergedIntoCartId` when a merge occurs.
- [ ] Ensure merged, ordered, and expired carts cannot be loaded as active carts.
- [ ] Add Storefront V2 login-flow hook to merge current guest cart after successful login when a cart token exists.
- [ ] Update OpenAPI security metadata for the merge endpoint.

Verification checklist:

- [ ] Guest cart survives login.
- [ ] Existing customer cart and guest cart quantities merge deterministically.
- [ ] Different personalization/artwork/attribute line keys remain separate.
- [ ] Browser cannot attach a cart to another customer by sending an id.
- [ ] Merge endpoint declares auth security metadata.
- [ ] Merged carts are no longer loadable as active carts.
- [ ] Storefront login flow tests pass.

Exit criteria:

- [ ] Customer identity comes only from trusted server/auth context.
- [ ] Guest-to-customer cart handoff is deterministic and tested.
- [ ] Public cart request schemas expose no customer/app user identity fields.

Suggested commit:

```text
feat(storefront-api): merge guest cart for current customer
```

## Phase 4 - Quantity Constraints And Item Limits

Goal: enforce practical cart limits and prepare the cart contract for availability/quantity rules.

Implementation checklist:

- [ ] Centralize cart quantity validation for:
  - [ ] minimum quantity.
  - [ ] maximum quantity.
  - [ ] quantity step.
  - [ ] allowed quantities list when available.
  - [ ] managed stock ceiling.
- [ ] Use existing sellability/selection services where available.
- [ ] Add application options for conservative cart limits:
  - [ ] max lines per cart.
  - [ ] max quantity per line where product max is absent.
  - [ ] max personalization payload size.
  - [ ] max selected attributes payload size if needed.
- [ ] Enforce item limit before creating database/checkout risk.
- [ ] Enforce personalization/artwork payload limits consistently.
- [ ] Return quantity constraints in line projection.
- [ ] Return stable warning/error codes; UI must not parse message text.
- [ ] Keep initial defaults simple where catalog fields do not exist:
  - [ ] minimum quantity `1`.
  - [ ] step `1`.
  - [ ] maximum from managed stock when applicable.
  - [ ] otherwise configured cart option limit.
- [ ] Publish validation metadata in OpenAPI:
  - [ ] `minimum: 1` for quantity.
  - [ ] payload max lengths where applicable.
  - [ ] request body required.

Verification checklist:

- [ ] Add-line rejects quantity below minimum.
- [ ] Update-line rejects quantity below minimum.
- [ ] Add/update reject quantity above max.
- [ ] Add/update reject invalid step.
- [ ] Add/update reject managed stock shortage.
- [ ] Large cart line count is rejected before write.
- [ ] Oversized personalization payload is rejected before write.
- [ ] Projection includes enough data to render quantity controls correctly.
- [ ] OpenAPI validation metadata is present.

Exit criteria:

- [ ] Cart quantity rules are enforced consistently.
- [ ] Large carts and oversized payloads are bounded.
- [ ] Storefront can render quantity controls from projection data.

Suggested commit:

```text
feat(commerce-node): enforce cart quantity limits
```

## Phase 5 - Storefront V2 Cart UI Consumption

Goal: move Storefront V2 from product-by-product lookup to the cart projection contract.

Implementation checklist:

- [ ] Update Storefront V2 local `/api/cart` endpoints to return the enriched projection.
- [ ] Update local cart response/client models.
- [ ] Update `CartPage.razor` to render projection fields directly:
  - [ ] image.
  - [ ] product URL.
  - [ ] display name.
  - [ ] selected attributes.
  - [ ] unit price.
  - [ ] line subtotal/total.
  - [ ] quantity constraints.
  - [ ] line warnings.
  - [ ] cart warnings.
  - [ ] subtotal/grand total.
  - [ ] checkout allowed state.
- [ ] Remove product detail N+1 dependency from cart page rendering.
- [ ] Update mini-cart badge to use `summaryCount`.
- [ ] Keep add/update/remove/clear flows through Storefront V2 local endpoints.
- [ ] Disable checkout button when `checkoutAllowed` is false.
- [ ] Show customer-safe warning text for invalid/unavailable cart lines.
- [ ] Keep cart page noindex/private behavior.
- [ ] Keep browser request payload free of price/customer identity.
- [ ] Keep text/layout stable on mobile and desktop.

Verification checklist:

- [ ] Storefront V2 build passes.
- [ ] Storefront API client/local cart tests pass.
- [ ] Cart page renders without product N+1 dependency.
- [ ] Cart badge count matches server projection.
- [ ] Invalid/unavailable cart lines show warnings.
- [ ] Invalid cart blocks checkout clearly.
- [ ] Existing add/update/remove/clear browser flows still work.
- [ ] Storefront host smoke tests pass.

Exit criteria:

- [ ] Cart page consumes the server projection as source of truth.
- [ ] Storefront cart UI remains noindex and customer-safe.
- [ ] Existing Storefront cart interactions still work.

Suggested commit:

```text
feat(storefront): consume cart projection
```

## Phase 6 - Expiration, Cleanup, QA, And Contract Finish

Goal: finish with operational behavior, contract safety, and regression coverage.

Implementation checklist:

- [ ] Formalize cart expiration policy in application options.
- [ ] Confirm expired carts cannot be used.
- [ ] Add cleanup job/task for expired carts if current task infrastructure is suitable.
- [ ] Keep cleanup simple:
  - [ ] expire active sessions older than `ExpiresAtUtc`.
  - [ ] do not hard delete by default.
  - [ ] do not remove active, merged, or ordered carts incorrectly.
- [ ] Update Commerce Node API contract tests:
  - [ ] stable operation ids.
  - [ ] summaries.
  - [ ] `X-Cart-Token` metadata.
  - [ ] request body required metadata.
  - [ ] quantity validation metadata.
  - [ ] auth metadata for merge endpoint.
  - [ ] typed response/error schemas.
  - [ ] no domain/admin schemas in public cart schemas.
  - [ ] recalculate uses POST.
  - [ ] snapshots refreshed.
- [ ] Update application/session tests:
  - [ ] create/resume guest cart.
  - [ ] token hash only.
  - [ ] store-scoped token loading.
  - [ ] expired cart marked expired.
  - [ ] merged/ordered carts are not active.
  - [ ] version increments on mutating commands.
  - [ ] add-line snapshot behavior.
  - [ ] same line merges quantity.
  - [ ] different variant/attributes/personalization/artwork keeps separate lines.
  - [ ] validate is non-mutating.
  - [ ] recalculate updates stale snapshots.
  - [ ] guest cart merges into current customer cart.
- [ ] Update checkout regression tests:
  - [ ] checkout rejects stale cart version.
  - [ ] checkout rejects invalid cart after product/variant availability changes.
  - [ ] checkout uses server-side snapshots/totals.
  - [ ] ordered cart cannot be loaded as active.
- [ ] Update Storefront V2 tests:
  - [ ] `/api/cart/lines` creates HttpOnly `bs-cart-token`.
  - [ ] browser request does not send price/customer identity.
  - [ ] cart page renders projection.
  - [ ] badge uses server summary count.
  - [ ] invalid cart blocks checkout.
  - [ ] login merge keeps guest cart lines.
  - [ ] legacy `my-cart` cookie import still works and deletes legacy cookie.
- [ ] Update QA docs:
  - [ ] `QA-CommerceNode.todo.md`.
  - [ ] `QA-StorefrontV2.todo.md`.
  - [ ] `QA-ControlPlane.todo.md` only if boundary evidence changed.
- [ ] Build active V2 projects touched by the phase.
- [ ] Run focused verification.
- [ ] Review diff for:
  - [ ] no legacy `BlazorShop.Presentation` feature changes.
  - [ ] no `AppDbContext` migration.
  - [ ] no new `api/internal/*`.
  - [ ] no discount/tax/shipping estimator engines.
  - [ ] no browser-owned customer identity or price fields.

Verification checklist:

- [ ] CommerceNode API build passes.
- [ ] Storefront V2 build passes.
- [ ] Storefront OpenAPI contract tests pass.
- [ ] Cart application/session tests pass.
- [ ] Checkout regression tests pass.
- [ ] Storefront V2 host/static/API client tests pass.
- [ ] Visible browser QA passes for changed cart flows when runtime is available, or is explicitly marked pending with reason.

Exit criteria:

- [ ] Expired carts cannot be used.
- [ ] Cleanup does not corrupt active/merged/ordered carts.
- [ ] OpenAPI remains generator-safe.
- [ ] Checkout/order tests still pass.
- [ ] QA checklist files contain evidence.

Suggested commit:

```text
test(cart-core): complete release gate
```

## QA Checklist Seeds

### Commerce Node

- [ ] Cart session token is stored hashed only.
- [ ] Cart token lookup is store-scoped.
- [ ] Expired cart is rejected or marked expired.
- [ ] Merged cart is not loadable as active.
- [ ] Ordered cart is not loadable as active.
- [ ] Guest cart create/resume works.
- [ ] Cart add-line snapshots server price and currency.
- [ ] Same line key merges quantity and increments version.
- [ ] Different variant/attributes/personalization/artwork keeps separate lines.
- [ ] Add-line rejects wrong-store product.
- [ ] Add-line rejects unpublished product.
- [ ] Add-line rejects archived product.
- [ ] Add-line rejects invalid variant.
- [ ] Add-line rejects invalid selected attributes.
- [ ] Add-line rejects invalid quantity.
- [ ] Add/update rejects managed stock shortage.
- [ ] Cart projection includes line display fields and totals.
- [x] Validate is non-mutating. 2026-07-16 Phase 2: application service test guards snapshot/version unchanged.
- [x] Recalculate updates stale snapshots. 2026-07-16 Phase 2: application/session tests guard stale price and snapshot currency updates.
- [x] Recalculate returns 409 for stale expected version. 2026-07-16 Phase 2: application service test guards stale expected version conflict.
- [ ] Customer cart merge derives identity from auth context only.
- [ ] Public request schemas do not include customer id, app user id, price, discount, tax, or order-owned fields.
- [ ] Storefront OpenAPI validates and snapshot passes.

### Storefront V2

- [ ] Local `/api/cart` uses HttpOnly `bs-cart-token`.
- [ ] Local `/api/cart` imports legacy `my-cart` cookie and deletes it after import.
- [ ] Browser add-line request does not send price/customer identity.
- [ ] Cart page renders projection without product N+1 calls.
- [ ] Cart page shows image, URL, selected attributes, unit price, line total, and warnings.
- [ ] Cart page shows subtotal/grand total from projection.
- [ ] Cart badge uses server summary count.
- [ ] Add/update/remove/clear flows still work.
- [ ] Invalid/unavailable cart line blocks checkout.
- [ ] Cart page remains noindex/private.
- [ ] Login merge keeps guest cart lines.
- [ ] Browser QA finds no unexpected console errors.

### Control Plane

- [ ] ControlPlane Web still calls only ControlPlane API.
- [ ] Control Plane does not participate in Storefront cart runtime.
- [ ] No ControlPlane Web direct call to CommerceNode cart API is introduced.

## Deferred Scope Checklist

- [ ] Full discount engine remains deferred.
- [ ] Full tax engine remains deferred.
- [ ] Full shipping estimator remains deferred.
- [ ] Partial checkout remains deferred.
- [ ] Wishlist/saved carts/multi-cart remains deferred.
- [ ] Quote cart remains deferred.
- [ ] Abandoned-cart marketing remains deferred.
- [ ] Customer-role pricing remains deferred.
- [ ] Store-specific price hooks remain deferred.
- [ ] Signed JWT cart tokens remain deferred.
- [ ] Hard delete cleanup remains deferred unless storage pressure requires it.

## Risk Register

- [ ] Unknown store token loads another store's cart.
- [ ] Browser sends customer id and attaches cart to another account.
- [ ] Cart token is persisted in plaintext.
- [x] Recalculate mutates state from a GET route. 2026-07-16 Phase 2: risk mitigated by POST-only `/cart/recalculate` OpenAPI contract assertion.
- [ ] Checkout uses stale price snapshots after product price changes.
- [ ] Storefront hides an unavailable item but still allows checkout.
- [ ] Product deleted/unpublished after add-to-cart causes null reference or checkout crash.
- [ ] Variant selection becomes invalid but checkout still places an order.
- [ ] Quantity update bypasses min/max/step or stock checks.
- [ ] Merge duplicates lines incorrectly or loses personalization/artwork differences.
- [ ] Legacy `my-cart` readable cookie remains after server cart import.
- [ ] Public API leaks provider secrets, internal ids, admin fields, or domain entities.
- [ ] OpenAPI schema silently breaks generated clients.

## Recommended Implementation Order

- [x] Phase 0 - baseline guardrails. 2026-07-16: guardrails added and focused verification passed.
- [x] Phase 1 - cart projection and basic totals. 2026-07-16: server projection, public response, Storefront client model, snapshot, and focused tests completed.
- [x] Phase 2 - recalculate command. 2026-07-16: POST command, OpenAPI metadata/snapshot, Storefront V2 typed client, and focused tests completed.
- [ ] Phase 3 - authenticated cart attach and merge.
- [ ] Phase 4 - quantity constraints and item limits.
- [ ] Phase 5 - Storefront V2 cart UI consumption.
- [ ] Phase 6 - expiration, cleanup, QA, and contract finish.

## Definition Of Done

- [ ] Storefront V2 can add, view, update, remove, clear, recalculate, and checkout cart lines through the server cart.
- [ ] Guest cart is store-scoped and token-secured.
- [ ] Authenticated login can attach or merge the current guest cart without trusting browser identity.
- [ ] Cart projection is rich enough for cart page, badge, warnings, and checkout eligibility.
- [ ] Server validates product, variant, quantity, price, currency, and availability before cart mutation and checkout.
- [ ] OpenAPI and tests protect the contract.
- [ ] No active V2 code depends on legacy cart storage.
- [ ] Deferred discount, tax, shipping, saved-cart, partial-checkout, wishlist, and marketing features remain unimplemented.

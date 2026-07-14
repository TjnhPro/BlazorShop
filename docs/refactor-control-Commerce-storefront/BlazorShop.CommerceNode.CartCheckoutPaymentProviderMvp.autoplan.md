# BlazorShop Commerce Node Cart, Checkout & Payment Provider MVP Autoplan

Status: planned  
Created: 2026-07-14  
Skill: `$autoplan` applied in local single-agent mode  
Scope: Commerce Node Storefront cart lifecycle, checkout hardening, customer resolution, POD cart metadata, payment attempt foundation, provider MVP, Storefront V2 migration, OpenAPI/QA hardening

## Autoplan Execution Notes

`$autoplan` requires CEO, Design, Eng, and DX review passes. In this Codex environment the external gstack Codex/Claude subagent wrappers are not available as callable tools, so the dual-voice portions are recorded as unavailable instead of fabricated. The plan still follows the required output shape: premise challenge, existing-code map, scope decisions, architecture diagram, failure registry, test diagram, DX checklist, decision audit trail, phase gates, and implementation boundaries.

## Plan Summary

The original cart/checkout/payment-provider idea is directionally compatible with BlazorShop V2, but it is too broad to implement as one or two phases. The current codebase has direct checkout from a browser-written `my-cart` cookie, no server-side cart session, no payment attempt ledger, no provider session model, no store-scoped customer profile separate from global Identity users, and payment handlers that only return immediate success/failure.

This plan breaks the work into stable implementation phases. Each phase must be independently buildable, testable, and committable. No phase should change legacy `BlazorShop.Presentation`, `AppDbContext`, or removed `api/internal/*` routes.

## Required Premises

| Premise | Evidence | Decision |
| --- | --- | --- |
| Storefront cart and checkout belong to Commerce Node Storefront APIs. | `AGENTS.md` and `docs/architecture/03-runtime-boundaries.md` define `api/storefront/stores/{storeKey}/*` as Storefront API ownership. | Keep all public cart/checkout/payment APIs under `api/storefront/stores/{storeKey}/*`. |
| V2 data belongs to `CommerceNodeDbContext`. | `AGENTS.md` and `docs/architecture/04-data-ownership.md`; current `CommerceNodeDbContext` already owns orders, payment methods, storefront auth, catalog, media, tasks. | Add cart/customer/payment tables to `CommerceNodeDbContext` only. |
| Current Storefront V2 cart is not server-authoritative. | `StorefrontCookieNames.Cart = "my-cart"` and `storefrontCommerce.js` write cart JSON from the browser. | Introduce opaque cart token and server-side cart sessions before provider checkout. |
| Checkout currently creates orders directly from request cart lines. | `CartService.CheckoutAsync(StorefrontCheckoutRequest, string?)` validates request, resolves customer, processes handler, creates order, deducts stock. | Split cart validation, checkout preview, order placement, and payment attempt flow. |
| `AppUser` is global Identity state, not store-scoped customer state. | `AppUser` has no `StoreId`; current lookup uses `FindByEmailAsync(email)`. | Do not add `StoreId` to `AppUser` in this phase. Add a store-scoped commerce customer profile. |
| Payment abstraction is currently too thin for online providers. | `PaymentHandlerContext` has `OrderId`, amount, metadata only; Stripe/PayPal handlers return not implemented or use unrelated legacy service. | Add payment attempt/session/provider event model before real provider MVP. |
| OpenAPI quality is now a product requirement. | `docs/architecture/09-api-contract-standards.md` and existing contract tests. | Every new endpoint gets operationId, summary, request/response DTOs, errors, auth, validation metadata, and snapshot tests. |

## Not In Scope

- No changes to `BlazorShop.Presentation/*` legacy projects.
- No `api/internal/*` revival.
- No `AppDbContext` migrations.
- No Control Plane Web direct calls to Commerce Node.
- No ABP-style module system.
- No broad rewrite of `CartService` before compatibility tests exist.
- No real refund/dispute/payout implementation in this MVP.
- No multi-provider marketplace routing. The first online provider must prove one provider path end to end only.
- No product designer/editor implementation for POD artwork upload unless the product/media asset model is already ready in the selected implementation phase.

## What Already Exists

| Area | Existing code | Use or change |
| --- | --- | --- |
| Storefront route boundary | `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs` | Extend with explicit cart/payment endpoints under existing scoped route style. |
| API contract DTO style | `Contracts/Storefront/StorefrontApiContracts.cs`, `StorefrontContractMappings.cs` | Add explicit request/response models here, not domain entities. |
| Checkout service | `BlazorShop.Application/Services/Payment/CartService.cs` | Keep current public methods initially; extract new cart/checkout services around it phase by phase. |
| Order persistence | `Order`, `OrderLine`, `IOrderRepository`, `CommerceNodeDbContext.Orders` | Reuse order entity and add only necessary fields such as `CustomerId` and payment attempt references if needed. |
| Store payment config | `PaymentMethod`, `StorePaymentMethod`, `PaymentMethodService`, `IStorePaymentMethodAdminService` | Reuse for enabled method lookup. Add provider settings schema and safe public projection later. |
| Payment handler skeleton | `IPaymentHandler`, `PaymentHandlerResolver`, `PaymentHandlers.cs` | Replace or extend into payment attempt/session-oriented contract. |
| Store scope | Store context and `{storeKey}` route pattern | Store remains route-derived. Do not use `X-Store-Key`. |
| Product lookup | `CommerceNodeProductReadRepository.GetPublishedProductDetailsByIdAsync`, `GetProductsByIdsAsync` | Add/route checkout cart resolution through published/store-safe lookup, not the current broad ID lookup. |
| Storefront V2 client | `StorefrontApiClient`, `Program.cs`, `CartPage.razor`, `CheckoutPage.razor`, `storefrontCommerce.js` | Migrate from JS JSON cart cookie to server cart token gradually. |
| Contract test harness | `CommerceNodeStorefrontOpenApiContractTests`, snapshot file | Expand for cart/payment endpoints and schema safety. |
| QA files | `QA-CommerceNode.todo.md`, `QA-StorefrontV2.todo.md` | Update per phase. |

## Current Risk Map

| Risk | Current cause | User impact | Plan response |
| --- | --- | --- | --- |
| Cart tampering | Browser stores product id, variant id, quantity, unit price in readable cookie. | Price/line data can be manipulated before checkout unless revalidated perfectly. | Server-side cart session; ignore client unit price; reprice on server. |
| Published-product bypass | Checkout uses `GetProductsByIdsAsync`, which does not enforce published/category storefront constraints. | Unpublished or detached products can be ordered if ID is known. | Add published/storefront checkout product resolver. |
| Store customer collision | Guest checkout reuses global Identity email. | Same email across stores can attach to the wrong customer profile. | Add `CommerceCustomer` unique by `(StoreId, NormalizedEmail)`. |
| Payment/order ordering problem | Handler runs before order exists and receives `Guid.Empty` as `OrderId`. | Online provider metadata cannot reliably reference order/payment attempt. | Create checkout session/payment attempt before provider session. |
| No idempotency | Checkout POST can be retried and create duplicate orders. | Customer can pay/order twice on refresh/network retry. | Add idempotency key and cart version checks. |
| Stock/POD mismatch | `DeductStockAsync` always decrements stock. | POD/non-stock products can fail or decrement meaningless inventory. | Add stock policy and fulfillment snapshot before order placement. |
| OpenAPI drift | New endpoints often fail metadata unless tests lock them. | Storefront/AI/client generation guesses models. | Contract tests first for every new endpoint group. |

## Target Architecture

```text
Browser
  -> BlazorShop.Storefront.V2
      - owns browser HttpOnly cart token cookie
      - renders cart/checkout pages
      - calls StorefrontApiClient
  -> BlazorShop.CommerceNode.API
      api/storefront/stores/{storeKey}/cart/*
      api/storefront/stores/{storeKey}/checkout/*
      api/storefront/stores/{storeKey}/payments/*
        -> Application services
            CartSessionService
            CheckoutSessionService
            StorefrontCustomerService
            PaymentAttemptService
            PaymentProviderService
        -> Infrastructure repositories
            CommerceNodeDbContext
              commerce_customers
              cart_sessions
              cart_lines
              checkout_sessions
              payment_attempts
              payment_provider_events
              orders
              order_lines
```

### Data Model Target

```text
CommerceStore 1 -- * CommerceCustomer
CommerceStore 1 -- * CartSession 1 -- * CartLine
CommerceCustomer 1 -- * CartSession
CartSession 1 -- * CheckoutSession
CheckoutSession 1 -- * PaymentAttempt
PaymentAttempt 1 -- * PaymentProviderEvent
CheckoutSession 1 -- 0..1 Order
Order 1 -- * OrderLine
```

## Public API Target

All operations follow `docs/architecture/09-api-contract-standards.md`.

| Route | Method | Auth | Purpose |
| --- | --- | --- | --- |
| `/api/storefront/stores/{storeKey}/cart/session` | POST | Anonymous/customer | Create or resume server cart and issue/refresh cart token. |
| `/api/storefront/stores/{storeKey}/cart` | GET | Anonymous/customer cart token | Read normalized cart summary. |
| `/api/storefront/stores/{storeKey}/cart/lines` | POST | Anonymous/customer cart token | Add line with product/variant/options/personalization. |
| `/api/storefront/stores/{storeKey}/cart/lines/{lineId}` | PUT | Anonymous/customer cart token | Update quantity/options where allowed. |
| `/api/storefront/stores/{storeKey}/cart/lines/{lineId}` | DELETE | Anonymous/customer cart token | Remove line. |
| `/api/storefront/stores/{storeKey}/cart/validate` | POST | Anonymous/customer cart token | Reprice and validate cart before checkout UI submit. |
| `/api/storefront/stores/{storeKey}/checkout/preview` | POST | Anonymous/customer cart token | Validate customer/shipping/payment method and return totals/errors. |
| `/api/storefront/stores/{storeKey}/checkout/place-order` | POST | Anonymous/customer cart token | Idempotently create order or payment attempt. |
| `/api/storefront/stores/{storeKey}/payments/methods` | GET | Anonymous | Existing method lookup, extended with provider capability metadata. |
| `/api/storefront/stores/{storeKey}/payments/attempts/{attemptId}` | GET | Anonymous/customer via cart/checkout token | Poll attempt state after redirect/webhook. |
| `/api/storefront/stores/{storeKey}/payments/provider-callback/{providerKey}` | POST | Provider/customer callback | Capture/confirm provider callback without side-effecting GET. |
| `/api/storefront/stores/{storeKey}/payments/webhooks/{providerKey}` | POST | Provider signature | Receive async provider event. |

If provider callback must accept a provider redirect `GET`, the `GET` endpoint must only render or redirect after reading already-recorded state. It must not capture money or mutate payment state.

## Phase Plan

### Phase 0 - Baseline, Contract Inventory, and Red Tests

Goal: lock the current behavior and expose gaps before schema changes.

Files to inspect/update:

- `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- `BlazorShop.Tests/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests/Application/Services/Payment/CartServiceTests.cs`
- `BlazorShop.Tests/PresentationV2/Storefront/*`

Tasks:

- Add failing or pending tests documenting that checkout must not order unpublished products.
- Add tests documenting duplicate checkout/idempotency gap.
- Add tests documenting that Storefront public schema cannot expose server-owned fields.
- Add QA checklist section for server cart, checkout session, payment attempts, provider callbacks, and visible browser flows.
- Capture current OpenAPI snapshot before new endpoints are added.

Done when:

- Tests compile and existing suites still pass or new tests are marked intentionally pending through a clear skip reason.
- QA files contain new checklist rows with phase tags.
- No runtime behavior is changed.

Commit boundary: `test: document cart checkout payment gaps`

### Phase 1 - Store-Scoped Customer Profile Foundation

Goal: stop using global Identity email lookup as the commerce customer identity.

Code design:

- Add domain entity `CommerceCustomer` under Commerce Node/payment/customer area.
- Add `DbSet<CommerceCustomer>` and EF mapping in `CommerceNodeDbContext`.
- Columns:
  - `Id`, `StoreId`, `AppUserId` nullable, `Email`, `NormalizedEmail`, `FullName`, `Phone`
  - `CreatedAt`, `UpdatedAt`, `LastCheckoutAt`
  - soft-delete/archive only if existing local pattern requires it
- Indexes:
  - unique `(StoreId, NormalizedEmail)`
  - index `(AppUserId)` filtered when not null
- Add `IStorefrontCustomerService`:
  - resolve authenticated user to store customer
  - resolve guest by `(StoreId, NormalizedEmail)`
  - create guest customer profile without creating `AppUser`
  - handle duplicate insert race by reloading existing row

Compatibility:

- Keep `Order.UserId` for existing authenticated order history.
- Add nullable `Order.CustomerId` only after customer table exists.
- Current guest checkout can keep creating AppUser until checkout service is migrated, but new code must use `CommerceCustomer`.

Tests:

- Customer resolve creates one row per store/email.
- Same email in two stores creates two `CommerceCustomer` rows.
- Authenticated user links `AppUserId` without changing Identity uniqueness.
- Duplicate create race returns existing customer.

Done when:

- Migration is added for Commerce Node only.
- Unit/service tests cover lookup behavior.
- No Storefront UI behavior changes yet.

Commit boundary: `feat: add store scoped commerce customers`

### Phase 2 - Cart Session Schema and Repository Layer

Goal: introduce server-side cart persistence without changing Storefront V2 yet.

Entities:

- `CartSession`
  - `Id`, `PublicId`, `StoreId`, `TokenHash`, `CustomerId`, `AppUserId`
  - `State`: `active`, `merged`, `ordered`, `expired`, `abandoned`
  - `Version`, `LastActivityAtUtc`, `ExpiresAtUtc`, `ConvertedOrderId`, `MergedIntoCartId`
  - `CreatedAtUtc`, `UpdatedAtUtc`
- `CartLine`
  - `Id`, `CartSessionId`, `ProductId`, `ProductVariantId`
  - `SelectedAttributesJson`
  - `PersonalizationHash`, `PersonalizationJson`
  - `ArtworkAssetId`, `ArtworkVersion`
  - `FulfillmentProviderKey`
  - `Quantity`
  - `UnitPriceSnapshot`, `CurrencyCodeSnapshot`
  - `CreatedAtUtc`, `UpdatedAtUtc`

Rules:

- Token stored as hash in DB, never plaintext.
- Store scope must always come from `{storeKey}` resolved to current store, not token.
- Cart version increments on every line mutation.
- `CartLine` uniqueness uses cart + product + variant + selected attribute signature + personalization hash + provider key.

Tests:

- EF mapping test or migration smoke.
- Repository can create session, add/update/remove line, increment version.
- Token lookup refuses wrong store.
- Expired carts cannot be mutated.

Done when:

- Data model exists and repository/service primitives are tested.
- No Storefront V2 browser behavior changes yet.

Commit boundary: `feat: add server cart session persistence`

### Phase 3 - Cart Application Service and Product Validation

Goal: make server cart authoritative and storefront-safe.

Service:

- Add `IStorefrontCartService` with:
  - `CreateOrResumeAsync`
  - `GetAsync`
  - `AddLineAsync`
  - `UpdateLineAsync`
  - `RemoveLineAsync`
  - `ClearAsync`
  - `ValidateAsync`
- Add a product resolver for checkout/cart that enforces:
  - current store
  - not archived
  - published storefront visibility
  - category/storefront availability if applicable
  - variant belongs to product
  - quantity >= 1
  - selected attributes allowed and normalized
- Reuse current selected-attribute validation logic from `CartService` by extracting a small helper only if duplication would otherwise grow.

POD rules:

- Cart lines may carry personalization metadata, but checkout must validate JSON size, known fields, and provider key.
- No client-supplied price is trusted.
- Stock policy is explicit:
  - stock-tracked product: require stock at add/validate/place-order
  - POD/non-stock product: skip stock deduction but require provider/product configuration

Tests:

- Add published product succeeds.
- Unpublished, archived, wrong-store products fail.
- Invalid variant fails.
- Quantity below 1 fails and OpenAPI advertises `minimum: 1`.
- Distinct personalization hash creates distinct cart lines.
- Same normalized line increments quantity.

Done when:

- Application service can run independently from existing cookie checkout.
- Current `CartService.CheckoutAsync` tests still pass.

Commit boundary: `feat: add storefront cart service validation`

### Phase 4 - Storefront Cart API Contract

Goal: expose server cart through generator-safe Storefront APIs.

Files:

- `StorefrontScopedControllers.cs`
- `Contracts/Storefront/StorefrontApiContracts.cs`
- `StorefrontContractMappings.cs`
- Swagger operation filter/annotations used by existing contract tests

Contracts:

- Request DTOs:
  - `StorefrontCreateCartSessionRequest`
  - `StorefrontCartLineCreateRequest`
  - `StorefrontCartLineUpdateRequest`
  - `StorefrontCartValidateRequest`
- Response DTOs:
  - `StorefrontCartSessionResponse`
  - `StorefrontCartResponse`
  - `StorefrontCartLineResponse`
  - `StorefrontCartValidationResponse`
  - `StorefrontCartProblemResponse` if standard error envelope needs extra detail

Auth/token:

- Anonymous cart is allowed.
- Cart token is accepted via cookie/header from Storefront V2 server-to-server call.
- Token identifies cart session only; store scope still comes from route.
- Protected customer cart merge must declare Bearer security when endpoint requires customer identity.

OpenAPI requirements:

- Stable operation IDs, e.g. `StorefrontCart_CreateSession`, `StorefrontCart_Get`, `StorefrontCart_AddLine`.
- Required request body metadata on POST/PUT.
- Standard error responses for 400, 401 where applicable, 404, 409, 422, 500.
- No domain entities in public schemas.
- No anonymous object responses.

Tests:

- Contract tests for every new operation.
- Snapshot update.
- Generated TypeScript/C# client smoke.
- Side-effecting endpoints are not GET.

Done when:

- New APIs work through tests even before Storefront V2 uses them.
- Existing `/cart/checkout` remains compatible.

Commit boundary: `feat: expose storefront cart api contract`

### Phase 5 - Storefront V2 Cart Token Migration

Goal: migrate the visible Storefront cart from JS JSON cookie to server cart token.

Files:

- `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/StorefrontCookieNames.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
- `CartPage.razor`, `CartPage.razor.cs`
- `CheckoutPage.razor`, `CheckoutPage.razor.cs`
- `Program.cs` checkout endpoints

Migration strategy:

- Add new cookie constant, for example `StorefrontCookieNames.CartToken`.
- Keep old `my-cart` reader for one migration phase.
- On first cart page/add-to-cart:
  - if old cookie exists and no token exists, import old lines through cart API
  - write HttpOnly cart token cookie
  - delete old `my-cart` cookie after successful import
- Remove client-side unit price from future cart writes.
- JS add-to-cart should call a Storefront V2 local endpoint that calls Commerce Node, not write authoritative cart data itself.

Cookie requirements:

- HttpOnly.
- SameSite Lax.
- Secure in non-development.
- Path `/`.
- Expiry aligned with `CartSession.ExpiresAtUtc`.

Tests:

- Host smoke for `/my-cart` with empty cart.
- Migration test from old cookie to cart token.
- Add/remove/update quantity browser flow.
- No readable `my-cart` price payload after migration success.
- Visible browser QA with `headless=false` when requested before release.

Done when:

- Storefront V2 uses server cart for cart page and local add/update/remove flows.
- Old cookie fallback remains temporarily for migration only.

Commit boundary: `feat: migrate storefront v2 to server cart token`

### Phase 6 - Checkout Session and Preview

Goal: introduce checkout as a validated session before order creation.

Entity:

- `CheckoutSession`
  - `Id`, `PublicId`, `StoreId`, `CartSessionId`, `CustomerId`
  - `State`: `draft`, `ready`, `order_pending`, `completed`, `expired`, `cancelled`
  - `CartVersion`
  - customer/shipping snapshot fields
  - `PaymentMethodKey`
  - `Subtotal`, `ShippingTotal`, `TaxTotal`, `DiscountTotal`, `GrandTotal`, `CurrencyCode`
  - `ExpiresAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`

Service:

- Add `IStorefrontCheckoutService`.
- `PreviewAsync` validates:
  - cart active and non-empty
  - cart version is current
  - customer email/name
  - shipping address requirements
  - payment method enabled for store
  - line price/stock/published/product state
- Return line-level validation errors instead of generic cart failure where possible.

API:

- Add `POST /checkout/preview`.
- Request body includes cart version and checkout fields, not raw cart lines.
- Response model includes normalized totals, line summaries, validation errors, and allowed next action.

Tests:

- Preview rejects stale cart version.
- Preview rejects invalid shipping country/postal/email.
- Preview returns response schema and error schemas in OpenAPI.
- Preview does not create order/payment attempt.

Done when:

- Checkout UI can call preview without placing order.
- Existing direct checkout still works until cutover.

Commit boundary: `feat: add checkout preview session`

### Phase 7 - Idempotent Place Order for COD

Status: completed 2026-07-14 in commit `feat: place cod orders from checkout session`.

Goal: create orders from a checkout session, not raw cart request lines, starting with COD.

Service behavior:

- Add `PlaceOrderAsync`.
- Require:
  - active checkout session
  - current cart version
  - idempotency key
  - enabled payment method
- For COD:
  - create order and order lines from server cart snapshot
  - set payment status according to existing locked decision: COD test method returns paid for MVP unless later changed
  - set order status processing
  - mark cart ordered
  - mark checkout completed
  - clear/expire cart token
- Move stock deduction behind a policy:
  - tracked stock: deduct inside same transaction after final stock check
  - POD/non-stock: no stock deduction

Compatibility:

- Keep `StorefrontScopedOrdersController.ConfirmOrder` protected path untouched until replacement tests exist.
- Deprecate direct `/cart/checkout` only after Storefront V2 fully uses `/checkout/place-order`.

Tests:

- Duplicate idempotency key returns same order result.
- Retry after network failure does not create duplicate order.
- Stale cart version returns 409.
- Order lines snapshot selected attributes and personalization.
- Unpublished product after preview but before place-order returns conflict.
- Transaction rollback leaves stock/cart/order consistent.

Done when:

- COD checkout can complete end to end from server cart.
- Direct raw cart checkout is no longer the preferred Storefront V2 path.

Commit boundary: `feat: place cod orders from checkout session`

### Phase 8 - Payment Attempt Foundation

Goal: create a provider-ready payment ledger before online payment work.

Entity:

- `PaymentAttempt`
  - `Id`, `PublicId`, `StoreId`, `CheckoutSessionId`, `OrderId` nullable
  - `PaymentMethodKey`, `ProviderKey`
  - `State`: `created`, `requires_action`, `authorized`, `captured`, `failed`, `cancelled`, `expired`
  - `Amount`, `CurrencyCode`
  - `IdempotencyKey`
  - `ProviderReference`, `ProviderSessionId`
  - `NextActionType`, `NextActionUrl`
  - `FailureCode`, `FailureMessage`
  - `MetadataJson`
  - `ExpiresAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`
- `PaymentProviderEvent`
  - `Id`, `StoreId`, `PaymentAttemptId` nullable
  - `ProviderKey`, `EventId`, `EventType`, `PayloadHash`, `PayloadJson`
  - `ProcessedAtUtc`, `CreatedAtUtc`
  - unique `(ProviderKey, EventId)` where event id exists

Service:

- Replace immediate `IPaymentHandler.ProcessAsync` dependency for new checkout flow with:
  - `IPaymentProvider.CreateAttemptAsync`
  - `IPaymentProvider.HandleCallbackAsync`
  - `IPaymentProvider.HandleWebhookAsync`
  - `IPaymentProvider.CancelAsync`
- Keep old `IPaymentHandler` only for compatibility until cutover.

Tests:

- Attempt creation is idempotent.
- Attempt state transitions are allowed only in defined order.
- Webhook event dedup works.
- Failed provider result stores safe failure code/message.

Done when:

- COD can be represented as a payment attempt.
- Online provider can be added without changing checkout/order API shape.

Commit boundary: `feat: add payment attempt ledger`

### Phase 9 - Payment API Contract and COD Adapter Cutover

Goal: expose payment attempt state and move COD through the provider-shaped pipeline.

API:

- `GET /payments/attempts/{attemptId}`
- `POST /payments/provider-callback/{providerKey}`
- `POST /payments/webhooks/{providerKey}`

DTOs:

- `StorefrontPaymentAttemptResponse`
- `StorefrontPaymentNextActionResponse`
- `StorefrontPaymentCallbackRequest`
- `StorefrontPaymentWebhookAcceptedResponse`

COD:

- COD provider immediately marks attempt captured/paid.
- Order placement can complete synchronously for COD.
- Payment metadata uses safe JSON and never exposes provider secrets.

OpenAPI:

- Callback and webhook endpoints declare expected error responses.
- Webhook endpoint documents provider signature header where applicable.
- No side-effecting GET.

Tests:

- COD checkout uses payment attempt.
- Attempt polling returns named states.
- Contract tests cover response schema and security metadata.

Done when:

- Existing COD checkout result stays user-compatible.
- Payment attempt APIs are generator-safe.

Commit boundary: `feat: route cod checkout through payment attempts`

### Phase 10 - First Online Provider MVP

Goal: implement exactly one online provider end to end after the ledger exists.

Provider selection gate:

- Choose PayPal if current business priority is fixing existing PayPal capture route.
- Choose Stripe if checkout session redirect support is simpler with existing `StripePaymentService`.
- Do not implement both in this phase.

Provider requirements:

- Store-scoped enablement comes from `StorePaymentMethod`.
- Secrets are never returned in Storefront public DTOs.
- Provider creates a hosted/redirect session and returns `NextActionUrl`.
- Provider callback verifies state and updates `PaymentAttempt`.
- Webhook validates signature if provider SDK/config supports it in this codebase.
- Provider event is deduped before state changes.

Order timing:

- Preferred MVP: create `PaymentAttempt` first, create `Order` only after successful authorization/capture if provider flow allows it.
- If provider requires an order reference before redirect, create order in `order_pending` state and only move to processing after successful payment event.
- This decision must be implemented explicitly in code and tests. No `Guid.Empty` provider order id.

Tests:

- Provider session created with amount/currency/idempotency.
- Callback success changes attempt and order state exactly once.
- Callback/webhook replay is idempotent.
- Failed/cancelled provider flow leaves checkout recoverable.
- Missing provider config returns safe 409/422 error.

Done when:

- One provider works in sandbox/local test mode with no guessed public contract.
- COD remains working.

Commit boundary: `feat: add first online payment provider mvp`

### Phase 11 - Storefront Checkout UX Cutover

Goal: connect Storefront V2 checkout pages to preview/place-order/payment attempt flow.

UI behavior:

- `/my-cart` reads server cart.
- `/checkout` calls preview before enabling final submit.
- COD submit shows confirmation.
- Online provider submit redirects to provider/next-action URL.
- Return/callback page polls payment attempt and shows success/failure/retry state.
- Errors are field-specific where possible and safe where provider-specific.

Files:

- `CheckoutPage.razor`, `CheckoutPage.razor.cs`
- `CartPage.razor`, `CartPage.razor.cs`
- `Program.cs` local endpoints
- `StorefrontApiClient`
- Shared DTOs if the Storefront project has local contract mirrors

States:

- empty cart
- stale cart
- validation error
- provider redirect
- provider pending
- payment failed
- order confirmed

Tests/QA:

- Playwright visible run for add cart -> checkout preview -> COD order.
- Provider sandbox happy path if configured.
- Provider cancel/failure path.
- Browser confirms old `my-cart` payload is gone after migration.
- Console/network errors checked.

Done when:

- Storefront no longer posts raw cart lines to checkout.
- User sees deterministic checkout/payment states.

Commit boundary: `feat: cut storefront checkout to session flow`

### Phase 12 - Deprecate Direct Raw Cart Checkout Path

Goal: remove or constrain the old direct checkout path after Storefront V2 cutover.

Candidate actions:

- Mark `POST /cart/checkout` obsolete in docs if external compatibility still needs it.
- Or remove it from Storefront Swagger if no longer needed.
- Or keep it as compatibility endpoint but internally resolve/create server cart and call the new checkout service.

Required decision before implementation:

- Check current consumers through `rg "CheckoutAsync|cart/checkout|StorefrontCart_Checkout"` and Storefront API client.
- If only Storefront V2 used it and is cut over, remove or hide direct raw cart submit.
- If tests or other runtimes still use it, keep compatibility but route through safe validation.

Tests:

- No public endpoint accepts client-supplied price/status/user/store fields.
- OpenAPI snapshot reflects the chosen compatibility contract.
- Existing order tests updated to new service shape.

Done when:

- There is one authoritative checkout path in application logic.
- Raw request cart cannot bypass server cart validation.

Commit boundary: `refactor: retire raw storefront checkout path`

### Phase 13 - Admin/Operations Visibility

Goal: make payment attempts and checkout failures supportable without exposing secrets.

Scope:

- Commerce admin API can view order payment attempt summary through existing admin order surface or a narrow payment attempt endpoint.
- Control Plane Web still calls Control Plane API only.
- Audit logs record admin-visible payment/order state changes.

Not included:

- Manual capture/refund UI unless explicitly selected.
- Provider secret editing UI unless store payment method settings already supports it safely.

Tests:

- Admin response does not expose provider secret/settings JSON.
- Store-scoped admin lookup cannot read another store's attempt.
- Audit entry written for manual retry/cancel if added.

Done when:

- Support can diagnose failed attempts by safe ids/status/timestamps.

Commit boundary: `feat: expose safe payment attempt admin visibility`

### Phase 14 - Cleanup, Contract Hardening, and Snapshot

Goal: lock OpenAPI and remove migration leftovers.

Tasks:

- Ensure every new/changed endpoint satisfies:
  - operationId
  - summary
  - explicit request model
  - explicit success response model
  - standard error response models
  - required body metadata
  - security metadata where protected
  - validation metadata
- Update `CommerceNodeStorefrontOpenApiContractTests`.
- Update snapshots.
- Generate TypeScript or C# client smoke.
- Add negative contract tests:
  - no domain entity public schema
  - no only-200 operations
  - no side-effecting GET
  - no `X-Store-Key`
  - no server-owned fields
- Remove old cookie fallback if migration window is complete.
- Update docs/QA checklist.

Done when:

- OpenAPI validation passes.
- Contract snapshot is stable.
- QA todos show completed rows with command/browser evidence.

Commit boundary: `test: harden cart checkout payment contracts`

### Phase 15 - End-to-End QA and Release Commit

Goal: prove the full flow in local runtime and document residual risk.

Commands:

```powershell
docker compose -f compose.commercenode.yml up -d
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "CommerceNode|StorefrontV2|CartService|OpenApi"
```

Browser QA:

- Run Storefront V2 with Commerce Node dependencies.
- Use Playwright with `headless=false` if requested.
- Verify:
  - product page add-to-cart
  - cart read/update/remove
  - checkout preview
  - COD order success
  - online provider happy path if sandbox configured
  - online provider cancel/failure if sandbox configured
  - order history for authenticated customer
  - no console errors
  - no readable raw cart payload after token migration

Docs:

- Update `QA-CommerceNode.todo.md`.
- Update `QA-StorefrontV2.todo.md`.
- Update any architecture/API docs if new payment contract rules become permanent.

Done when:

- Focused tests pass.
- Browser QA result is recorded.
- Final commit contains docs evidence.

Commit boundary: `docs: record cart checkout payment qa`

## Test Diagram

```text
Cart token creation
  -> unit: token hash, expiry, store mismatch
  -> API contract: create session schemas/errors
  -> browser: token cookie HttpOnly

Cart line mutation
  -> unit: add/update/remove/version
  -> service: published product, variant, attributes, POD hash
  -> API contract: request validation, 409/422 errors
  -> browser: add/update/remove visible cart

Checkout preview
  -> unit: customer/shipping/payment/cart validation
  -> service: stale version, wrong store, unpublished product
  -> API contract: response/error schemas
  -> browser: field errors and disabled submit

Place order COD
  -> unit: idempotency, stock policy, order snapshots
  -> integration: transaction rollback
  -> browser: confirmation and cart clear

Payment attempt
  -> unit: state transition, dedup event
  -> API contract: attempt polling/callback/webhook
  -> provider sandbox: success/failure/replay

OpenAPI
  -> contract: operationId, summaries, schemas, security, validation
  -> snapshot: breaking change detection
  -> generator smoke: TypeScript or C# client
```

## Failure Modes Registry

| Failure mode | Severity | Detection | Rescue behavior |
| --- | --- | --- | --- |
| Cart token missing/invalid | Medium | API token lookup fails | Create new empty cart or return 404 with safe message depending endpoint. |
| Cart token valid for another store | High | StoreId mismatch on token lookup | Return 404/409 without revealing cross-store cart existence. |
| Cart version stale | High | Request version differs from DB | Return 409 with current cart summary. |
| Product unpublished after cart add | High | Checkout preview/place-order product resolver | Return line-level conflict; block order. |
| Stock changes during checkout | High | Final transaction stock check | Return 409; keep cart active for correction. |
| Duplicate checkout POST | High | Idempotency key lookup | Return original result; no duplicate order/payment. |
| Provider callback replay | High | `PaymentProviderEvent` unique provider event id/hash | Acknowledge duplicate without state reapply. |
| Provider webhook before callback | Medium | Attempt lookup by provider reference | Update attempt idempotently; UI poll catches final state. |
| Payment succeeds but order create fails | Critical | Transaction/log around finalization | Keep attempt captured and create repair task/manual admin alert before marking complete. |
| Order created but payment fails | High | Attempt state failure | Keep order pending/cancelled according explicit state; do not mark processing. |
| Storefront old cookie import fails | Medium | Migration endpoint response | Keep old cookie until successful import; show recoverable cart error. |
| Provider secrets missing | Medium | Provider create attempt validation | Return 409/422 safe config error; do not expose secret names/values. |

## Error And Rescue Registry

| Error code | HTTP | Meaning | User-facing behavior |
| --- | --- | --- | --- |
| `cart.token_missing` | 404/400 | No usable cart token for cart-specific command | Show empty cart or ask user to refresh. |
| `cart.store_mismatch` | 404 | Token does not belong to current store | Start a new cart; do not reveal other store. |
| `cart.version_stale` | 409 | Cart changed since checkout page loaded | Refresh cart summary and ask user to review. |
| `cart.product_unavailable` | 409 | Product is unpublished/archived/wrong store | Remove/block line and show item-level error. |
| `cart.quantity_invalid` | 400/422 | Quantity below minimum or above allowed stock | Highlight quantity field. |
| `checkout.shipping_invalid` | 400/422 | Shipping address failed validation | Highlight address fields. |
| `checkout.payment_method_unavailable` | 409 | Method disabled/missing for store | Ask user to choose another method. |
| `checkout.idempotency_conflict` | 409 | Same key used with different payload | Ask user to refresh checkout. |
| `payment.provider_config_missing` | 409 | Store provider settings incomplete | Show payment unavailable; log admin-safe detail. |
| `payment.requires_action` | 202/200 | Provider redirect/next action required | Redirect or show pending action. |
| `payment.failed` | 409 | Provider rejected/cancelled/failed | Show retry with same checkout if still valid. |
| `payment.webhook_invalid` | 401/400 | Signature/payload invalid | Return safe failure, log provider event rejection. |

## DX Checklist

- New API DTO names are stable and generator-friendly.
- All enum-like public states are named strings.
- All validation constraints appear in OpenAPI.
- Every endpoint has examples or tests that show expected request/response shape.
- Error messages include problem, cause where safe, and next action.
- Provider configuration failures never leak secrets.
- Storefront API client methods match operation names.
- QA docs include commands and browser steps.
- New migrations are Commerce Node only and reversible through standard EF workflow.

## Phase Gate Checklist

Before starting each phase:

- Read affected code with `rg` and direct file reads.
- Confirm no unrelated dirty work will be staged.
- Confirm route/db boundary from architecture docs.
- Define focused tests before runtime behavior changes.

Before committing each phase:

- Run focused build/tests for touched area.
- Update `QA-CommerceNode.todo.md` or `QA-StorefrontV2.todo.md` when behavior changes.
- Update OpenAPI snapshot when API surface changes.
- Stage only intentional files.
- Commit with the phase boundary message.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Intake | Split original plan into 16 small phases. | Mechanical | Completeness | Current code lacks cart session, checkout session, payment attempt, provider event, Storefront migration, and contract hardening. One broad phase would be unstable. | Single big checkout/payment implementation. |
| 2 | Data | Add `CommerceCustomer` instead of adding `StoreId` to `AppUser`. | Mechanical | Explicit over clever | `AppUser` is global Identity state and current email lookup is global. Store-scoped commerce profile avoids breaking Identity uniqueness. | Mutating Identity user model into tenant customer model. |
| 3 | Cart | Use server cart token and DB cart session as source of truth. | Mechanical | Completeness | Browser cookie cart is tamperable and cannot support provider/idempotency/POD state safely. | Keeping JSON `my-cart` as authoritative cart. |
| 4 | Checkout | Add preview/session before place-order. | Mechanical | Completeness | Checkout needs stale cart detection, address/payment validation, and recoverable errors before order/payment mutation. | Direct raw cart checkout submit. |
| 5 | Payment | Add payment attempt ledger before online provider. | Mechanical | Explicit over clever | Current handler receives `Guid.Empty` order id and cannot model redirect/webhook/retry/replay. | Implementing PayPal/Stripe directly inside `CartService`. |
| 6 | Provider | Implement only one online provider first. | Taste | Pragmatic | One provider proves the ledger and callback model. Implementing PayPal and Stripe together doubles failure modes before the abstraction is proven. | Implementing PayPal and Stripe in the same MVP phase. |
| 7 | API | Add contract tests in the same phase as each endpoint. | Mechanical | Completeness | OpenAPI is a product surface and current docs require generator-safe metadata. | Deferring Swagger cleanup to the end only. |
| 8 | UI | Keep old `my-cart` import for one migration phase. | Taste | Bias toward action | Existing users/tests may have old cart cookies; phased migration reduces breakage while moving to token cart. | Immediate removal of old cookie reader. |

## Review Scores

| Review | Score | Notes |
| --- | --- | --- |
| CEO scope | 7/10 | Correct product direction, but original scope needed decomposition and clearer MVP provider gate. |
| Design/UI | 6/10 | Storefront checkout states are identifiable, but UI state handling must be explicit in Phase 11. |
| Engineering | 8/10 after decomposition | Boundaries are clear once customer/cart/checkout/payment are separated. Main risk is migration complexity. |
| DX/API | 8/10 if contract phases are enforced | OpenAPI standard exists and tests exist; plan requires extending them for each new endpoint. |

## Final Approval Gate

Recommended approval: approve the decomposed plan and implement phase by phase.

Taste choices to confirm before implementation:

1. First online provider: PayPal vs Stripe.
2. Old `my-cart` migration window: one phase fallback vs immediate cutover.
3. Direct `/cart/checkout` compatibility: remove, hide from public doc, or route through safe service after Storefront V2 cutover.

User challenge: none. The user asked for a larger, codebase-grounded, non-guessing plan; the decomposition supports that direction.

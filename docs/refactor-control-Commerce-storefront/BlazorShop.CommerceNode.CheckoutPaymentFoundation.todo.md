# BlazorShop Commerce Node Checkout Payment Foundation Todo

Status: planned
Created: 2026-07-13

## Goal

Build the Storefront V2 checkout/payment foundation directly inside the active V2 runtime while learning the order/payment workflow from Smartstore.

The target MVP is intentionally narrow:

- Storefront V2 owns the checkout page directly; no redirect to legacy Web/client checkout.
- Commerce Node owns checkout, order creation, payment method config, order/payment/shipping status, and admin completion.
- Control Plane Web never calls Commerce Node directly. Any admin order/payment action from Control Plane goes through Control Plane API.
- COD is the only enabled MVP payment handler and is treated as a test payment method.
- Stripe and PayPal are seeded/configured as disabled skeleton methods for later implementation.
- `bank_transfer` is removed from the MVP.

## Autoplan Review Summary

### Locked Decisions

| Area | Decision |
| --- | --- |
| Business reference | Use Smartstore workflow as reference only; do not copy Smartstore code. |
| Order status | `pending`, `processing`, `complete`, `cancelled`. |
| Payment status | `pending`, `authorized`, `paid`, `partially_refunded`, `refunded`, `voided`. |
| Shipping status | Keep simple MVP values, align toward `not_yet_shipped`, `shipped`, `delivered`, `shipping_not_required`. |
| Checkout success with COD | `payment_status = paid`, `payment_at = now`, `order_status = processing`, `shipping_status = not_yet_shipped`. |
| Complete order | Manual admin action after tracking/shipment update. |
| Payment method key | Persist `payment_method_key` on `Orders`. |
| Payment metadata | Persist provider-specific metadata in `payment_metadata_json` as PostgreSQL `jsonb`. |
| Payment config | Add store-scoped `store_payment_methods`. |
| Initial payment seed | `cod` enabled, `stripe` disabled, `paypal` disabled. |
| Guest checkout/customer creation | Checkout form email can auto-create a customer with random/internal password. |
| Cart after checkout | Create order lines snapshot and clear Storefront cart cookie after success. |
| Refund | Keep enum/schema ready; no real refund API/UI in MVP. |
| Cancel | Add only a simple manual admin cancel if it remains low-risk. |

### Smartstore Workflow Learning

Smartstore keeps three status axes separate:

```text
order_status    = operational order lifecycle
payment_status  = money/provider lifecycle
shipping_status = fulfillment/shipping lifecycle
```

Important behavior to mirror:

```text
Order created:
  order_status = pending
  payment_status = handler result
  shipping_status = not_yet_shipped or shipping_not_required

Payment authorized/paid:
  pending order -> processing

Payment paid + shipping delivered/not required:
  order_status -> complete
```

Do not use `paid` as an order status. `paid` belongs only to payment status.

## Current State

### Existing Commerce Node Behavior

- `CommerceNodeDbContext` owns `Orders`, `OrderLines`, `PaymentMethods`, `Shipments`, `CommerceStores`, Storefront Identity, and Storefront auth tables.
- `Order` currently has:
  - `Status`
  - `ShippingStatus`
  - shipment/tracking fields
  - `StoreId`
  - `CurrencyCode`
  - `TotalAmount`
  - `Lines`
- `CartService.CheckoutAsync` still uses legacy payment-method name checks:
  - `Credit Card`
  - `PayPal`
  - `Cash on Delivery`
  - `Bank Transfer`
- COD currently returns success but does not create an order.
- Bank transfer currently creates a `Pending` order and sends email instructions.
- `/checkout` in Storefront V2 currently redirects authenticated users to `/account/checkout` on the external client app.
- Shipment MVP already exists and syncs order shipping fields.

### Gaps

- No first-class `PaymentStatus`.
- No first-class `PaymentMethodKey`.
- No store-scoped payment method configuration.
- No local Storefront V2 checkout page.
- No checkout shipping address snapshot.
- No payment handler abstraction.
- No admin `Mark Complete` operation.
- Existing order status values are legacy/string ad hoc and include `Paid` in old flows.

## Scope Rules

- Keep `BlazorShop.Presentation` legacy untouched.
- Do not use `AppDbContext`.
- Use `CommerceNodeDbContext` and `CommerceNodeConnection`.
- Keep Control Plane data out of Commerce Node.
- Keep checkout/order/payment data out of Control Plane DB.
- Use Layered Architecture style:
  - Domain entities/constants.
  - Application DTO/contracts/services.
  - Infrastructure CommerceNode implementations.
  - PresentationV2 controllers/UI.
- Prefer adapting existing `CartService`, `PaymentMethodService`, `CommerceNodeAdminOrderService`, response helpers, store context, and audit service.
- Do not introduce ABP module structure.
- Do not add a separate payment module project in MVP.
- Do not implement real Stripe/PayPal processing in this phase.

## Target Runtime Flow

### Storefront Checkout

```text
Browser /checkout
  -> Storefront V2 checkout page
  -> Storefront V2 server/client posts to Commerce Node api/internal/cart/checkout
  -> Commerce Node validates store/cart/customer/payment
  -> Commerce Node creates/fetches customer by checkout email
  -> COD handler returns paid
  -> Commerce Node creates order + order lines snapshot
  -> Commerce Node deducts stock
  -> Commerce Node returns order reference
  -> Storefront V2 clears cart cookie
  -> Storefront V2 shows confirmation
```

### Admin Complete

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/admin/orders/{id}/complete
          -> CommerceNodeDbContext
```

Rule:

```text
order_status can become complete only when:
  payment_status = paid
  and shipping_status in (delivered, shipping_not_required)

MVP exception:
  manual complete can allow shipped orders if admin confirms.
  The service must still reject unpaid orders.
```

## Database Design

### Existing `Orders` table changes

Preferred implementation: rename/replace the legacy order status field into a first-class order status while keeping DTO compatibility.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `order_status` | `varchar(32)` | no | Replaces legacy `Status` meaning. Values: `pending`, `processing`, `complete`, `cancelled`. |
| `payment_status` | `varchar(32)` | no | Values: `pending`, `authorized`, `paid`, `partially_refunded`, `refunded`, `voided`. |
| `payment_method_key` | `varchar(64)` | no | `cod`, `stripe`, `paypal`. |
| `payment_at` | `timestamp with time zone` | yes | Set when `payment_status = paid`. |
| `payment_metadata_json` | `jsonb` | yes | Provider-specific metadata. |
| `customer_name` | `varchar(256)` | yes | Checkout snapshot. |
| `customer_email` | `varchar(256)` | yes | Checkout snapshot. |
| `shipping_full_name` | `varchar(256)` | no | Checkout shipping snapshot. |
| `shipping_email` | `varchar(256)` | no | Usually same as customer email, but snapshot can differ later. |
| `shipping_phone` | `varchar(64)` | yes | Checkout shipping snapshot. |
| `shipping_address1` | `varchar(400)` | no | Checkout shipping snapshot. |
| `shipping_address2` | `varchar(400)` | yes | Checkout shipping snapshot. |
| `shipping_city` | `varchar(160)` | no | Checkout shipping snapshot. |
| `shipping_state` | `varchar(160)` | yes | Checkout shipping snapshot. |
| `shipping_postal_code` | `varchar(64)` | no | Checkout shipping snapshot. |
| `shipping_country_code` | `varchar(2)` | no | ISO country code for MVP. |
| `updated_at` | `timestamp with time zone` | no | Set on order lifecycle changes. |
| `completed_at` | `timestamp with time zone` | yes | Set when admin marks complete. |
| `cancelled_at` | `timestamp with time zone` | yes | Set when admin cancels, if implemented. |

Migration notes:

- Existing `Status = 'Paid'` should become `order_status = 'processing'`, `payment_status = 'paid'`.
- Existing `Status = 'Pending'` should become `order_status = 'pending'`, `payment_status = 'pending'`.
- Existing unknown status values should be normalized conservatively to `pending` unless already equivalent to `processing`, `complete`, or `cancelled`.
- Existing `ShippingStatus = 'PendingShipment'` should map or be normalized by service to `not_yet_shipped` over time.
- DTO field `GetOrder.Status` may remain for compatibility but should be populated from `Order.OrderStatus`.

Recommended indexes:

| Index | Definition | Reason |
| --- | --- | --- |
| `ix_orders_store_order_status_created` | `(StoreId, order_status, CreatedOn DESC)` | Admin order filters. |
| `ix_orders_store_payment_status_created` | `(StoreId, payment_status, CreatedOn DESC)` | Payment filters/reporting. |
| `ix_orders_store_customer_email_created` | `(StoreId, customer_email, CreatedOn DESC)` | Customer/order lookup. |
| `ix_orders_payment_method_key` | `(payment_method_key)` | Payment method reporting. |

Recommended constraints:

```sql
order_status in ('pending', 'processing', 'complete', 'cancelled')
payment_status in ('pending', 'authorized', 'paid', 'partially_refunded', 'refunded', 'voided')
payment_method_key in ('cod', 'stripe', 'paypal')
```

### `PaymentMethods` changes

Current entity has only `Id` and `Name`. Extend it as a global method catalog.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Existing PK. |
| `Key` | `varchar(64)` | no | Unique: `cod`, `stripe`, `paypal`. |
| `Name` | `varchar(160)` | no | Display fallback. |
| `Description` | `varchar(500)` | yes | Optional. |
| `IsEnabledByDefault` | `boolean` | no | `cod=true`, `stripe=false`, `paypal=false`. |
| `SortOrder` | `int` | no | Display order. |

Seed:

| Key | Name | Default |
| --- | --- | --- |
| `cod` | Cash on Delivery | enabled |
| `stripe` | Stripe | disabled |
| `paypal` | PayPal | disabled |

Remove `bank_transfer` from the V2 seed and do not return it from Storefront payment APIs.

### New `store_payment_methods`

Store-scoped config table.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | PK. |
| `store_id` | `uuid` | no | FK to `commerce_store.id`. |
| `payment_method_key` | `varchar(64)` | no | `cod`, `stripe`, `paypal`. |
| `enabled` | `boolean` | no | Store-level toggle. |
| `display_name` | `varchar(160)` | no | Store-facing label. |
| `description` | `varchar(500)` | yes | Store-facing helper text. |
| `display_order` | `int` | no | UI ordering. |
| `settings_json` | `jsonb` | yes | Provider settings placeholder. |
| `created_at` | `timestamp with time zone` | no | Default current timestamp. |
| `updated_at` | `timestamp with time zone` | no | Updated by service. |

Constraints:

- Unique `(store_id, payment_method_key)`.
- FK `store_id -> commerce_store.id`.
- Check `payment_method_key in ('cod', 'stripe', 'paypal')`.

Development seeding:

- For each active/dev store, ensure:
  - `cod` enabled.
  - `stripe` disabled.
  - `paypal` disabled.
- Do not seed `bank_transfer`.

## Application Constants

Add or extend constants under a suitable existing namespace:

```text
OrderStatuses:
  pending
  processing
  complete
  cancelled

PaymentStatuses:
  pending
  authorized
  paid
  partially_refunded
  refunded
  voided

ShippingStatuses:
  shipping_not_required
  not_yet_shipped
  shipped
  delivered

PaymentMethodKeys:
  cod
  stripe
  paypal
```

Compatibility note:

- Existing UI/DTO labels can format these strings into readable labels.
- Do not introduce numeric enum storage for MVP; V2 tables already use string status patterns in newer features.

## Application DTOs

### Storefront checkout request

Create a new DTO instead of overloading the legacy `Checkout` request too heavily.

```text
StorefrontCheckoutRequest
  string CustomerEmail
  string CustomerName
  string PaymentMethodKey
  IReadOnlyList<ProcessCart> Carts
  CheckoutShippingAddress ShippingAddress
```

```text
CheckoutShippingAddress
  string FullName
  string Email
  string? Phone
  string Address1
  string? Address2
  string City
  string? State
  string PostalCode
  string CountryCode
```

```text
StorefrontCheckoutResult
  Guid OrderId
  string Reference
  string OrderStatus
  string PaymentStatus
  string PaymentMethodKey
  DateTime CreatedOn
```

Validation:

- Customer email required and valid email shape.
- Customer name required, max 256.
- Payment method key required.
- Cart items required.
- Shipping full name/email/address1/city/postal/country required.
- Country code exactly 2 characters.
- Reject disabled or unknown payment method for the current store.
- Reject carts with invalid product/variant/store scope using existing cart line resolution.
- Reject more than 5 selected attributes for custom variation products, preserving existing behavior.

### Admin order DTOs

Extend `GetOrder` with:

- `OrderStatus`
- `PaymentStatus`
- `PaymentMethodKey`
- `PaymentAt`
- `PaymentMetadataJson` only if needed for admin detail; avoid showing raw provider metadata in list.
- `CustomerName`
- `CustomerEmail`
- shipping address snapshot fields for detail.
- `CompletedAt`
- `CancelledAt`

Compatibility:

- Existing `Status` may remain and mirror `OrderStatus` while the UI migrates.

### Store payment method DTOs

```text
GetStorePaymentMethod
  Guid Id
  string PaymentMethodKey
  string DisplayName
  string? Description
  bool Enabled
  int DisplayOrder
  string? SettingsJson
  DateTime CreatedAt
  DateTime UpdatedAt
```

```text
UpdateStorePaymentMethodRequest
  bool Enabled
  string DisplayName
  string? Description
  int DisplayOrder
  string? SettingsJson
```

For Storefront public method lookup, return only:

- `PaymentMethodKey`
- `DisplayName`
- `Description`

Do not expose `settings_json` to Storefront.

## Service Design

### Payment handler abstraction

Add a small interface in Application or CommerceNode-specific service contracts:

```text
IPaymentHandler
  string Key
  Task<PaymentHandlerResult> ProcessAsync(PaymentHandlerContext context, CancellationToken cancellationToken)
```

```text
PaymentHandlerContext
  Guid StoreId
  string CustomerId
  string CustomerEmail
  decimal Amount
  string CurrencyCode
  IReadOnlyList<CartLineSnapshot> Lines
```

```text
PaymentHandlerResult
  bool Success
  string Message
  string PaymentStatus
  DateTime? PaymentAt
  string? MetadataJson
```

Handlers:

| Handler | Key | MVP behavior |
| --- | --- | --- |
| `CodPaymentHandler` | `cod` | Always succeeds as test payment. Returns `payment_status=paid`, `payment_at=now`, metadata `{ handler, mode, processedAt }`. |
| `StripePaymentHandler` | `stripe` | Skeleton only, not enabled. Returns failure/not implemented if called. |
| `PayPalPaymentHandler` | `paypal` | Skeleton only, not enabled. Existing PayPal capture route remains untouched unless implementation phase explicitly replaces it. |

Reason:

- This gives future Stripe/PayPal implementation one obvious contract.
- It prevents hardcoded payment names inside `CartService`.
- It keeps COD test behavior explicit.

### Payment method service

Replace global name filtering with store-scoped config:

- Resolve current store via `ICommerceStoreContext`.
- Load enabled `store_payment_methods`.
- Join/resolve against method catalog by key.
- Sort by `display_order`.
- Return public method DTOs for Storefront.

Admin service:

- List store payment methods.
- Update store payment method enabled/display/settings fields.
- Validate only known keys.
- Audit changes as `PaymentMethod.Updated`.

### Checkout service

Preferred implementation:

- Add a CommerceNode-specific checkout service or evolve `CartService` carefully.
- Avoid breaking legacy `ICartService` call sites.
- Keep existing cart line resolution/stock deduction logic if practical.
- Move legacy name-based `Credit Card`/`Bank Transfer` branches out of the V2 checkout path.

Responsibilities:

1. Resolve current store.
2. Validate cart items.
3. Resolve or create customer by checkout email.
4. Resolve enabled payment method by key and store.
5. Run payment handler.
6. Create `Order` with:
   - `OrderStatus = processing` for COD paid result.
   - `PaymentStatus = paid`.
   - `PaymentMethodKey = cod`.
   - `PaymentAt = now`.
   - `PaymentMetadataJson`.
   - customer snapshot.
   - shipping address snapshot.
   - order lines snapshot.
   - `ShippingStatus = not_yet_shipped`.
7. Deduct stock inside the same transaction.
8. Return order reference/result.

Customer creation:

- If checkout email belongs to an existing Storefront customer, attach the order to that user.
- If not, create `AppUser` in `CommerceNodeDbContext` with:
  - email from checkout.
  - full name from checkout.
  - random/internal strong password.
  - Storefront `User` role.
- Do not require the customer to set a password during checkout MVP.
- Do not send password email in MVP.

### Order workflow service

Add a small service to centralize status transitions.

```text
IOrderWorkflowService
  ApplyPaymentResult(...)
  MarkCompleteAsync(orderId)
  CancelAsync(orderId, reason)
```

Rules:

- `payment_status = paid` and order currently `pending` -> `processing`.
- Manual complete rejects unpaid orders.
- Manual complete sets:
  - `order_status = complete`
  - `completed_at = now`
  - `updated_at = now`
- Manual cancel sets:
  - `order_status = cancelled`
  - `cancelled_at = now`
  - `updated_at = now`
- Refund statuses are not exposed in MVP.

## API Design

### Commerce Node internal Storefront APIs

Route group: `api/internal/*`

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/internal/payments/methods` | Return enabled payment methods for current store. |
| `POST` | `/api/internal/cart/checkout` | Create checkout order with payment method key and shipping snapshot. |

`POST /api/internal/cart/checkout` authorization decision:

- Existing route is `[Authorize]`.
- New MVP checkout should support anonymous/guest checkout because the checkout form can auto-create a customer by email.
- Keep customer-order history routes authenticated.
- If JWT exists, prefer authenticated customer id/email.
- If JWT does not exist, create/fetch customer by request email.

### Commerce Node admin/control APIs

Route group: `api/commerce/admin/*`

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/commerce/admin/payment-methods` | List store payment method config. |
| `PUT` | `/api/commerce/admin/payment-methods/{paymentMethodKey}` | Update enabled/display/settings for current store. |
| `POST` | `/api/commerce/admin/orders/{id}/complete` | Mark order complete manually. |
| `POST` | `/api/commerce/admin/orders/{id}/cancel` | Optional MVP manual cancel. |

Order list/detail should include payment fields.

Security:

- Use existing Commerce admin route protection.
- Use current store context.
- Never trust `StoreId` from request body.

### Control Plane gateway APIs

Control Plane Web must call only Control Plane API.

Add proxy routes only if Control Plane Admin UI needs them:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/control-plane/stores/{storePublicId}/catalog/payment-methods` | Proxy CommerceNode payment config list. |
| `PUT` | `/api/control-plane/stores/{storePublicId}/catalog/payment-methods/{paymentMethodKey}` | Proxy update. |
| `POST` | `/api/control-plane/stores/{storePublicId}/catalog/orders/{orderId}/complete` | Proxy complete. |
| `POST` | `/api/control-plane/stores/{storePublicId}/catalog/orders/{orderId}/cancel` | Proxy cancel if implemented. |

Do not add Commerce Node URL/key/secret to Control Plane Web.

## Storefront V2 UI Design

### `/checkout`

Replace redirect behavior with a real Storefront V2 checkout page.

Layout:

- Left/main column:
  - Contact section: email, customer name.
  - Shipping address section.
  - Payment section: enabled methods from `api/internal/payments/methods`.
  - COD should appear as the only enabled method in MVP.
- Right/summary column:
  - Cart line summary.
  - subtotal/total.
  - Place order button.

Behavior:

- Read cart from existing `my-cart` cookie.
- Refresh product/variant details from Commerce Node, reusing cart page logic where possible.
- If cart is empty, show empty checkout state and link back to catalog/cart.
- Submit only on button click.
- On success:
  - clear cart cookie.
  - show confirmation with order reference.
  - no redirect to legacy or external client app.
- On API `success=false`, show API message.
- If Commerce Node is unavailable, show service unavailable state.

SEO/private page:

- Apply private/noindex headers/meta for checkout.
- Do not include checkout in sitemap.

### Control Plane Admin UI

If current Admin UX includes order drawer:

- Add payment status/order status fields to order detail.
- Add `Mark Complete` button.
- Disable `Mark Complete` when unpaid or already complete/cancelled.
- Show payment method key.
- Payment config UI can be a simple settings page/table:
  - key
  - display name
  - enabled toggle
  - display order
  - save

## Phase Plan

## Phase 1 - Domain Constants And Order Schema

Tasks:

- Add status/payment method constants.
- Update `Order` entity with first-class order/payment fields.
- Add customer and shipping snapshot fields.
- Add `PaymentMethod` key/display/config fields.
- Add `StorePaymentMethod` entity.
- Add `DbSet<StorePaymentMethod>`.
- Configure EF mapping, jsonb columns, indexes, and constraints.
- Add CommerceNode migration.
- Backfill existing order/payment status data conservatively.
- Seed `cod`, `stripe`, `paypal`; remove/deactivate `bank_transfer`.
- Seed default store payment methods.

Acceptance:

- `CommerceNodeDbContextModelSnapshot` updates correctly.
- Clean CommerceNode database migrates.
- Existing orders do not lose references/lines.
- Existing `GetOrder.Status` can still be populated from the new order status.

## Phase 2 - Payment Method Config Services

Tasks:

- Add DTOs for store payment methods.
- Add service contract for admin payment method config.
- Implement CommerceNode store-scoped payment method service.
- Update Storefront payment method lookup to use store config and return only enabled public methods.
- Keep `settings_json` admin-only.
- Add audit log for payment config updates.

Acceptance:

- `GET /api/internal/payments/methods` returns COD only for default store.
- Stripe/PayPal exist but are not returned while disabled.
- `bank_transfer` is not returned.
- Another store can have different enabled payment methods.

## Phase 3 - Payment Handler Skeleton

Tasks:

- Add `IPaymentHandler`, context/result DTOs.
- Add `CodPaymentHandler`.
- Add disabled skeleton handlers for Stripe/PayPal.
- Add handler resolver by `payment_method_key`.
- Make disabled methods reject checkout even if directly posted.

Acceptance:

- COD handler returns `paid` and metadata:
  - `handler = "cod"`
  - `mode = "test"`
  - `processedAt`
- Stripe/PayPal cannot be used unless store config enables them and handler is implemented.

## Phase 4 - Storefront Checkout Service And Internal API

Tasks:

- Add `StorefrontCheckoutRequest`, `CheckoutShippingAddress`, `StorefrontCheckoutResult`.
- Implement checkout flow using current store context.
- Support guest checkout auto customer creation by email.
- Attach authenticated customer when JWT exists.
- Create order and order lines snapshot.
- Persist payment fields and shipping address snapshot.
- Deduct stock transactionally.
- Replace COD legacy branch that returned success without order creation.
- Remove `bank_transfer` from the V2 checkout path.
- Update `StorefrontCartController.Checkout`.

Acceptance:

- Anonymous checkout can create/fetch customer by email.
- Authenticated checkout attaches to the authenticated user.
- COD creates an order with:
  - `order_status = processing`
  - `payment_status = paid`
  - `payment_method_key = cod`
  - `payment_at` set
  - `shipping_status = not_yet_shipped`
  - order lines snapshot
  - customer/shipping snapshot
- Out-of-stock cart is rejected.
- Unknown/disabled payment method is rejected.

## Phase 5 - Admin Order Workflow

Tasks:

- Add `IOrderWorkflowService`.
- Implement `MarkCompleteAsync`.
- Optional: implement simple manual `CancelAsync`.
- Add CommerceNode admin endpoints:
  - `POST /api/commerce/admin/orders/{id}/complete`
  - optional `POST /api/commerce/admin/orders/{id}/cancel`
- Update `CommerceNodeAdminOrderService` mapping to include payment fields.
- Add admin audit events:
  - `Order.Completed`
  - `Order.Cancelled` if cancel is implemented.

Acceptance:

- Unpaid orders cannot be marked complete.
- Completed orders get `completed_at`.
- Complete action is store-scoped.
- Audit log records the action.
- Existing shipment/tracking endpoints still work.

## Phase 6 - Control Plane Gateway And Admin UI

Tasks:

- Add Control Plane API gateway methods for payment config and order completion if Admin UI needs them.
- Add/update typed client methods in Control Plane Web.
- Add order detail payment/status fields.
- Add `Mark Complete` action in order drawer/detail.
- Add payment method config UI if not deferred.

Acceptance:

- Control Plane Web calls only Control Plane API.
- Control Plane API calls CommerceNode API with node credentials.
- UI reads API response `success/message/data`.
- Mark complete updates visible order state after refresh.

## Phase 7 - Storefront V2 Checkout Page

Tasks:

- Replace `/checkout` redirect in `Program.cs` with a real route/page.
- Add checkout page/component.
- Reuse cart cookie parsing and product refresh patterns from `CartPage`.
- Load enabled payment methods from Commerce Node.
- Add contact/shipping/payment form.
- Submit checkout request to Commerce Node.
- Clear cart cookie after success.
- Render confirmation state.
- Keep page noindex/private.

Acceptance:

- Anonymous `/checkout` renders checkout page, not redirect.
- Empty cart renders empty state.
- COD checkout succeeds and displays order reference.
- Cart cookie is cleared after success.
- Failed checkout shows API message.
- No legacy Web/API requests appear in browser network.

## Phase 8 - QA Documentation And Verification

Tasks:

- Update `QA-CommerceNode.todo.md`:
  - schema/migration checks
  - payment methods config
  - COD checkout creates order
  - disabled payment method rejected
  - complete order
  - store isolation
- Update `QA-StorefrontV2.todo.md`:
  - `/checkout` page visible
  - form validation
  - guest checkout
  - cart clear
  - confirmation
  - no legacy redirect/request
- Run focused build:
  - CommerceNode API
  - ControlPlane API/Web if gateway/UI changed
  - Storefront V2
- Run visible Playwright QA when user requests browser observation.

Acceptance:

- QA checklist is updated with pass/fail notes.
- Any failing runtime behavior is fixed before marking complete.
- Feature works against CommerceNode PostgreSQL on port `5434`.

## Out Of Scope

- Real Stripe payment flow.
- Real PayPal payment flow.
- Bank transfer.
- Refund API/UI.
- Provider webhook handling.
- Payment capture/authorize workflow beyond schema/skeleton.
- Email notifications.
- Shipping rates.
- Shipping methods.
- Partial shipment.
- Customer account password setup email.
- Multi-store customer membership model.
- Public exposure of `api/internal/*`.
- Any legacy `BlazorShop.Presentation` implementation work.

## QA Checklist Additions

Add to `QA-CommerceNode.todo.md`:

- [ ] `PaymentMethods` seed contains `cod`, `stripe`, `paypal`.
- [ ] `bank_transfer` is not returned by Storefront payment methods.
- [ ] Default store has `cod` enabled and `stripe/paypal` disabled.
- [ ] `GET /api/internal/payments/methods` returns only enabled methods for current store.
- [ ] `POST /api/internal/cart/checkout` with COD creates an order.
- [ ] Created COD order has `order_status=processing`.
- [ ] Created COD order has `payment_status=paid`.
- [ ] Created COD order has `payment_method_key=cod`.
- [ ] Created COD order has `payment_at`.
- [ ] Created COD order has `payment_metadata_json`.
- [ ] Created COD order has customer snapshot fields.
- [ ] Created COD order has shipping address snapshot fields.
- [ ] Disabled `stripe` checkout request returns `success=false`.
- [ ] Unknown payment method key returns `success=false`.
- [ ] Checkout creates customer when email does not exist.
- [ ] Checkout attaches existing customer when email exists.
- [ ] Admin order detail returns payment fields.
- [ ] Admin mark complete succeeds for paid/shipped order.
- [ ] Admin mark complete rejects unpaid order.
- [ ] Store isolation blocks another store from reading/completing the order.
- [ ] Audit log includes `Order.Completed`.

Add to `QA-StorefrontV2.todo.md`:

- [ ] `/checkout` renders local Storefront V2 checkout page.
- [ ] `/checkout` does not redirect to `/account/checkout`.
- [ ] Empty cart checkout shows empty state.
- [ ] Checkout page loads enabled payment methods.
- [ ] COD is selected/available in MVP.
- [ ] Required contact/shipping validation blocks submit.
- [ ] COD checkout succeeds with visible order reference.
- [ ] Cart cookie is cleared after checkout success.
- [ ] Browser network shows no legacy API/Web requests.
- [ ] Checkout page is noindex/private and absent from sitemap.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Renaming `Order.Status` breaks existing mappings | Medium | Keep DTO compatibility and update all CommerceNode mappings in one phase. |
| Checkout creates order but stock deduction fails | High | Use existing transaction manager and fail atomically. |
| Guest checkout creates duplicate customers | Medium | Lookup by normalized email before create. |
| Disabled payment method can be posted manually | Medium | Validate store config server-side before handler execution. |
| Storefront clears cart before order is persisted | High | Clear cookie only after API success. |
| Control Plane Web accidentally calls CommerceNode | High | Implement gateway only through ControlPlane API and update docs/QA. |
| Old bank transfer branch remains reachable | Medium | Remove from V2 payment method config and checkout service path. |

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Keep checkout/payment in CommerceNodeDbContext | Mechanical | Boundary ownership | Checkout/order/payment are node-local ecommerce data. | ControlPlane DB or AppDbContext |
| 2 | Workflow | Split order/payment/shipping statuses | Mechanical | Smartstore learning | Prevents `paid` from polluting order lifecycle. | Single `Status` field |
| 3 | Payment | Enable only COD for MVP | Mechanical | Simplicity | Gives test checkout without real provider complexity. | Bank transfer, real Stripe/PayPal now |
| 4 | Store Config | Use `store_payment_methods` | Mechanical | Multi-store readiness | Payment methods are per store and can be toggled independently. | Global hardcoded disabled method list |
| 5 | UI | Storefront V2 renders checkout directly | Mechanical | Legacy independence | V2 should not redirect checkout to legacy/client handoff. | `/account/checkout` redirect |
| 6 | Metadata | Use `payment_metadata_json` | Mechanical | Extensibility | Provider-specific fields stay flexible until they need query indexes. | Many nullable provider columns now |


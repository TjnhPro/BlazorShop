# BlazorShop Commerce Node Shipping Fulfillment Foundation Todo

Status: draft
Created: 2026-07-10

## Goal

Add a small Commerce Node shipment foundation for MVP order fulfillment without introducing shipping providers, shipping methods, partial shipments, or tracking lifecycle complexity.

The target is a simple admin operation:

- an admin opens an order;
- an admin creates or replaces the shipment information;
- Commerce Node persists one shipment row for that order;
- Commerce Node syncs the existing `Order` shipping fields so StorefrontV2 can keep reading the old order response.

## Scope Rules

- Keep using Layered Architecture style.
- Keep `BlazorShop.Presentation` legacy untouched.
- Do not use `AppDbContext`.
- Use `CommerceNodeDbContext` and `CommerceNodeConnection`.
- Reuse existing Application DTO/service contracts, CommerceNode services, API response envelope, audit service, and store context.
- Keep StorefrontV2 behavior compatible by continuing to expose shipping data through existing order fields.
- Keep old tracking/status endpoints available unless a later phase explicitly removes them.
- Commit each completed phase separately when implementation starts.

## Locked Decisions

| Decision | Result |
| --- | --- |
| Add `shipping_methods` | No |
| Add `shipment_items` | No |
| Support partial shipment | No |
| Shipment count per order | Maximum 1 |
| Replace shipment | Use upsert/update existing row |
| Delete/cancel shipment | No |
| Shipment lifecycle/tracking state | No |
| Real carrier tracking integration | No |
| Real customer email notification | No, audit/skeleton only |
| Customer-facing new Storefront shipment API | No |
| Customer view | Reuse existing order shipping fields |

## Current State

Commerce Node already has:

- `Order` with `StoreId`, `ShippingCarrier`, `TrackingNumber`, `TrackingUrl`, `ShippingStatus`, `ShippedOn`, `DeliveredOn`, and `LastTrackingUpdate`.
- `CommerceOrdersController` under `api/commerce/admin/orders`.
- `IAdminOrderService` with order list/detail, tracking update, shipping status update, and admin note update.
- `CommerceNodeAdminOrderService`, already store-scoped through `ICommerceStoreContext`.
- `CommerceNodeOrderTrackingService`, already scoped by current store.
- Storefront/customer order DTOs already expose shipping fields from `Order`.

Known gap:

- There is no first-class `Shipment` record. Tracking data is currently stored only on `Order`, so admin cannot treat shipment as its own fulfillment record.

## Target Behavior

### Create shipment

When `PUT /api/commerce/admin/orders/{orderId}/shipment` is called and the order has no shipment:

1. Resolve the current store using `ICommerceStoreContext`.
2. Find the order by `Order.Id` and `Order.StoreId`.
3. Insert one `Shipment`.
4. Sync existing `Order` fields:
   - `Order.ShippingStatus = "Shipped"`
   - `Order.ShippedOn = request.shipDate`
   - `Order.ShippingCarrier = request.carrierName`
   - `Order.TrackingNumber = request.trackingNumber`
   - `Order.TrackingUrl = request.trackingUrl`
   - `Order.LastTrackingUpdate = DateTime.UtcNow`
5. Write admin audit log `Order.ShipmentUpserted`.
6. Return the shipment DTO in the existing API response envelope.

### Replace shipment

When `PUT /api/commerce/admin/orders/{orderId}/shipment` is called and a shipment already exists:

1. Update the existing shipment row.
2. Re-sync the same `Order` fields.
3. Update `Shipment.UpdatedAt`.
4. Write admin audit log `Order.ShipmentUpserted`.

This intentionally acts as replace/update, not create-a-new-shipment-history.

### Read shipment

`GET /api/commerce/admin/orders/{orderId}/shipment` returns the shipment if it exists.

If no shipment exists, return the standard service failure response with `ServiceResponseType.NotFound` and message `Shipment not found.`.

## Database Design

### New entity: `Shipment`

Namespace recommendation: `BlazorShop.Domain.Entities.Payment`.

Reason: shipment belongs to the order/payment aggregate and syncs directly into `Order` shipping fields.

### New table: `Shipments`

Use PascalCase table name to align with existing order aggregate tables (`Orders`, `OrderLines`) in CommerceNode migrations.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Primary key. |
| `StoreId` | `uuid` | no | Copied from current store context. |
| `OrderId` | `uuid` | no | FK to `Orders.Id`. |
| `ShipDate` | `timestamp with time zone` | no | Normalized to UTC. |
| `CarrierName` | `varchar(128)` | no | Free-form carrier name, for example `UPS`, `USPS`, `FedEx`. |
| `CarrierService` | `varchar(128)` | yes | Free-form service name, for example `Ground`, `Priority Mail`. |
| `TrackingNumber` | `varchar(160)` | no | Free-form tracking number. |
| `TrackingUrl` | `varchar(1024)` | yes | Optional direct tracking URL. |
| `Note` | `varchar(1000)` | yes | Internal admin note for shipment. |
| `CreatedAt` | `timestamp with time zone` | no | Default `CURRENT_TIMESTAMP`. |
| `UpdatedAt` | `timestamp with time zone` | no | Set by service on create/update. |

### Indexes and constraints

| Index/constraint | Definition | Reason |
| --- | --- | --- |
| PK | `Id` | Entity identity. |
| Unique | `(StoreId, OrderId)` | Enforces maximum one shipment per order in a store scope. |
| Index | `StoreId` | Store-scoped admin queries. |
| Index | `OrderId` | Order detail lookup. |
| FK | `OrderId -> Orders.Id` | Shipment belongs to order. |

Recommended delete behavior: `Cascade`, matching order aggregate behavior. Orders should not normally be deleted in production, but if a test order is deleted the shipment should not remain orphaned.

Do not add:

- `ShippingMethodId`;
- provider table;
- shipment item table;
- tracking event table;
- email/outbox table in this MVP.

## Application DTOs

Add under `BlazorShop.Application/DTOs/Payment` or a nearby existing admin order DTO folder.

### `GetShipment`

Fields:

- `Guid Id`
- `Guid StoreId`
- `Guid OrderId`
- `DateTime ShipDate`
- `string CarrierName`
- `string? CarrierService`
- `string TrackingNumber`
- `string? TrackingUrl`
- `string? Note`
- `DateTime CreatedAt`
- `DateTime UpdatedAt`

### `UpsertShipmentRequest`

Fields:

- `DateTime ShipDate`
- `string CarrierName`
- `string? CarrierService`
- `string TrackingNumber`
- `string? TrackingUrl`
- `string? Note`

Validation:

- `orderId` must not be empty.
- `ShipDate` is required and normalized to UTC.
- `CarrierName` is required and max 128 chars.
- `CarrierService` max 128 chars.
- `TrackingNumber` is required and max 160 chars.
- `TrackingUrl` max 1024 chars.
- `Note` max 1000 chars.

## Service Design

Preferred MVP approach after code inspection: add `IAdminShipmentService`.

Reason:

- `IAdminOrderService` is also implemented by legacy `BlazorShop.API`.
- Extending `IAdminOrderService` would force legacy service changes for a CommerceNode-only feature.
- `CommerceOrdersController` can still own the route while delegating shipment behavior to a dedicated service.
- `CommerceNodeAdminShipmentService` can reuse the same store context, API response, and audit patterns without touching legacy.
- A separate `IShipmentService` can be introduced later if shipment grows into provider integration, labels, webhooks, or notification workflows.

Add contract:

```csharp
public interface IAdminShipmentService
{
Task<ServiceResponse<GetShipment>> GetShipmentAsync(Guid orderId);
Task<ServiceResponse<GetShipment>> UpsertShipmentAsync(Guid orderId, UpsertShipmentRequest request);
}
```

Implementation notes:

- Use the existing `GetCurrentStoreOrdersAsync(asTracking: true)` pattern to ensure store isolation.
- Do not trust `StoreId` from request body.
- Copy `StoreId` from current store context/order.
- Save shipment and order field sync in the same `SaveChangesAsync` call when possible.
- Keep old `UpdateTrackingAsync` and `UpdateShippingStatusAsync` methods for compatibility.

## API Design

Controller: `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceOrdersController.cs`

Routes:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/commerce/admin/orders/{orderId}/shipment` | Get the single shipment for an order. |
| `PUT` | `/api/commerce/admin/orders/{orderId}/shipment` | Create or replace the single shipment for an order. |

Security:

- Keep inherited Commerce admin security from `CommerceAdminControllerBase`.
- Request must be store-scoped by existing CommerceNode admin store context.
- Do not expose this as `api/internal/*`.

Response:

- Use existing API response envelope.
- UI/client should read `success`, `message`, and `data`.
- HTTP status behavior should follow current `FromServiceResponse` behavior.

## UI Plan

No StorefrontV2 UI change is required for MVP.

StorefrontV2 should continue to display customer shipping information from existing order response fields:

- `ShippingStatus`
- `ShippingCarrier`
- `TrackingNumber`
- `TrackingUrl`
- `ShippedOn`

Commerce admin UI is optional for this todo unless an existing order detail page is already present in ControlPlane/Commerce admin flow.

If a UI exists, add only a compact shipment editor on order detail:

- ship date input;
- carrier name text input;
- carrier service text input;
- tracking number text input;
- tracking URL text input;
- note textarea;
- save button;
- show existing shipment if present;
- show API `message` on success/failure.

Important boundary:

- `BlazorShop.ControlPlane.Web` must not call `BlazorShop.CommerceNode.API` directly.
- Any future ControlPlane admin UI must call `BlazorShop.ControlPlane.API`, and ControlPlane API must proxy/dispatch to CommerceNode.

## QA Checklist Additions

Add these cases to `QA-CommerceNode.todo.md` when implementation starts:

- Migration applies on clean `blazorshop-commercenode-postgres` database.
- `GET /api/commerce/admin/orders/{orderId}/shipment` returns not found before shipment exists.
- `PUT /api/commerce/admin/orders/{orderId}/shipment` creates shipment.
- `GET /api/commerce/admin/orders/{orderId}/shipment` returns created shipment.
- Second `PUT` replaces existing shipment instead of inserting duplicate.
- Database enforces unique `(StoreId, OrderId)`.
- Order fields are synced after create:
  - `ShippingStatus`
  - `ShippedOn`
  - `ShippingCarrier`
  - `TrackingNumber`
  - `TrackingUrl`
  - `LastTrackingUpdate`
- Store isolation: a request scoped to another store cannot read/update the shipment.
- Audit log includes `Order.ShipmentUpserted`.
- Existing tracking/status endpoints still work after migration.
- Storefront order detail still reads shipping info from order fields.
- No new Storefront shipment endpoint is exposed.

## Phase 1 - Database Foundation

Tasks:

- Add `Shipment` entity.
- Add `DbSet<Shipment>` to `CommerceNodeDbContext`.
- Configure `Shipment` in `CommerceNodeDbContext.OnModelCreating`.
- Add CommerceNode EF migration.
- Verify migration updates `CommerceNodeDbContextModelSnapshot`.

Acceptance:

- Project builds.
- Migration can be applied to clean CommerceNode PostgreSQL database.
- `Shipments` table has unique `(StoreId, OrderId)`.

## Phase 2 - DTOs and Service Contract

Tasks:

- Add `GetShipment`.
- Add `UpsertShipmentRequest`.
- Add `IAdminShipmentService` with shipment methods.
- Keep existing order service methods unchanged.

Acceptance:

- No existing compile errors in services/controllers.
- DTOs use existing namespace and style conventions.

## Phase 3 - CommerceNode Service Implementation

Tasks:

- Implement `GetShipmentAsync`.
- Implement `UpsertShipmentAsync`.
- Register `IAdminShipmentService` to `CommerceNodeAdminShipmentService`.
- Validate request fields.
- Resolve current store.
- Load order by current `StoreId`.
- Create or update the single shipment row.
- Sync `Order` shipping fields.
- Add admin audit log action `Order.ShipmentUpserted`.

Acceptance:

- Missing order returns `Order not found.`.
- Missing shipment returns `Shipment not found.`.
- Invalid input returns validation failure.
- Shipment upsert and order sync happen atomically through `CommerceNodeDbContext`.

## Phase 4 - API Endpoints

Tasks:

- Add `GET {id:guid}/shipment` endpoint to `CommerceOrdersController`.
- Add `PUT {id:guid}/shipment` endpoint to `CommerceOrdersController`.
- Use `FromServiceResponse`.
- Keep existing `tracking`, `shipping-status`, and `admin-note` endpoints.

Acceptance:

- Endpoints use route prefix `api/commerce/admin/orders`.
- Endpoints return existing API response envelope.
- Endpoints do not bypass Commerce admin auth/store scope.

## Phase 5 - QA Documentation and Verification

Tasks:

- Update `QA-CommerceNode.todo.md` with shipment cases.
- Run CommerceNode API against clean database.
- Seed or reuse an order for a current store.
- Verify shipment create/read/replace through API.
- Verify order fields sync through order detail API.
- Verify audit log row is created.
- Run relevant build/test commands.

Acceptance:

- QA checklist entries are marked with actual verification result.
- Any failing behavior is fixed before marking complete.
- The implementation remains independent from legacy `BlazorShop.Presentation`.

## Out of Scope

- Shipping rates.
- Shipping method admin.
- Carrier provider abstraction.
- Label purchase.
- Tracking event ingestion.
- Tracking webhooks.
- Partial shipment.
- Multi-package shipment.
- Shipment item allocation.
- Shipment delete/cancel.
- Real email notification.
- Customer-facing shipment API.
- Storefront shipment page redesign.

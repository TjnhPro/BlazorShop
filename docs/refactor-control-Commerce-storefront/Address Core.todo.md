# Address Core.todo

Generated: 2026-07-17

Source plan: `Address Core.md`

Status: In progress. Phase 0 completed.

Scope: add practical customer address book, country/state lookup, billing/shipping address support, and checkout address selection to active V2 without replacing the current checkout/order snapshot model.

## Scope Lock

Approved:

- [ ] Customer address book.
- [ ] Billing address and shipping address support.
- [ ] Add, edit, delete customer addresses.
- [ ] Default shipping address.
- [ ] Default billing address.
- [ ] Country to state/province lookup.
- [ ] Basic field required/enabled configuration shape.
- [ ] Address normalization and validation hook.
- [ ] Snapshot selected checkout address into checkout session and order.
- [ ] Keep guest checkout address entry working.

Deferred:

- [ ] External address verification providers.
- [ ] Geocoding, latitude/longitude, delivery-zone routing, or map behavior.
- [ ] Full tax address rules.
- [ ] Full shipping-rate address validation.
- [ ] VAT/company tax identity.
- [ ] Pickup point, locker, warehouse, or multi-origin fulfillment address models.
- [ ] Fax as a first-class field unless a store explicitly needs it later.
- [ ] Arbitrary custom field persistence before a real admin/UI need exists.
- [ ] Rewriting existing checkout/order snapshot columns into a large owned object refactor.
- [ ] Extending legacy `AppDbContext` or legacy presentation projects.

## Current Baseline

- [x] `CommerceCustomer` exists under Commerce Node domain.
- [x] `StorefrontCustomerService` resolves or creates store-scoped customer during checkout by email.
- [x] `StorefrontCheckoutShippingAddressDto` exists and supports direct checkout shipping address.
- [x] `StorefrontCheckoutService.ValidateCheckoutFields` validates current required shipping fields.
- [x] Storefront API checkout shipping address contract has DataAnnotations for required fields and formats.
- [x] `CheckoutSession` stores shipping address snapshot fields.
- [x] `Order` stores shipping address snapshot fields.
- [x] Storefront V2 checkout page renders direct shipping address form fields.

Missing:

- [ ] No `CommerceCustomerAddress` entity.
- [ ] No customer address book table.
- [ ] No Storefront API for customer address CRUD/defaults.
- [ ] No billing address model.
- [ ] No country/state/province catalog endpoint.
- [ ] No address field configuration endpoint.
- [ ] No reusable address validation service.
- [ ] No safe authenticated address management route in Storefront V2.

## Core Decisions

- [x] Keep checkout/order snapshots as source of historical truth.
- [ ] Add address book as Commerce Node customer data in `CommerceNodeDbContext`.
- [ ] Require storefront customer auth for address book CRUD.
- [ ] Derive customer identity from claims/auth context; never from browser-supplied `customerId`.
- [ ] Support one default shipping and one default billing address per store/customer.
- [ ] Start country/state lookup as static or seeded catalog, not full admin country management.
- [ ] Start field configuration as DTO/config shape, not full settings UI.
- [ ] Add provider-free `IAddressValidationService` before any external verification provider.

## Target Boundary

```text
Storefront V2 browser
  -> Storefront V2 local account/checkout endpoints or pages
      -> StorefrontApiClient
          -> Commerce Node Storefront API
              api/storefront/stores/{storeKey}/customer/addresses/*
              api/storefront/stores/{storeKey}/checkout/*
                  -> Address/customer/checkout application services
                      -> CommerceNodeDbContext
```

Boundary rules:

- [x] Store scope comes from `{storeKey}` route resolution.
- [ ] Customer identity comes from authenticated user claims or trusted auth result.
- [x] Browser requests do not include `customerId`, `storeId`, audit fields, or order-owned address snapshot fields.
- [ ] Checkout may accept either a direct guest address or an authenticated `addressId`.
- [x] Checkout always snapshots the resolved address into `CheckoutSession` and `Order`.
- [x] Control Plane is not part of the customer address runtime path.
- [x] Legacy projects remain reference only.

## Phase 0 - Baseline And Contract Guard

Goal: protect current checkout/order behavior before adding address book.

Implementation checklist:

- [x] Re-read current checkout service tests.
- [x] Re-read order/payment placement tests that assert order snapshot fields.
- [x] Re-read Storefront V2 checkout host/static tests.
- [x] Re-read Storefront OpenAPI contract tests for checkout.
- [x] Identify current Storefront V2 checkout request/client models that need additive fields later.
- [x] Confirm checkout still accepts direct shipping address.
- [x] Confirm checkout preview writes shipping snapshot into `CheckoutSession`.
- [x] Confirm place-order copies checkout session address into `Order`.
- [x] Confirm order snapshot remains independent from customer profile/address book mutation.
- [x] Add baseline tests if snapshot behavior is not explicitly protected.
- [x] Add source/static guard that no public checkout/address request accepts `customerId`.
- [x] Make no schema, route, or behavior change unless needed to close a baseline test gap.

Verification checklist:

- [x] Existing checkout service tests pass.
- [x] Existing Storefront V2 checkout host/static tests pass.
- [x] Existing Storefront OpenAPI contract tests pass.
- [x] No legacy project is touched.
- [x] No `AppDbContext` usage is introduced.
- [x] Current checkout request contract remains compatible.

Exit criteria:

- [x] Existing checkout flow is protected before address book work starts.
- [x] Snapshot behavior is protected by focused tests.
- [x] Baseline gaps are documented in this file and QA checklists.

Phase 0 evidence:

- 2026-07-17: Added `StorefrontCheckoutServiceTests.PlaceOrderAsync_CopiesCheckoutAddressSnapshotAndDoesNotReadMutatedCustomerProfile`.
- 2026-07-17: Added `AddressCorePhase0InventoryTests` source guard for checkout/address public request contracts and Storefront V2 local checkout payload.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~AddressCorePhase0InventoryTests"` passed 22/22.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontV2HostSmokeTests"` passed 61/61.

Suggested commit:

```text
test(address-core): add checkout address baseline guardrails
```

## Phase 1 - Address Domain Model And Migration

Goal: add customer address persistence without changing checkout behavior yet.

Implementation checklist:

- [x] Add `CommerceCustomerAddress` domain entity under Commerce Node entities.
- [x] Add fields:
  - [x] `Id`.
  - [x] `PublicId`.
  - [x] `StoreId`.
  - [x] `CustomerId`.
  - [x] `FirstName`.
  - [x] `LastName`.
  - [x] `Company`.
  - [x] `Address1`.
  - [x] `Address2`.
  - [x] `City`.
  - [x] `PostalCode`.
  - [x] `CountryCode`.
  - [x] `StateProvinceCode`.
  - [x] `StateProvinceName`.
  - [x] `Phone`.
  - [x] `Email`.
  - [x] `IsDefaultShipping`.
  - [x] `IsDefaultBilling`.
  - [x] `CreatedAtUtc`.
  - [x] `UpdatedAtUtc`.
  - [x] `DeletedAtUtc`.
- [x] Add `DbSet<CommerceCustomerAddress>` to `CommerceNodeDbContext`.
- [x] Add EF mapping with explicit table name `commerce_customer_addresses`.
- [x] Add public ID uniqueness.
- [x] Add `(StoreId, CustomerId, DeletedAtUtc)` lookup index.
- [x] Add default shipping uniqueness with filtered index where supported, or document service-level enforcement.
- [x] Add default billing uniqueness with filtered index where supported, or document service-level enforcement.
- [x] Add `(StoreId, CustomerId, CountryCode)` index.
- [x] Add navigation collection from `CommerceCustomer` if it matches local patterns.
- [x] Add CommerceNode-only EF migration.
- [x] Do not change `checkout_sessions` or `orders` address snapshot columns.

Verification checklist:

- [x] Migration creates `commerce_customer_addresses`.
- [x] Address rows are store-scoped and customer-scoped.
- [x] Soft delete column exists.
- [x] Historical order/checkout tables are unchanged.
- [x] CommerceNode model tests pass.

Exit criteria:

- [x] Persistence exists and is additive.
- [x] No checkout behavior changed.
- [x] No legacy `AppDbContext` migration was generated.

Phase 1 evidence:

- 2026-07-17: Added `CommerceCustomerAddress`, `CommerceCustomer.Addresses`, `CommerceNodeDbContext.CommerceCustomerAddresses`, and EF mapping for `commerce_customer_addresses`.
- 2026-07-17: Generated CommerceNode-only migration `20260717001452_CommerceNodeCustomerAddresses`.
- 2026-07-17: Migration inspection confirms it creates only `commerce_customer_addresses` and drops only that table in `Down`.
- 2026-07-17: Added `CommerceNodeDbContextModelTests.CommerceCustomerAddress_HasStoreCustomerScopeAndDefaultIndexes`.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeDbContextModelTests|FullyQualifiedName~StorefrontCheckoutServiceTests"` passed 38/38.

Suggested commit:

```text
feat(address-core): add customer address model
```

## Phase 2 - Address Validation And Normalization Core

Goal: centralize address rules before exposing API endpoints.

Implementation checklist:

- [x] Add application DTOs for address create/update input.
- [x] Add application DTOs for address response/output.
- [x] Add stable validation issue DTO with code/message/field.
- [x] Add `IAddressValidationService`.
- [x] Implement provider-free validation:
  - [x] first name required.
  - [x] last name required.
  - [x] address line 1 required.
  - [x] city required.
  - [x] postal code required by default.
  - [x] country code must be two letters.
  - [x] state/province required only when country catalog says so.
  - [x] email optional but valid when present.
  - [x] phone optional with max length.
  - [x] max lengths align with API contract and DB mapping.
- [x] Implement normalization:
  - [x] trim whitespace.
  - [x] uppercase country codes.
  - [x] uppercase state/province codes.
  - [x] collapse empty optional fields to null.
  - [x] preserve user-entered casing for names/address lines.
- [x] Return stable validation codes rather than message-only failures.
- [x] Add tests for validation and normalization.

Verification checklist:

- [x] Missing required fields return stable codes.
- [x] Invalid country/state/email return stable codes.
- [x] Normalized output is deterministic.
- [x] No checkout code depends on UI-only validation.

Exit criteria:

- [x] Address validation can run independently from checkout.
- [x] Server-side validation owns the rules.

Phase 2 evidence:

- 2026-07-17: Added `CustomerAddressCreateRequest`, `CustomerAddressUpdateRequest`, `CustomerAddressDto`, `AddressValidationIssue`, `AddressValidationResult`, and `IAddressValidationService`.
- 2026-07-17: Added provider-free `AddressValidationService` with trim/uppercase/null-collapse normalization and stable issue codes.
- 2026-07-17: State/province requirement is currently backed by a small provider-free country list (`US`, `CA`, `AU`) until Phase 3 exposes lookup/config metadata.
- 2026-07-17: Registered `IAddressValidationService` in Application and CommerceNode infrastructure DI.
- 2026-07-17: Added `AddressValidationServiceTests`.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~AddressValidationServiceTests|FullyQualifiedName~CommerceNodeDbContextModelTests"` passed 27/27.

Suggested commit:

```text
feat(address-core): add address validation service
```

## Phase 3 - Country And State/Province Lookup

Goal: replace free-text country code UX with safe lookup data.

Implementation checklist:

- [ ] Add address lookup application service.
- [ ] Add country response DTO.
- [ ] Add state/province response DTO.
- [ ] Add address field configuration response DTO.
- [ ] Seed or static-load a small country catalog suitable for current stores.
- [ ] Include common development/default countries used by local tests.
- [ ] Add state/province list for countries where state is commonly required.
- [ ] Add Storefront API endpoints:
  - [ ] `GET /api/storefront/stores/{storeKey}/address/countries`.
  - [ ] `GET /api/storefront/stores/{storeKey}/address/countries/{countryCode}/states`.
  - [ ] `GET /api/storefront/stores/{storeKey}/address/configuration`.
- [ ] Keep lookup/config endpoints anonymous.
- [ ] Add OpenAPI metadata:
  - [ ] stable operation IDs.
  - [ ] summaries.
  - [ ] typed response schemas.
  - [ ] error response schemas.
  - [ ] no protected security requirement on lookup endpoints.
- [ ] Add OpenAPI contract tests and snapshot updates.

Verification checklist:

- [ ] Countries endpoint returns public-safe metadata.
- [ ] States endpoint returns public-safe metadata.
- [ ] Unknown country returns typed not-found or empty response consistently.
- [ ] Lookup endpoints do not expose provider secrets or internal settings.
- [ ] Storefront OpenAPI contract tests pass.

Exit criteria:

- [ ] Checkout UI can fetch country/state metadata from Commerce Node.
- [ ] Lookup API is generator-safe and anonymous.

Suggested commit:

```text
feat(storefront-api): add address lookup endpoints
```

## Phase 4 - Authenticated Address Book API

Goal: let customers manage saved addresses safely.

Implementation checklist:

- [ ] Add `IStorefrontCustomerAddressService`.
- [ ] Implement list current customer addresses.
- [ ] Implement create address.
- [ ] Implement update address.
- [ ] Implement soft delete address.
- [ ] Implement set default shipping.
- [ ] Implement set default billing.
- [ ] Resolve current `CommerceCustomer` by authenticated app user and store.
- [ ] Create/link `CommerceCustomer` from auth profile where possible when missing.
- [ ] Enforce ownership by `StoreId + CustomerId + PublicId`.
- [ ] Enforce one default shipping address per store/customer.
- [ ] Enforce one default billing address per store/customer.
- [ ] Soft-deleted addresses cannot be read, modified, defaulted, or selected.
- [ ] Add Storefront API routes under `api/storefront/stores/{storeKey}/customer/addresses`.
- [ ] Require `[Authorize]` for all address book endpoints.
- [ ] Publish Bearer security metadata in OpenAPI.
- [ ] Add contract tests proving request DTOs do not expose `customerId`, `storeId`, audit fields, or snapshot fields.

Verification checklist:

- [ ] Authenticated customer can create address.
- [ ] Authenticated customer can update own address.
- [ ] Authenticated customer can soft delete own address.
- [ ] Customer cannot access another customer's address by public ID.
- [ ] Default flags are deterministic.
- [ ] Protected endpoints declare Bearer security.
- [ ] Side-effecting operations are not GET.

Exit criteria:

- [ ] Address book CRUD is secure and store-scoped.
- [ ] Public contracts are explicit and generator-safe.

Suggested commit:

```text
feat(storefront-api): add customer address book
```

## Phase 5 - Checkout Address Selection

Goal: connect address book to checkout while preserving guest checkout.

Implementation checklist:

- [ ] Extend checkout preview request additively:
  - [ ] `ShippingAddressId`.
  - [ ] `BillingAddressId`.
  - [ ] `UseShippingAddressAsBillingAddress`.
- [ ] Keep existing `ShippingAddress` request object working.
- [ ] Resolve selected shipping address only for authenticated customers.
- [ ] Reject address ID selection for anonymous checkout.
- [ ] Reject address ID not owned by current store/customer.
- [ ] Reject deleted address ID.
- [ ] If direct `ShippingAddress` is provided, validate and use it.
- [ ] If both `ShippingAddressId` and direct `ShippingAddress` are provided, use the explicit authenticated ID or return a deterministic validation error according to implementation choice.
- [ ] Use billing same-as-shipping by default for MVP.
- [ ] Add billing snapshot fields only if billing behavior is actively used by checkout/order confirmation.
- [ ] Snapshot resolved shipping address into `CheckoutSession`.
- [ ] Snapshot resolved shipping address into `Order`.
- [ ] Update `StorefrontCheckoutService.ValidateCheckoutFields` to use `IAddressValidationService`.
- [ ] Keep payment method availability using resolved shipping country.
- [ ] Add checkout regression tests for guest direct address and authenticated address ID.

Verification checklist:

- [ ] Existing guest checkout still works.
- [ ] Authenticated customer can checkout using saved default shipping address.
- [ ] Invalid/deleted/other-customer address IDs are rejected.
- [ ] Checkout session snapshots resolved address.
- [ ] Order snapshots checkout address.
- [ ] Changing/deleting saved address after order placement does not change order history.

Exit criteria:

- [ ] Checkout supports saved address selection additively.
- [ ] Snapshot-safe order history remains intact.

Suggested commit:

```text
feat(checkout): support saved shipping address selection
```

## Phase 6 - Storefront V2 UI Integration

Goal: make the feature usable without overbuilding account UI.

Implementation checklist:

- [ ] Update Storefront V2 API client with address lookup methods.
- [ ] Update Storefront V2 API client with address book methods.
- [ ] Update checkout page to load country list from API.
- [ ] Update checkout page to show state selector when catalog has states.
- [ ] Keep direct entry fallback when no saved address exists.
- [ ] For authenticated customer, load saved addresses.
- [ ] For authenticated customer, preselect default shipping address.
- [ ] Allow manual/direct address entry fallback.
- [ ] Add minimal account address page or component only if active account pages are ready enough.
- [ ] Keep component boundaries friendly to future WASM migration.
- [ ] Do not embed address business rules in Razor components.
- [ ] Use API/configuration for field required/enabled behavior.

Verification checklist:

- [ ] Checkout renders country choices from API.
- [ ] Checkout renders state choices for state-aware countries.
- [ ] Authenticated customer can use a saved address in checkout.
- [ ] Direct entry still works.
- [ ] UI does not require a full account redesign.
- [ ] Storefront V2 host/static/API client tests pass.

Exit criteria:

- [ ] Address selection is usable in Storefront V2 checkout.
- [ ] UI remains additive and does not duplicate server rules.

Suggested commit:

```text
feat(storefront): add checkout address selection UI
```

## Phase 7 - Admin/Settings Preparation

Goal: prepare for store-level address configuration without building a full settings module.

Implementation checklist:

- [ ] Define address field configuration DTO contract.
- [ ] Return current defaults from `GET /address/configuration`.
- [ ] Document future typed settings:
  - [ ] company enabled.
  - [ ] phone required.
  - [ ] state required by country.
  - [ ] postal code required.
  - [ ] billing required.
- [ ] Avoid Control Plane/Admin UI until there is a concrete need.
- [ ] Ensure API shape can later support store overrides without breaking clients.

Verification checklist:

- [ ] Configuration response is explicit and generator-safe.
- [ ] Storefront can consume field metadata.
- [ ] No unnecessary admin UI or Control Plane runtime path is introduced.

Exit criteria:

- [ ] Future settings can be added without breaking API shape.
- [ ] Current implementation remains simple and store-safe.

Suggested commit:

```text
feat(storefront-api): expose address field configuration
```

## Phase 8 - QA And Regression Coverage

Goal: finish with focused verification and checklist updates.

Implementation checklist:

- [ ] Add application tests:
  - [ ] validation/normalization.
  - [ ] default shipping behavior.
  - [ ] default billing behavior.
  - [ ] soft delete behavior.
  - [ ] ownership isolation.
  - [ ] checkout address snapshot.
- [ ] Add API contract tests:
  - [ ] operation IDs.
  - [ ] summaries.
  - [ ] security metadata.
  - [ ] request body requirements.
  - [ ] validation metadata.
  - [ ] no unsafe fields.
  - [ ] response/error schemas.
  - [ ] side-effecting operations are not GET.
  - [ ] snapshots refreshed.
- [ ] Add Storefront V2 smoke/static tests:
  - [ ] country lookup.
  - [ ] guest checkout direct address.
  - [ ] authenticated checkout with saved address.
  - [ ] direct-entry fallback.
- [ ] Update QA checklist files:
  - [ ] `QA-CommerceNode.todo.md`.
  - [ ] `QA-StorefrontV2.todo.md`.
  - [ ] `QA-ControlPlane.todo.md` only for boundary evidence if relevant.
- [ ] Run focused tests.
- [ ] Run visible browser QA if UI changed and runtime is available.

Verification checklist:

- [ ] Address book works for authenticated customer.
- [ ] Guest checkout still works.
- [ ] Order history remains snapshot-safe.
- [ ] OpenAPI remains generator-safe.
- [ ] Active V2 projects touched by the phase build.

Exit criteria:

- [ ] QA checklist files contain evidence.
- [ ] No active V2 boundary rule is violated.
- [ ] Deferred scope remains deferred.

Suggested commit:

```text
test(address-core): complete release gate
```

## QA Checklist Seeds

### Commerce Node

- [ ] `CommerceCustomerAddress` is stored only in `CommerceNodeDbContext`.
- [ ] Address rows are store-scoped and customer-scoped.
- [ ] Address public IDs are used in API responses.
- [ ] Address book endpoints require Bearer auth.
- [ ] Lookup/config endpoints are anonymous and public-safe.
- [ ] Address request DTOs do not expose `customerId`, `storeId`, audit fields, or order snapshot fields.
- [ ] Customer cannot read/update/delete another customer's address.
- [ ] Soft-deleted address is excluded from active list and selection.
- [ ] Default shipping uniqueness is enforced.
- [ ] Default billing uniqueness is enforced.
- [ ] Address validation returns stable issue codes.
- [ ] Checkout with saved address snapshots address data.
- [ ] Guest direct checkout address still works.
- [ ] OpenAPI validates and snapshots pass.

### Storefront V2

- [ ] Checkout loads country list from Storefront API.
- [ ] Checkout shows state/province selector when country has states.
- [ ] Guest checkout direct address still works.
- [ ] Authenticated checkout preselects default shipping address.
- [ ] Authenticated customer can select saved address.
- [ ] Manual address fallback remains available.
- [ ] Browser request does not send `customerId`, `storeId`, price, audit fields, or order snapshot fields.
- [ ] No legacy `my-cart` or legacy checkout route dependency is introduced.
- [ ] Browser QA shows no unexpected console errors after address UI changes.

### Control Plane

- [ ] Control Plane is not part of customer address runtime path.
- [ ] ControlPlane Web does not call CommerceNode address APIs directly.
- [ ] No ControlPlane database table is added for customer address book.
- [ ] Any future settings gateway remains behind ControlPlane API, not Web-to-CommerceNode.

## Failure Modes To Design Against

- [ ] Customer can read or edit another customer's address by guessing address ID.
- [ ] Browser sends `customerId` and creates address under another account.
- [ ] Checkout references address row live instead of snapshotting it.
- [ ] Deleting an address changes historical order address.
- [ ] Multiple default shipping addresses exist for one customer/store.
- [ ] Multiple default billing addresses exist for one customer/store.
- [ ] Country/state lookup returns internal settings or secrets.
- [ ] Guest checkout breaks because `ShippingAddressId` becomes required.
- [ ] Billing address fields are added to order but not populated consistently.
- [ ] Store scope is taken from body instead of route.
- [ ] Address validation lives only in UI and can be bypassed.
- [ ] OpenAPI omits security metadata for protected address endpoints.

## Migration And Compatibility

- [ ] Database migration is additive only.
- [ ] Existing shipping columns on `checkout_sessions` are not removed or renamed.
- [ ] Existing shipping columns on `orders` are not removed or renamed.
- [ ] Existing checkout clients are not forced to send `ShippingAddressId`.
- [ ] Existing `ShippingAddress` request object keeps working.
- [ ] New address IDs exposed through API are public IDs.
- [ ] Existing order history is untouched.

## Recommended Implementation Order

- [ ] Phase 0 - baseline and contract guard.
- [ ] Phase 1 - address domain model and migration.
- [ ] Phase 2 - address validation and normalization core.
- [ ] Phase 3 - country and state/province lookup.
- [ ] Phase 4 - authenticated address book API.
- [ ] Phase 5 - checkout address selection.
- [ ] Phase 6 - Storefront V2 UI integration.
- [ ] Phase 7 - admin/settings preparation.
- [ ] Phase 8 - QA and regression coverage.

## Definition Of Done

- [ ] Authenticated customers can maintain a basic address book.
- [ ] Guest checkout can still enter address directly.
- [ ] Checkout can select a saved shipping address.
- [ ] Checkout/order continue to snapshot address fields.
- [ ] Storefront can render country/state choices without hard-coded `US` input.
- [ ] Address validation is centralized server-side.
- [ ] Protected address APIs use auth metadata and do not trust browser-supplied ownership.
- [ ] The implementation stays inside active V2 Commerce Node and Storefront V2 boundaries.
- [ ] No legacy presentation project or `AppDbContext` is extended.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Keep checkout/order address snapshots | Auto-decided | Preserve working architecture | Existing snapshot columns already protect order history and minimize migration risk. | Replacing snapshots with live address references |
| 2 | Scope | Add address book as Commerce Node customer data | Auto-decided | Data ownership | Customer ecommerce data belongs in `CommerceNodeDbContext`. | Control Plane storage, legacy `AppDbContext` |
| 3 | Security | Require auth for address book CRUD | Auto-decided | Boundary safety | Address book is customer private data and must derive ownership from claims. | Anonymous address CRUD, browser-supplied customer ID |
| 4 | Product | Keep direct guest checkout address | Auto-decided | Conversion and compatibility | Guest checkout already works and should not regress. | Forcing saved address for checkout |
| 5 | Scope | Defer external address verification | Auto-decided | Avoid unused complexity | Local validation is enough for MVP-to-real-use baseline. | Provider integration in first phase |
| 6 | UX | Add country/state lookup before full admin settings | Auto-decided | Practical usability | Replaces hard-coded country input without building a large settings subsystem. | Full country/state admin UI |

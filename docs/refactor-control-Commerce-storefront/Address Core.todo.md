# Address Core.todo

Generated: 2026-07-17

Source plan: `Address Core.md`

Status: In progress. Phase 3 completed.

Scope: add practical customer address book, country/state lookup, billing/shipping address support, and checkout address selection to active V2 without replacing the current checkout/order snapshot model.

## Scope Lock

Approved:

- [x] Customer address book persistence foundation.
- [x] Billing address and shipping address support.
- [x] Add, edit, delete customer addresses.
- [x] Default shipping address.
- [x] Default billing address.
- [x] Country to state/province lookup.
- [x] Basic field required/enabled configuration shape.
- [x] Address normalization and validation hook.
- [x] Snapshot selected checkout address into checkout session and order.
- [x] Keep guest checkout address entry working.

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

- [x] `CommerceCustomerAddress` entity gap closed. 2026-07-17 Phase 1: entity added under Commerce Node domain.
- [x] Customer address book table gap closed. 2026-07-17 Phase 1: `commerce_customer_addresses` migration added.
- [x] Storefront API for customer address CRUD/defaults gap closed. 2026-07-17 Phase 4: protected customer address book endpoints added.
- [ ] No billing address model.
- [x] Country/state/province catalog endpoint gap closed. 2026-07-17 Phase 3: resolved by Storefront address lookup endpoints.
- [x] Address field configuration endpoint gap closed. 2026-07-17 Phase 3: resolved by `GET /address/configuration`.
- [x] Reusable address validation service gap closed. 2026-07-17 Phase 2: `IAddressValidationService` added.
- [x] Safe authenticated address consumption route in Storefront V2 exists for checkout selection. 2026-07-17 Phase 6: Storefront V2 refreshes customer access token through the existing session resolver and calls protected address-book APIs through `StorefrontApiClient`.

## Core Decisions

- [x] Keep checkout/order snapshots as source of historical truth.
- [x] Add address book as Commerce Node customer data in `CommerceNodeDbContext`.
- [x] Require storefront customer auth for address book CRUD.
- [x] Derive customer identity from claims/auth context; never from browser-supplied `customerId`.
- [x] Support one default shipping and one default billing address per store/customer.
- [x] Start country/state lookup as static or seeded catalog, not full admin country management.
- [x] Start field configuration as DTO/config shape, not full settings UI.
- [x] Add provider-free `IAddressValidationService` before any external verification provider.

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
- [x] Customer identity comes from authenticated user claims or trusted auth result.
- [x] Browser requests do not include `customerId`, `storeId`, audit fields, or order-owned address snapshot fields.
- [x] Checkout may accept either a direct guest address or an authenticated `addressId`.
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

- [x] Add address lookup application service.
- [x] Add country response DTO.
- [x] Add state/province response DTO.
- [x] Add address field configuration response DTO.
- [x] Seed or static-load a small country catalog suitable for current stores.
- [x] Include common development/default countries used by local tests.
- [x] Add state/province list for countries where state is commonly required.
- [ ] Add Storefront API endpoints:
- [x] Add Storefront API endpoints:
  - [x] `GET /api/storefront/stores/{storeKey}/address/countries`.
  - [x] `GET /api/storefront/stores/{storeKey}/address/countries/{countryCode}/states`.
  - [x] `GET /api/storefront/stores/{storeKey}/address/configuration`.
- [x] Keep lookup/config endpoints anonymous.
- [x] Add OpenAPI metadata:
  - [x] stable operation IDs.
  - [x] summaries.
  - [x] typed response schemas.
  - [x] error response schemas.
  - [x] no protected security requirement on lookup endpoints.
- [x] Add OpenAPI contract tests and snapshot updates.

Verification checklist:

- [x] Countries endpoint returns public-safe metadata.
- [x] States endpoint returns public-safe metadata.
- [x] Unknown country returns typed not-found or empty response consistently.
- [x] Lookup endpoints do not expose provider secrets or internal settings.
- [x] Storefront OpenAPI contract tests pass.

Exit criteria:

- [x] Checkout UI can fetch country/state metadata from Commerce Node.
- [x] Lookup API is generator-safe and anonymous.

Phase 3 evidence:

- 2026-07-17: Added `IAddressLookupService` and provider-free static lookup/config implementation for `AU`, `CA`, `DE`, `FR`, `GB`, `US`, and `VN`.
- 2026-07-17: Added Storefront address lookup contracts and anonymous scoped endpoints under `api/storefront/stores/{storeKey}/address`.
- 2026-07-17: Added OpenAPI metadata and refreshed Storefront Swagger snapshots for `StorefrontAddress_ListCountries`, `StorefrontAddress_ListStates`, and `StorefrontAddress_GetConfiguration`.
- 2026-07-17: Added `AddressLookupServiceTests`.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~AddressLookupServiceTests|FullyQualifiedName~AddressValidationServiceTests|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"` passed 38/38.

Suggested commit:

```text
feat(storefront-api): add address lookup endpoints
```

## Phase 4 - Authenticated Address Book API

Goal: let customers manage saved addresses safely.

Implementation checklist:

- [x] Add `IStorefrontCustomerAddressService`.
- [x] Implement list current customer addresses.
- [x] Implement create address.
- [x] Implement update address.
- [x] Implement soft delete address.
- [x] Implement set default shipping.
- [x] Implement set default billing.
- [x] Resolve current `CommerceCustomer` by authenticated app user and store.
- [x] Create/link `CommerceCustomer` from auth profile where possible when missing.
- [x] Enforce ownership by `StoreId + CustomerId + PublicId`.
- [x] Enforce one default shipping address per store/customer.
- [x] Enforce one default billing address per store/customer.
- [x] Soft-deleted addresses cannot be read, modified, defaulted, or selected.
- [x] Add Storefront API routes under `api/storefront/stores/{storeKey}/customer/addresses`.
- [x] Require `[Authorize]` for all address book endpoints.
- [x] Publish Bearer security metadata in OpenAPI.
- [x] Add contract tests proving request DTOs do not expose `customerId`, `storeId`, audit fields, or snapshot fields.

Verification checklist:

- [x] Authenticated customer can create address.
- [x] Authenticated customer can update own address.
- [x] Authenticated customer can soft delete own address.
- [x] Customer cannot access another customer's address by public ID.
- [x] Default flags are deterministic.
- [x] Protected endpoints declare Bearer security.
- [x] Side-effecting operations are not GET.

Exit criteria:

- [x] Address book CRUD is secure and store-scoped.
- [x] Public contracts are explicit and generator-safe.

Phase 4 evidence:

- 2026-07-17: Added `IStorefrontCustomerAddressService` and `StorefrontCustomerAddressService`.
- 2026-07-17: Added protected Storefront endpoints under `api/storefront/stores/{storeKey}/customer/addresses` for list/create/update/delete/default-shipping/default-billing.
- 2026-07-17: Address book service resolves customer by authenticated app user/store, links by auth email when available, and does not accept `customerId` or `storeId` from request body.
- 2026-07-17: Added public `StorefrontCustomerAddressRequest`/`StorefrontCustomerAddressResponse` contracts and OpenAPI metadata/security.
- 2026-07-17: Added `StorefrontCustomerAddressServiceTests` for create/defaults, ownership isolation, soft delete, and default updates.
- 2026-07-17: Added Storefront OpenAPI contract coverage for address book request schema, Bearer security, response schemas, and unsafe-field exclusion.
- 2026-07-17: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontCustomerAddressServiceTests|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~AddressCorePhase0InventoryTests"` passed 38/38.

Suggested commit:

```text
feat(storefront-api): add customer address book
```

## Phase 5 - Checkout Address Selection

Goal: connect address book to checkout while preserving guest checkout.

Implementation checklist:

- [x] Extend checkout preview request additively:
  - [x] `ShippingAddressId`.
  - [x] `BillingAddressId`.
  - [x] `UseShippingAddressAsBillingAddress`.
- [x] Keep existing `ShippingAddress` request object working.
- [x] Resolve selected shipping address only for authenticated customers.
- [x] Reject address ID selection for anonymous checkout.
- [x] Reject address ID not owned by current store/customer.
- [x] Reject deleted address ID.
- [x] If direct `ShippingAddress` is provided, validate and use it.
- [x] If both `ShippingAddressId` and direct `ShippingAddress` are provided, use the explicit authenticated ID or return a deterministic validation error according to implementation choice.
- [x] Use billing same-as-shipping by default for MVP.
- [x] Add billing snapshot fields only if billing behavior is actively used by checkout/order confirmation.
- [x] Snapshot resolved shipping address into `CheckoutSession`.
- [x] Snapshot resolved shipping address into `Order`.
- [x] Update `StorefrontCheckoutService.ValidateCheckoutFields` to use `IAddressValidationService`.
- [x] Keep payment method availability using resolved shipping country.
- [x] Add checkout regression tests for guest direct address and authenticated address ID.

Verification checklist:

- [x] Existing guest checkout still works.
- [x] Authenticated customer can checkout using saved default shipping address.
- [x] Invalid/deleted/other-customer address IDs are rejected.
- [x] Checkout session snapshots resolved address.
- [x] Order snapshots checkout address.
- [x] Changing/deleting saved address after order placement does not change order history.

Exit criteria:

- [x] Checkout supports saved address selection additively.
- [x] Snapshot-safe order history remains intact.

Phase 5 evidence:

- 2026-07-17: Added additive checkout preview fields `ShippingAddressId`, `BillingAddressId`, and `UseShippingAddressAsBillingAddress`.
- 2026-07-17: Checkout preview resolves saved shipping address only through authenticated `CustomerAppUserId` and `StoreId + AppUserId + PublicId`.
- 2026-07-17: Direct guest `ShippingAddress` remains supported; `ShippingAddressId` takes precedence when both are supplied.
- 2026-07-17: Checkout service now uses `IAddressValidationService` for direct checkout shipping-address field validation.
- 2026-07-17: Added checkout regression tests for saved-address snapshot and anonymous saved-address rejection.
- 2026-07-17: Storefront OpenAPI contract test now asserts checkout preview exposes saved-address fields.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~AddressCorePhase0InventoryTests|FullyQualifiedName~StorefrontCustomerAddressServiceTests|FullyQualifiedName~StorefrontCheckoutServiceTests"` passed 57/57.

Suggested commit:

```text
feat(checkout): support saved shipping address selection
```

## Phase 6 - Storefront V2 UI Integration

Goal: make the feature usable without overbuilding account UI.

Implementation checklist:

- [x] Update Storefront V2 API client with address lookup methods.
- [x] Update Storefront V2 API client with address book methods.
- [x] Update checkout page to load country list from API.
- [x] Update checkout page to show state selector when catalog has states.
- [x] Keep direct entry fallback when no saved address exists.
- [x] For authenticated customer, load saved addresses.
- [x] For authenticated customer, preselect default shipping address.
- [x] Allow manual/direct address entry fallback.
- [x] Add minimal account address page or component only if active account pages are ready enough.
- [x] Keep component boundaries friendly to future WASM migration.
- [x] Do not embed address business rules in Razor components.
- [x] Use API/configuration for field required/enabled behavior.

Verification checklist:

- [x] Checkout renders country choices from API.
- [x] Checkout renders state choices for state-aware countries.
- [x] Authenticated customer can use a saved address in checkout.
- [x] Direct entry still works.
- [x] UI does not require a full account redesign.
- [x] Storefront V2 host/static/API client tests pass.

Exit criteria:

- [x] Address selection is usable in Storefront V2 checkout.
- [x] UI remains additive and does not duplicate server rules.

Phase 6 evidence:

- 2026-07-17: Storefront V2 `StorefrontApiClient` now consumes anonymous address country/state/config lookup endpoints and protected customer address-book CRUD/default endpoints.
- 2026-07-17: Storefront session resolution keeps the refreshed customer access token inside server-side session info so checkout can call protected address-book APIs without exposing browser-supplied ownership fields.
- 2026-07-17: Checkout page loads address metadata from API, renders country select/state select when metadata exists, and falls back to direct manual entry when lookup/address-book calls are unavailable.
- 2026-07-17: Authenticated checkout loads saved addresses, preselects default shipping when available, and uses a small JS toggle to disable manual fields when a saved address is selected.
- 2026-07-17: No standalone account address page was added because current active account pages are auth-facing only; checkout consumption is the stable minimal UI for this phase.
- 2026-07-17: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2ApiClientTests|FullyQualifiedName~AddressCorePhase0InventoryTests|FullyQualifiedName~StorefrontBrandingMarkupTests"` passed 34/34.
- 2026-07-17: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2HostSmokeTests"` passed 34/34.

Suggested commit:

```text
feat(storefront): add checkout address selection UI
```

## Phase 7 - Admin/Settings Preparation

Goal: prepare for store-level address configuration without building a full settings module.

Implementation checklist:

- [x] Define address field configuration DTO contract.
- [x] Return current defaults from `GET /address/configuration`.
- [x] Document future typed settings:
  - [x] company enabled.
  - [x] phone required.
  - [x] state required by country.
  - [x] postal code required.
  - [x] billing required.
- [x] Avoid Control Plane/Admin UI until there is a concrete need.
- [x] Ensure API shape can later support store overrides without breaking clients.

Verification checklist:

- [x] Configuration response is explicit and generator-safe.
- [x] Storefront can consume field metadata.
- [x] No unnecessary admin UI or Control Plane runtime path is introduced.

Exit criteria:

- [x] Future settings can be added without breaking API shape.
- [x] Current implementation remains simple and store-safe.

Phase 7 evidence:

- 2026-07-17: `StorefrontAddressFieldConfigurationResponse` already exposes the stable future override shape: company enablement, phone enable/required, postal-code required, billing enabled, same-as-shipping default, max lengths, and state-required country codes.
- 2026-07-17: `GET /api/storefront/stores/{storeKey}/address/configuration` remains anonymous, Storefront-scoped, and backed by address lookup configuration defaults.
- 2026-07-17: No Control Plane address settings UI or gateway path was introduced; admin settings remain deferred until a concrete operational need exists.
- 2026-07-17: Added `AddressCorePhase7ConfigurationTests` to guard the configuration contract shape, Storefront route ownership, and no unnecessary Control Plane Web dependency.

Suggested commit:

```text
feat(storefront-api): expose address field configuration
```

## Phase 8 - QA And Regression Coverage

Goal: finish with focused verification and checklist updates.

Implementation checklist:

- [x] Add application tests:
  - [x] validation/normalization.
  - [x] default shipping behavior.
  - [x] default billing behavior.
  - [x] soft delete behavior.
  - [x] ownership isolation.
  - [x] checkout address snapshot.
- [x] Add API contract tests:
  - [x] operation IDs.
  - [x] summaries.
  - [x] security metadata.
  - [x] request body requirements.
  - [x] validation metadata.
  - [x] no unsafe fields.
  - [x] response/error schemas.
  - [x] side-effecting operations are not GET.
  - [x] snapshots refreshed.
- [x] Add Storefront V2 smoke/static tests:
  - [x] country lookup.
  - [x] guest checkout direct address.
  - [x] authenticated checkout with saved address.
  - [x] direct-entry fallback.
- [x] Update QA checklist files:
  - [x] `QA-CommerceNode.todo.md`.
  - [x] `QA-StorefrontV2.todo.md`.
  - [x] `QA-ControlPlane.todo.md` only for boundary evidence if relevant.
- [x] Run focused tests.
- [ ] Run visible browser QA if UI changed and runtime is available.

Verification checklist:

- [x] Address book works for authenticated customer.
- [x] Guest checkout still works.
- [x] Order history remains snapshot-safe.
- [x] OpenAPI remains generator-safe.
- [x] Active V2 projects touched by the phase build.

Exit criteria:

- [x] QA checklist files contain evidence.
- [x] No active V2 boundary rule is violated.
- [x] Deferred scope remains deferred.

Phase 8 evidence:

- 2026-07-17: Address Core release gate passed with `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~AddressValidationServiceTests|FullyQualifiedName~AddressLookupServiceTests|FullyQualifiedName~StorefrontCustomerAddressServiceTests|FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontV2ApiClientTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontV2HostSmokeTests|FullyQualifiedName~AddressCorePhase0InventoryTests|FullyQualifiedName~AddressCorePhase7ConfigurationTests"`: 134/134 passed.
- 2026-07-17: Storefront V2 and CommerceNode API focused builds passed during Phase 6/7 verification.
- 2026-07-17: Visible browser QA remains pending for a seeded authenticated saved-address selection fixture; automated host smoke and static/client coverage protect the code path until that fixture is available.

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

- [x] Database migration is additive only.
- [x] Existing shipping columns on `checkout_sessions` are not removed or renamed.
- [x] Existing shipping columns on `orders` are not removed or renamed.
- [x] Existing checkout clients are not forced to send `ShippingAddressId`.
- [x] Existing `ShippingAddress` request object keeps working.
- [x] New address IDs exposed through API are public IDs.
- [x] Existing order history is untouched.

## Recommended Implementation Order

- [x] Phase 0 - baseline and contract guard.
- [x] Phase 1 - address domain model and migration.
- [x] Phase 2 - address validation and normalization core.
- [x] Phase 3 - country and state/province lookup.
- [x] Phase 4 - authenticated address book API.
- [x] Phase 5 - checkout address selection.
- [x] Phase 6 - Storefront V2 UI integration.
- [x] Phase 7 - admin/settings preparation.
- [x] Phase 8 - QA and regression coverage.

## Definition Of Done

- [x] Authenticated customers can maintain a basic address book.
- [x] Guest checkout can still enter address directly.
- [x] Checkout can select a saved shipping address.
- [x] Checkout/order continue to snapshot address fields.
- [x] Storefront can render country/state choices without hard-coded `US` input.
- [x] Address validation is centralized server-side.
- [x] Protected address APIs use auth metadata and do not trust browser-supplied ownership.
- [x] The implementation stays inside active V2 Commerce Node and Storefront V2 boundaries.
- [x] No legacy presentation project or `AppDbContext` is extended.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Keep checkout/order address snapshots | Auto-decided | Preserve working architecture | Existing snapshot columns already protect order history and minimize migration risk. | Replacing snapshots with live address references |
| 2 | Scope | Add address book as Commerce Node customer data | Auto-decided | Data ownership | Customer ecommerce data belongs in `CommerceNodeDbContext`. | Control Plane storage, legacy `AppDbContext` |
| 3 | Security | Require auth for address book CRUD | Auto-decided | Boundary safety | Address book is customer private data and must derive ownership from claims. | Anonymous address CRUD, browser-supplied customer ID |
| 4 | Product | Keep direct guest checkout address | Auto-decided | Conversion and compatibility | Guest checkout already works and should not regress. | Forcing saved address for checkout |
| 5 | Scope | Defer external address verification | Auto-decided | Avoid unused complexity | Local validation is enough for MVP-to-real-use baseline. | Provider integration in first phase |
| 6 | UX | Add country/state lookup before full admin settings | Auto-decided | Practical usability | Replaces hard-coded country input without building a large settings subsystem. | Full country/state admin UI |

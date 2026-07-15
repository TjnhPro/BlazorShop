# BlazorShop Currency Autoplan

Generated: 2026-07-15

Scope: 5.3 Currency

Requested features:

- Primary/base currency.
- Working currency cua customer.
- Currency selector.
- Exchange rates.
- Exchange-rate provider abstraction.
- Auto update exchange rate.
- Currency formatting va culture.
- Currency rounding.
- Round unit price, line total va order total theo rule.

This plan is based on the active V2 codebase and must preserve these boundaries:

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API

Storefront.V2
  -> CommerceNode.API api/storefront/stores/{storeKey}/*
```

All currency runtime data belongs to `CommerceNodeDbContext`. Do not add V2 currency schema or behavior to legacy `AppDbContext`.

Autoplan note: multi-agent explorer review was used for independent engineering and CEO/DX checks. The completed reviews agreed that V2 is currently single-currency with currency-aware display/snapshots, not true multi-currency. They recommended a stricter single-currency hardening phase before opening selector/exchange-rate behavior, and they confirmed that schema/core plus public projection must come before customer-facing selector UI. The plan also applies the autoplan review lens: CEO, Design, Eng, DX, completeness, avoid boil-the-ocean scope, pragmatic value, DRY, explicit over clever, and bias toward action.

## 1. Premise Challenge

Multi-currency should not start with exchange-rate providers. The current system is a single-currency storefront with partial currency fields. If exchange rates are added before rounding, minor-unit conversion, and server-side working-currency validation, checkout and payment can disagree about the amount charged.

The correct first problem is money correctness:

1. Define the store base currency without breaking existing `DefaultCurrencyCode`.
2. Centralize currency validation, formatting metadata, rounding, and payment minor-unit conversion.
3. Stop trusting client-supplied cart currency as authoritative.
4. Only then add customer working currency, selector UI, exchange rates, and auto updates.

## 2. What Already Exists

| Area | Current codebase evidence | Meaning |
| --- | --- | --- |
| Store base currency field | `CommerceStore.DefaultCurrencyCode` and `DefaultCulture` exist. | Use this as current primary/base currency source. Do not create a duplicate base currency field first. |
| Store validation | `CommerceStoreService` validates 3-letter default currency code. | Reuse validation pattern, but expand into a real currency service. |
| Public configuration | `StorefrontCurrencyOptionsResponse` already exists. Mapping currently returns `[store.DefaultCurrencyCode]`. | Public projection is ready to expose supported currencies later without a new endpoint. |
| Storefront display context | `StorefrontDisplayContextProvider` normalizes store currency and culture. | Keep this path for current single-currency formatting. |
| Price formatter | `StorefrontPriceFormatter` formats `"USD 1,234.00"` with culture. | Replace/extend with currency metadata and decimal digits. |
| Cart snapshot | Cart lines store `UnitPriceSnapshot` and `CurrencyCodeSnapshot`. | Useful for order consistency, but currency currently comes from client request. |
| Checkout currency | Checkout resolves currency from cart line snapshot or store default. | Needs server-side working-currency resolver and validation. |
| Orders/payment attempts | `Order.CurrencyCode`, `Order.TotalAmount`, `PaymentAttempt.CurrencyCode`, and `PaymentAttempt.Amount` exist. | Currency is already persisted on transaction records. |
| Payment availability | `StorePaymentMethod.SupportedCurrencyCodesJson`, min/max order total, and country filters exist. | Reuse for supported payment methods by currency. |
| Stripe minor units | Stripe provider currently multiplies amount by 100. | Correct for USD/EUR, unsafe for zero-decimal and special currencies. |
| Exchange rates | No exchange-rate entity/service/provider found. | New schema/service is required, but should be later phase. |

## 3. Dream State Delta

Target state:

- Every store has one base currency.
- A store can expose an allowlisted set of supported display/checkout currencies.
- Storefront can resolve a customer working currency from a trusted source.
- Product base prices remain stable and can be converted for display/checkout.
- Cart, checkout, order, payment attempt, and provider session use the same rounded money totals.
- Exchange rates are auditable and never silently guessed.
- Auto update runs through existing Commerce Node task patterns, not a new worker by default.
- Public configuration exposes only safe currency metadata and supported codes.

Non-goals:

- Do not rename or remove `CommerceStore.DefaultCurrencyCode` in the first phase.
- Do not add V2 currency schema to `AppDbContext`.
- Do not let Storefront V2 call Control Plane.
- Do not let ControlPlane.Web call CommerceNode directly.
- Do not add a broad provider/plugin framework just for exchange rates.
- Do not implement product price lists per currency until exchange-rate conversion proves insufficient.
- Do not add currency fields to `Product` or `ProductVariant` in the MVP.
- Do not add a customer-facing currency dropdown before server-side working-currency validation exists.
- Do not change historic order currency/amounts when store base currency changes.
- Do not trust client-supplied `CurrencyCode` without server validation.
- Do not mix legacy Stripe/payment paths into active V2 Storefront payment work.

## 4. Core Decisions

| ID | Decision | Reason |
| --- | --- | --- |
| D1 | Treat `CommerceStore.DefaultCurrencyCode` as the current base currency. | Existing store profile, public config, deployment, and manager code already use it. |
| D2 | Add currency core before exchange rates. | Rounding/minor-unit mistakes are more damaging than missing provider automation. |
| D3 | Store supported currencies per store in Commerce Node. | Currency availability is node-local storefront runtime behavior. |
| D4 | Working currency must be server-resolved. | Client request can suggest, but server must validate against store supported currencies. |
| D5 | Product catalog prices remain base-currency values for MVP. | Avoid broad product schema churn and preserve current catalog/order behavior. |
| D6 | Orders and payment attempts store final currency, totals, and later rate snapshot. | Historic records must remain stable when rates change. |
| D7 | Exchange-rate provider abstraction is minimal and late. | Avoid building a provider framework before manual rates and rounding are correct. |
| D8 | Auto update uses `commerce_task`/existing task worker pattern first. | Matches current Commerce Node async architecture. |

## 5. Target Architecture

```text
CommerceStore
  DefaultCurrencyCode       current base currency
  DefaultCulture            default formatting culture

CommerceNodeDbContext
  StoreCurrency             supported currencies and rounding metadata
  StoreCurrencyExchangeRate manual/provider rates
  CommerceTask              exchange-rate update tasks, later phase

Storefront public config
  CurrencyOptions
    defaultCurrencyCode
    supportedCurrencyCodes
    display metadata

Storefront request flow
  cookie/customer preference/query suggestion
    -> StoreCurrencyResolver
      -> supported currency validation
        -> conversion + rounding
          -> cart/checkout/order/payment snapshots
```

Admin path:

```text
ControlPlane.Web
  -> ControlPlane.API currency gateway
      -> CommerceNode.API api/commerce/admin/currencies?storeKey={storeKey}
```

Public path:

```text
Storefront.V2
  -> api/storefront/stores/{storeKey}/configuration
  -> api/storefront/stores/{storeKey}/cart/*
  -> api/storefront/stores/{storeKey}/checkout/*
```

## 6. Proposed Data Model

### StoreCurrency

Purpose: store-level supported currency metadata and rounding rules.

Suggested fields:

```text
StoreCurrency
  Id
  StoreId
  CurrencyCode
  IsEnabled
  IsDefaultDisplayCurrency
  DisplayOrder
  CultureName
  Symbol
  DecimalDigits
  UnitPriceRoundingMode
  UnitPriceRoundingIncrement
  LineTotalRoundingMode
  LineTotalRoundingIncrement
  OrderTotalRoundingMode
  OrderTotalRoundingIncrement
  CreatedAtUtc
  UpdatedAtUtc
```

Rules:

- Unique index: `(StoreId, CurrencyCode)`.
- `CurrencyCode` is upper-case ISO-like 3-letter code.
- `DecimalDigits` is explicit because provider minor units must not assume 2.
- The store base currency must always be enabled.
- Store base currency should come from `CommerceStore.DefaultCurrencyCode`, not a second editable base flag.

### StoreCurrencyExchangeRate

Purpose: auditable conversion from store base currency to a target currency.

Suggested fields:

```text
StoreCurrencyExchangeRate
  Id
  StoreId
  BaseCurrencyCode
  TargetCurrencyCode
  Rate
  ProviderKey
  Source
  EffectiveAtUtc
  ExpiresAtUtc
  IsManual
  CreatedAtUtc
  UpdatedAtUtc
```

Rules:

- Unique active rate should be resolved by `(StoreId, BaseCurrencyCode, TargetCurrencyCode)` and effective time.
- Rate must be positive.
- Same-currency rate is treated as `1`, no row required.
- Checkout/order should snapshot rate metadata later when conversion is used.

### Money Services

Suggested services:

```text
ICurrencyMetadataService
IStoreCurrencyService
IStoreCurrencyResolver
IMoneyRoundingService
IMoneyConversionService
IPaymentMinorUnitConverter
IExchangeRateProvider
IExchangeRateUpdateService
```

Keep interfaces narrow. Do not create a generic settings/provider framework.

## 7. Phase Plan

### Phase 0 - Baseline Inventory And Guardrails

Goal: freeze the current single-currency behavior and identify unsafe assumptions before schema changes.

Tasks:

- Add tests covering current `StorefrontCurrencyOptionsResponse` shape.
- Add tests proving public configuration does not expose internal exchange-provider settings.
- Add tests documenting that public configuration currently returns only `[DefaultCurrencyCode]`.
- Add cart/checkout tests that expose current client-supplied currency behavior.
- Add tests for Stripe minor-unit conversion assumption before changing it.
- Inventory all direct money math:
  - cart line total.
  - checkout subtotal/grand total.
  - order total.
  - payment attempt amount.
  - Stripe minor units.
  - Control Plane order display.

Exit criteria:

- Currency risk points are documented with tests or TODO assertions.
- No schema change.
- No public contract break.

### Phase 1 - Single-Currency Authority Hardening

Goal: make the current one-currency-per-store behavior correct before adding supported currencies or exchange rates.

Tasks:

- Treat `CommerceStore.DefaultCurrencyCode` as the only authoritative checkout currency for current stores.
- Change cart add/update behavior so client `CurrencyCode` is only a hint, not authority.
- Normalize cart line `CurrencyCodeSnapshot` from the current store default unless the later working-currency resolver is active.
- Update cart validation so the returned currency is the store default when no approved working currency exists.
- Update checkout preview/place-order so unsupported or mismatched cart line currency cannot drive payment currency.
- Add admin warning/documentation: changing store default currency affects new cart/checkout flows only; it must not reprice existing carts, orders, payment attempts, or historic reports.
- Add tests:
  - client sends `EUR` for a `USD` store and cart/checkout remains `USD`.
  - mixed cart line snapshots cannot force checkout into another currency.
  - order and payment attempt currency match store default in single-currency mode.
- Do not add Storefront currency selector in this phase.
- Do not add exchange-rate schema in this phase.

Exit criteria:

- Single-currency stores have one server-owned currency from cart to payment.
- Client cannot spoof checkout/payment currency.
- Existing public configuration remains compatible.

### Phase 2 - Currency Metadata And Store Supported Currencies

Goal: introduce store-supported currency metadata without enabling conversion yet.

Tasks:

- Add `StoreCurrency` entity/table to `CommerceNodeDbContext`.
- Seed or create a default `StoreCurrency` row from `CommerceStore.DefaultCurrencyCode` for each store.
- Add service to resolve enabled currencies for a store.
- Enforce that base/default currency is always present and enabled.
- Add Commerce Node admin APIs:
  - list store currencies.
  - enable/disable supported currency.
  - update display order.
  - update culture/symbol/decimal digits/rounding metadata.
- Add Control Plane API gateway later only if manager UI is included in this phase.
- Invalidate `store-public-config:{storeKey}` when supported currencies change.
- Keep public `CurrencyOptions.DefaultCurrencyCode` unchanged.
- Expand `CurrencyOptions.SupportedCurrencyCodes` from real `StoreCurrency` rows.

Exit criteria:

- Store can have an allowlisted set of supported currencies.
- Public configuration exposes supported currency codes safely.
- Checkout still charges only base currency until working-currency and conversion phases are complete.

Sequencing note: this phase may include Commerce Node admin APIs and public configuration projection, but it must not add a customer-facing selector that changes checkout currency yet.

### Phase 3 - Central Money Rounding And Minor Units

Goal: make money math deterministic before conversion.

Tasks:

- Add `MoneyAmount` or equivalent application-level value object if it reduces repeated `(decimal, currencyCode)` handling.
- Add `IMoneyRoundingService` with separate methods:
  - round unit price.
  - round line total.
  - round order total.
  - round payment amount.
- Add `IPaymentMinorUnitConverter` using currency decimal digits instead of `amount * 100`.
- Replace Stripe `ToMinorUnits(amount)` with converter that accepts currency code and metadata.
- Apply rounding to:
  - Storefront cart validation totals.
  - Storefront checkout preview line totals.
  - Storefront place-order totals.
  - payment attempt amount.
- Keep existing output DTOs additive and compatible.

Exit criteria:

- Unit price, line total, order total, and payment amount use one rounding policy.
- Zero-decimal currencies are testable even before multi-currency display is enabled.
- Stripe conversion no longer assumes 2 decimal places.

### Phase 4 - Server-Side Working Currency Resolver

Goal: stop treating client-supplied currency as authoritative.

Tasks:

- Add `IStorefrontWorkingCurrencyResolver`.
- Resolve working currency in this order:
  - authenticated customer preference, if field exists or is added later.
  - signed/validated storefront cookie.
  - query/form selection only after server validation.
  - store default currency.
- Add Storefront route/API behavior for setting currency preference:
  - command method, not side-effecting `GET`.
  - validates against enabled `StoreCurrency`.
  - writes a safe cookie or customer preference.
- Change cart add/update path:
  - client may send currency as a hint.
  - server validates or ignores unsupported hints.
  - server writes normalized `CurrencyCodeSnapshot`.
- Checkout must reject mixed unsupported currency states or normalize them through resolver.
- Add tests for unsupported currency spoofing.

Exit criteria:

- Cart and checkout currency is server-approved.
- Unsupported currency cannot be persisted by client request.
- Existing single-currency stores behave the same.

### Phase 5 - Exchange Rates Manual MVP

Goal: support deterministic conversion without external provider automation.

Tasks:

- Add `StoreCurrencyExchangeRate` table.
- Add admin API for manual rates:
  - list rates.
  - upsert rate.
  - disable/expire rate.
- Resolve conversion:
  - base currency to same currency returns 1.
  - base to enabled target currency requires active positive rate.
  - missing/stale rate returns clear validation/configuration error.
- Add `IMoneyConversionService`.
- Add conversion tests for:
  - base to target.
  - target unavailable.
  - stale rate.
  - rounding after conversion.
- Update public configuration with safe rate freshness metadata only if useful:
  - no provider credentials.
  - no internal provider config.

Exit criteria:

- Admin can configure manual exchange rates.
- Storefront can convert display amounts through a service.
- Checkout cannot proceed with missing/stale rate for non-base currency.

### Phase 6 - Storefront Currency Selector And Display Conversion

Goal: expose working currency to customers after server-side correctness exists.

Tasks:

- Add currency selector UI in Storefront layout/header.
- Read supported currencies from public configuration.
- Post currency selection through a command endpoint or Storefront handler that validates server-side.
- Display prices in working currency:
  - product cards.
  - product detail.
  - cart.
  - checkout.
- Keep base price fields unchanged in existing product DTOs unless a contract phase approves additive display-price DTOs.
- Add display metadata:
  - currency code.
  - symbol.
  - decimal digits.
  - culture.
- Avoid converting order history amounts after order placement; show stored order currency.

Exit criteria:

- Customer can select only supported currencies.
- UI display, cart, checkout, and payment agree on currency and totals.
- Existing stores with one supported currency show no confusing selector.

Implementation note 2026-07-15:

- Storefront V2 now renders a currency selector in the header only when public configuration exposes multiple supported currencies.
- Selector changes post to Storefront V2 `/currency`, which calls the scoped Commerce Node `currency/preference` command and writes a Storefront-local `bs-currency` cookie only when the server accepts the requested currency.
- Storefront catalog/product requests can send optional `currencyCode`; Commerce Node returns additive `displayPrice`, `displayComparePrice`, and `displayCurrencyCode` fields while preserving base `price` and `comparePrice`.
- Storefront product cards/details prefer additive display money fields, and cart/checkout display cart line `CurrencyCodeSnapshot`.
- Automated verification passed for Storefront API client currency query/preference command, display-context cookie support, Storefront focused formatter/markup tests, full Storefront OpenAPI contract tests, and solution build. Visible browser QA for the full selector -> cart -> checkout path remains in `QA-StorefrontV2.todo.md`.

### Phase 7 - Checkout, Order, And Payment Rate Snapshots

Goal: make converted orders auditable.

Tasks:

- Add order/payment snapshot fields only if conversion is enabled:
  - base currency code.
  - working/order currency code.
  - exchange rate used.
  - rate provider/source.
  - rate timestamp.
  - base subtotal/total if needed for admin reconciliation.
- Ensure `Order.TotalAmount` remains the charged order currency amount.
- Add `OrderLine` snapshot fields only if needed:
  - base unit price.
  - converted unit price.
  - line total.
  - currency code.
- Decide whether to add `OrderLine.LineTotal` as persisted value to avoid recalculating historic line totals from changed rounding rules.
- Update admin order display to show currency and rate snapshot safely.

Exit criteria:

- Historic orders are stable even after rates or rounding settings change.
- Payment attempt and order totals match exactly.
- Admin can reconcile base and working currency when conversion is used.

Implementation note 2026-07-15:

- Cart lines now snapshot converted unit price plus base unit price, base currency, exchange rate, provider key, source, and rate effective/expiry timestamps when a non-base currency conversion is used.
- Checkout preview/place-order resolve a single cart rate snapshot and reject mixed or missing exchange-rate metadata instead of creating an order that cannot be audited.
- Checkout sessions, orders, payment attempts, and order lines now persist nullable conversion snapshot fields. `Order.TotalAmount` and `PaymentAttempt.Amount` remain the charged working-currency amounts.
- `OrderLine.LineTotal` is persisted for CommerceNode orders so historical totals do not change when rounding rules evolve.
- Control Plane order detail shows base total/rate/provider/source/effective timestamp and base line totals only when conversion metadata exists.
- Legacy `AppDbContext` ignores the new V2 order snapshot fields to avoid changing the legacy schema surface.
- Automated verification passed for focused cart, checkout, payment attempt, and CommerceNode model tests. Visible admin/browser reconciliation remains in QA checklists.

### Phase 8 - Exchange-Rate Provider Abstraction

Goal: add provider integration after manual rates prove the domain model.

Tasks:

- Add minimal `IExchangeRateProvider`:
  - `ProviderKey`.
  - fetch rates for base and target currencies.
  - return timestamp/source metadata.
- Add provider config through the existing settings/provider secret boundary:
  - no public secrets.
  - no raw provider config in public DTOs.
  - admin APIs return status/masked metadata only.
- Start with one provider plus a manual provider.
- Add provider failure handling:
  - timeout.
  - invalid response.
  - missing currency.
  - stale source.
- Do not add generic provider framework unless more provider families need it.

Exit criteria:

- Exchange rates can be fetched from a provider into the same rate table.
- Provider secrets are not exposed.
- Manual override remains possible.

Implementation note 2026-07-15:

- Added a minimal `IExchangeRateProvider` abstraction with safe status metadata and provider fetch results.
- Added two providers: `manual` for existing manual upsert behavior and `configuration` for server-side configured rates. The configuration provider is disabled by default and reads only server configuration, not public DTOs.
- Added Commerce Admin APIs to list configured providers and fetch provider rates into the existing `store_currency_exchange_rates` table.
- Provider fetch validates enabled target currencies, missing rates, invalid currency/rate data, expired rates, and stale configured rates before persisting.
- Provider-fetched rows use their provider key and `IsManual=false`; existing manual override rows remain possible through the manual upsert endpoint.
- Swagger metadata now includes stable operation IDs, response DTOs, required request body metadata, and error responses for the new provider endpoints.
- Automated verification passed for configuration provider fetch/status/stale handling, existing manual exchange-rate behavior, and Storefront OpenAPI contract tests.

### Phase 9 - Auto Update Exchange Rates

Goal: schedule safe rate refresh without adding a new worker architecture.

Tasks:

- Use existing `CommerceTaskWorker` and `commerce_task` pattern for update jobs.
- Add task type, for example `currency.exchange-rate.update`.
- Add admin command to queue update for a store.
- Add scheduled/periodic trigger only if the current task infrastructure supports it cleanly.
- Add retry/backoff and failure audit.
- Never update rates silently without recording source/time.
- Invalidate public config and relevant currency caches after successful update.

Exit criteria:

- Rates can update asynchronously.
- Failures are visible to admin.
- Storefront never uses stale rates without policy approval.

### Phase 10 - Control Plane Manager Integration

Goal: expose currency management through the correct admin boundary.

Tasks:

- Add Control Plane API gateway endpoints for currency admin APIs.
- Use granular settings/provider permissions already introduced by Configuration and Feature State Core.
- Add Control Plane Web UI sections:
  - base currency display.
  - supported currencies.
  - rounding rules.
  - manual exchange rates.
  - provider status.
  - rate update action/status.
- ControlPlane.Web must call ControlPlane.API only.
- Show validation errors from Commerce Node clearly.

Exit criteria:

- Admin can manage currencies without browser access to node credentials.
- UI cannot expose provider secrets.
- Closed/maintenance stores can still be managed.

Sequencing note: Control Plane manager UI can be implemented earlier for admin setup if the API surface exists, but it must keep the required path `ControlPlane.Web -> ControlPlane.API -> CommerceNode.API` and must not imply that Storefront multi-currency checkout is enabled before Phases 3-6 are complete.

### Phase 11 - QA, Contract Tests, And Release Gate

Goal: finish with proof across money, API, UI, and task behavior.

Tasks:

- Update:
  - `QA-ControlPlane.todo.md`.
  - `QA-CommerceNode.todo.md`.
  - `QA-CommerceNode-TaskOrchestration.todo.md` if auto update lands.
  - `QA-StorefrontV2.todo.md`.
- Add tests for:
  - supported currency resolution.
  - public configuration currency projection.
  - currency selector validation.
  - unsupported client currency spoofing.
  - rounding unit price, line total, order total.
  - Stripe/minor-unit conversion for 2-decimal and zero-decimal currencies.
  - manual exchange rate conversion.
  - stale/missing exchange rate failure.
  - payment provider supported-currency availability.
  - checkout/order/payment exact total match.
  - OpenAPI operation metadata and schemas.
- Use Playwright for Storefront selector and checkout display behavior once UI changes.

Exit criteria:

- Single-currency behavior remains stable.
- Multi-currency path is covered by automated tests.
- API contracts are generator-safe.
- No active V2 boundary rule is violated.

## 8. Test Diagram

```text
Currency metadata
  -> StoreCurrencyService tests
  -> CommerceNodeDbContext model tests
  -> public configuration contract tests

Rounding
  -> MoneyRoundingService unit tests
  -> cart validation integration tests
  -> checkout preview/place-order tests
  -> payment attempt tests
  -> Stripe minor-unit converter tests

Working currency
  -> resolver unit tests
  -> cart spoofing tests
  -> Storefront selector browser tests

Exchange rates
  -> manual rate service tests
  -> conversion tests
  -> stale/missing rate tests
  -> provider adapter contract tests

Auto update
  -> commerce task handler tests
  -> retry/failure tests
  -> cache invalidation tests

Admin/API
  -> Control Plane gateway tests
  -> Commerce Node admin contract tests
  -> permission tests
```

## 9. Failure Modes Registry

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Payment amount differs from checkout display | Customers can be charged wrong amount. | Central rounding and minor-unit conversion before exchange rates. |
| Zero-decimal currencies charge 100x wrong amount | Stripe currently assumes `amount * 100`. | Currency decimal digits and `IPaymentMinorUnitConverter`. |
| Client spoofs currency | Cart currently accepts currency hints. | Server-side working currency resolver and supported-currency validation. |
| Store base currency changes after orders exist | Historic orders become confusing. | Keep historic currency snapshots; require explicit admin warning for base currency change. |
| Missing/stale exchange rate | Checkout may use wrong conversion. | Reject non-base checkout when rate is missing/stale unless policy explicitly allows. |
| Rounding drift between line/order/payment | Totals do not reconcile. | Round at defined stages and persist final totals. |
| Payment min/max totals compare wrong currency | Provider availability becomes inaccurate. | Evaluate provider availability against final working/order currency amount. |
| Public config leaks provider settings | Exchange providers may have secrets. | Allowlist DTOs; reuse secret boundary rules. |
| Overbuilt provider system | Slows delivery and adds maintenance cost. | Manual rates first, minimal provider interface later. |
| Direct Control Plane Web to Commerce Node | Breaks boundary and exposes node details. | Gateway through ControlPlane.API only. |
| Legacy schema churn | Shared entities can affect `AppDbContext`. | CommerceNode migrations only; avoid changing shared CLR nullability unnecessarily. |

## 10. Alternatives Considered

### Alternative A - Add Exchange Provider First

Rejected.

Provider automation does not solve rounding, minor units, or server-side currency validation. It would make incorrect money math easier to automate.

### Alternative B - Rename `DefaultCurrencyCode` To `BaseCurrencyCode`

Rejected for first implementation.

The field is already used across store creation, public configuration, Storefront display, deployment, and tests. Keep the field and document it as base currency. A later contract migration can add an alias if needed.

### Alternative C - Product Prices Per Currency Immediately

Deferred.

The current catalog stores one `Product.Price` and variant price. Adding per-currency price lists is larger than required for MVP. Start with base price plus exchange-rate conversion.

### Alternative D - Store Working Currency Only In Browser State

Rejected.

Browser-only currency state is not enough for checkout/payment. Server must validate and persist the final currency snapshot.

### Alternative E - Generic Provider Framework

Deferred.

Exchange-rate provider abstraction should be narrow. A generic provider/module framework is only justified after multiple unrelated provider families need the same lifecycle.

## 11. Review Scorecards

### CEO Review

Score: 8/10.

This plan prioritizes correctness where money leaves the system. Explorer review pushed the plan to harden current single-currency behavior before multi-currency, which lowers product and payment risk.

### Design Review

Score: 7/10.

Currency selector UI is intentionally delayed until server rules exist. The eventual UI should hide the selector for single-currency stores and avoid showing converted prices that cannot be checked out.

### Engineering Review

Score: 8/10.

The plan respects Commerce Node ownership, Storefront route scope, and Control Plane gateway boundaries. Independent engineering review confirmed the largest risks are client currency spoofing, label-only conversion without amount conversion, and Stripe minor-unit assumptions. Main engineering risk is touching cart/checkout/payment totals; phases isolate that behind tests before UI.

### DX Review

Score: 7/10.

Typed services and explicit DTOs add work, but they make rounding, conversion, and API contracts reviewable. Error messages must say which currency/rate is missing and how admin can fix it.

## 12. Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | CEO | Keep `DefaultCurrencyCode` as base currency for MVP. | Auto-decided | DRY/pragmatic | Existing code already depends on it. | New duplicate base field. |
| 2 | CEO/DX | Add single-currency authority hardening before supported currencies. | Auto-decided | Pragmatic/completeness | Existing cart can carry client-supplied currency snapshots. | Jump directly to selector or FX. |
| 3 | Eng | Build rounding/minor-unit core before exchange rates. | Auto-decided | Completeness | Prevents payment/checkout drift. | Provider-first implementation. |
| 4 | Eng | Server validates working currency. | Auto-decided | Explicit over clever | Client currency hints are not authoritative. | Browser-only selector state. |
| 5 | Eng | Manual rates before provider automation. | Auto-decided | Pragmatic | Gives deterministic behavior and tests before external integration. | Immediate provider framework. |
| 6 | Design | Selector appears only for stores with multiple enabled currencies. | Auto-decided | Simpler UX | Avoids confusing single-currency stores. | Always-visible selector. |
| 7 | DX | Error messages must include missing currency/rate and admin fix path. | Auto-decided | Developer/operator clarity | Operators need actionable failures. | Generic checkout error. |

## 13. Recommended Implementation Order

1. Phase 0 - baseline and guardrails.
2. Phase 1 - single-currency authority hardening.
3. Phase 2 - store supported currencies.
4. Phase 3 - rounding and minor-unit conversion.
5. Phase 4 - server-side working currency resolver.
6. Phase 5 - manual exchange rates.
7. Phase 6 - Storefront selector and display conversion.
8. Phase 7 - order/payment rate snapshots.
9. Phase 8 - exchange-rate provider abstraction.
10. Phase 9 - auto update through `commerce_task`.
11. Phase 10 - Control Plane manager integration.
12. Phase 11 - QA and release gate.

## 14. Definition Of Done

Currency work is done when:

- Store base currency is stable and documented.
- Store supported currencies are managed in Commerce Node.
- Storefront public configuration exposes safe currency options.
- Working currency is server-resolved and cannot be spoofed.
- Currency selector works only for enabled currencies.
- Unit price, line total, order total, payment amount, and provider minor units use central rounding rules.
- Exchange rates are auditable and fail clearly when missing/stale.
- Auto update uses existing Commerce Node task orchestration if enabled.
- Orders and payment attempts have stable currency/rate snapshots.
- Contract, unit, integration, and browser tests cover the full flow.
- QA checklists are updated.

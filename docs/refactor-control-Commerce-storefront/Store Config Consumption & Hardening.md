# Store Config Consumption & Hardening Autoplan

Generated: 2026-07-15
Branch: improve-codebase-architecture
Scope: consume and harden existing store lifecycle/config fields after the store lifecycle work already landed.

## 1. Goal

Finish the store-scoped configuration work without redoing the lifecycle foundation.

This phase should make the Storefront and admin/runtime code actually use the store profile fields that already exist:

- `DefaultCulture`
- `DefaultCurrencyCode`
- `LogoUrl`
- favicon/icon URLs
- company information
- support contact information
- maintenance/unavailable state
- store-scoped home metadata through existing store/page/SEO concepts where possible
- Control Plane management UI/API for editing those runtime store config fields

The phase must not add speculative `DefaultLanguage` or `DefaultTheme` database fields.

## 2. Current State Verified In Code

| Area | Current state | Plan impact |
| --- | --- | --- |
| Store runtime model | `CommerceStore` already contains branding, company/contact, default currency/culture, maintenance, icon URLs, and metadata JSON. | Do not add a second config table for the same fields. Consume and harden the existing model first. |
| Store lifecycle | `active`, `provisioning`, `disabled`, and `archived` exist for runtime stores and Control Plane registry constraints. | Do not rebuild lifecycle. Keep public flows gated through current-store resolution. |
| Current store API | `api/storefront/stores/{storeKey}/store/current` returns store profile/config fields through explicit Storefront DTOs. | Storefront V2 should depend on this API instead of duplicating config. |
| Store resolver | Public resolution still uses active-store queries; readiness resolution is explicit. | Do not loosen catalog/cart/order queries to read inactive stores. |
| Maintenance UX | Storefront V2 has `/maintenance` and middleware redirects HTML traffic there for closed/not-ready/maintenance stores. | Keep this behavior and only improve config consumption. |
| Storefront UI | Header reads current store name/domain, but logo is not used. `App.razor` hard-codes favicon and `lang="en"`. Footer hard-codes `BLAZORSHOP` and generic text. | Wire UI to current store config. |
| Storefront pricing | Product/cart/checkout UI still hard-codes `EUR`, `€`, and invariant numeric display. Cart/checkout services still have internal `USD` fallback paths. | Add a display context and currency formatter, then remove hard-coded public currency display. |
| SEO/structured data | Structured data reads current store for some company fields, but still prefers SEO logo and singleton SEO settings in places. | Prefer current store profile where it is the runtime source. |
| Storefront pages | `StorefrontPage` is already store-scoped and has SEO metadata fields. | Use this for legal/info pages and optionally home metadata, instead of adding another generic config blob. |
| Commerce admin store API | Store CRUD/lifecycle endpoints exist, but Storefront Swagger metadata is stronger than Commerce Admin store metadata. | Any changed admin/config endpoint must meet API contract standards. |
| Control Plane manager | `ControlPlane.Web` already has store manager runtime-store flows and `ControlPlaneStoreClient` DTOs include several runtime config fields. | Keep Control Plane as the admin surface, but route changes through Control Plane API to Commerce Node. Do not duplicate runtime config in Control Plane DB. |

## 3. Architecture Decisions

### 3.1 Source of truth

Use `CommerceStore` as the runtime source for public store profile/config:

```text
CommerceStore
  -> Storefront current-store API
      -> Storefront display context
          -> header, footer, head icons, price formatting, SEO structured data

ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API
          -> CommerceStore
```

Do not make Storefront V2 read Control Plane APIs or Control Plane DB data.

Do not make Control Plane Web call Commerce Node directly. Control Plane Web is the management UI only; Control Plane API owns node credentials and gateway calls.

### 3.2 Default language

Decision: do not add `DefaultLanguage`.

Use `DefaultCulture` as the stored source and derive language at runtime:

```text
DefaultCulture = en-US -> LanguageCode = en
DefaultCulture = vi-VN -> LanguageCode = vi
```

Implementation should introduce a thin display context interface because the Storefront already needs culture/currency/branding data in several components:

```csharp
public interface IStorefrontDisplayContextProvider
{
    Task<StorefrontDisplayContext> GetAsync(CancellationToken cancellationToken = default);
}
```

The context should be explicit and small:

```csharp
public sealed record StorefrontDisplayContext(
    string StoreKey,
    string StoreName,
    string CultureName,
    string LanguageCode,
    string CurrencyCode,
    string? LogoUrl,
    string? FaviconUrl,
    string? PngIconUrl,
    string? AppleTouchIconUrl,
    string? MsTileImageUrl,
    string? MsTileColor,
    string? CompanyName,
    string? CompanyEmail,
    string? CompanyPhone,
    string? CompanyAddress,
    string? SupportEmail,
    string? SupportPhone);
```

### 3.3 Default theme

Decision: do not implement theme now.

Reason:

- There is no theme registry.
- There is no CSS token pipeline.
- There is no runtime theme asset loading boundary.
- Adding `DefaultTheme` now would create a field with no real behavior.

Future-compatible rule:

- If theme is added later, use a typed/allowlisted `ThemeKey`, not arbitrary CSS or JSON.
- Keep this out of this phase except for documentation.

### 3.4 Store-specific override

Do not add generic key-value overrides.

Typed fields already exist for the current blast radius. If a reset feature is added later, reset by section:

- branding
- contact/company
- locale/currency
- SEO/home metadata

Do not implement a broad "reset all settings" command in this phase.

## 4. Non-Goals

- Do not add `DefaultLanguage` to the database or public API.
- Do not add `DefaultTheme` to the database or public API.
- Do not add a generic settings override table.
- Do not add V2 features to legacy `BlazorShop.Presentation/*`.
- Do not add config fields to `AppDbContext`.
- Do not make Storefront V2 call Control Plane.
- Do not make Control Plane Web call Commerce Node directly.
- Do not duplicate runtime store config into `ControlPlaneDbContext`.
- Do not bypass current-store middleware for catalog/cart/checkout pages.
- Do not change public store resolution semantics.
- Do not remove existing singleton `SeoSettings` in this phase.

## 5. Phase Plan

### Phase 0 - Baseline and contract guardrails

Goal: lock the phase to "consume and harden existing config", not expand the data model.

Tasks:

- Confirm the latest store lifecycle commits are present:
  - `Implement store lifecycle controls`
  - `docs: document store resolution hardening`
  - `fix: normalize storefront public urls`
  - `feat: guard storefront requests with current store`
- Confirm `CommerceStore` already has all fields needed for this phase.
- Confirm `StorefrontCurrentStoreResponse` exposes the public-safe subset.
- Confirm `StorefrontApiClient.StorefrontCurrentStore` matches the API response.
- Confirm no new DB migration is needed for default language/theme.
- Record any field missing from current-store response before starting UI work.

Files to inspect:

- `BlazorShop.Domain/Entities/CommerceNode/CommerceStore.cs`
- `BlazorShop.Application/CommerceNode/Stores/CommerceStoreDtos.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontApiContracts.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontContractMappings.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`

Exit criteria:

- No schema change is required for this phase start.
- Storefront current-store contract remains generator-safe.
- Plan implementation can proceed without guessing about default language/theme.

Suggested commit:

```text
docs: plan store config consumption hardening
```

### Phase 1 - Storefront display context foundation

Goal: create one small Storefront-facing abstraction for current store display settings.

Tasks:

- Add `StorefrontDisplayContext` and `IStorefrontDisplayContextProvider` under Storefront V2 services/contracts.
- Implement provider by wrapping `IStorefrontCurrentStoreProvider`.
- Derive:
  - `CultureName` from `DefaultCulture`, fallback `en-US`.
  - `LanguageCode` from `CultureInfo.GetCultureInfo(DefaultCulture).TwoLetterISOLanguageName`, fallback `en`.
  - `CurrencyCode` from `DefaultCurrencyCode`, fallback `USD`.
- Keep the provider request-scoped through existing `StorefrontCurrentStoreProvider` request cache.
- Add a small `IStorefrontPriceFormatter` or equivalent helper that formats amounts by display context.
- Do not introduce a global static culture mutation for the whole process.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs`

Tests:

- Provider returns fallback context when current store is unavailable.
- Provider derives `LanguageCode` correctly from `en-US`, `vi-VN`, and invalid cultures.
- Price formatter uses currency code and culture without throwing on unknown/invalid config.

Exit criteria:

- Storefront components can consume one provider instead of calling `GetCurrentStoreAsync()` independently for display config.
- No extra Commerce Node API call is added per component beyond existing request cache behavior.

Suggested commit:

```text
feat: add storefront display context provider
```

### Phase 2 - Branding, head icons, and document language

Goal: make visible Storefront branding use current store config.

Tasks:

- Update `StorefrontHeader`:
  - show `LogoUrl` when present.
  - fallback to text brand using store name.
  - keep layout stable when logo loads or fails.
- Add a head/branding component that renders from `StorefrontDisplayContext`:
  - favicon
  - PNG icon
  - Apple touch icon
  - MS tile image/color
- Replace hard-coded favicon in `App.razor` only when a dynamic head strategy is verified.
- Apply document language from `LanguageCode`.
  - Preferred: set root `html lang` from display context if `App.razor` can safely resolve it server-side.
  - Fallback: set `document.documentElement.lang` after current store resolves, and keep static `lang="en"` as a safe SSR default.
- Keep asset URL behavior aligned with existing public URL resolver rules.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/*` or new display/head component
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js` only if root `lang` requires JS fallback

Tests:

- Header renders logo when configured.
- Header falls back to store name when logo is absent.
- Head output includes configured favicon/icon links.
- Invalid/missing store config does not break page rendering.

QA checklist:

- Storefront home with logo.
- Storefront home without logo.
- Favicon request no longer depends only on hard-coded `icon-192.png`.
- Maintenance page still renders for disabled/provisioning stores.

Exit criteria:

- Storefront no longer presents generic `BLAZORSHOP` branding in the header when current store config is available.
- Browser head contains store-specific icon metadata where configured.
- Document language is at least updated client-side and does not regress SSR rendering.

Suggested commit:

```text
feat: consume store branding in storefront shell
```

### Phase 3 - Currency and culture consumption

Goal: remove hard-coded public price currency display and align cart/checkout snapshots with the current store.

Tasks:

- Replace hard-coded public `EUR` and `€` display in:
  - `ProductCard.razor`
  - `ProductPage.razor`
  - `CartPage.razor`
  - `CheckoutPage.razor`
- Use `IStorefrontPriceFormatter` for display labels.
- Keep invariant formatting only for API query strings and data attributes that require machine-readable decimal values.
- Add `CurrencyCode` to Storefront V2 cart line creation client request model if missing.
- Pass current store `CurrencyCode` when Storefront adds a cart line.
- Harden `StorefrontCartService`:
  - prefer cart line currency snapshot.
  - fallback to current store default currency instead of static `USD` when possible.
- Harden `StorefrontCheckoutService`:
  - when cart/session currency is missing, resolve store default currency by `StoreId`.
  - keep `USD` only as final defensive fallback.
- Ensure currency codes are normalized to uppercase 3-letter values.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CartPage.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CartPage.razor.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CheckoutPage.razor.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`
- `BlazorShop.Application/CommerceNode/Carts/StorefrontCartService.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCheckoutService.cs`

Tests:

- Product price displays with `EUR` when store default currency is `EUR`.
- Product price displays with `USD` when store default currency is `USD`.
- Add-to-cart sends currency code.
- Cart validation falls back to store default currency.
- Checkout preview/place-order fall back to store default currency when snapshots are missing.

Exit criteria:

- No user-visible Storefront V2 price text hard-codes `EUR`, `€`, or `USD`.
- Machine-readable decimal values still use invariant formatting.
- Cart and checkout currency behavior is store-scoped.

Suggested commit:

```text
feat: use store currency and culture in storefront
```

### Phase 4 - Footer, company/contact, and structured data hardening

Goal: make public store identity consistent across visible UI and JSON-LD.

Tasks:

- Update footer to use display context:
  - store name/company name
  - support email/phone
  - company address when present
  - fallback text only when config is absent
- Keep legal links backed by existing `StorefrontRoutes` and `StorefrontPage`.
- Update structured data:
  - prefer `CommerceStore.LogoUrl` for organization logo when present.
  - prefer store company/contact fields over singleton SEO settings.
  - include address only when enough fields exist to avoid malformed schema.
- Avoid making `SeoSettings` another editable store profile source.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontStructuredDataComposer.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSeoComposer.cs` only if metadata fallback needs current store identity.

Tests:

- Footer renders configured store support email/phone.
- Footer hides empty contact rows.
- JSON-LD organization uses store company/contact data before singleton SEO data.
- Empty company config still produces valid JSON-LD or no JSON-LD for that field.

Exit criteria:

- Public storefront no longer shows generic company/store identity when current store config exists.
- Structured data uses the same source of truth as visible Storefront UI.

Suggested commit:

```text
feat: render store contact profile in storefront
```

### Phase 5 - Store config validation and admin contract hardening

Goal: harden the config inputs that are now visible in public Storefront UI.

Tasks:

- Extend `CommerceStoreService` validation:
  - `LogoUrl`
  - `FaviconUrl`
  - `PngIconUrl`
  - `AppleTouchIconUrl`
  - `MsTileImageUrl`
  - `CdnHost`
  - `MsTileColor`
- Accept either:
  - absolute `http`/`https` URLs, or
  - safe root-relative public paths for assets.
- Reject unsafe schemes such as `javascript:`, `data:`, and malformed host values.
- Validate `MsTileColor` as a hex color or documented safe named value.
- Add or improve OpenAPI metadata for changed Commerce Admin store endpoints:
  - stable operation IDs
  - summaries
  - request body required
  - response schema
  - standard error schema
  - node credential/header metadata
- Keep Storefront current-store public response explicit and safe.

Files likely touched:

- `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceStoreService.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceStoresController.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs`
- contract tests under `BlazorShop.Tests`

Tests:

- Valid absolute asset URLs pass.
- Valid root-relative asset paths pass.
- Unsafe schemes fail validation.
- Invalid tile color fails validation.
- Commerce Admin store endpoints have response schemas and error responses in Swagger.
- Protected admin endpoints include node credential metadata.

Exit criteria:

- Publicly rendered store config cannot inject unsafe URLs.
- Any changed API satisfies `docs/architecture/09-api-contract-standards.md`.

Suggested commit:

```text
fix: harden store profile validation and contracts
```

### Phase 6 - Control Plane config management surface

Goal: let operators edit the runtime store config from Control Plane without breaking runtime ownership.

Tasks:

- Audit existing Control Plane store manager support for runtime store config fields:
  - branding: `LogoUrl`, favicon/icon URLs, `MsTileColor`
  - locale: `DefaultCulture`, `DefaultCurrencyCode`
  - company/contact: company name/email/phone/address, support email/phone
  - maintenance: enabled/message
  - base URL and CDN host
- Fill gaps in `ControlPlane.API` gateway calls to Commerce Node store endpoints.
- Fill gaps in `ControlPlane.Web` store manager form:
  - group fields into clear sections: Identity, Branding, Locale, Contact, Availability, Advanced URLs.
  - show validation messages returned by Commerce Node.
  - show a safe preview of logo/favicon URLs without requiring public storefront access.
- Keep `StoreRegistry` focused on platform registry/deployment state.
- Do not copy these runtime config fields into `StoreRegistry`.
- Reuse existing `ControlPlaneStoreClient` and store manager patterns where possible.
- If a new Control Plane endpoint is added or changed, apply API contract standards:
  - operationId
  - summary
  - explicit request/response DTOs
  - standard error schema
  - platform auth/security metadata

Files likely touched:

- `BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneCommerceCatalogService.cs`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Controllers/*`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Stores/ControlPlaneStoreClient.cs`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Stores.razor`
- `BlazorShop.Application/ControlPlane/*` if gateway DTOs need extension

Tests:

- Control Plane API sends runtime config updates to Commerce Node.
- Control Plane Web can edit and refresh runtime config.
- Commerce Node validation errors are shown in Control Plane Web.
- Closed/disabled store can still be edited from Control Plane.
- Control Plane Web does not require Commerce Node credentials in browser-side code.

Exit criteria:

- Store config can be managed from Control Plane.
- Runtime source of truth remains `CommerceStore`.
- No direct ControlPlane.Web -> CommerceNode.API call is introduced.

Suggested commit:

```text
feat: manage store config from control plane
```

### Phase 7 - Home page metadata through existing store-scoped content

Goal: support store-scoped home metadata without adding another config storage model.

Decision: use existing `StorefrontPage` SEO fields first.

Tasks:

- Reserve a documented home metadata slug, recommended `home`.
- Update `Home.razor` SEO composition:
  - try to load published Storefront page slug `home`.
  - use its SEO fields for `/` metadata when present.
  - do not render `/pages/home` content on `/` in this phase unless explicitly selected later.
  - fallback to existing static home metadata and singleton SEO settings.
- Ensure sitemap behavior does not create duplicate canonical confusion:
  - `/` remains home canonical.
  - `/pages/home` should either be hidden from sitemap by admin setting, or documented as not recommended when used only for home metadata.
- Do not add `HomeMetadataJson` to `CommerceStore`.

Files likely touched:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Home.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSeoComposer.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSitemapService.cs` only if duplicate sitemap behavior must be guarded.
- docs for Storefront page slug conventions.

Tests:

- Home page uses `home` page SEO metadata when published for the current store.
- Home page falls back when `home` page is absent or unpublished.
- Store A home metadata does not leak into Store B.
- Sitemap/canonical output remains stable.

Exit criteria:

- Store-scoped home metadata exists using current store-scoped content infrastructure.
- No new generic config blob is introduced.

Suggested commit:

```text
feat: use store-scoped page metadata for home
```

### Phase 8 - QA checklist and documentation

Goal: lock the new behavior into the repo's QA and architecture docs.

Tasks:

- Update `QA-StorefrontV2.todo.md`:
  - logo render
  - favicon/head icons
  - footer contact/company
  - currency/culture display
  - home metadata fallback
  - disabled/provisioning/maintenance still blocked
- Update `QA-CommerceNode.todo.md`:
  - store profile validation
  - current-store contract
  - admin store OpenAPI metadata
  - unsafe URL rejection
- Update `QA-ControlPlane.todo.md`:
  - runtime store config edit from Control Plane
  - validation error display
  - disabled/provisioning store remains manageable
  - browser does not receive node credentials
- Update architecture docs:
  - current store display context rule
  - `DefaultCulture` derives language
  - no `DefaultTheme` until a real theme system exists
  - future theme should be `ThemeKey` allowlist
- Add a short docs note for home metadata slug convention.

Verification:

- Run focused .NET tests for Storefront/Commerce Node store config behavior.
- Run contract tests for changed Swagger docs.
- Run visible-browser QA only after implementation changes, not during plan creation.

Exit criteria:

- QA files describe the real user-visible checks.
- Architecture docs prevent future agents from adding speculative `DefaultLanguage` or generic theme settings.

Suggested commit:

```text
docs: document store config consumption rules
```

## 6. Test Matrix

| Codepath | Unit/integration coverage | Browser QA |
| --- | --- | --- |
| Display context fallback | Unit test provider invalid/missing current store | Storefront still renders when API unavailable if middleware allows page |
| Language derivation | Unit test culture to language | Inspect `<html lang>` or JS-applied document language |
| Header logo | Component/render test if available | Home page with and without logo |
| Head icons | Render/head test or snapshot | Browser network/head inspection |
| Price formatting | Unit test formatter and page model helpers | Product, cart, checkout visible price labels |
| Cart currency snapshot | Service test `AddLineAsync` and cart response | Add product to cart and inspect cart line currency |
| Checkout currency fallback | Service test missing snapshot/session currency | Checkout preview/order currency matches store |
| Footer contact/company | Component/render test if available | Storefront footer visible data |
| Structured data | Unit test JSON-LD composer | Inspect rendered JSON-LD on home/product |
| Asset URL validation | Service validation tests | Admin rejects unsafe values |
| Commerce Admin OpenAPI contract | Swagger contract tests | Swagger UI sanity check |
| Control Plane config gateway | API/controller tests | Manager UI edits runtime config |
| Home metadata | Page/SEO composer tests | Home title/meta/canonical visible |

## 7. Risk Controls

| Risk | Control |
| --- | --- |
| Extra API calls per component | Use request-scoped display context provider backed by existing current-store provider cache. |
| Invalid store config breaks public pages | Provider and formatter must use defensive fallbacks. |
| Unsafe URL injection through logo/favicon | Validate URLs before saving store profile fields. |
| Currency display diverges from cart/order currency | Send store currency into cart line snapshots and use store fallback in cart/checkout services. |
| Default language becomes duplicate state | Derive language from `DefaultCulture`; do not add `DefaultLanguage`. |
| Theme scope expands prematurely | Document future `ThemeKey` only; do not implement theme runtime. |
| Home metadata duplicates SEO settings | Use `StorefrontPage` SEO fields and fallback; do not add another config blob. |
| Control Plane duplicates runtime config | Keep `StoreRegistry` as platform registry only; write runtime config through gateway to Commerce Node. |
| API contract drift | Apply `09-api-contract-standards.md` to every changed endpoint and add contract tests. |

## 8. Recommended Implementation Order

1. Phase 1 - display context provider.
2. Phase 2 - branding/head icons/document language.
3. Phase 3 - currency/culture consumption.
4. Phase 4 - footer/company/contact/structured data.
5. Phase 5 - validation and admin contract hardening.
6. Phase 6 - Control Plane config management surface.
7. Phase 7 - home metadata via store-scoped page.
8. Phase 8 - QA and docs.

Reasoning:

- The display context provider removes duplication first.
- Branding and footer then become simple consumers.
- Currency touches deeper service behavior, so it deserves its own phase and tests.
- Validation and OpenAPI hardening should happen before relying on the fields publicly.
- Control Plane management comes after validation so operators see the same safe rules the runtime enforces.
- Home metadata is last because it touches SEO/canonical behavior and has higher regression risk.

## 9. Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Scope | Consume existing `CommerceStore` config before adding new storage. | Mechanical | DRY | The fields already exist and current-store API exposes them. | New config table or generic override store. |
| 2 | Language | Derive language from `DefaultCulture`. | Mechanical | Explicit over clever | Culture is already stored and validated; language alone is not a current business feature. | New `DefaultLanguage` field. |
| 3 | Theme | Defer theme runtime and document future `ThemeKey`. | Taste | Pragmatic | No existing theme system exists, so an interface would be speculative. | `DefaultTheme` DB/API field in this phase. |
| 4 | Display context | Add a thin provider interface because multiple current Storefront consumers need the same settings. | Mechanical | DRY | Header, footer, head icons, formatter, and SEO need the same current-store data. | Each component calling `GetCurrentStoreAsync()` directly. |
| 5 | Home metadata | Reuse store-scoped `StorefrontPage` SEO fields before adding a new model. | Taste | DRY | The project already has store-scoped page metadata and admin/page API flows. | `HomeMetadataJson` or generic store metadata contract. |
| 6 | Control Plane | Manage runtime config from Control Plane through gateway calls. | Mechanical | Boundary correctness | Operators need a management surface, but runtime config belongs to Commerce Node. | Storefront editing config directly or duplicating config in `StoreRegistry`. |

## 10. Definition of Done

- Storefront visible brand, footer, icons, currency, and culture reflect current store config.
- Storefront still blocks disabled/provisioning/maintenance stores through existing guard behavior.
- Cart and checkout currency fallbacks are store-scoped.
- Unsafe asset URLs cannot be saved into public store profile fields.
- Changed API endpoints pass contract standards.
- Control Plane can edit runtime store config through the correct API boundary.
- QA checklists include browser-visible validation for the new behavior.
- No `DefaultLanguage`, `DefaultTheme`, or generic override table is added in this phase.

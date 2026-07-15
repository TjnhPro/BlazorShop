# BlazorShop Configuration va Feature State Core Autoplan

Generated: 2026-07-15

Scope:

- 4.1 Settings behavior.
- 4.2 Feature activation.
- 4.3 Public configuration projection.

This plan is based on the active V2 codebase, architecture docs, and the existing Store lifecycle, Store mapping, and Store Config Consumption plans. It intentionally avoids extending legacy presentation projects or `AppDbContext`.

Autoplan note: external dual-voice subagents were not available in this Codex runtime, so the CEO, Design, Eng, and DX review passes are recorded as an internal autoplan audit using the six autoplan decision principles: completeness, avoid boil-the-ocean scope, pragmatic value, DRY, explicit over clever, and bias toward action.

## 1. Premise Challenge

The requested feature set is valid, but it should not become a generic settings framework too early.

Configuration and feature state should solve three concrete V2 problems first:

1. Storefront needs a public-safe configuration projection that cannot leak secrets or internal settings.
2. Commerce Node needs typed, store-scoped settings for domains that are currently singleton or not store-aware.
3. Control Plane needs explicit permissions and admin APIs so settings, features, and providers are not managed through broad store write permission forever.

The plan should not duplicate fields already owned by `CommerceStore`. Store identity, branding, company/contact, currency, culture, and maintenance state already belong on `CommerceStore` and are covered by Store lifecycle and Store Config Consumption. New settings work should reference those fields through a projection, not move them into a second settings store.

## 2. Existing Code Leverage Map

| Area | Current codebase fact | Decision |
| --- | --- | --- |
| Store profile | `CommerceStore` owns logo, company/contact, default currency, default culture, maintenance, and metadata. | Keep as runtime store profile. Do not duplicate in generic settings. |
| Store cache | `CommerceStoreDomainResolver` and `CommerceStoreService` already cache and invalidate store lookup/profile keys. | Reuse the cache pattern and add invalidation for new settings domains. |
| Admin settings | `AdminSettings` is a singleton and includes global defaults plus SMTP fields. | Treat as compatibility/admin singleton. Do not extend it for store-specific configuration. |
| SEO settings | `SeoSettings` is currently singleton. Storefront has a public SEO settings endpoint. | Add store-aware SEO defaults later through typed store settings or explicit override table. |
| Payment provider state | `StorePaymentMethod` already has `StoreId`, `Enabled`, `DisplayOrder`, and `SettingsJson`. | Use as the first concrete provider activation model. Harden secrets and DTOs before broadening. |
| Public storefront store config | `StorefrontCurrentStoreResponse` maps safe store fields and omits `MetadataJson`. | Preserve existing endpoint and add a broader public configuration projection. |
| Public payment metadata | `StorefrontPaymentMethodResponse` exposes only safe method metadata. | Keep this contract safe and reuse in the public projection. |
| Permissions | `ControlPlanePermissions` has broad store read/write permissions. | Add granular settings, feature, and provider permissions before expanding admin surfaces. |
| Storefront route scope | Storefront APIs use `api/storefront/stores/{storeKey}/*`. | All public configuration APIs must remain store-key scoped by route. |
| Commerce admin scope | Control Plane API calls Commerce Node `api/commerce/*` and appends `storeKey` query. | New admin settings APIs must follow this gateway pattern. |
| Theme/module system | No real theme or generic module registry exists. Architecture docs warn not to add `DefaultTheme` yet. | Defer theme/module constraints until real systems exist. |

## 3. Dream State Delta

Target state:

- Storefront receives one public-safe configuration projection per store.
- Admin users can view/edit store configuration only through Control Plane permissions and gateway APIs.
- Commerce Node owns runtime configuration in `CommerceNodeDbContext`.
- Settings are typed by domain, validated on write, and resolved with a clear fallback chain.
- Feature state is explicit and store-scoped, but limited to real runtime capabilities.
- Provider activation is store-scoped, ordered, validated, and secret-safe.
- Public API contracts never return provider secrets, SMTP passwords, private keys, node credentials, internal metadata, or raw settings blobs.

Non-goals:

- No ABP-style module framework.
- No generic expression engine for provider rules.
- No new `api/internal/*` routes.
- No direct `ControlPlane.Web -> CommerceNode.API` calls.
- No migration work in legacy `AppDbContext`.
- No theme settings until a real theme system exists.
- No public exposure of `MetadataJson`, `SettingsJson`, node credentials, audit fields, or secret values.

## 4. Target Architecture

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API
           api/commerce/admin/*?storeKey={storeKey}

Storefront.V2
  -> CommerceNode.API
       api/storefront/stores/{storeKey}/configuration

CommerceNodeDbContext
  - CommerceStore
  - typed store settings tables
  - StoreFeatureState
  - StorePaymentMethod
  - future provider secret references
```

Configuration resolution:

```text
store override
  -> commerce-node global default
      -> code default
          -> defensive fallback
```

Public projection:

```text
CommerceStore
  + typed public settings
  + public feature states
  + public provider metadata
  + public SEO defaults
      -> StorefrontPublicConfigurationResponse
```

## 5. Core Model Decisions

### Decision A: Keep Store Profile In CommerceStore

Keep identity, branding, company/contact, currency, culture, and maintenance state on `CommerceStore`.

Reason: these fields are already store-owned runtime profile data, not arbitrary settings. Moving them into a settings table would create duplicate sources of truth and break the existing Store lifecycle plan.

### Decision B: Use Typed Settings By Domain

Prefer explicit domain models over one generic key-value table.

Recommended first candidates:

- `CommerceStoreCheckoutSettings` or `StoreCheckoutSettings`.
- `CommerceStoreSeoSettings` or `StoreSeoSettings`.
- `CommerceStoreNotificationSettings` for non-secret notification metadata only.

Each typed setting should have:

- `StoreId` nullable only if global editable defaults are required.
- Clear domain fields.
- Validation rules.
- Audit fields.
- A resolver service with deterministic fallback.

Do not create a broad `SettingValue` table as the first implementation. If a registry is needed later, keep definitions in code and expose typed adapters.

### Decision C: Treat AdminSettings As Compatibility

`AdminSettings` is singleton-shaped and contains mixed global/admin concerns, including SMTP fields. It should not become the foundation for V2 store-specific configuration.

Use it only where existing behavior depends on it. New store settings should live in Commerce Node V2 tables and services.

### Decision D: Feature State Is Capability State, Not Module Registry

Add `StoreFeatureState` only for real runtime capabilities.

Suggested shape:

```text
StoreFeatureState
  Id
  StoreId
  FeatureKey
  Enabled
  DisplayOrder
  PublicMetadataJson
  CreatedAt
  UpdatedAt
```

Feature keys should be allowlisted in code. Initial examples should only be added when the runtime consumes them, such as checkout, customer accounts, newsletter, reviews, or recommendations.

### Decision E: Provider Activation Starts With Payment

Payment already has `StorePaymentMethod.Enabled` and `DisplayOrder`, so use that as the first concrete provider activation path.

Do not add a generic provider framework until at least two provider families need shared behavior. Shipping provider activation should wait until there is a concrete shipping method/provider model.

### Decision F: Split Public Metadata, Admin Config, And Secrets

Provider config must not rely on raw `SettingsJson` traveling through public or broad admin contracts.

Target split:

- Public metadata: key, display name, description, icon, display order, availability.
- Admin non-secret settings: validated typed fields or safe JSON.
- Secret state: `SecretsConfigured`, masked labels, `LastRotatedAt`, and secret references.
- Secret values: never returned by API after save.

For MVP, keep secrets in environment/config where possible. If database-backed secrets are required, design encrypted storage or external secret references explicitly before implementation.

## 6. Phase Plan

### Phase 0 - Baseline Inventory And Contract Tests

Goal: lock down current behavior before changing configuration.

Status 2026-07-15: completed baseline inventory and public leak contract guardrail.

Inventory:

- `StorefrontCurrentStoreResponse` is the current public-safe store profile projection. It includes identity, branding, locale/currency, contact, maintenance, and HTML body id fields. It does not include `CommerceStore.MetadataJson` or `ControlPlaneStorePublicId`.
- `StorefrontPaymentMethodResponse` is the current public payment method projection. It includes only id, key, name, and description.
- `SeoSettingsDto` is the current public SEO defaults projection. It is still backed by singleton SEO settings and is documented as a later store-scoped override candidate.
- Admin payment DTO `StorePaymentMethodDto` still exposes `SettingsJson`. This is intentionally recorded as a Phase 5 hardening target, not changed in Phase 0.
- `AdminSettings` remains singleton/compatibility-shaped and is not extended for new store-scoped behavior.

Implementation notes:

- Added a focused Storefront OpenAPI contract test covering public configuration-adjacent schemas.
- The guardrail rejects public schema properties such as `settingsJson`, `metadataJson`, SMTP/password/secret/private key/token fields, node credentials, audit fields, archived/deleted fields, and `controlPlaneStorePublicId`.
- No runtime behavior or schema was changed in this phase.

Verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_PublicConfigurationSchemasDoNotExposeSecretsOrInternalFields" --no-restore -p:UseSharedCompilation=false` passed 1/1.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore -p:UseSharedCompilation=false` passed.

Tasks:

- [x] Inventory current public storefront DTOs:
  - `StorefrontCurrentStoreResponse`.
  - `StorefrontPaymentMethodResponse`.
  - `SeoSettingsDto`.
- [x] Inventory admin DTOs that expose raw provider settings, especially payment method admin DTOs.
- [x] Add or update contract tests proving public storefront APIs do not expose:
  - `SettingsJson`.
  - `MetadataJson`.
  - SMTP password.
  - private keys.
  - node credentials.
  - audit/internal fields.
- [x] Record current singleton settings sources:
  - `AdminSettings`.
  - `SeoSettings`.
- [x] Confirm no new schema changes are needed in this phase.

Exit criteria:

- [x] Public leak baseline tests exist.
- [x] Current settings/provider risk points are documented.
- [x] No runtime behavior changes.

### Phase 1 - Permissions And API Guardrails

Goal: create explicit authorization boundaries before adding settings surfaces.

Status 2026-07-15: completed permission and policy foundation.

Implementation notes:

- Added granular Control Plane permission constants and policies for commerce settings, features, and providers.
- Added Control Plane seed permissions and role assignments through migration `ControlPlaneConfigurationFeaturePermissions`.
- `platform_owner` and `node_operator` receive read/write settings, feature, and provider permissions.
- `auditor` receives read-only settings, feature, and provider permissions.
- Added authorization guardrail test proving the new policy names map to granular permission keys and not broad `stores.write`.
- No new API endpoints were introduced in this phase; endpoint-level OpenAPI contract tests remain attached to the phase that introduces each endpoint.

Verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneAuthorizationTests" --no-restore -p:UseSharedCompilation=false` passed 6/6.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore -p:UseSharedCompilation=false` passed.
- `git diff --check` passed, with only existing line-ending normalization warnings.

Tasks:

- [x] Add Control Plane permissions:
  - [x] `commerce.settings.read`.
  - [x] `commerce.settings.write`.
  - [x] `commerce.features.read`.
  - [x] `commerce.features.write`.
  - [x] `commerce.providers.read`.
  - [x] `commerce.providers.write`.
- [x] Add matching policy names.
- [n/a] Apply these permissions to new Control Plane API endpoints as they are introduced. No new endpoint exists in this phase.
- [x] Keep Commerce Node admin routes protected by node credentials and `storeKey` query scope.
- [x] Keep Control Plane Web calling only Control Plane API.
- [n/a] Add OpenAPI contract tests for:
  - operation IDs.
  - summaries.
  - request/response DTOs.
  - security metadata.
  - expected errors.
  No new operation exists in this phase; contract tests will be added with the endpoint phases.

Exit criteria:

- [x] Permission constants and policies are available.
- [x] New settings/provider/feature APIs cannot reuse only broad `StoresWrite`.
- [x] API contract standards are enforced from the start.

### Phase 2 - Public Configuration Projection MVP

Goal: give Storefront one allowlisted config endpoint without moving existing data.

Status 2026-07-15: completed additive public configuration projection.

Endpoint:

```text
GET api/storefront/stores/{storeKey}/configuration
```

Response DTO:

```text
StorefrontPublicConfigurationResponse
  StoreIdentity
  Branding
  LocaleOptions
  CurrencyOptions
  MaintenanceState
  FeatureFlags
  PaymentMethods
  SeoDefaults
```

Implementation notes:

- Added `StorefrontScopedConfigurationController.Get`.
- Added nested allowlist response contracts:
  - `StorefrontPublicConfigurationResponse`.
  - `StorefrontStoreIdentityResponse`.
  - `StorefrontBrandingResponse`.
  - `StorefrontLocaleOptionsResponse`.
  - `StorefrontCurrencyOptionsResponse`.
  - `StorefrontMaintenanceStateResponse`.
  - `StorefrontFeatureFlagsResponse`.
  - `StorefrontSeoDefaultsResponse`.
- Added `StorefrontConfiguration_Get` Swagger metadata with success and error response schemas.
- Added `StorefrontApiClient.GetPublicConfigurationAsync` and Storefront V2 client DTOs.
- Updated Storefront OpenAPI snapshots after generating Swagger from CommerceNode runtime.
- Existing `store/current`, `payments/methods`, and `seo/settings` endpoints remain unchanged.

Verification:

- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore -p:UseSharedCompilation=false` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -p:UseSharedCompilation=false` passed.
- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests" --no-restore -p:UseSharedCompilation=false` passed 23/23.
- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontV2ApiClientTests.GetPublicConfigurationAsync" --no-restore -p:UseSharedCompilation=false` passed 1/1.
- A parallel first test attempt hit a Windows PDB file lock from concurrent `dotnet test`; rerunning sequentially passed.

Data sources:

- [x] `CommerceStore` for identity, branding, locale, currency, and maintenance.
- [x] existing payment method service for public payment metadata.
- [x] existing SEO settings for initial SEO defaults.
- [x] code defaults for feature flags until `StoreFeatureState` lands.

Rules:

- [x] Response is allowlist-only.
- [x] Do not include `CommerceStore.MetadataJson`.
- [x] Do not include provider `SettingsJson`.
- [x] Do not include SMTP settings, private keys, node credentials, or internal settings.
- [x] Do not remove existing current-store/payment/seo endpoints yet; this is additive.

Exit criteria:

- [x] Storefront can request a single safe public config projection.
- [x] Existing storefront behavior remains compatible.
- [x] Contract tests prove forbidden fields are absent.

### Phase 3 - Typed Settings Core And Resolver

Goal: add typed settings where the current model is singleton or missing store overrides.

Status 2026-07-15: completed first typed store-scoped settings domain for SEO defaults.

Implementation notes:

- Added Commerce Node entity/table `StoreSeoSettings` with one override row per store.
- Added `IStoreSeoSettingsService` and `StoreSeoSettingsService`.
- Resolver fallback order for SEO defaults is:
  - store override.
  - existing singleton SEO settings.
  - empty defensive `SeoSettingsDto` from the existing global service.
- Resolver cache key is `store-settings:{storeId}:seo-defaults`; saving an override invalidates that store cache key.
- Storefront public `configuration` and `seo/settings` reads now use the store-scoped SEO resolver.
- Save path validates with the existing `UpdateSeoSettingsDtoValidator`.
- No `AdminSettings` extension, no legacy `AppDbContext` migration, and no admin API/UI surface in this phase.

Verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StoreSeoSettingsServiceTests|FullyQualifiedName~CommerceNodeDbContextModelTests" --no-restore -p:UseSharedCompilation=false` passed 9/9.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore -p:UseSharedCompilation=false` passed.

Tasks:

- [x] Add a small resolver abstraction per typed settings domain.
- [x] Implement fallback order:
  - [x] store override.
  - [x] commerce-node global default if editable global defaults are needed.
  - [n/a] code default. Existing global SEO service already returns an empty defensive DTO when no singleton exists.
  - [x] defensive fallback.
- [x] Add typed tables in `CommerceNodeDbContext` for approved domains.
- [x] Start with the narrowest useful domains:
  - [n/a] checkout/order settings if current behavior needs store-specific control.
  - [x] SEO defaults if singleton SEO is not enough for multi-store.
  - [n/a] notification public/non-secret metadata if needed by admin display.
- [x] Keep secrets out of typed public settings.
- [x] Add validators on write DTOs.
- [x] Add migrations only to Commerce Node.

Exit criteria:

- [x] At least one typed store setting can be read, saved, validated, cached, and resolved with fallback.
- [x] `AdminSettings` is not extended for new store-scoped behavior.
- [x] No legacy `AppDbContext` migration is added.

### Phase 4 - Cache And Invalidation

Status: completed on 2026-07-15 by commit `pending`.

Goal: make settings fast without stale storefront behavior.

Tasks:

- Cache resolved settings by store and domain.
- Use explicit cache keys, for example:
  - `store-settings:{storeId}:{domain}`.
  - `store-features:{storeId}`.
  - `store-public-config:{storeKey}`.
- Invalidate affected cache keys when:
  - `CommerceStore` profile changes.
  - typed settings change.
  - feature states change.
  - provider activation or public metadata changes.
- Keep invalidation local to Commerce Node.
- Add tests for update-then-read behavior.

Implementation notes:

- Added `IStorefrontPublicConfigurationCache` with the explicit `store-public-config:{storeKey}` cache domain.
- Storefront public configuration reads now cache the consolidated response after resolving the scoped store.
- Commerce store profile updates invalidate the affected public configuration cache key.
- Store SEO override saves invalidate both resolved SEO settings and public configuration cache for the same store.
- Payment method metadata updates invalidate public configuration cache for the same store.

Verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StoreSeoSettingsServiceTests|FullyQualifiedName~CommerceStoreServiceValidationTests|FullyQualifiedName~CommerceNodePaymentMethodServiceCacheTests" --no-restore -p:UseSharedCompilation=false` passed 20/20.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore -p:UseSharedCompilation=false` passed.

Exit criteria:

- [x] Storefront sees updated public config after admin changes.
- [x] Cache invalidation is deterministic and scoped.
- [x] No cross-boundary cache mutation from Control Plane Web.

### Phase 5 - Secret Boundary And Provider Config Hardening

Goal: prevent provider and notification secrets from leaking through DTOs or logs.

Tasks:

- Replace or phase out admin DTOs that return raw provider `SettingsJson` when those settings may contain secrets.
- Introduce provider admin DTO split:
  - safe public metadata.
  - safe non-secret admin config.
  - secret status fields.
- Add write endpoints for secret updates that never echo values back.
- Add validation for provider config.
- Add audit redaction rules for secret updates.
- Keep public storefront payment metadata unchanged and safe.

Exit criteria:

- Public APIs never expose provider settings.
- Admin APIs never echo saved secret values.
- Tests cover secret redaction and public leak prevention.

### Phase 6 - Store Feature State Core

Goal: add store-scoped feature activation without a generic module system.

Tasks:

- Add `StoreFeatureState` entity/table in `CommerceNodeDbContext`.
- Add code allowlist for feature keys.
- Add Commerce Node admin endpoints:
  - list feature states for a store.
  - update feature state for a store.
- Add Control Plane gateway endpoints and UI integration later in Phase 8.
- Add runtime enforcement for any feature that affects server behavior.
- Include only public feature flags in `StorefrontPublicConfigurationResponse`.

Initial candidate features should be added only when consumed:

- `checkout`.
- `customerAccounts`.
- `newsletter`.
- `reviews`.
- `recommendations`.

Exit criteria:

- Feature state is store-scoped.
- Public projection exposes only safe feature flags.
- Server-side behavior does not rely only on UI hiding.

### Phase 7 - Provider Availability And Display Order

Goal: evolve payment provider behavior before generalizing.

Tasks:

- Keep using `StorePaymentMethod.DisplayOrder` for payment ordering.
- Add safe public metadata if needed:
  - icon URL.
  - short display text.
  - supported currencies.
  - supported countries.
- Add availability filters only for concrete needs:
  - currency.
  - country.
  - min/max order total.
  - customer group if that model exists later.
- Avoid arbitrary rule expressions in MVP.
- Defer shipping provider activation until a shipping method/provider model exists.

Exit criteria:

- Payment provider display and availability are deterministic.
- Storefront receives only available public payment metadata.
- No generic provider framework is introduced prematurely.

### Phase 8 - Control Plane Manager Integration

Goal: expose the new admin controls without breaking V2 boundaries.

Tasks:

- Add Control Plane Web pages/components for:
  - settings.
  - feature state.
  - providers.
- Call Control Plane API only.
- Control Plane API calls Commerce Node API with node credentials and `storeKey`.
- Show validation errors from API responses.
- Hide or disable UI actions based on granular permissions.
- Never expose node credentials, provider secrets, or raw secret config in browser state.

Exit criteria:

- Admins can manage approved settings/features/providers from manager UI.
- Users without write permissions cannot mutate configuration.
- Browser network traffic contains no secrets.

### Phase 9 - Theme And Module Constraints Deferred

Goal: keep future hooks documented without adding false systems.

Deferred items:

- `DefaultTheme`.
- generic module registry.
- theme/module constraints by store.

Future acceptance rule:

- Add theme keys only after a real theme system exists.
- Theme keys must be allowlisted.
- Add module state only after multiple real modules need shared activation behavior.
- Module activation must not replace domain-specific feature state.

Exit criteria:

- No theme/module schema is added in this work.
- Future constraints are documented and explicitly deferred.

### Phase 10 - QA, Docs, And Release Gate

Goal: finish each implementation phase with verifiable behavior.

Tasks:

- Update relevant QA checklists:
  - `QA-ControlPlane.todo.md`.
  - `QA-CommerceNode.todo.md`.
  - `QA-StorefrontV2.todo.md`.
- Add tests for:
  - public projection allowlist.
  - secret redaction.
  - permission failures.
  - store-key scoping.
  - settings validation.
  - cache invalidation.
  - fallback resolution.
  - OpenAPI validity and generator safety.
- Use Playwright only when browser UI behavior changes.
- Commit phase work only after focused verification passes.

Exit criteria:

- Each phase has matching QA checklist updates.
- Public API contracts are stable.
- No active V2 boundary rule is violated.

## 7. Test Diagram

```text
Unit tests
  - typed settings validation
  - resolver fallback
  - feature key allowlist
  - provider availability filtering

Integration tests
  - CommerceNodeDbContext migrations
  - admin settings/provider/feature endpoints
  - storefront public configuration endpoint
  - cache invalidation after update

Contract tests
  - OpenAPI operationId and security metadata
  - public DTO forbidden field assertions
  - request validation metadata

Authorization tests
  - settings read/write permissions
  - provider read/write permissions
  - feature read/write permissions
  - storeKey scope enforcement

Browser QA
  - Control Plane manager settings screens
  - Storefront behavior after feature disable
```

## 8. Failure Modes Registry

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Public API leaks secrets | Storefront is public-facing. | Allowlist DTOs, forbidden field tests, no raw settings blobs. |
| Provider `SettingsJson` contains secrets | Current model can mix safe and private config. | Split public metadata, admin config, and secret state. |
| Singleton SEO/settings leak across stores | Multi-store behavior becomes incorrect. | Add typed store-scoped overrides and fallback resolver. |
| Cache returns stale config | Admin changes appear broken. | Explicit invalidation per domain and update-then-read tests. |
| Feature flag only hides UI | Users can call APIs directly. | Enforce server-side where capability affects behavior. |
| Broad store write permission controls secrets | Too much admin power. | Add granular settings/provider/feature permissions. |
| Generic settings system becomes unmaintainable | Low signal, weak validation, unclear ownership. | Start with typed domain settings and code allowlists. |
| Theme/module fields added too early | Creates dead schema and false promises. | Defer until real theme/module runtime exists. |
| ControlPlane.Web calls CommerceNode directly | Breaks V2 boundary. | Keep Control Plane API gateway pattern. |
| Legacy `AppDbContext` gets new V2 schema | Splits ownership and migrations. | Commerce Node migrations only. |

## 9. Alternatives Considered

### Alternative 1: One Generic Settings Table For Everything

Rejected for MVP.

It looks flexible, but it weakens validation, makes public/private separation harder, and encourages arbitrary keys. Typed settings fit the current architecture better.

### Alternative 2: Move CommerceStore Fields Into Settings

Rejected.

`CommerceStore` already owns runtime store profile and lifecycle state. Moving these fields would duplicate sources of truth and conflict with existing plans.

### Alternative 3: Generic Module Framework Now

Rejected.

The codebase has no real module runtime. Store feature state should represent concrete capabilities first.

### Alternative 4: Store Secrets In Provider SettingsJson

Rejected as a target design.

It is hard to validate, hard to redact, and easy to leak through DTOs/logs. Use secret references or explicitly encrypted secret storage only after a separate security decision.

## 10. Scorecards

### CEO Review

Score: 8/10.

The plan focuses on the highest business risk first: safe public config and multi-store correctness. It avoids broad framework work that does not immediately help store operation.

### Design Review

Score: 7/10.

The admin user experience is intentionally deferred until API boundaries and permissions are stable. Phase 8 should keep UI dense and operational, not marketing-style.

### Engineering Review

Score: 8/10.

The plan respects V2 boundaries, DbContext ownership, and existing payment/store models. Main implementation risk is secret handling, which is isolated before provider expansion.

### DX Review

Score: 7/10.

Typed settings and explicit DTOs are more verbose, but they produce better contracts, validation, and generator safety than generic key-value settings.

## 11. Decision Audit Trail

| Decision | Status | Reason |
| --- | --- | --- |
| Keep `CommerceStore` as profile owner | Approved | Prevents duplicate store identity/config sources. |
| Add public configuration projection | Approved | Storefront needs a safe consolidated config contract. |
| Add typed store settings | Approved | Required for validation and store-specific override behavior. |
| Treat `AdminSettings` as compatibility | Approved | Current singleton shape does not fit multi-store settings. |
| Add granular permissions | Approved | Broad store write permission is too coarse for config/secrets. |
| Harden provider config before expanding providers | Approved | Existing payment provider state is the right first path. |
| Add feature state for real capabilities | Approved | Avoids premature generic module system. |
| Defer theme/module constraints | Approved | No real theme/module runtime exists yet. |
| Keep all schema in CommerceNodeDbContext | Approved | Matches data ownership rules. |

## 12. Recommended Implementation Order

1. Phase 0 - baseline tests and inventory.
2. Phase 1 - permissions and contract guardrails.
3. Phase 2 - public configuration projection MVP.
4. Phase 3 - typed settings core for one domain.
5. Phase 4 - cache and invalidation.
6. Phase 5 - secret/provider hardening.
7. Phase 6 - feature state core.
8. Phase 7 - payment provider availability refinement.
9. Phase 8 - Control Plane manager UI.
10. Phase 10 - QA and release gate for each landed phase.

Phase 9 remains deferred documentation until a real theme/module runtime exists.

## 13. Definition Of Done

The feature set is done when:

- Storefront can read a public-safe configuration projection for a store.
- Admins can manage approved settings/features/providers through Control Plane boundaries.
- Store-specific overrides resolve predictably with fallback.
- Public APIs and browser traffic contain no provider secrets or internal settings.
- Provider activation and display order work per store for payment.
- Feature flags are enforced server-side where relevant.
- Cache invalidates after configuration changes.
- Contract, authorization, validation, and public leak tests pass.
- QA checklist files are updated for the changed behavior.

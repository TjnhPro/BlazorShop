# BlazorShop Product Variant Attribute Todo

Generated: 2026-07-16

Source plan:

- `docs/refactor-control-Commerce-storefront/Product Variant Attribute.md`

Scope:

- Product attribute options for selectable product variations such as size and color.
- Product attribute mapping through existing `VariationTemplate`, `VariationTemplateOption`, `VariationTemplateValue`, and `ProductVariant.AttributesJson`.
- Attribute display order through existing option/value `SortOrder`.
- Required/optional selection metadata.
- Control type metadata for `dropdown`, `radio`, and `color`.
- Product variant combinations through existing `ProductVariant.AttributeSignature`.
- Combination SKU, stock, price override, active state, and default selection.
- Storefront product detail selection preview from backend-calculated state.

Explicitly out of scope:

- Specification attributes.
- Full localization for attribute labels/values.
- Variant image/gallery switching.
- Variant media assignment.
- Delivery promise calculation.
- GTIN/barcode per variant unless a separate Product Identity/Variant Identity phase is approved.
- Price adjustment rules separate from existing variant price override.
- Bundle, gift card, subscription, downloadable, and customer-entered-price product types.
- Rewriting Product Detail to WASM.

Boundary checklist:

- [x] Keep variant/variation data in `CommerceNodeDbContext`. 2026-07-16 Phase 0: baseline confirmed.
- [x] Keep Commerce Admin writes under `api/commerce/*` with required `storeKey` query for store-scoped endpoints. 2026-07-16 Phase 0: baseline confirmed.
- [x] Keep Storefront reads/preview under `api/storefront/stores/{storeKey}/*`. 2026-07-16 Phase 0: baseline confirmed.
- [x] Keep Storefront V2 store-scoped through configured current store and route store key. 2026-07-16 Phase 0: baseline confirmed.
- [x] Keep Control Plane Web calling Control Plane API only. 2026-07-16 Phase 0: baseline confirmed.
- [x] Do not add `api/internal/*`. 2026-07-16 Phase 0: no route changes.
- [x] Do not extend legacy `BlazorShop.Presentation` or `AppDbContext`. 2026-07-16 Phase 0: only active test/docs guardrails changed.
- [x] Reuse existing `VariationTemplate` and `ProductVariant.AttributeSignature`; do not introduce a parallel generic attribute-definition model in this phase. 2026-07-16 Phase 0: guardrails target existing model.

Current code facts to preserve:

- [x] `VariationTemplate` is already store-scoped and owns `Options`.
- [x] `VariationTemplateOption` already has `Name`, `SortOrder`, and `IsActive`.
- [x] `VariationTemplateValue` already has `Value`, `SortOrder`, and `IsActive`.
- [x] `Product` already links to `VariationTemplateId` and `Variants`.
- [x] `ProductVariant` already has `Sku`, `AttributesJson`, `AttributeSignature`, `DisplayName`, `Price`, `Stock`, `Color`, and `IsDefault`.
- [x] `ProductVariantAttributeNormalizer` already normalizes selected attributes and stable signatures.
- [x] `ProductVariantService` already rejects duplicate signatures and multiple default variants. 2026-07-16 Phase 0: duplicate signature guardrail test added.
- [x] `StorefrontCartService` already validates selected attributes for `CustomVariations`, resolves variants/defaults, checks stock, and applies currency conversion.
- [x] `PublicCatalogService` already exposes active variation templates to Storefront product details. 2026-07-16 Phase 0: active option/value mapping guardrail test added.
- [x] `ProductPage.razor` currently renders mostly client-side selection behavior from preloaded product/variant data.

## Phase 0 - Baseline And Guardrails

Goal: lock current variant/cart behavior before schema changes.

Implementation checklist:

- [x] Re-read active V2 files before implementation:
  - [x] `BlazorShop.Domain/Entities/ProductVariant.cs`
  - [x] `BlazorShop.Domain/Entities/CommerceNode/VariationTemplate.cs`
  - [x] `BlazorShop.Domain/Entities/CommerceNode/VariationTemplateOption.cs`
  - [x] `BlazorShop.Domain/Entities/CommerceNode/VariationTemplateValue.cs`
  - [x] `BlazorShop.Application/Services/ProductVariantService.cs`
  - [x] `BlazorShop.Infrastructure/Data/CommerceNode/Services/VariationTemplateService.cs`
  - [x] `BlazorShop.Application/CommerceNode/Carts/StorefrontCartService.cs`
  - [x] `BlazorShop.Application/Services/PublicCatalogService.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor`
- [x] Capture or identify focused tests for current behavior:
  - [x] duplicate product variant signature is rejected. 2026-07-16: `ProductVariantServiceTests.AddAsync_WhenAttributeSignatureAlreadyExistsForProduct_ReturnsFailure` added.
  - [x] only one default variant is allowed. Existing `ProductVariantServiceTests.AddAsync_WhenDefaultVariantAlreadyExists_ReturnsFailure`.
  - [x] cart rejects invalid selected attributes for `CustomVariations`. Existing `StorefrontCartServiceTests` selected-attribute coverage.
  - [x] cart rejects unavailable product/variant stock. Existing `StorefrontCartServiceTests` stock/unavailable coverage.
  - [x] product detail returns active template options and values. 2026-07-16: `PublicCatalogServiceTests.GetPublishedProductBySlugAsync_MapsActiveVariationTemplateOptionsAndValues` added.
  - [x] existing product detail page still renders product variants. Existing Storefront V2 host/markup coverage identified; no UI change in Phase 0.
- [x] Confirm existing OpenAPI/contract coverage for:
  - [x] Storefront catalog product detail endpoint. `CommerceNodeStorefrontOpenApiContractTests`.
  - [x] Storefront cart endpoints. `CommerceNodeStorefrontOpenApiContractTests`.
  - [x] Commerce Admin variation template endpoints. Controller/gateway coverage identified; contract expansion waits for Phase 1.
  - [x] Commerce Admin product variant endpoints. Controller/gateway coverage identified; contract expansion waits for Phase 2.
- [x] Add QA checklist seeds to `QA-CommerceNode.todo.md`, `QA-StorefrontV2.todo.md`, and `QA-ControlPlane.todo.md`.
- [x] Make no data-model change in this phase.

Verification checklist:

- [x] Focused `ProductVariantService` tests pass. 2026-07-16 Phase 0 focused run passed.
- [x] Focused `StorefrontCartService` tests pass. 2026-07-16 Phase 0 focused run passed.
- [x] Focused product detail/catalog tests pass. 2026-07-16 Phase 0 focused run passed.
- [x] No active V2 route ownership changes.

Exit criteria:

- [x] Current behavior is documented by tests or explicit QA checklist entries.
- [x] Known coverage gaps are written down before schema changes.
- [x] The implementation plan is ready for incremental commits.

Suggested commit:

```text
docs: plan product variant attribute hardening
```

## Phase 1 - Variation Template Option Metadata

Goal: let admin define how each option should render and whether it is required.

Implementation checklist:

- [x] Add Commerce Node schema fields:
  - [x] `VariationTemplateOption.ControlType`. 2026-07-16 Phase 1: `CommerceNodeVariationOptionMetadata` migration generated.
  - [x] `VariationTemplateOption.IsRequired`. 2026-07-16 Phase 1: `CommerceNodeVariationOptionMetadata` migration generated.
  - [x] `VariationTemplateValue.ColorHex`. 2026-07-16 Phase 1: `CommerceNodeVariationOptionMetadata` migration generated.
- [x] Persist `ControlType` as a stable string or string-backed enum value. 2026-07-16 Phase 1: `VariationControlTypes` constants plus EF check constraint.
- [x] Default existing options to:
  - [x] `ControlType = dropdown`. 2026-07-16 Phase 1: EF default and model test added.
  - [x] `IsRequired = true`. 2026-07-16 Phase 1: EF default and model test added.
- [x] Add MVP control type constants:
  - [x] `dropdown`.
  - [x] `radio`.
  - [x] `color`.
- [x] Add validation:
  - [x] unknown control type is rejected. 2026-07-16 Phase 1: `VariationTemplateServiceTests.CreateOptionAsync_RejectsUnknownControlType`.
  - [x] `ColorHex` is empty/null or valid 6-digit hex color. 2026-07-16 Phase 1: service tests cover valid, invalid, and non-color rejection.
  - [x] existing name/value length validation remains unchanged. 2026-07-16 Phase 1: validation path extended without removing existing checks.
- [x] Update Commerce Admin DTOs:
  - [x] create/update option accepts `ControlType`.
  - [x] create/update option accepts `IsRequired`.
  - [x] create/update value accepts optional `ColorHex`.
  - [x] list/detail responses expose control metadata.
- [x] Update Storefront product detail DTOs:
  - [x] option name.
  - [x] option control type.
  - [x] option required state.
  - [x] active values with display value.
  - [x] optional color hex.
- [x] Update `VariationTemplateService` mapping and validation. 2026-07-16 Phase 1: focused service tests passed.
- [x] Update `PublicCatalogService` product detail mapping. 2026-07-16 Phase 1: product detail mapping test asserts control metadata and color hex.
- [x] Update Control Plane variation template manager:
  - [x] edit option control type.
  - [x] edit required state.
  - [x] edit value color hex only when the parent option is `color`.
  - [x] preserve existing `SortOrder` editing.
- [x] Add Commerce Node migration only. 2026-07-16 Phase 1: `20260716111419_CommerceNodeVariationOptionMetadata`.
- [x] Update OpenAPI metadata and snapshots for changed admin/storefront contracts. 2026-07-16 Phase 1: Storefront OpenAPI snapshot refreshed.

Verification checklist:

- [x] Existing templates render as required dropdown options after migration. 2026-07-16 Phase 1: EF defaults and model tests passed.
- [x] Unknown control type returns a validation failure. 2026-07-16 Phase 1: service test passed.
- [x] Invalid `ColorHex` returns a validation failure. 2026-07-16 Phase 1: service test passed.
- [x] Product detail response includes option/value metadata. 2026-07-16 Phase 1: `PublicCatalogServiceTests.GetPublishedProductBySlugAsync_MapsActiveVariationTemplateOptionsAndValues`.
- [x] Storefront OpenAPI contract tests pass. 2026-07-16 Phase 1: focused OpenAPI run passed.
- [x] Commerce Admin OpenAPI contract tests pass. 2026-07-16 Phase 1: admin DTO compile path covered; dedicated admin OpenAPI snapshot is not present in this repo.
- [x] Control Plane Web build passes if UI changed. 2026-07-16 Phase 1: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` passed.

Exit criteria:

- [x] Variation template option metadata is persisted and returned.
- [x] Storefront product detail can render controls from backend metadata.
- [x] No storefront route shape changes are required.

Suggested commit:

```text
feat(commerce-node): add variation option metadata
```

## Phase 2 - Product Variant Active State And Combination Validation

Goal: support unavailable combinations without deleting them and prevent invalid combinations from being created.

Implementation checklist:

- [x] Add `ProductVariant.IsActive`.
- [x] Backfill existing variants as active. 2026-07-16 Phase 2: migration default value true.
- [x] Update create/update/get variant DTOs with `IsActive`.
- [x] Preserve unique combination behavior by `ProductId + AttributeSignature`. 2026-07-16 Phase 2: existing duplicate-signature test remains green.
- [x] Preserve one-default-variant behavior. 2026-07-16 Phase 2: existing default guard remains green.
- [x] Enforce `IsDefault` requires `IsActive`. 2026-07-16 Phase 2: `ProductVariantServiceTests.AddAsync_WhenDefaultVariantIsInactive_ReturnsFailure`.
- [x] When product has `VariationTemplateId`, validate:
  - [x] attribute names match active template options. 2026-07-16 Phase 2: unknown option test passed.
  - [x] attribute values match active template values. 2026-07-16 Phase 2: inactive/unknown value test passed.
  - [x] required options are present unless intentionally allowed by product type. 2026-07-16 Phase 2: service/cart required option checks passed.
  - [x] inactive options/values are rejected for new or updated variants.
- [x] Keep additive compatibility:
  - [x] do not assume only `ProductTypes.CustomVariations` can have variants.
  - [x] only enforce template option/value matching when a product has a `VariationTemplateId`.
- [x] Update Storefront product detail to expose active variants by default. 2026-07-16 Phase 2: public catalog test asserts inactive variants are filtered.
- [x] Update cart/checkout resolution to reject inactive variants. 2026-07-16 Phase 2: Storefront cart test passed; checkout cart path now checks active before stock deduction.
- [x] Update default variant resolution:
  - [x] ignore inactive variants where safe.
  - [x] return clear validation if saved default is inactive and no usable fallback exists.
- [x] Update Control Plane product variant manager:
  - [x] active/inactive toggle.
  - [x] validation messages before save.
  - [x] existing SKU, stock, price, color, and default controls remain.
- [x] Add Commerce Node migration only. 2026-07-16 Phase 2: `20260716112609_CommerceNodeProductVariantActiveState`.

Verification checklist:

- [x] Existing variants remain active after migration. 2026-07-16 Phase 2: EF default/model test passed.
- [x] Inactive variant cannot be default. 2026-07-16 Phase 2: service test passed.
- [x] Inactive variant is not exposed as selectable in Storefront detail. 2026-07-16 Phase 2: public catalog test passed.
- [x] Cart rejects inactive variant. 2026-07-16 Phase 2: Storefront cart test passed.
- [x] Template option/value mismatch is rejected when product has a template. 2026-07-16 Phase 2: service tests passed.
- [x] Variant data without a template remains compatible. 2026-07-16 Phase 2: compatibility test passed.

Exit criteria:

- [x] Admin can disable a variant without deleting it.
- [x] Storefront does not offer inactive variants.
- [x] Add-to-cart cannot add inactive variants.
- [x] Existing variant data remains valid after migration.

Suggested commit:

```text
feat(commerce-node): add active state for product variants
```

## Phase 3 - Shared Product Selection Resolver

Goal: prevent cart, checkout, product detail, and preview behavior from drifting.

Implementation checklist:

- [ ] Add application service interface such as `IProductSelectionResolver`.
- [ ] Add resolver input model with:
  - [ ] store id or store context.
  - [ ] product id or slug.
  - [ ] optional product variant id.
  - [ ] optional selected attributes.
  - [ ] quantity with minimum 1.
  - [ ] optional working currency code.
  - [ ] resolution mode: preview or cart.
- [ ] Add resolver output model with:
  - [ ] product id.
  - [ ] resolved product variant id.
  - [ ] normalized selected attributes.
  - [ ] attribute signature.
  - [ ] `IsValid`.
  - [ ] `IsAvailable`.
  - [ ] `CanAddToCart`.
  - [ ] validation messages.
  - [ ] SKU.
  - [ ] display name.
  - [ ] unit price.
  - [ ] compare price when available.
  - [ ] currency code.
  - [ ] stock quantity.
  - [ ] min quantity.
  - [ ] max quantity.
- [ ] Resolver responsibilities:
  - [ ] confirm product belongs to current store.
  - [ ] confirm product is publicly visible for storefront mode.
  - [ ] validate selected attributes against active variation templates.
  - [ ] resolve default variant when applicable.
  - [ ] reject inactive variants.
  - [ ] reject invalid or incomplete required selections.
  - [ ] apply existing price/currency conversion/rounding rules.
  - [ ] return structured validation messages for customer-correctable input.
- [ ] Refactor reusable logic out of `StorefrontCartService`:
  - [ ] product lookup.
  - [ ] selected-attribute normalization.
  - [ ] template attribute validation.
  - [ ] variant resolution.
  - [ ] price/currency calculation.
- [ ] Keep `StorefrontCartService` responsible for:
  - [ ] cart persistence.
  - [ ] cart-line snapshots.
  - [ ] cart session behavior.
- [ ] Keep checkout responsible for order placement and final stock re-checking.

Verification checklist:

- [ ] Resolver valid-selection test passes.
- [ ] Resolver invalid-attribute test passes.
- [ ] Resolver missing-required-option test passes.
- [ ] Resolver inactive-variant test passes.
- [ ] Resolver stock-limited test passes.
- [ ] Storefront cart behavior remains backward compatible.
- [ ] Checkout behavior remains backward compatible.

Exit criteria:

- [ ] Add-to-cart and preview can use the same selection rules.
- [ ] Cart behavior remains compatible with existing tests.
- [ ] Resolver has focused unit tests for valid, invalid, inactive, missing, and stock-limited combinations.

Suggested commit:

```text
feat(storefront): share product selection resolution
```

## Phase 4 - Storefront Product Selection Preview API

Goal: allow Product Detail to ask the backend what the selected combination means.

Implementation checklist:

- [ ] Add public Storefront endpoint:

```text
POST api/storefront/stores/{storeKey}/catalog/products/{productId}/selection-preview
```

- [ ] Use `POST` because the request body contains selected attributes and quantity.
- [ ] Keep the endpoint read-only; it must not mutate cart state.
- [ ] Add request DTO:
  - [ ] optional `ProductVariantId`.
  - [ ] optional `SelectedAttributes`.
  - [ ] required `Quantity`.
  - [ ] `Quantity` minimum 1.
  - [ ] optional `CurrencyCode`.
- [ ] Add response DTO:
  - [ ] `ProductId`.
  - [ ] optional `ProductVariantId`.
  - [ ] `IsValid`.
  - [ ] `IsAvailable`.
  - [ ] `CanAddToCart`.
  - [ ] `ValidationMessages`.
  - [ ] `SelectedAttributes`.
  - [ ] `AttributeSignature`.
  - [ ] `Sku`.
  - [ ] `DisplayName`.
  - [ ] `UnitPrice`.
  - [ ] optional `ComparePrice`.
  - [ ] `CurrencyCode`.
  - [ ] `StockQuantity`.
  - [ ] `MinQuantity`.
  - [ ] `MaxQuantity`.
  - [ ] optional `PrimaryImageUrl` using product image fallback only.
- [ ] Keep deferred fields out of the response:
  - [ ] variant image/gallery selection.
  - [ ] delivery information.
  - [ ] complex quantity increment rules.
  - [ ] customer-role-specific price visibility.
- [ ] Add OpenAPI metadata:
  - [ ] stable `operationId`.
  - [ ] short summary.
  - [ ] explicit request schema.
  - [ ] explicit response schema.
  - [ ] standard error response schemas.
  - [ ] request body marked required.
  - [ ] public storefront-safe security metadata.
  - [ ] validation metadata for `Quantity`.
- [ ] Add contract tests and snapshot updates.
- [ ] Add Storefront V2 API client method.

Verification checklist:

- [ ] Preview endpoint returns valid result for valid selection.
- [ ] Preview endpoint returns customer-readable messages for invalid selection.
- [ ] Preview endpoint rejects quantity below 1.
- [ ] Preview endpoint cannot resolve a product from another store.
- [ ] Preview endpoint does not expose admin DTOs, domain entities, or internal fields.
- [ ] OpenAPI validation and generator-safety tests pass.

Exit criteria:

- [ ] Product detail preview can be calculated without adding to cart.
- [ ] Invalid selections return customer-readable messages.
- [ ] API response is store-scoped and generator-safe.

Suggested commit:

```text
feat(storefront): add product selection preview API
```

## Phase 5 - Storefront Product Detail Integration

Goal: make Storefront product selection behavior backend-authoritative while preserving SSR.

Implementation checklist:

- [ ] Update `ProductPage.razor` to render controls from variation template metadata:
  - [ ] dropdown for `dropdown`.
  - [ ] radio group for `radio`.
  - [ ] color swatch buttons for `color`.
- [ ] Keep existing variant select behavior for products that do not use a variation template.
- [ ] On selection change, call selection-preview API.
- [ ] On quantity change, call selection-preview API.
- [ ] Update visible state from preview:
  - [ ] price.
  - [ ] compare/regular price where already available.
  - [ ] SKU/display name.
  - [ ] stock/availability.
  - [ ] add-to-cart enabled state.
  - [ ] validation messages.
- [ ] Send the same selected attribute payload to the existing cart API.
- [ ] Keep SSR fallback usable before JavaScript loads.
- [ ] Do not move Product Detail to WASM.
- [ ] Do not add cart, checkout, account, or contact component migration here.
- [ ] Do not add image-gallery switching until Media Core introduces a stable variant image assignment model.

Verification checklist:

- [ ] Product page renders without JavaScript.
- [ ] Product page renders dropdown/radio/color controls from API metadata.
- [ ] Selection change updates preview state.
- [ ] Quantity change updates preview state.
- [ ] Invalid selection blocks add-to-cart.
- [ ] Valid selection sends matching payload to cart API.
- [ ] Preview and add-to-cart agree on valid/invalid states.

Exit criteria:

- [ ] Product detail can preview a selected combination before add-to-cart.
- [ ] Add-to-cart and preview agree on selection rules.
- [ ] SSR page still renders usable product information before JavaScript loads.

Suggested commit:

```text
feat(storefront): use backend product selection preview
```

## Phase 6 - Manager Workflow Hardening

Goal: make admin workflows clear enough that stores do not accidentally create broken combinations.

Implementation checklist:

- [ ] Update variation template manager:
  - [ ] show option control type.
  - [ ] edit option control type.
  - [ ] show required/optional state.
  - [ ] edit required/optional state.
  - [ ] show active/inactive state.
  - [ ] show value color hex for color options.
- [ ] Update product variant manager:
  - [ ] show active/inactive state.
  - [ ] edit active/inactive state.
  - [ ] show default state.
  - [ ] show normalized combination signature read-only.
  - [ ] warn when product has a template and a required option is missing.
  - [ ] warn when a variant value no longer exists in the active template.
- [ ] Preserve existing ControlPlane Web routing through ControlPlane API.
- [ ] Do not call CommerceNode API directly from ControlPlane Web.
- [ ] Keep validation errors visible and actionable in ControlPlane Web.

Verification checklist:

- [ ] Control Plane Web build passes.
- [ ] Control Plane API gateway tests pass.
- [ ] Control Plane boundary tests prove Web does not call CommerceNode API directly.
- [ ] Manager UI shows disabled/default/incomplete combination states.
- [ ] Validation errors from Commerce Node surface clearly.

Exit criteria:

- [ ] Admin can tell which combinations are valid, disabled, default, or incomplete.
- [ ] UI edits call only approved V2 API boundaries.

Suggested commit:

```text
feat(control-plane): harden variant attribute workflow
```

## Phase 7 - QA, Contracts, And Release Gate

Goal: finish the phase without breaking catalog, cart, checkout, or API clients.

Implementation checklist:

- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md`.
- [ ] Update `QA-ControlPlane.todo.md` if manager UI changed.
- [ ] Build active V2 projects.
- [ ] Run focused Commerce Node tests for:
  - [ ] template option metadata validation.
  - [ ] color hex validation.
  - [ ] variant active/inactive behavior.
  - [ ] required option validation.
  - [ ] duplicate signature rejection.
  - [ ] default variant behavior.
  - [ ] resolver output.
  - [ ] cart add-line compatibility.
- [ ] Run Storefront API contract tests for:
  - [ ] product detail response schema.
  - [ ] selection-preview request/response schema.
  - [ ] error response schemas.
  - [ ] quantity minimum metadata.
  - [ ] security metadata.
  - [ ] no domain entities in public schemas.
- [ ] Run Storefront V2 browser QA when UI changes land:
  - [ ] product page renders.
  - [ ] attribute selection updates preview.
  - [ ] invalid selection blocks add-to-cart.
  - [ ] valid selection adds to cart.
  - [ ] inactive variant is not selectable.
- [ ] Review diff for:
  - [ ] no legacy `BlazorShop.Presentation` feature changes.
  - [ ] no `AppDbContext` V2 migration.
  - [ ] no direct ControlPlane.Web to CommerceNode API call.

Release gate:

- [ ] Variation template options support control type and required state.
- [ ] Variation template values support optional color hex.
- [ ] Product variants support active/inactive state.
- [ ] Invalid template/variant combinations are rejected or clearly reported.
- [ ] Storefront product detail has a backend selection-preview API.
- [ ] Product detail preview and add-to-cart use the same selection rules.
- [ ] Storefront does not expose inactive variants as selectable choices.
- [ ] OpenAPI is generator-safe for changed Storefront and admin contracts.
- [ ] QA checklists contain evidence.
- [ ] No Specification Attribute implementation is included.

Suggested commit:

```text
test(variant-attributes): complete release gate
```

## QA Checklist Seeds

### Commerce Node

- [ ] Variation template option control type defaults to `dropdown` for existing data.
- [ ] Variation template option required state defaults to `true` for existing data.
- [ ] Unknown variation control type is rejected.
- [ ] Invalid color hex is rejected.
- [ ] Storefront product detail response includes variation option control metadata.
- [ ] Product variant active state defaults to `true`.
- [ ] Inactive variant cannot be set as default.
- [ ] Variant combination validation rejects unknown template option names.
- [ ] Variant combination validation rejects unknown template values.
- [ ] Duplicate variant attribute signature is still rejected.
- [ ] Shared product selection resolver rejects missing required options.
- [ ] Shared product selection resolver rejects inactive variants.
- [ ] Cart add-line uses resolver output without changing cart persistence behavior.
- [ ] Storefront selection-preview endpoint is store-scoped.
- [ ] Storefront selection-preview endpoint rejects quantity below 1.

### Storefront V2

- [ ] Product detail renders dropdown controls from option metadata.
- [ ] Product detail renders radio controls from option metadata.
- [ ] Product detail renders color swatches from option metadata.
- [ ] Product detail preview updates price/SKU/stock after selection.
- [ ] Product detail preview blocks invalid selection.
- [ ] Product detail add-to-cart sends the same selected attributes used by preview.
- [ ] Inactive variants are not selectable.
- [ ] Product detail remains usable before JavaScript loads.

### Control Plane

- [ ] Variation template manager can edit option control type.
- [ ] Variation template manager can edit option required state.
- [ ] Variation template manager can edit color hex for color values.
- [ ] Product variant manager can mark a variant inactive.
- [ ] Product variant manager shows normalized signature read-only.
- [ ] Product variant manager warns for missing required option.
- [ ] Product variant manager warns for stale template value.
- [ ] ControlPlane Web calls only ControlPlane API.

## Deferred Scope Checklist

- [ ] Specification attributes remain deferred.
- [ ] Full localization for attribute labels/values remains deferred.
- [ ] Variant image/gallery switching remains deferred.
- [ ] Variant media assignment remains deferred.
- [ ] Delivery promise calculation remains deferred.
- [ ] Variant GTIN/barcode remains deferred unless separately approved.
- [ ] Price adjustment rules beyond existing variant price override remain deferred.
- [ ] Bundle/gift-card/subscription/downloadable/customer-entered-price product types remain deferred.
- [ ] Product Detail WASM rewrite remains deferred.

## Risk Register

- [ ] Resolver extraction could change cart behavior unexpectedly.
- [ ] Existing variants without templates could become invalid if validation is too broad.
- [ ] Default variant and inactive variant rules could conflict with current data.
- [ ] Selection-preview API could expose admin/internal data if DTOs are reused carelessly.
- [ ] Color swatch support could expand into unapproved media work.
- [ ] Storefront UI could drift from backend validation if preview/add-to-cart payloads differ.
- [ ] ControlPlane Web boundary could be broken by direct CommerceNode calls.

## Recommended Implementation Order

- [x] Phase 0 - baseline and guardrails. 2026-07-16: focused Phase 0 run passed 48/48.
- [ ] Phase 1 - variation template option metadata.
- [ ] Phase 2 - product variant active state and combination validation.
- [ ] Phase 3 - shared product selection resolver.
- [ ] Phase 4 - Storefront product selection preview API.
- [ ] Phase 5 - Storefront product detail integration.
- [ ] Phase 6 - manager workflow hardening.
- [ ] Phase 7 - QA/contracts/release gate.

## Definition Of Done

- [ ] Variation template options support control type and required state.
- [ ] Variation template values support optional color hex.
- [ ] Product variants support active/inactive state.
- [ ] Invalid template/variant combinations are rejected or clearly reported.
- [ ] Storefront product detail has a backend selection-preview API.
- [ ] Product detail preview and add-to-cart use the same selection rules.
- [ ] Storefront does not expose inactive variants as selectable choices.
- [ ] QA checklists and focused tests cover the new behavior.
- [ ] No Specification Attribute implementation is included.
- [ ] No legacy presentation or `AppDbContext` V2 feature work is introduced.

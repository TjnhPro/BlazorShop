# Storefront Shared Semantic Render Extraction

Status: complete
Track: Phase 3.4 - V2 Shared Semantic Render Extraction
Owner boundary: Storefront Components / Storefront Components.Primitives / Storefront Components.Ssr / Storefront Presentation / Storefront V2 / Storefront V2.WASM
Primary goal: extract the final approved reusable semantic rendering from Storefront V2 while preserving the current V2 markup contract, browser behavior, routes, final Tailwind styling, copy, and runtime boundaries.

## Decision

Phase 3.4 extracts exactly these three render surfaces:

- [x] `StorefrontProductPurchasePanel` semantic rendering into `BlazorShop.Storefront.Components.Primitives/Product`.
- [x] `StorefrontConsentPanel` semantic rendering into `BlazorShop.Storefront.Components.Ssr/Security`.
- [x] `StorefrontToastRegion` semantic rendering into `BlazorShop.Storefront.Components.Primitives/System`.

Target ownership:

```text
BlazorShop.Storefront.Components
├── Contracts/Product
│   ├── ProductPurchaseLabels.cs              # extend existing contract
│   └── ProductPurchasePanelClasses.cs         # new neutral class slots
└── Contracts/System
    ├── StorefrontToastRegionClasses.cs        # new neutral class slots
    └── StorefrontToastRegionLabels.cs         # new semantic labels

BlazorShop.Storefront.Components.Primitives
├── Product
│   └── StorefrontProductPurchasePanel.razor   # render-only purchase semantics
└── System
    └── StorefrontToastRegion.razor            # region + template, no extra wrapper

BlazorShop.Storefront.Components.Ssr
└── Security
    ├── StorefrontConsentPanel.razor
    ├── StorefrontConsentPanelClasses.cs
    └── StorefrontConsentPanelLabels.cs

BlazorShop.Storefront.V2
├── Components/Product
│   └── ProductPurchasePanelVisuals.cs          # final V2 classes and copy
├── Components/Security
│   ├── StorefrontConsentBanner.razor           # thin registered V2 wrapper
│   └── StorefrontConsentVisuals.cs             # final V2 classes and copy
├── Components/Layout
│   ├── MainLayout.razor                        # owns toast placement
│   └── StorefrontToastVisuals.cs               # final V2 classes and copy
└── wwwroot/js/storefrontCommerce.js            # V2 visual reactions and toast behavior

BlazorShop.Storefront.Presentation
└── wwwroot/js/storefront.application.js        # purchase command transport and consent behavior
```

The final dependency direction must remain:

```text
Components.Contracts
        ^
        |
Components.Primitives        Components.Ssr
        ^                         ^
        |                         |
        +------------ V2 --------+

Presentation JS owns same-origin commands and consent state.
V2 JS owns V2-specific visual reactions, toast presentation, and gallery feedback.
```

Namespace adoption also requires explicit `_Imports.razor` review:

- [x] Components.Primitives imports the new Product/System contracts used by its Razor files.
- [x] Components.Ssr imports the Presentation consent context namespace used by its Security Razor file.
- [x] V2 imports `Components.Contracts.System`, `Components.Primitives.System`, and `Components.Ssr.Security` where needed.
- [x] The shared purchase component and deleted V2 purchase component use the same short type name only within one atomic cutover; no ambiguous active import remains after the phase.
- [x] No project-reference change is expected because V2 already references Components.Primitives and Components.Ssr; any proposed `.csproj` change requires a dependency review first.

## Approved Ownership Rules

Shared render packages own:

- [x] semantic HTML structure;
- [x] accessibility attributes;
- [x] stable `data-storefront-*` hooks;
- [x] parameterized class slots;
- [x] parameterized labels and accessible text;
- [x] pure render helpers required to produce stable DOM IDs or select semantic variants;
- [x] no transport, route construction, runtime service resolution, or final storefront design.

Storefront V2 owns:

- [x] final Tailwind and `bs-*` class values;
- [x] final English storefront copy;
- [x] final component placement and page/layout composition;
- [x] registered V2 visual wrappers;
- [x] V2-specific toast visual reactions and gallery feedback;
- [x] product detail page arrangement;
- [x] visual fallback treatment for a missing color swatch;
- [x] any render-mode placement.

Storefront Presentation owns:

- [x] purchase command binding and same-origin BFF transport;
- [x] product selection preview command binding;
- [x] consent current/save/revoke calls;
- [x] consent state application and event publication;
- [x] no final V2 classes or final V2 copy.

## Codebase Evidence

Current purchase rendering:

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor` is a 225-line V2 Razor component.
- [x] It consumes `ProductPurchasePanelModel` and `ProductPurchaseActionDescriptor` only.
- [x] It does not inject `HttpClient`, `IJSRuntime`, Runtime, Client, Browser, or backend services.
- [x] It owns final Tailwind classes, fixed English labels, semantic purchase hooks, option rendering, quantity rendering, and pure DOM-ID helpers.
- [x] `ProductPurchaseLabels` already exists in `Components/Contracts/Product`; a second purchase labels type is unnecessary.
- [x] `ProductPurchasePanelModel.Empty` and `ProductPurchaseActionDescriptor.Empty` remain used by Starter compatibility components, but the new primitive does not need to use those defaults.
- [x] The current color swatch helper includes the V2 visual fallback `#f5f5f5`; that literal must not move into Primitives.
- [x] Radio/color group labels currently target a `div`; extraction is the bounded point to correct the group semantics with `fieldset`/`legend` without changing hooks or values.

Current consent rendering:

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Security/StorefrontConsentBanner.razor` consumes prepared `StorefrontConsentContext` from Presentation.
- [x] `V2FoundationViewRegistration` registers `StorefrontConsentBanner`; retaining a thin V2 wrapper preserves the registered visual slot.
- [x] Consent current/save/revoke behavior is implemented in Presentation `storefront.application.js`, not in V2 `storefrontCommerce.js`.
- [x] Consent visibility currently depends on adding/removing the literal CSS class `hidden`; this is a portability leak.
- [x] `StarterConsentBanner.razor` is a separate Starter implementation and remains outside the V2/V2.WASM scope of this phase.

Current toast rendering:

- [x] `MainLayout.razor` owns the current `data-storefront-toast-region` and `data-storefront-toast-template` markup.
- [x] `storefrontCommerce.js` queries those exact selectors and clones the template for V2 visual notifications.
- [x] The region and template are sibling nodes; extraction must not add a wrapper or nest the template into the region.
- [x] V2 JavaScript fallback headings/messages remain V2-owned and are not moved into shared labels.
- [x] Semantic SVG icons may remain in the primitive because they are selected by stable `data-storefront-toast-icon` hooks and do not establish a generic icon system.

Existing reusable-component precedent:

- [x] `StorefrontProductGallery` lives in Components.Primitives and receives classes/labels from `ProductGalleryVisuals` in V2.
- [x] Product pricing, availability, and variant list rendering live in Components.Ssr and receive final V2 classes/labels.
- [x] Pagination lives in Components.Primitives and receives prepared items, classes, and labels.
- [x] Breadcrumb and catalog filter rendering live in Components.Ssr and receive prepared Presentation context plus final V2 classes/labels.
- [x] Existing `HtmlRenderer` tests prove Razor components can be tested as rendered markup without adding bUnit.

## What Already Exists And Must Be Reused

- [x] `ProductPurchasePanelModel`.
- [x] `ProductPurchaseOptionItem` and `ProductPurchaseOptionValueItem`.
- [x] `ProductPurchaseVariantItem`.
- [x] `ProductPurchaseLabels`.
- [x] `ProductPurchaseSnapshot` and `ProductPurchaseSelectionState`.
- [x] `ProductPurchaseActionDescriptor`.
- [x] `StorefrontConsentContext` and its prepared action/event descriptors.
- [x] `StorefrontFoundationViewSet` and `V2FoundationViewRegistration`.
- [x] Presentation purchase and consent JavaScript binders.
- [x] V2 toast JavaScript behavior and CSS state selectors.
- [x] Components.Primitives and Components.Ssr dependency guardrails.
- [x] visual-neutrality source scanning.
- [x] existing `HtmlRenderer` component test patterns.
- [x] `scripts/qa/storefront-application-js-split-proof.js`.
- [x] `scripts/run-v2-local.ps1` for browser QA.

## Explicit Non-Goals

Do not change:

- [x] cart, checkout, account, payment, shipping, order, or backend commerce behavior;
- [x] Commerce Node API routes or contracts;
- [x] Storefront Runtime or generated Storefront Client;
- [x] Storefront Browser BFF routes;
- [x] product selection-preview request/response contracts;
- [x] product sellability, pricing, inventory, variant, or quantity business rules;
- [x] consent persistence, retention, categories, antiforgery, or API behavior;
- [x] V2 toast timing, levels, animation state names, event names, or message source;
- [x] product page layout or responsive design;
- [x] V2 header, footer, navigation, gallery, pricing, availability, or variant list;
- [x] database schema or migrations;
- [x] Control Plane;
- [x] StorefrontBuilder generation templates;
- [x] Starter implementation or Starter QA in this phase;
- [x] generated `Storefront.{Name}` projects;
- [x] a new component package;
- [x] a generic form, notification, icon, design-system, or localization framework.

Do not claim:

- [x] repository-wide consent deduplication while Starter retains `StarterConsentBanner`;
- [x] that V2 owns purchase command or consent transport JavaScript;
- [x] that all shared rendering has been extracted until the post-implementation audit confirms it;
- [x] that this phase redesigns or improves the V2 visual language.

## Compatibility Invariants

Purchase invariants:

- [x] Preserve `data-storefront-product-purchase`.
- [x] Preserve `data-storefront-product-purchase-panel`.
- [x] Preserve selection preview route, product, currency, resolved variant, image, SKU, and GTIN data attributes.
- [x] Preserve `data-storefront-attribute-group`.
- [x] Preserve `data-storefront-purchase-attribute` and attribute-name hooks.
- [x] Preserve radio input names, values, checked state, and deterministic IDs.
- [x] Preserve variant selector hook and option metadata.
- [x] Preserve min/max/step/value quantity attributes.
- [x] Preserve `data-storefront-command="cart.add-line"`.
- [x] Preserve submit, default-label, success-label, resolved-variant, selector, disabled, and feedback-target attributes.
- [x] Preserve feedback element ID, `aria-live`, level, and initial message.
- [x] Preserve the cart link URL supplied by the model.

Consent invariants:

- [x] Preserve every `data-storefront-consent-*` hook currently consumed by Presentation JavaScript.
- [x] Preserve current/accept/revoke route and HTTP method attributes from `StorefrontConsentContext`.
- [x] Preserve changed/manage event attributes.
- [x] Preserve policy link, preferences, analytics, marketing, essential, selected, all, and revoke controls.
- [x] Preserve initial no-flash hidden behavior using native `hidden`, not a theme class.
- [x] Preserve the registered V2 `ConsentBanner` component type.

Toast invariants:

- [x] Preserve exactly one `data-storefront-toast-region` in the composed V2 layout.
- [x] Preserve exactly one `data-storefront-toast-template`.
- [x] Keep region and template as siblings and in the same relative order.
- [x] Preserve `aria-live="polite"` and `aria-atomic="true"`.
- [x] Preserve toast root, level, state, accent, icon, heading, message, and close hooks.
- [x] Preserve all four icon hook values: `info`, `success`, `warning`, and `error`.
- [x] Preserve V2 CSS selectors and V2 JavaScript selectors without compatibility aliases.

## Autoplan Review Summary

### CEO Review

- [x] Premise accepted: the phase removes duplicated host-owned semantic markup without expanding ecommerce features.
- [x] Scope is intentionally limited to three already-approved candidates.
- [x] User value is indirect but concrete: future V2/Starter/generated storefront composition can reuse behavior contracts without copying transport or business logic.
- [x] No backend rewrite, framework introduction, or speculative generic system is justified.
- [x] Dream-state delta: shared packages own stable semantic render contracts; each store owns final presentation and copy.

CEO score: 9/10. Remaining risk is closure language that could overstate Starter deduplication.

### Design Review

- [x] This is a behavior-preserving extraction, not a redesign.
- [x] Current DOM order, responsive classes, labels, controls, and feedback placement remain visually equivalent after V2 supplies them through visual configuration.
- [x] No extra card, wrapper, spacing, palette, typography, animation, or icon-system changes are allowed.
- [x] Accessibility correction is limited to valid grouping semantics for product radio/color options.
- [x] Desktop and mobile screenshots must prove no visual regression.

Design score: 9/10. The only intentional markup change is accessibility structure with stable external hooks.

### Engineering Review

- [x] Dependency direction matches established Primitives and Ssr projects.
- [x] Existing contracts and prepared contexts are sufficient; no new service interface is needed.
- [x] Native hidden state removes consent behavior coupling to Tailwind.
- [x] `HtmlRenderer` tests provide compile-time and rendered-markup coverage.
- [x] Existing source ownership tests must be migrated rather than deleted.
- [x] V2.WASM build is mandatory because shared contract changes flow transitively into its component host.

Engineering score: 9/10. Highest regression risk is stale source-string tests that continue reading deleted V2 markup.

### Developer Experience Review

- [x] File placement follows existing capability folders and naming.
- [x] V2 visual configuration uses the same `*Visuals` convention as gallery and product detail displays.
- [x] Each phase names concrete files, consumers, tests, and exit criteria.
- [x] Ownership documentation must explain the Presentation/V2 JavaScript split so future agents do not move transport behavior into visual packages.
- [x] Deferred Starter adoption must be explicit and searchable.

DX score: 9/10. The plan avoids introducing a second labels type or an ambiguous generic component registry.

### Cross-Phase Themes

- [x] Preserve behavior before deleting old markup.
- [x] Keep final copy and class values in V2.
- [x] Use rendered tests in addition to source guardrails.
- [x] State deferred scope truthfully.
- [x] Close only after browser evidence and a fresh ownership audit.

## Failure Modes Registry

| Failure mode | Prevention | Detection | Recovery |
| --- | --- | --- | --- |
| Primitive contains literal Tailwind classes | Require all classes as parameters/contracts | Visual-neutrality tests | Move value to V2 `*Visuals` configuration |
| Primitive contains English copy | Require labels | Copy scan and rendered tests | Move copy to V2 labels |
| Purchase hooks drift | Characterize exact attributes before move | Rendered semantic tests and JS proof | Restore exact hook/value contract |
| Purchase group semantics break JS | Preserve input names, IDs, values, and hooks | Rendered tests plus variant browser flow | Revert only grouping implementation, not extraction |
| Invalid color becomes inline CSS | Validate dynamic color and avoid shared fallback literal | Unit/render tests for valid/missing color | Render no inline color and use V2 fallback class |
| Consent stays permanently hidden | Replace class coupling with native hidden property | JS tests and browser consent flow | Correct Presentation state application |
| Consent flashes before JS loads | Render native `hidden` initially | Browser reload/video/screenshot check | Restore initial hidden attribute |
| V2 registration breaks | Keep thin `StorefrontConsentBanner` wrapper | architecture and host smoke tests | Restore wrapper registration |
| Starter is accidentally changed | Scope and path guard | git diff audit | Revert only unrelated Starter edits |
| Toast template is nested or wrapped | Specify sibling no-wrapper contract | rendered DOM test | Restore exact fragment order |
| Toast selector count becomes two | Layout composition test | source/render count and browser toast | Remove duplicate host markup |
| V2 CSS/JS no longer finds hooks | Preserve selectors | JS split proof and browser flows | Restore semantic hook contract |
| Tailwind drops classes moved from Razor into `*Visuals.cs` | Keep V2 Tailwind `.cs` content scanning and rebuild generated CSS | Tailwind build plus browser screenshots | Correct content scan or class literals, regenerate `site.css`, never hand-edit generated CSS |
| Source tests pass while rendered markup is wrong | Add HtmlRenderer coverage | rendered assertions | Fix Razor output before adoption |
| Old V2 component remains active | Consumer and duplicate audits | `rg` and architecture tests | remove old active implementation after cutover |
| Deleted source test silently removes coverage | Map every old assertion to new owner | test migration checklist | restore equivalent assertion at correct layer |
| Shared model default leaks final copy | Require primitive parameters | source guard and rendered empty/default tests | remove primitive use of `Empty` defaults |

## Error And Rescue Registry

| Condition | Required signal | Required action |
| --- | --- | --- |
| Required purchase model/actions/classes/labels missing | Razor compile/editor-required warning or explicit test failure | Supply all parameters from V2; do not invent shared final defaults |
| Required consent context/classes/labels missing | Razor compile/editor-required warning or explicit test failure | Supply all parameters from thin V2 wrapper |
| Required toast classes/labels missing | Razor compile/editor-required warning or explicit test failure | Supply all parameters from MainLayout |
| Invalid/missing color value | No unsafe inline style; semantic fallback state remains renderable | Use host class/CSS fallback without a shared color literal |
| Consent API fails | Existing Presentation error/event behavior remains authoritative | Do not add transport handling to SSR component |
| Toast event has no heading/message | Existing V2 JS fallback remains authoritative | Do not add fallback copy to primitive |
| Browser QA cannot produce info/warning toast through a real user flow | Record JS proof for all levels and browser proof for reachable success/error flows | Do not add production-only QA endpoints or hooks |

## Phase 3.4.0 - Baseline And Characterization Lock

Goal: record every active consumer, semantic hook, ownership test, and browser behavior before moving markup.

Required reading:

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read Components.Primitives and Components.Ssr READMEs.
- [x] Read `Storefront Product Detail Display Components.todo.md`.
- [x] Read `Storefront Catalog Navigation Controls.todo.md`.
- [x] Read the relevant current entries in `QA-StorefrontV2.todo.md`.

Inventory tasks:

- [x] Record `git status --short` and preserve unrelated changes.
- [x] Record all `StorefrontProductPurchasePanel` consumers.
- [x] Record all `StorefrontConsentBanner` registrations and consumers.
- [x] Record all toast region/template sources and JavaScript selectors.
- [x] Record Starter consent duplication as deferred scope.
- [x] Record every `data-storefront-product-purchase*` attribute.
- [x] Record every consent hook and action/method/event attribute.
- [x] Record every toast hook, icon value, and DOM relationship.
- [x] Record product labels, final V2 class values, and color fallback behavior.
- [x] Record current accessibility structure for option groups.
- [x] Record Presentation-owned purchase and consent JavaScript functions.
- [x] Record V2-owned toast and visual feedback JavaScript functions.
- [x] Record CSS selectors for toast levels and purchase feedback levels.

Characterization tests to add or strengthen before deletion:

- [x] Assert current purchase attributes, values, disabled state, quantity bounds, and feedback target.
- [x] Assert current consent action URLs/methods/events and all controls.
- [x] Assert current toast sibling structure, selector counts, and all four icons.
- [x] Assert `storefront.application.js` owns purchase and consent command behavior.
- [x] Assert `storefrontCommerce.js` does not invoke Presentation application commands.
- [x] Assert V2 registration still resolves the consent wrapper.

Baseline commands:

```powershell
rg -n "StorefrontProductPurchasePanel|StorefrontConsentBanner|StarterConsentBanner|data-storefront-toast" `
  BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs

dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj `
  --filter "FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests"
```

Exit criteria:

- [x] all active consumers and selectors are listed;
- [x] current focused tests pass before implementation;
- [x] no implementation source has moved;
- [x] any pre-existing failure is recorded separately and not hidden by this phase.

Baseline evidence (2026-08-11): `git status --short` contained only this newly supplied untracked plan. The consumer/selector scan recorded the V2 product page as the active purchase consumer, `V2FoundationViewRegistration` as the V2 consent wrapper registration, `MainLayout` as the sole inline toast source, Presentation `storefront.application.js` as the purchase/consent binder, and V2 `storefrontCommerce.js` as the toast/visual binder. `StarterConsentBanner` and its registration remain a separately deferred Starter surface. The focused baseline command passed `55/55`; existing MessagePack advisory and Browserslist warnings were unchanged.

## Phase 3.4.1 - Define Neutral Render Contracts

Goal: define the smallest contracts needed to remove final classes and copy from shared markup.

### Product Purchase Contracts

Files:

- [x] Extend `BlazorShop.Storefront.Components/Contracts/Product/ProductPurchaseLabels.cs`.
- [x] Add `BlazorShop.Storefront.Components/Contracts/Product/ProductPurchasePanelClasses.cs`.
- [x] Do not add `ProductPurchasePanelLabels.cs`.

`ProductPurchaseLabels` requirements:

- [x] Preserve existing `AddToCart`, `AddedToCart`, `ViewCart`, `FreeShipping`, and `Optional` fields.
- [x] Add a purchase section heading label.
- [x] Add choose-variant and select-variant labels.
- [x] Add a quantity label.
- [x] Add a host-controlled select-option format or equivalent formatter input for `Select {option name}`.
- [x] Keep `Empty` neutral; it must contain empty strings only.
- [x] Do not add V2 routes, endpoint names, store-specific wording, or localization services.
- [x] Review constructor compatibility and update all direct construction sites atomically.

`ProductPurchasePanelClasses` requirements:

- [x] Cover root, heading, message, blocked message, delivery metadata, badges, option groups, labels, choices, selects, quantity, actions, buttons, cart link, feedback, and color swatch states.
- [x] Provide separate enabled and disabled add-button slots.
- [x] Provide separate valid-color and missing-color swatch slots if the rendered state needs different treatment.
- [x] Keep values neutral/empty in the shared contract.
- [x] Do not encode Tailwind, `bs-*`, palette, spacing, radius, breakpoint, or layout values.

### Toast Contracts

Files:

- [x] Add `BlazorShop.Storefront.Components/Contracts/System/StorefrontToastRegionClasses.cs`.
- [x] Add `BlazorShop.Storefront.Components/Contracts/System/StorefrontToastRegionLabels.cs`.

Requirements:

- [x] Classes cover region, toast root, content, accent, icon, text, heading, message, close button, and close icon.
- [x] Labels contain close-button accessible text and optional region accessible text only if the current rendered contract adopts it.
- [x] Do not move V2 runtime fallback headings/messages into the labels contract.
- [x] Do not add duration, animation timing, event names, or JavaScript configuration to the render contract.

### Consent Contracts

Files:

- [x] Add `BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanelClasses.cs`.
- [x] Add `BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanelLabels.cs`.

Requirements:

- [x] Classes cover root, inner layout, description, heading, body, policy link, choices, choice label/input, actions, secondary button, and primary button.
- [x] Labels cover aria label, heading, description, policy link, Preferences, Analytics, Marketing, Essential only, Revoke, Save choices, and Accept all.
- [x] Keep these Ssr-local, matching current catalog filter and breadcrumb precedent.
- [x] Do not add API routes or HTTP methods; those remain in prepared `StorefrontConsentContext`.
- [x] Do not add JavaScript service abstractions.

Contract tests:

- [x] Base Components contracts contain no Presentation, Runtime, Client, Browser, V2, V2.WASM, Starter, backend, or API references.
- [x] New contracts contain no literal Tailwind classes or final V2 copy.
- [x] Ssr consent contracts contain no Browser, Runtime, Client, V2, Starter, backend, or API references.
- [x] Public contract shape is covered by focused source or reflection tests where repository convention requires it.

Exit criteria:

- [x] Components builds;
- [x] Components.Ssr builds;
- [x] contracts are sufficient for the current markup without an untyped attribute dictionary;
- [x] no generic design-system or localization abstraction was introduced.

Phase 3.4.1 evidence (2026-08-11): Components and Components.Ssr built successfully. `StorefrontSharedSemanticRenderContractTests` locks the extended purchase label surface, neutral `Empty` values, and class-slot contracts without V2 visual values. `Contracts/System` required the existing descriptor validator to use `global::System.Text.RegularExpressions` so the new contract namespace cannot shadow the framework `System` namespace.

## Phase 3.4.2 - Extract Product Purchase Primitive

Goal: move purchase semantic rendering and pure rendering behavior into Components.Primitives while V2 retains presentation configuration.

Files:

- [x] Add `BlazorShop.Storefront.Components.Primitives/Product/StorefrontProductPurchasePanel.razor`.
- [x] Add `BlazorShop.Storefront.V2/Components/Product/ProductPurchasePanelVisuals.cs`.
- [x] Update `BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor`.
- [x] Update Components.Primitives and V2 `_Imports.razor` files for the new contract/component namespaces.
- [x] Remove `BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor` after cutover and test migration.

Primitive inputs:

- [x] `[Parameter, EditorRequired] ProductPurchasePanelModel Model`.
- [x] `[Parameter, EditorRequired] ProductPurchaseActionDescriptor Actions`.
- [x] `[Parameter, EditorRequired] ProductPurchaseLabels Labels`.
- [x] `[Parameter, EditorRequired] ProductPurchasePanelClasses Classes`.
- [x] Do not use `ProductPurchasePanelModel.Empty` or `ProductPurchaseActionDescriptor.Empty` as active primitive defaults.
- [x] Use `default!` only to satisfy Razor initialization for required parameters.

Rendering tasks:

- [x] Move current semantic structure without changing the order of purchase message, blocking message, delivery metadata, variation controls, quantity, actions, and feedback.
- [x] Replace every fixed English label with `Labels`.
- [x] Replace every literal class with a class slot.
- [x] Preserve all hooks and action attributes listed in Compatibility Invariants.
- [x] Preserve pure stable DOM-ID sanitization behavior.
- [x] Preserve filtering of empty variation values.
- [x] Preserve radio, color, select, and legacy variant selector behavior.
- [x] Preserve initial selected/default values.
- [x] Preserve server-computed disabled state.
- [x] Preserve cart URL exactly as supplied by `Model`.

Accessibility correction:

- [x] Render each radio/color option group with `fieldset` and `legend`, or an equivalent valid accessible grouping.
- [x] Do not point `<label for>` at a non-labelable `div`.
- [x] Keep input IDs, names, values, checked state, and `data-storefront-*` attributes unchanged.
- [x] Keep select and quantity labels associated with their inputs.
- [x] Keep feedback `aria-live="polite"`.

Color handling:

- [x] Treat `ColorHex` as untrusted display data.
- [x] Reuse an existing color validation/normalization helper if one exists.
- [x] If no helper exists, add the smallest pure validator at the component-contract/headless layer only when required by current data shape.
- [x] Emit inline `background-color` only for an accepted CSS hex value.
- [x] Do not move `#f5f5f5` or another final fallback color into Primitives.
- [x] Render a semantic missing-color class/state supplied by V2 when no valid color exists.

V2 adoption:

- [x] `ProductPurchasePanelVisuals` supplies all current classes and labels.
- [x] V2 values preserve current visual output exactly, except the bounded option-group accessibility markup.
- [x] `V2ProductPageView` composes the shared primitive with model, actions, labels, and classes.
- [x] Do not add a V2 wrapper unless compilation or a registered visual slot requires it; current purchase usage does not require one.
- [x] Add the shared primitive and remove the same-named V2 component in one compiling cutover so Razor component resolution is never left ambiguous.

Rendered component tests:

- [x] Add `StorefrontProductPurchasePanelPrimitiveTests` using `HtmlRenderer`.
- [x] Render a simple purchasable product.
- [x] Render a blocked product with a purchase-block message and disabled button.
- [x] Render variation-template radio options.
- [x] Render color options with valid and missing color values.
- [x] Render optional select options and formatted placeholder copy.
- [x] Render legacy variant selector options.
- [x] Assert quantity min/max/step/value.
- [x] Assert all semantic hooks and feedback-target linkage.
- [x] Assert host labels/classes appear.
- [x] Assert no hardcoded copy or literal visual class appears in primitive source.
- [x] Assert missing required parameters are not silently replaced with final-copy defaults.

Exit criteria:

- [x] Components.Primitives builds;
- [x] V2 and V2.WASM build;
- [x] V2 product page uses the shared primitive;
- [x] old V2 purchase implementation is removed;
- [x] purchase command and selection-preview behavior are unchanged.

Phase 3.4.2 evidence (2026-08-11): the V2 page now composes the Primitives purchase panel with `ProductPurchasePanelVisuals`; the former V2 component was deleted. The primitive preserves purchase descriptors and pure IDs, uses `fieldset`/`legend` for radio/color groups, validates hex swatches before producing inline color, and defers missing-color presentation to a V2 class slot. Components.Primitives, V2, and V2.WASM builds passed; `StorefrontProductPurchasePanelPrimitiveTests` passed `3/3`.

## Phase 3.4.3 - Extract Consent SSR Rendering And Remove Hidden-Class Coupling

Goal: move consent semantic rendering to Components.Ssr while keeping V2 registration, final visuals, and Presentation-owned consent behavior.

Files:

- [x] Add `BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanel.razor`.
- [x] Add `BlazorShop.Storefront.V2/Components/Security/StorefrontConsentVisuals.cs`.
- [x] Convert `BlazorShop.Storefront.V2/Components/Security/StorefrontConsentBanner.razor` into a thin wrapper.
- [x] Update Components.Ssr and V2 `_Imports.razor` files for the new Security component namespace.
- [x] Update `BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js` only for native hidden-state handling.
- [x] Update consent-focused JavaScript/source tests.

Shared SSR component inputs:

- [x] `[Parameter, EditorRequired] StorefrontConsentContext Context`.
- [x] `[Parameter, EditorRequired] StorefrontConsentPanelLabels Labels`.
- [x] `[Parameter, EditorRequired] StorefrontConsentPanelClasses Classes`.
- [x] Do not inject services.
- [x] Do not call consent APIs.
- [x] Do not contain V2 route literals or fixed English copy.

Rendering tasks:

- [x] Preserve section semantics and every current consent hook.
- [x] Preserve prepared action URLs, methods, and event names from `Context`.
- [x] Preserve policy URL from `Context.PolicyPagePath`.
- [x] Preserve preferences, analytics, and marketing checkboxes.
- [x] Preserve essential-only, revoke, save-selected, and accept-all buttons.
- [x] Render native `hidden` initially to prevent content flash before consent state loads.
- [x] Remove literal `hidden` from the V2 root class value.

Presentation JavaScript tasks:

- [x] Replace consent-only `classList.add("hidden")` and `classList.remove("hidden")` visibility operations with the native `banner.hidden` property or equivalent attribute operation.
- [x] Do not change consent API routes, methods, payloads, visitor identity, antiforgery behavior, events, or error semantics.
- [x] Do not move consent behavior into V2 JavaScript.
- [x] Confirm no other unrelated use of class `hidden` is changed.

V2 wrapper tasks:

- [x] Keep the public V2 component name `StorefrontConsentBanner`.
- [x] Keep `V2FoundationViewRegistration` mapped to `StorefrontConsentBanner`.
- [x] Wrapper only supplies Context, V2 labels, and V2 classes to `StorefrontConsentPanel`.
- [x] Wrapper contains no duplicated semantic banner markup.

Rendered tests:

- [x] Add `StorefrontConsentPanelSsrTests` using `HtmlRenderer`.
- [x] Assert the initial native `hidden` state.
- [x] Assert all action/method/event attributes are rendered from context.
- [x] Assert all category controls and command hooks.
- [x] Assert policy link and accessible label.
- [x] Assert host labels/classes appear.
- [x] Assert no final copy, V2 classes, `HttpClient`, `IJSRuntime`, or `@rendermode` in the Ssr source.

JavaScript tests:

- [x] Assert consent binder uses native hidden state.
- [x] Assert no consent visibility code depends on Tailwind class `hidden`.
- [x] Assert current/save/revoke calls and changed/manage events remain present.
- [x] Assert V2 script still does not call Presentation consent commands.

Starter scope:

- [x] Do not edit `StarterConsentBanner.razor`.
- [x] Do not edit Starter registration or Starter smoke tests.
- [x] Add a clear deferred note in closure evidence: Starter remains a separate consumer candidate.
- [x] Scope duplicate-implementation assertions to V2, not the entire repository.

Exit criteria:

- [x] Components.Ssr builds;
- [x] V2 and V2.WASM build;
- [x] V2 registered consent slot remains functional;
- [x] V2 wrapper contains no duplicated semantic banner implementation;
- [x] consent no-flash and change/revoke behavior pass browser QA later;
- [x] Starter remains unchanged.

## Phase 3.4.4 - Extract Toast Region Primitive

Goal: move the stable toast region/template markup into a render-only primitive while MainLayout remains owner of placement and V2 remains owner of behavior and visuals.

Files:

- [x] Add `BlazorShop.Storefront.Components.Primitives/System/StorefrontToastRegion.razor`.
- [x] Add `BlazorShop.Storefront.V2/Components/Layout/StorefrontToastVisuals.cs`.
- [x] Update `BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor`.
- [x] Update Components.Primitives and V2 `_Imports.razor` files for the new System contracts/component namespace.
- [x] Do not change `storefrontCommerce.js` unless a test proves a selector compatibility issue; selector changes are not planned.

Primitive inputs:

- [x] `[Parameter, EditorRequired] StorefrontToastRegionClasses Classes`.
- [x] `[Parameter, EditorRequired] StorefrontToastRegionLabels Labels`.
- [x] No services, routes, events, durations, transport clients, or JS interop parameters.

Rendering tasks:

- [x] Render the toast region and toast template as sibling top-level fragments.
- [x] Do not add a containing element.
- [x] Keep region before template.
- [x] Preserve `aria-live="polite"` and `aria-atomic="true"`.
- [x] Preserve all toast hooks and initial data values.
- [x] Preserve the four semantic SVG icon variants and hook values.
- [x] Use labels for close-button accessible text.
- [x] Use class slots for all class attributes.
- [x] Do not add final fallback heading/message copy.

V2 adoption:

- [x] `StorefrontToastVisuals` supplies current V2 classes and close-button label.
- [x] `MainLayout` renders exactly one `StorefrontToastRegion` between header and main content.
- [x] MainLayout continues to own the placement order: header, toast region/template, main, footer.
- [x] Existing V2 CSS selectors continue to match.
- [x] Existing V2 JavaScript selector constants continue to match.

Rendered tests:

- [x] Add `StorefrontToastRegionPrimitiveTests` using `HtmlRenderer`.
- [x] Assert exactly one region and one template.
- [x] Assert the two outputs are siblings with no new wrapper.
- [x] Assert region precedes template.
- [x] Assert all hooks and all four icon values.
- [x] Assert host classes and close label appear.
- [x] Assert no literal V2 class or final fallback message exists in the primitive.
- [x] Assert composed MainLayout contains the component once and does not retain inline duplicated markup.

Exit criteria:

- [x] Components.Primitives builds;
- [x] V2 and V2.WASM build;
- [x] composed V2 layout exposes exactly one region/template pair;
- [x] no V2 CSS or JS compatibility alias was added;
- [x] toast success/error behavior remains browser-testable.

## Phase 3.4.5 - Migrate Ownership Tests And Guardrails

Goal: move every existing assertion to the correct source owner without deleting behavior coverage.

Tests that must be reviewed and updated:

- [x] `StorefrontComponentsHeadlessPresentationRefactorTests.cs`.
- [x] `StorefrontBrandingMarkupTests.cs`.
- [x] `StorefrontVisualSourceOwnershipTests.cs`.
- [x] `SecurityPrivacyPhase3ConsentTests.cs`.
- [x] `LayoutAssetFoundationTests.cs`.
- [x] `StorefrontPresentationFoundationBoundaryTests.cs`.
- [x] `StorefrontComponentVisualNeutralityTests.cs`.
- [x] `StorefrontPrimitiveDependencyTests.cs`.
- [x] `StorefrontComponentModeDependencyTests.cs`.
- [x] `StorefrontCommerceScriptRegressionTests.cs`.
- [x] `StorefrontV2WASMRuntimeFoundationTests.cs` where component-mode ownership or V2.WASM graph expectations are affected.
- [x] `StorefrontV2HostSmokeTests.cs` for existing consent host/BFF integration coverage; do not replace its API assertions with source checks.
- [x] `StorefrontBuilderQaRegenerationTests.cs` only if the V2 composition assertion references the moved purchase component contract.

Required assertion migration:

- [x] Assertions for semantic purchase hooks read/render the new primitive.
- [x] Assertions for final purchase Tailwind classes and labels read `ProductPurchasePanelVisuals` or rendered V2 composition.
- [x] Assertions for V2 product page composition require labels and classes to be supplied.
- [x] Assertions no longer require `ProductPurchaseActionDescriptor.Empty` in the primitive.
- [x] Assertions for consent semantic hooks render/read `StorefrontConsentPanel`.
- [x] Assertions for final consent classes/copy read `StorefrontConsentVisuals` or rendered V2 wrapper.
- [x] Assertions retain the V2 wrapper registration requirement.
- [x] Assertions for toast semantics render/read `StorefrontToastRegion`.
- [x] Assertions for final toast class values read `StorefrontToastVisuals` or rendered V2 composition.
- [x] `ExtractToastTemplate` or equivalent helper no longer assumes the template source is `MainLayout.razor`.
- [x] MainLayout tests assert component placement and exactly one composed hook, not inline ownership.

Guardrails to add:

- [x] Components.Primitives new files reference only Components contracts.
- [x] Components.Ssr consent files reference only Components and Presentation.
- [x] Shared files contain no `@rendermode`.
- [x] Shared files contain no `HttpClient`, `IJSRuntime`, local API routes, absolute URLs, or Commerce Node references.
- [x] Shared files contain no final V2 class literals.
- [x] Shared files contain no fixed storefront copy from the migrated V2 implementation.
- [x] V2 contains no second active purchase semantic implementation.
- [x] V2 contains no second active consent semantic implementation outside the thin wrapper.
- [x] MainLayout contains no inline duplicate toast region/template markup.
- [x] Starter duplicate consent implementation is explicitly excluded from the V2-only duplicate rule.

Test quality rules:

- [x] Do not replace rendered tests with source-string tests only.
- [x] Do not remove a regression assertion solely because a file moved.
- [x] Do not assert incidental whitespace or generated Razor output.
- [x] Assert stable semantic hooks, contracts, ownership, and behavior.
- [x] Keep final visual value assertions at the V2 layer.

Exit criteria:

- [x] all stale source paths are removed from active assertions;
- [x] equivalent or stronger coverage exists at the new owner;
- [x] focused architecture/component/JS tests pass;
- [x] no test falsely claims repository-wide Starter deduplication.

## Phase 3.4.6 - Build And Automated Verification Gate

Goal: prove package boundaries, rendered semantics, V2 integration, and V2.WASM compatibility before browser QA.

Build gate:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj
```

V2 asset gate:

```powershell
Push-Location BlazorShop.PresentationV2/BlazorShop.Storefront.V2
npm ci
npm run tailwind:build
Pop-Location
```

- [x] Confirm `tailwind.config.js` still scans `./**/*.cs` before moving final classes into `*Visuals.cs`.
- [x] Keep all Tailwind class alternatives as complete literal strings in V2 visual configuration; do not construct class names dynamically in a way Tailwind cannot detect.
- [x] Regenerate `wwwroot/css/site.css` through the package script; do not edit generated CSS manually.
- [x] Confirm representative purchase, consent, and toast classes remain in generated CSS.
- [x] Confirm asset generation does not scan or copy final V2 classes into shared packages.

Focused test gate:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj `
  --filter "FullyQualifiedName~StorefrontProductPurchasePanelPrimitiveTests|FullyQualifiedName~StorefrontConsentPanelSsrTests|FullyQualifiedName~StorefrontToastRegionPrimitiveTests|FullyQualifiedName~StorefrontPrimitiveDependencyTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~StorefrontV2HostSmokeTests"
```

JavaScript proof:

```powershell
node scripts/qa/storefront-application-js-split-proof.js
```

Automated checks:

- [x] All six projects build.
- [x] V2 Tailwind production CSS build passes after final classes move to `*Visuals.cs`.
- [x] Rendered component tests pass.
- [x] dependency and visual-neutrality tests pass.
- [x] Presentation/V2 JavaScript ownership tests pass.
- [x] JS split proof passes all configured toast/purchase states.
- [x] `rg` finds no deleted V2 purchase implementation references.
- [x] `rg` finds no inline toast region/template left in MainLayout.
- [x] `rg` confirms V2 consent wrapper has no duplicated banner markup.
- [x] `git diff --check` passes.

Failure handling:

- [x] Fix failures within the owning phase; do not weaken a guardrail to obtain green output.
- [x] If an unrelated baseline test fails, record command/output and prove it existed before this phase.
- [x] Do not proceed to browser QA with a failed build, rendered test, dependency test, or JS proof.

Exit criteria:

- [x] automated gate is green;
- [x] no package boundary changed beyond approved references;
- [x] V2.WASM remains compatible;
- [x] worktree contains only intentional phase changes.

## Phase 3.4.7 - Playwright Browser Regression QA

Goal: verify real browser behavior and visual equivalence through V2 user flows, not only source or smoke tests.

Environment:

- [x] Start the configured V2 local environment with `scripts/run-v2-local.ps1 -StopExisting`.
- [x] Confirm Commerce Node, Storefront V2, and required local dependencies are healthy.
- [x] Use the configured current test store; do not introduce fallback store behavior.
- [x] Record tested Storefront URL, store key, commit SHA, viewport, and browser.

Product purchase scenarios:

- [x] Open a simple purchasable product detail page.
- [x] Confirm purchase message, quantity bounds, add button, cart link, and feedback region render.
- [x] Add the product to cart through the real same-origin command path.
- [x] Confirm success feedback and cart state update.
- [x] Open a variant product with radio/select/color attributes.
- [x] Change selections and confirm server selection-preview recalculates resolved variant, SKU/GTIN, availability, image, and add eligibility as currently supported.
- [x] Confirm keyboard interaction and focus indication for option groups.
- [x] Confirm missing color data does not produce invalid inline CSS or broken layout.
- [x] Open or fixture a blocked/non-purchasable product and confirm disabled state plus reason.

Consent scenarios:

- [x] Use actual consent BFF endpoints and browser storage/cookies; do not fake only the DOM state.
- [x] Load a visitor without stored consent and confirm the banner appears after state resolution without an initial flash.
- [x] Choose Essential only and confirm saved state plus hidden banner.
- [x] Reopen management from the existing footer action.
- [x] Save optional Preferences/Analytics/Marketing selections and confirm persisted state after reload.
- [x] Revoke consent and confirm expected banner/state behavior after reload.
- [x] Confirm no absolute Commerce Node call is made from the browser.

Toast scenarios:

- [x] Trigger a reachable success toast through an actual product/cart browser flow.
- [x] Trigger a reachable error toast through a real invalid/blocking browser flow when the fixture safely supports it.
- [x] Confirm correct icon, heading, message, level, enter/visible state, close control, and dismissal behavior.
- [x] Confirm only one toast region exists after enhanced navigation and repeated actions.
- [x] Confirm no duplicate event listeners create duplicate toasts.
- [x] Use the existing JavaScript split proof for info/warning template states if no production user flow emits those levels.
- [x] Do not add a production QA endpoint or global test-only JavaScript hook solely to trigger info/warning.

Viewport and quality coverage:

- [x] Desktop viewport: 1440x900 or repository-standard equivalent.
- [x] Mobile viewport: 390x844 or repository-standard equivalent.
- [x] Product panel content does not overflow or overlap.
- [x] Consent banner controls remain reachable and do not obscure required content incoherently.
- [x] Toast region and cards remain within viewport and do not block primary actions after dismissal.
- [x] No unexpected console errors.
- [x] No failed purchase/consent requests outside intentionally tested failure scenarios.
- [x] Capture screenshots for product simple, product variant, consent open, and toast visible states.

Exit criteria:

- [x] real purchase and consent browser workflows pass;
- [x] reachable toast success/error workflows pass;
- [x] JS proof covers remaining semantic toast levels;
- [x] desktop and mobile evidence exists;
- [x] no selector, event, layout, or visual regression is observed.

### Phase 3.4.7 execution evidence (2026-08-11)

- Started the configured V2 environment with `./scripts/run-v2-local.ps1 -StopExisting -NoOpenBrowser`; `http://localhost:5180/health`, `http://localhost:18598/health`, and `http://localhost:18598/` returned HTTP 200.
- Tested `http://localhost:18598` with store key `default` on commit `8211ee97`, using headed Chromium at 1440x900 and 390x844.
- Real same-origin purchase flows passed for `/product/qa-simple-product-100` and `/product/catalog-qa-t-shirt`; the latter updated price, stock, SKU, GTIN, image, and eligibility after variant selection. The cart command showed success feedback, a badge update, and one success toast.
- `node scripts/qa/storefront-browser-action-boundary-proof.js` and `node scripts/qa/storefront-browser-semantics-v2-proof.js` passed. They record only same-origin BFF calls and no direct Commerce Node calls.
- The default local fixture had consent disabled. It was enabled only for the browser run, then restored to `consent_enabled=false` and `consent_banner_required=false`. Actual BFF current/save/revoke requests returned HTTP 200; Essential-only, selected-preferences save, footer manage/reopen, reload persistence, and revoke/reload behavior were observed.
- No browser console errors were reported. A safe fixture did not expose an enabled error submitter after it became non-purchasable; the existing JS split proof remains the coverage for error/info/warning templates.
- Screenshots (ignored QA artifacts): `output/playwright/storefront-shared-semantic-render/product-simple-desktop.png`, `product-variant-desktop-1440.png`, `product-variant-mobile.png`, `consent-open-desktop.png`, `consent-open-mobile.png`, and `toast-success-desktop.png`.

## Phase 3.4.8 - Documentation, Duplication Audit, And Closure

Goal: make ownership discoverable, remove obsolete V2 markup, and close only with reproducible evidence.

Documentation files to review/update:

- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/README.md`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/README.md`.
- [x] `docs/architecture/05-project-and-folder-guide.md`.
- [x] `docs/architecture/10-v2-contract-ownership.md`.
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- [x] `AGENTS.md` because its active Ssr ownership statement was incomplete after extraction.

Required documentation corrections:

- [x] Document purchase semantic rendering in Primitives and final purchase visuals in V2.
- [x] Document consent semantic rendering in Ssr, behavior in Presentation JS, and registered visual wrapper/final presentation in V2.
- [x] Document toast semantic region/template in Primitives and placement/configuration/behavior in V2.
- [x] Clarify that neutral class-slot contracts are permitted while final class values are forbidden in shared packages.
- [x] Replace any statement that MainLayout owns inline toast DOM with a statement that MainLayout owns toast placement.
- [x] Preserve historical QA notes and append current ownership evidence instead of rewriting history.
- [x] Record Starter consent adoption as deferred and outside Phase 3.4.

Duplication audit:

```powershell
rg -n "data-storefront-product-purchase-panel|data-storefront-consent-banner|data-storefront-toast-region|data-storefront-toast-template" `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2 `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives `
  BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr
```

- [x] Purchase semantic implementation exists once in Primitives and is consumed by V2.
- [x] Consent semantic implementation exists once for V2 in Ssr; V2 retains only the thin wrapper.
- [x] Toast region/template implementation exists once in Primitives and is placed once by V2.
- [x] Starter duplicate is reported separately and is not counted as an accidental V2 duplicate.
- [x] No deleted component remains registered or referenced.
- [x] No empty compatibility file/folder is retained solely to preserve the old V2 location.

Closure verification:

- [x] Re-run the complete Phase 3.4.6 automated gate.
- [x] Re-run the required Phase 3.4.7 browser scenarios from a clean application restart.
- [x] Run `git diff --check`.
- [x] Review `git diff --stat` and full diff for scope drift.
- [x] Record commands, pass counts, browser URLs, viewports, screenshots, and known deferred items in this file or the QA checklist.
- [x] Change this plan status from `planned` to `complete` only after all required checks pass.

Fresh post-implementation review:

- [x] Re-run `rg` without assuming the planned paths are the only consumers.
- [x] Review package project references again.
- [x] Review rendered output rather than source ownership only.
- [x] Review Presentation/V2 JavaScript ownership again.
- [x] Confirm no final copy/class/route/default leaked into shared components.
- [x] Confirm no follow-up compatibility extraction is required for these three V2 surfaces.
- [x] A source scanner false-positive was fixed in the owning Phase 3.4 task and its full gate was repeated; no cleanup phase was created.

Final exit criteria:

- [x] all three approved semantic render extractions are active in V2;
- [x] old V2 duplicate implementations are removed;
- [x] final V2 classes, copy, placement, and visual behavior remain V2-owned;
- [x] purchase command and consent behavior remain Presentation-owned;
- [x] shared package dependency and visual-neutrality rules pass;
- [x] V2 and V2.WASM build and test successfully;
- [x] Playwright evidence confirms real browser behavior;
- [x] docs and QA accurately describe current ownership;
- [x] Starter remains explicitly deferred, not silently forgotten;
- [x] fresh review finds no unresolved Phase 3.4 blocker.

### Phase 3.4.8 closure evidence (2026-08-11)

- Re-ran the six-project build gate and the V2 Tailwind generation. All builds completed with `0 Warning(s), 0 Error(s)`; Tailwind retained the extracted V2 visual classes. `npm ci` still reports two pre-existing high-severity dependency advisories and Browserslist reports stale `caniuse-lite`.
- Focused Phase 3.4 gate passed `247/247`; `node scripts/qa/storefront-application-js-split-proof.js`, `storefront-browser-action-boundary-proof.js`, and `storefront-browser-semantics-v2-proof.js` passed. The final full `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore` passed `1962/1964`, with the existing two skips and no failures.
- Restarted the configured V2 runtime cleanly. Headless Chromium at `1440x900` revisited `http://localhost:18598/product/qa-simple-product-100`; actual same-origin consent current/save/revoke requests all returned `200`, Preferences persisted after reload, and revoke made the consent banner return after reload. Browser console reported `0` errors and `0` warnings. The local consent fixture was restored to `consent_enabled=false` and `consent_banner_required=false` immediately afterward.
- Existing browser artifacts remain at `output/playwright/storefront-shared-semantic-render/`: product simple/variant desktop, variant mobile, consent desktop/mobile, and success toast desktop. V2-only Starter consent adoption remains deferred by scope.

## Test Coverage Map

```text
ProductPurchasePanelModel + ProductPurchaseActionDescriptor
    -> StorefrontProductPurchasePanel primitive
        -> HtmlRenderer semantic tests
        -> V2 ProductPurchasePanelVisuals ownership tests
        -> V2ProductPageView composition tests
        -> Presentation purchase command JS tests
        -> Playwright simple/variant/blocked purchase flows

StorefrontConsentContext
    -> StorefrontConsentPanel SSR component
        -> HtmlRenderer semantic tests
        -> V2 thin wrapper + visual ownership tests
        -> Presentation native-hidden consent JS tests
        -> Host consent BFF tests
        -> Playwright save/manage/revoke flows

StorefrontToastRegion contracts
    -> StorefrontToastRegion primitive
        -> HtmlRenderer no-wrapper/hook tests
        -> MainLayout placement/count tests
        -> V2 CSS/JS selector tests
        -> JS split proof for all levels
        -> Playwright reachable success/error flows
```

## Implementation Order And Dependencies

```text
3.4.0 Baseline lock
    |
    v
3.4.1 Neutral contracts
    |
    +--> 3.4.2 Purchase primitive
    |
    +--> 3.4.3 Consent SSR + native hidden
    |
    +--> 3.4.4 Toast primitive
              |
              v
       3.4.5 Test/guardrail migration
              |
              v
       3.4.6 Automated gate
              |
              v
       3.4.7 Playwright QA
              |
              v
       3.4.8 Docs/audit/closure
```

- [x] Do not delete old V2 markup before the corresponding shared implementation, V2 adoption, and migrated tests compile together.
- [x] Product, consent, and toast extraction commits may remain separate, but Phase 3.4 is not complete until the common test/browser/docs gates pass.
- [x] A failed phase blocks downstream phases.

## Decision Audit Trail

| # | Review | Decision | Classification | Principle | Rationale | Rejected alternative |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | CEO | Extract exactly three approved surfaces | Auto-decided | Scope discipline | Matches current candidates without ecommerce feature expansion | Broad shared-component sweep |
| 2 | Engineering | Reuse and extend `ProductPurchaseLabels` | Auto-decided | Reuse existing contracts | Avoids duplicate public concepts | New `ProductPurchasePanelLabels` |
| 3 | Engineering | Keep class slots explicit and typed | Auto-decided | Compile-time safety | Matches existing gallery/navigation patterns | Untyped attribute/class dictionary |
| 4 | Engineering | Use Components.Primitives for purchase/toast and Components.Ssr for consent | Auto-decided | Dependency ownership | Matches actual model dependencies | Put all three in one package |
| 5 | Engineering | Keep thin V2 consent wrapper | Auto-decided | Preserve registration contract | `V2FoundationViewRegistration` needs a V2 visual type | Register shared component directly and remove V2 slot |
| 6 | Engineering | Replace consent `hidden` class coupling with native hidden state | Auto-decided | Portability and semantic behavior | Shared behavior must not require Tailwind | Require every host to provide a literal hidden class |
| 7 | Design | Correct product option grouping accessibility during extraction | Auto-decided | Correct semantics | Current label-to-div association is invalid | Preserve invalid association indefinitely |
| 8 | Design | Preserve current final visuals and DOM order | Auto-decided | Regression control | Phase is extraction, not redesign | Visual refresh during move |
| 9 | Engineering | Keep Starter out of implementation scope but disclose it as deferred | Approved user direction | Scope truthfulness | Current work focuses on V2/V2.WASM | Claim repository-wide consent deduplication |
| 10 | Engineering | Require HtmlRenderer tests plus source guardrails | Auto-decided | Behavioral evidence | Source checks alone cannot prove Razor output | Source-string assertions only |
| 11 | QA | Build V2.WASM unconditionally | Auto-decided | Transitive compatibility | Shared contracts affect its host graph | Build only V2 server host |
| 12 | QA | Browser-test real purchase/consent flows | Approved project standard | Production confidence | Smoke/source tests do not prove user behavior | DOM-only smoke test |
| 13 | QA | Cover unreachable toast levels with JS proof, not production QA hooks | Auto-decided | Avoid test-only production surface | Existing proof can validate all templates | Add global production test hook |
| 14 | DX | Reserve completion claim until fresh post-review | Auto-decided | Evidence-based closure | Prevents a final cleanup phase caused by stale assumptions | Mark complete after build only |

## Definition Of Done

```text
Architecture
[x] Purchase and toast primitives reference Components only.
[x] Consent SSR component references only approved Components/Presentation contracts.
[x] No shared component references V2, V2.WASM, Browser, Runtime, Client, or backend projects.
[x] No shared component owns final V2 classes, copy, routes, JS behavior, or render mode.

Product purchase
[x] Shared primitive renders the complete current purchase semantic contract.
[x] V2 supplies final classes and labels.
[x] Valid accessible grouping replaces label-to-div association.
[x] Invalid/missing colors do not leak unsafe or V2-specific fallback styles.
[x] Actual add-to-cart and variant selection-preview flows pass.

Consent
[x] Shared SSR component renders the complete consent semantic contract.
[x] Thin V2 wrapper remains registered.
[x] Presentation owns current/save/revoke and native hidden state.
[x] Actual save/manage/revoke flows pass without initial flash.
[x] Starter remains unchanged and explicitly deferred.

Toast
[x] Shared primitive renders one sibling region/template pair without a wrapper.
[x] MainLayout owns placement and contains no duplicated inline toast markup.
[x] V2 owns classes, accessible close copy, CSS, and visual JavaScript behavior.
[x] JS proof covers all levels and browser QA covers reachable real flows.

Verification
[x] Components, Primitives, Ssr, Presentation, V2, and V2.WASM build.
[x] V2 Tailwind asset generation retains the moved visual classes.
[x] Focused rendered, architecture, ownership, and JavaScript tests pass.
[x] Playwright desktop/mobile evidence exists.
[x] No unexpected console/network errors remain.
[x] Documentation and QA checklist match the final code.
[x] Fresh review finds no unresolved blocker.
```

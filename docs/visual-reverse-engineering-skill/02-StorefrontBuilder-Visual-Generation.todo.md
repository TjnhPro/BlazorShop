# StorefrontBuilder Visual Generation.todo

Status: in-progress
Scope: BlazorShop.AI.StorefrontBuilder Visual Reverse Engineering Skill MVP
Phase group: S10-S19

## Purpose

Turn captured reference-site evidence into visual artifacts, composition decisions, generated CSS, generated presentation components, and a generated storefront project named `BlazorShop.Storefront.{Name}`.

This phase group depends on `01-StorefrontBuilder-Foundation.todo.md`.

## Current Codebase Anchors

- Starter project to read from: `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter`
- Generated project target pattern: `BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}`
- Generated client package boundary: `BlazorShop.Storefront.Client`
- Runtime package boundary: `BlazorShop.Storefront.Runtime`
- Existing feature activation example: `Features/feature-manifest.json`
- Existing render ownership folders: `Pages/Ssr`, `Pages/Hybrid`, `Pages/WasmHost`
- Existing BFF pattern: `Endpoints/StarterBffEndpoints.cs`
- Existing hydration marker: `Composition/StarterHydrationMode.cs`

## S10 - Design Token Extraction

Goal: extract a target visual foundation from evidence.

Token groups:

- [x] Color palette.
- [x] Semantic color candidates.
- [x] Typography families.
- [x] Font sizes.
- [x] Font weights.
- [x] Line heights.
- [x] Spacing scale.
- [x] Container widths.
- [x] Breakpoints.
- [x] Border widths.
- [x] Border radius.
- [x] Shadows.
- [x] Motion durations.
- [x] Motion easing.

Rules:

- [x] Prefer computed styles over screenshot color picking when available.
- [x] Use screenshot sampling only when computed styles are insufficient.
- [x] Keep token names semantic enough for generated CSS maintenance.
- [x] Record confidence and evidence IDs.
- [x] Record AI inference entries for uncertain values.

Output:

- [x] `design-tokens.yaml`

Done when:

- [x] Global visual language can be generated without re-reading screenshots manually.

Verification:

- [x] Token schema validation.
- [x] Fixture extraction produces stable token output.

## S11 - UI Pattern Inventory

Goal: identify reusable ecommerce UI patterns from the target site.

Patterns:

- [x] Header.
- [x] Footer.
- [x] Main navigation.
- [x] Mobile navigation.
- [x] Breadcrumb.
- [x] Product card.
- [x] Category card.
- [x] Banner/hero section.
- [x] Product grid.
- [x] Product gallery.
- [x] Product information block.
- [x] Product purchase block.
- [x] Primary button.
- [x] Secondary button.
- [x] Icon button.
- [x] Text input.
- [x] Search input.
- [x] Select.
- [x] Checkbox.
- [x] Quantity control.
- [x] Pagination.
- [x] Empty state.
- [x] Error state.
- [x] Loading state.

Each pattern records:

- [x] Evidence IDs.
- [x] DOM selector samples.
- [x] Visual properties.
- [x] States observed.
- [x] Responsive notes.
- [x] Target slot if known.
- [x] Fallback behavior.

Output:

- [x] `ui-patterns.yaml`

Done when:

- [x] Pattern inventory covers shell, catalog, product, controls, and state components.

Verification:

- [x] Pattern schema validation.
- [x] Validator fails if product card or product purchase pattern is missing without fallback reason.

## S12 - Behavior And Responsive Model

Goal: classify interactive and responsive behavior before generation.

Behavior classes:

- [x] CSS-only.
- [x] Hover-driven.
- [x] Focus-driven.
- [x] Click-driven visual-only.
- [x] Scroll-driven visual-only.
- [x] Starter-feature-driven.
- [x] BFF-action-driven.
- [x] Approved JS interop.
- [x] Unsupported.

Responsive records:

- [x] Breakpoint.
- [x] Layout change.
- [x] Header/nav behavior.
- [x] Product grid columns.
- [x] Product detail media/action stacking.
- [x] Footer stacking.
- [x] Sticky/fixed elements.
- [x] Drawer/menu behavior.

Rules:

- [x] Commerce state changes must be Starter-feature-driven or BFF-action-driven.
- [x] JS interop cannot own cart/checkout/account state.
- [x] Unsupported target behavior must be hidden or replaced by Starter fallback.

Output:

- [x] `behaviors.yaml`
- [x] `responsive.yaml`

Done when:

- [x] Generator knows which interactions are visual and which must bind to Starter contracts.

Verification:

- [x] Validator fails if add-to-cart is classified as direct JS or direct HTTP.

## S13 - Layout Topology

Goal: model page and shell structure as generation input.

Topologies:

- [x] Global shell.
- [x] Home page sections.
- [x] Catalog page regions.
- [x] Search result page regions if present.
- [x] Product detail regions.
- [x] Cart fallback style regions.
- [x] Checkout fallback style regions.
- [x] Account fallback style regions.
- [x] Content/error/system page shell.

Region metadata:

- [x] Region ID.
- [x] Parent region.
- [x] Slot ID.
- [x] Render owner.
- [x] Hydration mode.
- [x] Source: target, target-with-starter-binding, starter, hidden, unsupported.
- [x] Evidence IDs.
- [x] Responsive behavior.

Output:

- [x] `page-topology.yaml`

Done when:

- [x] Composition can be generated without re-inventing page structure.

Verification:

- [x] Topology schema validation.
- [x] Required slots from Starter contract are either mapped or explicitly skipped with reason.

## S14 - Lightweight Ecommerce Capability Mapping

Goal: map target visual features to actual Starter/backend capability.

Inputs:

- [x] Starter feature manifest.
- [x] Backend public configuration feature map.
- [x] Store module manifest if available.
- [x] Target visual detections.
- [x] Starter generation contract slots.

Decision values:

- [x] `target`
- [x] `target-with-starter-binding`
- [x] `starter`
- [x] `hidden`
- [x] `unsupported`

Examples:

- [x] Product gallery visual exists and Starter slot exists: `target-with-starter-binding`.
- [x] Wishlist visual exists but backend capability missing: `hidden` or `unsupported`.
- [x] Target checkout unavailable: `starter`.
- [x] Product reviews visual exists but module unavailable: `hidden`.
- [x] Cart badge visual exists and BFF slot exists: `target-with-starter-binding`.

Output:

- [x] `capability-decisions.yaml`

Done when:

- [x] No target ecommerce feature is silently faked.

Verification:

- [x] Validator fails when `target-with-starter-binding` references a missing slot.
- [x] Validator fails when unsupported feature has no user-facing fallback decision.

## S15 - Composition Manifest

Goal: produce the main generation input from topology, tokens, patterns, and capability decisions.

Manifest must include:

- [x] Project name: `BlazorShop.Storefront.{Name}`.
- [x] Store key.
- [x] Source Starter path.
- [x] Starter contract version.
- [x] Storefront.Client package version.
- [x] Storefront.Runtime package version.
- [x] Generated file root.
- [x] Asset root.
- [x] Shell composition.
- [x] Page composition.
- [x] Slot bindings.
- [x] Feature decisions.
- [x] Fallback pages.
- [x] Evidence references.
- [x] Inference references.

Output:

- [x] `composition-manifest.yaml`

Done when:

- [x] The generator can run from this manifest without re-analyzing the reference site.

Verification:

- [x] Manifest schema validation.
- [x] Missing Starter contract version fails.
- [x] Missing package versions fail.

## S16 - Generation Ownership Plan

Goal: write an exact file-level plan before creating or changing generated project files.

Each file entry includes:

- [ ] File path.
- [ ] Ownership: generated, managed, protected, external.
- [ ] Action: create, replace, patch, skip.
- [ ] Source artifact IDs.
- [ ] Expected slot.
- [ ] Validation rule IDs.
- [ ] Conflict behavior.

Rules:

- [ ] Protected files cannot have create/replace/patch action.
- [ ] Existing manual files cannot be replaced unless marked generated by previous manifest.
- [ ] New project must be generated from Starter first, then visual files are applied.
- [ ] Re-generation must compare source/spec hash and generated hash.

Output:

- [ ] `generation-plan.yaml`

Done when:

- [ ] Every generated change is known before file edits.

Verification:

- [ ] Dry-run mode prints the file plan.
- [ ] Validator fails if a protected file is scheduled for edit.

## S17 - Generated Storefront Project Creation

Goal: create or refresh `BlazorShop.Storefront.{Name}` from Starter without copying V2.

Tasks:

- [ ] Extend or wrap existing deterministic sample generation pattern.
- [ ] Accept `-Name BlazorShop.Storefront.{Name}`.
- [ ] Accept `-StoreKey`.
- [ ] Copy Starter template.
- [ ] Rewrite namespace/root namespace safely.
- [ ] Keep package references to Storefront.Client and Storefront.Runtime.
- [ ] Keep Starter BFF/security files protected.
- [ ] Preserve feature manifest unless generation plan changes allowed placements.
- [ ] Create `docs/storefront-analysis`.
- [ ] Write `metadata.yaml`.

Done when:

- [ ] A named generated storefront builds before visual changes.

Verification:

- [ ] `dotnet restore BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}/BlazorShop.Storefront.{Name}.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}/BlazorShop.Storefront.{Name}.csproj --no-restore`
- [ ] Dependency guard confirms no backend/core/API/V2 references.

## S18 - Visual Foundation Generation

Goal: generate theme CSS and reusable style primitives.

Generated surfaces:

- [ ] CSS custom properties.
- [ ] Typography rules.
- [ ] Container rules.
- [ ] Grid helpers.
- [ ] Button styles.
- [ ] Input/select styles.
- [ ] Card styles.
- [ ] Product image 1x1 rules.
- [ ] Focus states.
- [ ] Loading/empty/error states.
- [ ] Responsive media queries.

Rules:

- [ ] Keep CSS under generated storefront `wwwroot`.
- [ ] Do not alter Runtime, generated client, or BFF code.
- [ ] Do not inject arbitrary third-party scripts.
- [ ] Use stable class naming.

Done when:

- [ ] Starter pages visually adopt target tokens while preserving Starter behavior.

Verification:

- [ ] CSS lint or static syntax validation.
- [ ] Browser render check for home/catalog/product at desktop/tablet/mobile.

## S19 - Shell, Home, Catalog, And Product Composition Generation

Goal: generate the first useful storefront presentation.

Shell:

- [ ] Header.
- [ ] Footer.
- [ ] Navigation.
- [ ] Mobile navigation.
- [ ] Search presentation.
- [ ] Cart badge presentation.
- [ ] Account menu presentation.

Home:

- [ ] Target section order.
- [ ] Static sections.
- [ ] Content-backed sections.
- [ ] Catalog-backed product sections.
- [ ] Starter fallback sections for missing data.

Catalog:

- [ ] Category header.
- [ ] Breadcrumb.
- [ ] Product grid.
- [ ] Product card.
- [ ] Sorting presentation.
- [ ] Filter presentation.
- [ ] Pagination presentation.
- [ ] Empty state.

Product:

- [ ] Product gallery region.
- [ ] Product information region.
- [ ] Product purchase region.
- [ ] Quantity control presentation.
- [ ] Add-to-cart/buy presentation binding.
- [ ] Product content/additional sections.

Rules:

- [ ] Product purchase binds only through Starter slot/action contract.
- [ ] Product gallery remains presentation and uses safe product media data.
- [ ] Product images stay 1x1 unless Starter contract explicitly changes.
- [ ] Cart/Checkout/Account remain Starter fallback pages themed with target tokens.

Done when:

- [ ] Generated storefront has shell, home, catalog, product, cart fallback, checkout fallback, and account fallback.

Verification:

- [ ] Build passes.
- [ ] Browser can navigate generated home, catalog, product, cart, checkout, and account routes.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | S10 | Extract tokens from evidence before CSS generation | Auto-decided | Artifact first | CSS generated directly from screenshots is hard to debug. | Prompt-only CSS generation |
| 2 | S14 | Use capability decisions to hide unsupported target features | User direction preserved | No fake features | Storefront must not pretend unsupported ecommerce features exist. | Fake wishlist/review/inventory UI |
| 3 | S16 | Require generation ownership plan before edits | User direction preserved | Protect codebase | Prevents AI from editing protected Starter/runtime/security files. | Direct generation without file plan |
| 4 | S17 | Generate named project from Starter template | User direction preserved | Starter remains neutral | Store-specific output belongs in `Storefront.{Name}`. | Mutating Starter baseline |
| 5 | S19 | Keep Cart/Checkout/Account as Starter fallback for MVP | Auto-decided | Enough usable scope | These are behavior-heavy and already protected behind Starter contracts. | Reconstructing target checkout/account flows |

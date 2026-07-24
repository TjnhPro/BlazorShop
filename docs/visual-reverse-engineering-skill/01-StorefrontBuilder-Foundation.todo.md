# StorefrontBuilder Foundation.todo

Status: in-progress
Scope: BlazorShop.AI.StorefrontBuilder Visual Reverse Engineering Skill MVP
Phase group: S0-S9

## Purpose

Create the foundation that lets a development-time StorefrontBuilder skill analyze a reference ecommerce site and prepare generation safely.

This phase group must not generate store presentation yet. It locks the architecture, contract, schemas, preflight rules, and evidence model so later generation targets `BlazorShop.Storefront.{Name}` instead of mutating `BlazorShop.Storefront.Starter`.

## Current Codebase Anchors

- Starter source: `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter`
- Sample proof: `BlazorShop.PresentationV2/BlazorShop.Storefront.Sample`
- Generated client package: `BlazorShop.PresentationV2/BlazorShop.Storefront.Client`
- Neutral runtime package: `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime`
- Existing generator: `scripts/generate-storefront-sample.ps1`
- Starter isolation gate: `scripts/qa/run-storefront-starter-isolation-gate.ps1`
- Sample release gate: `scripts/qa/run-storefront-sample-release-gate.ps1`
- AI generator guardrails: `docs/storefront-platform/storefront-ai-generator-plan.md`
- Starter ADR: `docs/architecture/adr/2026-07-24-storefront-starter-foundation.md`
- Starter feature manifest: `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Features/feature-manifest.json`
- Starter route ownership: `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/README.md`

## Non-Negotiable Rules

- StorefrontBuilder is development-time tooling, not a production ASP.NET service.
- StorefrontBuilder generates into `BlazorShop.Storefront.{Name}`.
- StorefrontBuilder must not write store-specific presentation into `BlazorShop.Storefront.Starter`.
- Starter remains the neutral source skeleton.
- Generated storefronts consume `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` through package boundaries.
- Browser/WASM code must stay behind same-origin BFF endpoints.
- Generated visual components must not call Commerce Node directly.
- Generated code must not duplicate generated API DTOs.
- Business truth stays in Commerce Node API, Storefront.Client, Runtime, and Starter BFF contracts.

## S0 - Architecture And Scope Lock

Goal: lock the MVP as a safe visual reverse-engineering skill.

Tasks:

- [x] Add a short ADR or planning note under `docs/visual-reverse-engineering-skill/` explaining that StorefrontBuilder is development-time only.
- [x] Define generated storefront target naming: `BlazorShop.Storefront.{Name}`.
- [x] Define that `{Name}` must be normalized to a safe project/folder identifier.
- [x] Define that Starter is read-only template input for StorefrontBuilder generation.
- [x] Define that generated storefront output is the editable/tunable store project.
- [x] Reconfirm source-of-truth order:
  - [x] Storefront OpenAPI/client contract.
  - [x] Starter generation/runtime contract.
  - [x] Backend capability state.
  - [x] Starter feature manifest.
  - [x] Visual evidence from target.
  - [x] AI inference.
- [x] Reconfirm out-of-scope:
  - [x] API contract changes.
  - [x] Runtime security changes.
  - [x] BFF security changes.
  - [x] Cart/checkout/order/payment/pricing/sellability business logic.
  - [x] Optional module installation.
  - [x] Marketplace UI.

Done when:

- [x] The role of StorefrontBuilder cannot be confused with runtime service or backend generator.
- [x] The plan clearly says generation targets `BlazorShop.Storefront.{Name}`, not Starter.

Verification:

- [x] `rg -n -F "StorefrontBuilder" docs/visual-reverse-engineering-skill; rg -n -F "BlazorShop.Storefront.{Name}" docs/visual-reverse-engineering-skill; rg -n -F "Starter" docs/visual-reverse-engineering-skill`

## S1 - Starter Generation Contract

Goal: create the machine-readable contract that tells StorefrontBuilder exactly where it may generate and what is protected.

Recommended file:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml
```

Contract must include:

- [x] Contract version.
- [x] Starter version.
- [x] Target framework.
- [x] Package dependencies expected by generated storefronts.
- [x] Generated project naming convention.
- [x] Generated output root convention.
- [x] Allowed generated zones.
- [x] Managed zones.
- [x] Protected zones.
- [x] Asset zones.
- [x] Analysis artifact zone.
- [x] Slot IDs.
- [x] Route ownership map.
- [x] Render ownership map: SSR, Hybrid, WASM-host.
- [x] Hydration modes matching `StarterHydrationMode`.
- [x] Feature manifest location.
- [x] Required BFF action names.
- [x] Build/test commands.
- [x] Isolation/release gate commands.

Minimum slots:

- [x] `layout.header`
- [x] `layout.footer`
- [x] `layout.main-navigation`
- [x] `layout.mobile-navigation`
- [x] `layout.cart-badge`
- [x] `layout.account-menu`
- [x] `home.sections`
- [x] `catalog.product-card`
- [x] `catalog.filters`
- [x] `catalog.sorting`
- [x] `catalog.pagination`
- [x] `product.gallery`
- [x] `product.information`
- [x] `product.purchase`
- [x] `cart.page`
- [x] `checkout.page`
- [x] `account.shell`
- [x] `system.error`

Done when:

- [x] StorefrontBuilder can answer: "Which files may I generate?"
- [x] StorefrontBuilder can answer: "Which files are protected?"
- [x] StorefrontBuilder can answer: "Which slot owns add-to-cart presentation?"
- [x] StorefrontBuilder can answer: "Which route is SSR, Hybrid, or WASM-host?"

Verification:

- [x] Add schema validation for `starter-generation.contract.yaml`.
- [x] Add test that required slot IDs exist.
- [x] Add test that protected zones include generated client, Runtime security, BFF security, package manifests, and generated storefront manifests.

## S2 - StorefrontBuilder Repository Or Tooling Location

Goal: decide where the skill MVP lives without coupling it to production runtime.

Preferred long-term location:

```text
BlazorShop.AI.StorefrontBuilder/
```

Monorepo POC location if separate repository is not ready:

```text
tools/BlazorShop.AI.StorefrontBuilder/
```

Required structure:

- [x] `skills/storefront-builder/SKILL.md`
- [x] `knowledge/visual-reverse-engineering.md`
- [x] `knowledge/blazor-starter-boundaries.md`
- [x] `knowledge/ecommerce-visual-patterns.md`
- [x] `knowledge/asset-safety.md`
- [x] `schemas/*.schema.json`
- [x] `templates/*.template.yaml`
- [x] `scripts/capture`
- [x] `scripts/validate`
- [x] `scripts/generate`
- [x] `scripts/visual-qa`
- [x] `tests/schemas`
- [x] `tests/generation`
- [x] `tests/playwright`

Done when:

- [x] Tooling has no production project reference from BlazorShop runtime projects.
- [x] Build/test/runtime projects do not depend on StorefrontBuilder.

Verification:

- [x] `rg -n -F "BlazorShop.AI.StorefrontBuilder" BlazorShop.PresentationV2 -g "*.csproj"; rg -n -F "tools/BlazorShop.AI.StorefrontBuilder" BlazorShop.PresentationV2 -g "*.csproj"`
- [x] Confirm no production `.csproj` references StorefrontBuilder.

## S3 - Artifact Schema Foundation

Goal: make every intermediate result reviewable before generation.

Schemas required:

- [ ] `metadata.schema.json`
- [ ] `page-inventory.schema.json`
- [ ] `page-topology.schema.json`
- [ ] `design-tokens.schema.json`
- [ ] `ui-patterns.schema.json`
- [ ] `behaviors.schema.json`
- [ ] `responsive.schema.json`
- [ ] `capability-decisions.schema.json`
- [ ] `composition-manifest.schema.json`
- [ ] `generation-plan.schema.json`
- [ ] `generated-files.schema.json`
- [ ] `asset-manifest.schema.json`
- [ ] `ai-inference-log.schema.json`

Generated storefront artifact path:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}/docs/storefront-analysis/
```

Done when:

- [ ] Every artifact has a JSON schema.
- [ ] Missing required fields fail validation before generation.
- [ ] AI inference is never stored only in prose.

Verification:

- [ ] Schema tests cover valid and invalid fixtures.
- [ ] Invalid artifact examples fail with actionable error messages.

## S4 - Preflight And Input Validation

Goal: reject unsafe or incomplete generation requests before browser capture.

Inputs:

- [ ] Reference URL list.
- [ ] Storefront project name `{Name}`.
- [ ] Store key.
- [ ] Starter source path.
- [ ] Starter generation contract path.
- [ ] Storefront API/client version metadata.
- [ ] Capability snapshot source.
- [ ] Feature manifest path.
- [ ] Output project root.
- [ ] Mode: analyze-only, plan-only, generate, update, validate-only, full.

Validation:

- [ ] URL must be http/https.
- [ ] `{Name}` must be safe for project/folder/namespace.
- [ ] Output must resolve inside the approved workspace or explicit external target root.
- [ ] Existing output project requires `update` or `--force`.
- [ ] Starter contract must pass schema validation.
- [ ] Package versions must be resolvable.
- [ ] Required gates must exist.
- [ ] Protected path list must be non-empty.

Done when:

- [ ] Bad input fails before creating files.
- [ ] Error messages explain problem, cause, and fix.

Verification:

- [ ] Unit tests for invalid URL, invalid project name, missing contract, protected output path, and existing output conflict.

## S5 - Protected File And Dependency Guard

Goal: stop generated code from breaking the established Storefront architecture.

Forbidden generated content:

- [ ] `HttpClient` in generated presentation components.
- [ ] Direct Commerce Node API URL.
- [ ] Browser token/local-storage credential handling.
- [ ] `ProjectReference` to backend/core/API/V2 projects.
- [ ] `using BlazorShop.Application`.
- [ ] `using BlazorShop.Domain`.
- [ ] `using BlazorShop.Infrastructure`.
- [ ] `using BlazorShop.PresentationV2.BlazorShop.CommerceNode.API`.
- [ ] Handwritten duplicate API DTOs when generated DTO exists.
- [ ] Pricing/sellability/cart/checkout/order/payment validation logic in presentation.

Allowed generated content:

- [ ] CSS.
- [ ] layout components.
- [ ] presentation-only Razor components.
- [ ] page composition.
- [ ] store assets and manifests.
- [ ] copy/content.
- [ ] slot bindings declared by Starter contract.

Done when:

- [ ] Guard can scan generated project and fail on forbidden imports, APIs, and dependencies.

Verification:

- [ ] Add tests with intentionally bad generated files.
- [ ] Guard failure includes exact file path and rule ID.

## S6 - Browser Capture Adapter

Goal: capture visual evidence from reference websites using browser automation without building a heavy custom browser stack first.

Recommended MVP:

- [ ] Use Playwright-based capture as first adapter.
- [ ] Keep provider abstraction small enough to replace later if needed.
- [ ] Do not require a custom MCP server for MVP.

Capture operations:

- [ ] Navigate.
- [ ] Wait for network/DOM readiness.
- [ ] Resize viewport.
- [ ] Scroll.
- [ ] Click.
- [ ] Hover.
- [ ] Focus.
- [ ] Screenshot.
- [ ] Read DOM snapshot.
- [ ] Read computed styles.
- [ ] Read bounding boxes.
- [ ] Read asset URLs.

Viewports:

- [ ] Desktop 1440px.
- [ ] Tablet 768px.
- [ ] Mobile 390px.

Done when:

- [ ] One URL can produce screenshots, DOM snapshot, computed-style evidence, bounding boxes, and asset list.

Verification:

- [ ] Playwright fixture test captures a static local test page at all viewports.

## S7 - Page Discovery And Archetype Inventory

Goal: find representative pages without crawling an entire site.

Priority archetypes:

- [ ] Home.
- [ ] Catalog/category.
- [ ] Product detail.
- [ ] Search result if discoverable.
- [ ] Cart if public/reachable.
- [ ] Checkout if test flow is configured.
- [ ] Login/account shell if public/reachable.
- [ ] Important content page.

Rules:

- [ ] Multiple products with the same layout become one product archetype.
- [ ] Multiple categories with the same layout become one catalog archetype.
- [ ] Non-ecommerce pages are optional unless they affect shell/layout.
- [ ] Capture must record how URL was discovered.

Output:

- [ ] `page-inventory.yaml`

Done when:

- [ ] Each archetype has URL, evidence path, confidence, and reason.

Verification:

- [ ] Inventory schema validation.
- [ ] Fixture site with duplicate product/category pages collapses correctly.

## S8 - Evidence Storage And Traceability

Goal: make every generated decision traceable to evidence or explicit inference.

Evidence path:

```text
docs/storefront-analysis/evidence/
```

Required folders:

- [ ] `home`
- [ ] `catalog`
- [ ] `product`
- [ ] `cart`
- [ ] `checkout`
- [ ] `account`
- [ ] `content`
- [ ] `shared`

Evidence metadata:

- [ ] URL.
- [ ] Timestamp.
- [ ] Viewport.
- [ ] Browser.
- [ ] Screenshot file.
- [ ] DOM snapshot file.
- [ ] Computed-style sample file.
- [ ] Asset list file.
- [ ] Interaction state.

Done when:

- [ ] Generated plans can reference exact evidence IDs.

Verification:

- [ ] Artifact validator fails when referenced evidence does not exist.

## S9 - AI Inference Log

Goal: keep AI guesses visible and reviewable.

Log every inferred decision:

- [ ] Token inference.
- [ ] Component role inference.
- [ ] Responsive behavior inference.
- [ ] Hidden/unsupported feature inference.
- [ ] Asset replacement inference.
- [ ] Layout fallback inference.

Each entry must include:

- [ ] Inference ID.
- [ ] Decision.
- [ ] Evidence IDs.
- [ ] Confidence.
- [ ] Alternatives considered.
- [ ] Impact if wrong.
- [ ] Human review status.

Done when:

- [ ] No generation plan depends on unlogged inference.

Verification:

- [ ] Validator fails when a generation decision marks `source: inference` without an inference-log entry.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | S0 | Generate into `BlazorShop.Storefront.{Name}`, not Starter | User direction preserved | Boundary first | Starter must remain neutral and reusable. | Store-specific generation inside Starter |
| 2 | S1 | Require machine-readable Starter generation contract before code generation | User direction preserved | Contract before code | AI cannot safely know slots/protected files from prose docs alone. | Hard-coded folder assumptions |
| 3 | S3 | Require artifact schemas before generation | Auto-decided | Reviewable intermediates | Debugging generated UI requires traceable artifacts. | Free-form prompt-only generation |
| 4 | S5 | Add forbidden API/dependency guard for generated presentation | User direction preserved | Preserve security boundary | Prevents browser/direct API and DTO clone regressions. | Manual review only |
| 5 | S9 | Add AI inference log as first-class artifact | User direction preserved | No hidden guesses | The project goal is accurate generation without guessing. | Implicit AI decisions |

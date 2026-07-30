# StorefrontReverseEngineering Phase 3B - Visual Analysis And Ecommerce Mapping

Status: In progress  
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`  
Target folder: `docs/visual-reverse-engineering-skill`  
Depends on: Phase 3A final-fix evidence foundation  
Primary goal: convert Phase 3A rendered evidence into a structured, semantic, reviewable Visual Blueprint v1.  

## Current Codebase Facts

- `BlazorShop.AI.StorefrontReverseEngineering` is a development-time executable only.
- ReverseEngineering artifacts live under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` or `obj/storefront-reverse-engineering/projects/{ProjectId}`.
- Phase 3A currently produces capture artifacts, normalized element/asset evidence, draft page topology, draft page/component specifications, `analysis/visual-blueprint.draft.json`, originality audit, workflow run state, and readiness reports.
- Current CLI commands are `init`, `discover`, `capture`, `analyze`, `inspect`, `validate`, `run`, and `resume`.
- Current workflow steps are `initialize-project`, `discover-reference`, `capture-viewport-{viewportId}`, `analyze-draft`, `originality-audit`, and `validate-readiness`.
- Current `VisualBlueprintDraft` is a handoff shell containing page/component specification IDs, evidence IDs, generation restrictions, and confidence. It is not yet a rich visual blueprint.
- Phase 3A docs explicitly defer design-token extraction, semantic token normalization, section segmentation, responsive comparison, component detection, ecommerce region mapping, confidence scoring, human review, and StorefrontBuilder consumption.
- `StorefrontBuilder` generation must not consume Phase 3B output until a later approved phase explicitly wires it.
- `StorefrontFoundationViewSet` and `starter-generation.contract.yaml` are the current Presentation/Starter mapping sources of truth.

## Scope

In scope:

- Evidence aggregation from Phase 3A artifacts.
- Deterministic visual analysis.
- Design-token extraction.
- Semantic token draft.
- Page archetype classification.
- Section segmentation.
- Responsive behavior modeling.
- Interaction interpretation.
- Visual component candidate detection.
- Ecommerce region classification.
- Presentation component catalog and mapping.
- Unsupported pattern detection.
- Confidence scoring.
- JSON/Markdown human review workflow.
- Visual Blueprint v1 draft/reviewed artifacts.
- Generation readiness report for later Phase 4 handoff.
- Phase 3B local QA gate.

Not in scope:

- Generating Razor, CSS, Tailwind, Blazor components, React components, or storefront projects.
- Changing Commerce Node, Control Plane, Storefront Runtime, Storefront Presentation, Storefront V2, or Starter runtime behavior.
- Making StorefrontBuilder actively consume `visual-blueprint.v1.*.json`.
- Replacing StorefrontBuilder Node Playwright capture scripts.
- Reopening Phase 3A capture fallback, readiness depth, inspect state, or Node bridge cleanup.
- Executing cart, checkout, payment, account, auth, or Commerce API mutations.
- Declaring reference assets/copy/logo safe to reuse without human review.

## Architecture Principles

- Evidence-first: every conclusion must reference evidence IDs, page ID, viewport ID, artifact path, confidence, and reason code.
- Deterministic before AI: exact extraction, clustering, comparison, validation, and catalog mapping must work with `--no-ai`.
- Unknown is valid: low-confidence or unsupported areas must remain `unknown` or `no-match`, not forced into an archetype/component.
- Presentation-first mapping: map visual evidence to neutral visual candidates first, then to Presentation/Starter slots and contracts.
- Behavior boundary: Phase 3B describes visual role and required contract shape only; it does not implement ecommerce behavior.
- Draft/reviewed separation: human review decisions must produce reviewed artifacts without overwriting draft artifacts.
- Gate before handoff: Phase 4 may only read reviewed blueprint when Phase 3B readiness passes.

## Proposed Project Shape

Create folders only when the corresponding phase lands:

```text
tools/BlazorShop.AI.StorefrontReverseEngineering/
  Analysis/
    Aggregation/
    Tokens/
    Pages/
    Segmentation/
    Responsive/
    Interactions/
    Components/
    Ecommerce/
    Mapping/
    Confidence/
    Review/
    Blueprint/
  PresentationCatalog/
    Contracts/
    Loading/
    Validation/
  Schemas/
```

Do not create empty folders just to mirror the future structure.

## Artifact Rules

- Every new JSON artifact needs an `artifactKind`, `schemaVersion`, `artifactId`, `createdUtc`, and schema file.
- Every derived artifact must include source artifact paths or source evidence IDs.
- Every major conclusion must include `confidence`, `reasonCodes`, and `humanReviewRequired`.
- Review decisions must preserve the original proposed value and write reviewer metadata.
- Readiness reports must distinguish `blocking`, `warning`, and `info`.

## CLI Strategy

Do not add a large public command surface immediately.

Keep the existing workflow-driven model:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url <url> --name <name> --output-root obj/storefront-reverse-engineering/projects --no-ai
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project <project> --force-step <step>
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project <project>
```

Add only these public commands when needed:

- [ ] `analyze-all` as a clear alias for the full Phase 3B analysis workflow.
- [ ] `prepare-review` to create JSON/Markdown review packs.
- [ ] `apply-review` to create reviewed artifacts from decisions.
- [ ] `validate-generation-readiness` to validate Phase 3B handoff readiness.

Keep step-level execution behind `resume --force-step <step>` unless a command proves useful enough to expose.

## Phase 3B.0 - Prerequisite Closure Check

Goal: prove Phase 3A is still a trustworthy base before adding interpretation.

Implementation checklist:

- [x] Run the existing ReverseEngineering tests before modifying code.
- [x] Run or document why skipping `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1`.
- [x] Confirm `reports/readiness-report.json` remains the machine-readable source of truth.
- [x] Confirm `inspect` still reads readiness state from JSON, not old Markdown validation.
- [x] Confirm existing boundary scan still prevents production runtime references to ReverseEngineering.
- [x] Add a short Phase 3B preflight helper if needed:
  - [x] Verify project root has `project.json`.
  - [x] Verify latest run exists and is succeeded.
  - [x] Verify current readiness report passed.
  - [x] Verify `analysis/visual-blueprint.draft.json` exists.

Files likely touched:

- [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/Application`
- [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/tests`
- [x] `scripts/qa/run-storefront-reverse-engineering-phase3b-gate.ps1` not created in 3B.0; release gate ownership remains Phase 3B.15.

Tests:

- [x] Existing ReverseEngineering test project passes.
- [x] Phase 3A gate remains green or is intentionally skipped only for documented local missing Playwright setup.

Done when:

- [x] Phase 3B implementation starts from a passing Phase 3A project and does not repair Phase 3A capture/runtime behavior.

Implementation evidence:

- Baseline before Phase 3B code edits: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj` passed `99/99`.
- Baseline Phase 3A gate before Phase 3B analysis work: `powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1` passed. Report: `obj/storefront-reverse-engineering/reports/phase3a-final-fix-gate-20260730171608.md`.
- Gate output confirmed `inspect` reads `reports/readiness-report.json`, reported `Readiness passed: true`, zero blockers, zero warnings, and latest run status `Succeeded`.
- Gate boundary scan confirmed production projects still do not reference ReverseEngineering.
- Added `Phase3BPreflightService` to verify project root, latest succeeded run, passed Phase 3A readiness, and `analysis/visual-blueprint.draft.json` before Phase 3B analyzers run.
- Added preflight tests for ready project, missing project, failed latest run, failed readiness, and missing Phase 3A blueprint.
- Verification after preflight helper: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Phase3BPreflight|Cli|Boundary"` passed `32/32`.

## Phase 3B.1 - Evidence Snapshot And Aggregation

Goal: create one analysis-ready snapshot so later steps do not repeatedly parse and correlate raw artifact files differently.

Artifacts:

```text
analysis/evidence-snapshot.json
reports/evidence-snapshot.md
Schemas/evidence-snapshot.schema.json
```

Implementation checklist:

- [x] Add `Analysis/Aggregation` contracts:
  - [x] `EvidenceSnapshot`
  - [x] `EvidenceSnapshotPage`
  - [x] `EvidenceSnapshotViewport`
  - [x] `EvidenceSnapshotElement`
  - [x] `EvidenceSnapshotAsset`
  - [x] `EvidenceSnapshotIssue`
- [x] Load `project.json`.
- [x] Load `configuration.json`.
- [x] Load `discovery/capture-plan.json`.
- [x] Load each `captures/{pageId}/capture-manifest.json`.
- [x] Load each viewport `manifest.json`.
- [x] Load each viewport `element-evidence-index.json`.
- [x] Load each viewport `asset-inventory.normalized.json`.
- [x] Load each viewport `capture-quality-report.json`.
- [x] Load `analysis/originality-audit.json`.
- [x] Load interaction evidence when present.
- [x] Validate configured page/viewport coverage.
- [x] Validate capture correlation IDs across manifest, quality, element evidence, and asset inventory.
- [x] Detect orphan evidence.
- [x] Detect artifact kind/schema mismatch.
- [x] Preserve raw selector, text snippet, style groups, box, asset metadata, and viewport IDs.
- [x] Normalize artifact paths to repo-relative or project-relative paths.
- [x] Include `sourceArtifactPaths`.
- [x] Include `sourceEvidenceIds`.
- [x] Include blocking/warning/info issues.

Workflow:

- [x] Add `aggregate-evidence` step after `validate-readiness` or as first Phase 3B step after Phase 3A readiness.
- [x] Ensure downstream Phase 3B steps read `analysis/evidence-snapshot.json`, not raw capture files directly.
- [x] Add `--force-step aggregate-evidence` support through existing workflow runner.

Tests:

- [x] Multi-viewport evidence is merged.
- [x] Missing viewport produces blocking issue.
- [x] Orphan evidence is reported.
- [x] Capture correlation mismatch is blocking.
- [x] Schema mismatch is blocking.
- [x] Snapshot validates against schema.

Done when:

- [x] Snapshot contains every configured page and viewport.
- [x] Later steps can use snapshot as their only raw evidence input.

Implementation evidence:

- Added `Analysis/Aggregation/EvidenceSnapshotContracts.cs` and `EvidenceSnapshotAggregator` with project/config/capture-plan/readiness/originality/capture/viewport/element/asset/quality/interaction loading.
- Registered `Schemas/evidence-snapshot.schema.json` and added workflow step `aggregate-evidence` after `validate-readiness`; existing `--force-step` runner path can rerun it directly.
- Adjusted Phase 3B preflight/readiness handling so the Phase 3A baseline remains complete while the first Phase 3B workflow step is running.
- Added `EvidenceSnapshotAggregationTests` for multi-viewport merge, missing viewport artifact, orphan evidence, correlation mismatch, schema mismatch, optional interaction evidence, and schema validation.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "EvidenceSnapshot|Phase3BPreflight|Readiness_FailedAndPartialLatestRunFail"` passed `13/13`.

## Phase 3B.2 - Raw Design Token Extraction

Goal: extract literal visual tokens from computed styles and element boxes without assigning semantic names too early.

Artifacts:

```text
analysis/tokens/raw-design-tokens.json
analysis/tokens/token-frequency-report.json
Schemas/raw-design-tokens.schema.json
```

Token groups:

- [x] Color:
  - [x] background colors
  - [x] text colors
  - [x] border colors
  - [x] accent-like high-frequency colors
  - [x] overlay colors
  - [x] hover/focus colors when interaction evidence proves them
- [x] Typography:
  - [x] font family
  - [x] font size
  - [x] font weight
  - [x] line height
  - [x] letter spacing
  - [x] text transform
  - [x] heading/body/label/caption candidates
- [x] Spacing:
  - [x] margin
  - [x] padding
  - [x] gap
  - [x] section spacing
  - [x] container gutters
- [x] Shape:
  - [x] border radius
  - [x] border width/style
  - [x] shadow
  - [x] outline/focus ring
- [x] Layout:
  - [x] container widths
  - [x] max widths
  - [x] columns
  - [x] grid/flex signals
  - [x] aspect ratio
  - [x] image fit behavior
- [x] Motion:
  - [x] transition duration
  - [x] transition property
  - [x] timing function
  - [x] transform states, only when captured

Implementation checklist:

- [x] Add `RawDesignTokenExtractor`.
- [x] Add style value normalization that preserves original literal values.
- [x] Add frequency counting by project/page/viewport.
- [x] Add token source evidence IDs.
- [x] Add outlier detection.
- [x] Add hidden/noise element exclusion using Phase 3A capture policy signals.
- [x] Add near-duplicate clustering only when thresholds are explicit and tested.
- [x] Keep raw token names machine-generated, not semantic.
- [x] Do not invent missing hover/focus/motion tokens without interaction evidence.

Tests:

- [x] Colors are extracted from fixture styles.
- [x] Typography scale is extracted.
- [x] Spacing values are counted.
- [x] Outliers are reported without being merged.
- [x] Hidden/noise elements are ignored.
- [x] Raw artifact preserves literal values.

Done when:

- [x] Raw tokens are evidence-backed and deterministic.

Implementation evidence:

- Added `Analysis/Tokens/RawDesignTokenExtractor` and raw token/frequency report contracts.
- Registered `Schemas/raw-design-tokens.schema.json` and `Schemas/raw-design-token-frequency-report.schema.json`.
- Added workflow step `extract-raw-tokens` after `aggregate-evidence`; existing `--force-step` support can rerun it.
- Extractor reads `analysis/evidence-snapshot.json`, preserves literal values, normalizes comparable values, counts project/page/viewport frequency, records source evidence IDs and artifact paths, flags outliers, and assigns explicit 1px near-duplicate clusters without merging raw tokens.
- Extractor excludes hidden/configured noise elements using Phase 3A capture policy selectors and only extracts interaction state tokens from changed `interaction-evidence` style evidence.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "RawTokens"` passed `6/6`.

## Phase 3B.3 - Semantic Token Normalization

Goal: map raw token values into stable semantic roles while preserving ambiguity.

Artifacts:

```text
analysis/tokens/semantic-tokens.draft.json
analysis/tokens/token-conflicts.json
Schemas/semantic-tokens.schema.json
```

Semantic roles MVP:

- [x] Colors:
  - [x] `surface-page`
  - [x] `surface-section`
  - [x] `surface-card`
  - [x] `surface-elevated`
  - [x] `text-primary`
  - [x] `text-secondary`
  - [x] `text-muted`
  - [x] `text-inverse`
  - [x] `border-default`
  - [x] `border-strong`
  - [x] `accent-primary`
  - [x] `accent-secondary`
  - [x] `state-success`
  - [x] `state-warning`
  - [x] `state-error`
  - [x] `focus-ring`
  - [x] `overlay`
- [x] Typography:
  - [x] `font-body`
  - [x] `font-heading`
  - [x] `text-display`
  - [x] `text-h1`
  - [x] `text-h2`
  - [x] `text-h3`
  - [x] `text-body`
  - [x] `text-small`
  - [x] `text-label`
  - [x] `text-caption`
- [x] Spacing and shape:
  - [x] `space-1`
  - [x] `space-2`
  - [x] `space-3`
  - [x] `space-4`
  - [x] `space-5`
  - [x] `space-section`
  - [x] `space-container`
  - [x] `radius-small`
  - [x] `radius-medium`
  - [x] `radius-large`
  - [x] `radius-pill`
  - [x] `shadow-card`
  - [x] `shadow-elevated`

Implementation checklist:

- [x] Add `SemanticTokenNormalizer`.
- [x] Add deterministic role assignment rules.
- [x] Add conflict detection when multiple raw values compete for the same semantic role.
- [x] Allow multiple raw values per role when context requires it.
- [x] Add page-local override detection.
- [x] Add component-local override detection after component candidates exist.
- [x] Add `humanReviewRequired` for ambiguous high-impact tokens.
- [x] Keep original raw value IDs and evidence IDs.

Tests:

- [x] Stable semantic roles are assigned from fixture tokens.
- [x] Ambiguous accent roles create conflict report.
- [x] Human review flag appears for low-confidence critical token.
- [x] Raw values remain traceable.

Done when:

- [x] Token artifact is suitable for later CSS generation but does not generate CSS.

Implementation evidence:

- Added `SemanticTokenNormalizer`, semantic token contracts, and conflict report contracts.
- Registered `Schemas/semantic-tokens.schema.json` and `Schemas/semantic-token-conflicts.schema.json`.
- Added workflow step `normalize-semantic-tokens` after `extract-raw-tokens`; existing `--force-step` support can rerun it.
- Normalizer assigns deterministic color, typography, spacing, radius, and shadow roles from raw token frequency/hints, records raw token IDs/evidence IDs, writes page-local overrides, reserves component-local overrides until component candidates exist, and flags ambiguous accent conflicts for human review.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "SemanticTokens|RawTokens"` passed `10/10`.

## Phase 3B.4 - Page Archetype Classification

Goal: classify pages into ecommerce page types conservatively.

Artifacts:

```text
analysis/pages/{pageId}/page-archetype.json
Schemas/page-archetype.schema.json
```

Archetype MVP:

- [x] `home`
- [x] `product-listing`
- [x] `search-results`
- [x] `product-detail`
- [x] `cart-shell`
- [x] `checkout-shell`
- [x] `account-auth-shell`
- [x] `content`
- [x] `unknown`

Implementation checklist:

- [x] Add `PageArchetypeClassifier`.
- [x] Use route signals.
- [x] Use DOM landmark signals.
- [x] Use heading/text signals.
- [x] Use repeated product-card signals.
- [x] Use product gallery/title/price/add-to-cart visual control signals.
- [x] Use form density signals.
- [x] Use cart/order-summary signals.
- [x] Include alternative candidates.
- [x] Include confidence and reason codes.
- [x] Keep `unknown` as successful output when confidence is below threshold.

Tests:

- [x] Home fixture classifies as `home`.
- [x] PLP fixture classifies as `product-listing`.
- [x] PDP fixture classifies as `product-detail`.
- [x] Unsupported/custom fixture classifies as `unknown`.
- [x] Low confidence does not force a wrong archetype.

Done when:

- [x] Every page has one primary archetype or `unknown`, with evidence trace.

Implementation evidence:

- Added `Analysis/Pages/PageArchetypeClassifier` and page archetype contracts.
- Registered `Schemas/page-archetype.schema.json`.
- Added workflow step `classify-page-archetypes` after semantic token normalization.
- Classifier uses route, landmark/section, text/heading, repeated product-card, product gallery/price/add-to-cart, form density, and cart/order-summary signals, with alternative candidates, confidence, reason codes, and evidence IDs.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "PageArchetype|SemanticTokens"` passed `9/9`.

## Phase 3B.5 - Section Segmentation

Goal: segment each page into ordered visual regions that can later become generated layout sections.

Artifacts:

```text
analysis/pages/{pageId}/sections.draft.json
Schemas/sections.schema.json
```

Section candidates MVP:

- [x] announcement bar
- [x] header
- [x] navigation
- [x] hero
- [x] promotional banner
- [x] category navigation
- [x] product grid
- [x] product carousel
- [x] featured product
- [x] product gallery
- [x] product information
- [x] purchase actions
- [x] trust/benefit strip
- [x] editorial/content block
- [x] newsletter
- [x] reviews/testimonials
- [x] FAQ/accordion
- [x] cross-sell/upsell region
- [x] footer
- [x] cookie/banner overlay
- [x] unknown section

Implementation checklist:

- [x] Add `SectionSegmenter`.
- [x] Use DOM landmarks.
- [x] Use bounding-box gaps.
- [x] Use background changes.
- [x] Use container width changes.
- [x] Use heading boundaries.
- [x] Use repeated card groups.
- [x] Use grid/flex transitions.
- [x] Use sticky/fixed region signals.
- [x] Preserve top-to-bottom order.
- [x] Include bounding boxes and parent/child relationships.
- [x] Detect invalid overlapping peer sections.
- [x] Support merge/split ambiguity.
- [x] Support cross-viewport section identity placeholders for Phase 3B.6.

Tests:

- [x] Sections are ordered by page flow.
- [x] Peer sections do not overlap illegally.
- [x] Repeated product card group becomes product grid section.
- [x] Unknown section is emitted for unsupported content.
- [x] Human review item is created for merge/split ambiguity.

Done when:

- [x] Major visual regions are covered by named or unknown sections.

Implementation evidence:

- Added `SectionSegmenter`, section draft contracts, and `Schemas/sections.schema.json`.
- Added workflow step `segment-sections` after page archetype classification.
- Segmenter emits ordered sections with bounding boxes, evidence IDs, reason codes, parent/child fields, and `crossViewportIdentityKey` placeholders for Phase 3B.6.
- Segmenter classifies all MVP section labels, merges repeated product-card candidates into a product grid, detects invalid peer overlap, and reports merge/split ambiguity as warnings.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Sections|PageArchetype"` passed `10/10`.

## Phase 3B.6 - Responsive Behavior And Interaction Interpretation

Goal: compare the same page across configured viewports and interpret captured interactions as visual state only.

Artifacts:

```text
analysis/pages/{pageId}/responsive-behavior.json
analysis/pages/{pageId}/interaction-model.json
Schemas/responsive-behavior.schema.json
Schemas/interaction-model.schema.json
```

Responsive checklist:

- [x] Match same section across desktop/tablet/mobile.
- [x] Match same component candidate across viewport when possible.
- [x] Compare display, visibility, position, size, grid, flex, gap, typography, gutters, order, sticky behavior, and assets.
- [x] Detect hidden-on-mobile.
- [x] Detect desktop navigation to mobile menu replacement.
- [x] Detect multi-column to stacked.
- [x] Detect horizontal overflow or carousel.
- [x] Detect image crop change.
- [x] Detect compact spacing.
- [x] Detect typography downscale.
- [x] Preserve observed viewport values.
- [x] Mark unobserved breakpoint ranges as inferred.

Interaction checklist:

- [x] Interpret hover.
- [x] Interpret click/open.
- [x] Interpret expand/collapse.
- [x] Interpret mobile menu.
- [x] Interpret accordion.
- [x] Interpret tabs.
- [x] Interpret carousel navigation.
- [x] Interpret modal/drawer.
- [x] Interpret sticky transition.
- [x] Interpret focus state.
- [x] Interpret quantity/select visual pattern.
- [x] Interpret product option selector visual pattern.
- [x] Classify each interaction as:
  - [x] visual-only
  - [x] presentation interaction
  - [x] business behavior required
  - [x] unsupported/unsafe
- [x] Do not execute commerce mutation flows.

Tests:

- [x] Grid-to-stack is detected.
- [x] Hide/show is detected.
- [x] Replacement and restyle are separate outputs.
- [x] Before/after interaction evidence is used.
- [x] Button visual does not become cart logic.

Done when:

- [x] Responsive and interaction artifacts are evidence-backed and conservative.

Implementation evidence:

- Added `ResponsiveInteractionAnalyzer` and contracts for `responsive-behavior` and `interaction-model`.
- Registered `Schemas/responsive-behavior.schema.json` and `Schemas/interaction-model.schema.json`.
- Added workflow step `analyze-responsive-interactions` after section segmentation.
- Analyzer matches section/component placeholders across viewports, preserves observed display/visibility/position/size/gap/font/assets, flags grid-to-stack, hide/show, mobile menu replacement, overflow/carousel, asset change, compact spacing, and typography downscale.
- Interaction model reads before/after interaction evidence only; it classifies visual-only, presentation interaction, business behavior required, and unsupported/unsafe without executing commerce mutation flows.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Responsive|Interaction_Button"` passed `4/4`.

## Phase 3B.7 - Visual Component Candidate Detection

Goal: cluster repeated visual structures into neutral component candidates before Presentation mapping.

Artifacts:

```text
analysis/components/component-candidates.json
analysis/components/component-instances.json
Schemas/component-candidates.schema.json
```

Candidate families MVP:

- [ ] Global shell:
  - [ ] announcement bar
  - [ ] header
  - [ ] navigation
  - [ ] search trigger
  - [ ] account trigger
  - [ ] cart trigger
  - [ ] footer
- [ ] Commerce:
  - [ ] product card
  - [ ] product grid
  - [ ] product carousel
  - [ ] price display
  - [ ] product badge
  - [ ] product image
  - [ ] product gallery
  - [ ] variant selector visual
  - [ ] quantity selector visual
  - [ ] purchase action visual
  - [ ] rating/review card
  - [ ] breadcrumb/category card
  - [ ] filter trigger/panel
  - [ ] sort selector
  - [ ] pagination
  - [ ] cart line visual
  - [ ] order summary visual
- [ ] Content:
  - [ ] hero
  - [ ] promo banner
  - [ ] feature list
  - [ ] media/text split
  - [ ] newsletter visual
  - [ ] FAQ item
  - [ ] testimonial card

Implementation checklist:

- [ ] Add `VisualComponentCandidateDetector`.
- [ ] Use DOM subtree similarity.
- [ ] Use style similarity.
- [ ] Use box-shape similarity.
- [ ] Use repetition count.
- [ ] Use stable slot patterns.
- [ ] Use shared responsive behavior.
- [ ] Use shared interaction behavior.
- [ ] Assign family, variant, and instance IDs.
- [ ] Detect slots inside component candidates.
- [ ] Include token references.
- [ ] Include local overrides.
- [ ] Include confidence, alternatives, and human review flags.

Tests:

- [ ] Product cards cluster into one family.
- [ ] Variants remain in same family when structure is similar.
- [ ] Distinct unrelated sections are not merged.
- [ ] Slot detection captures image/title/price/action where evidence exists.

Done when:

- [ ] Component candidates are neutral and do not reference Blazor component types yet.

## Phase 3B.8 - Ecommerce Region Classification

Goal: classify visual sections/components into ecommerce roles without implementing behavior.

Artifacts:

```text
analysis/pages/{pageId}/ecommerce-regions.json
Schemas/ecommerce-regions.schema.json
```

Region role MVP:

- [ ] Store navigation:
  - [ ] store header
  - [ ] primary/category navigation
  - [ ] search access
  - [ ] account access
  - [ ] cart access
- [ ] Catalog:
  - [ ] product listing
  - [ ] product card collection
  - [ ] filter
  - [ ] sort
  - [ ] pagination/load-more
  - [ ] empty/search result region
- [ ] Product detail:
  - [ ] product media
  - [ ] product title
  - [ ] price
  - [ ] variant options
  - [ ] quantity
  - [ ] add-to-cart/buy-now visual
  - [ ] description/metadata/trust/reviews
  - [ ] related/cross-sell/upsell
- [ ] Cart/checkout shell:
  - [ ] cart line items visual
  - [ ] cart summary
  - [ ] promo-code visual
  - [ ] checkout CTA visual
  - [ ] checkout form region
  - [ ] order summary visual
  - [ ] payment visual placeholder
- [ ] Shared states:
  - [ ] loading
  - [ ] empty
  - [ ] error
  - [ ] not found
  - [ ] service unavailable
- [ ] Unknown role.

Implementation checklist:

- [ ] Add `EcommerceRegionClassifier`.
- [ ] Consume page archetype, sections, component candidates, tokens, and interactions.
- [ ] Mark behavior contract requirement.
- [ ] Mark data dependency:
  - [ ] shell
  - [ ] catalog
  - [ ] product
  - [ ] cart
  - [ ] checkout
  - [ ] account
  - [ ] system state
- [ ] Mark SEO relevance.
- [ ] Mark presentation-only regions.
- [ ] Mark unsupported roles.
- [ ] Preserve alternatives.

Tests:

- [ ] Product grid region detected.
- [ ] PDP gallery/title/price/purchase regions detected.
- [ ] Cart shell visual does not require cart business execution.
- [ ] Unknown ecommerce role is valid.

Done when:

- [ ] Critical ecommerce visual roles are classified without crossing into runtime logic.

## Phase 3B.9 - Presentation Component Catalog

Goal: build a versioned catalog of real Presentation/Starter slots and capabilities that mapping can validate against.

Artifacts:

```text
presentation-catalog/presentation-component-catalog.json
presentation-catalog/catalog-validation-report.json
Schemas/presentation-component-catalog.schema.json
```

Catalog source of truth:

- [ ] `BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs`
- [ ] `BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewOptionsValidator.cs`
- [ ] `BlazorShop.Storefront.Starter/starter-generation.contract.yaml`
- [ ] `BlazorShop.Storefront.Components/Contracts`
- [ ] `BlazorShop.Storefront.Components/Headless`
- [ ] `BlazorShop.Storefront.Browser` only for browser-side same-origin behavior descriptors when relevant.

Catalog entry fields:

- [ ] `componentId`
- [ ] `category`
- [ ] `supportedPageArchetypes`
- [ ] `supportedRegionRoles`
- [ ] `slots`
- [ ] `variants`
- [ ] `visualProperties`
- [ ] `responsiveCapabilities`
- [ ] `interactionCapabilities`
- [ ] `dataContract`
- [ ] `behaviorOwnedByPresentation`
- [ ] `behaviorOwnedByRuntime`
- [ ] `visualOverrideAllowed`
- [ ] `behaviorOverrideAllowed`
- [ ] `requiredChildren`
- [ ] `optionalChildren`
- [ ] `unsupportedPatterns`
- [ ] `sourceFiles`
- [ ] `contractVersion`

Implementation checklist:

- [ ] Add catalog contracts.
- [ ] Add catalog loader.
- [ ] Add schema validation.
- [ ] Add alias support for visual terms.
- [ ] Add required slot validation.
- [ ] Add behavior ownership validation.
- [ ] Add drift test between catalog and `StorefrontFoundationViewSet`.
- [ ] Add drift test between catalog and `starter-generation.contract.yaml` slots/actions/routes.
- [ ] Do not claim unsupported or non-existing components as supported.

Tests:

- [ ] Catalog loads and validates.
- [ ] Missing required foundation view slot fails.
- [ ] Missing Starter slot mapping fails.
- [ ] Unknown component remains unsupported.
- [ ] Drift test catches removed/renamed slot.

Done when:

- [ ] Presentation mapping has a truthful catalog to match against.

## Phase 3B.10 - Presentation Mapping And Unsupported Pattern Detection

Goal: map neutral visual/ecommerce candidates to real Presentation/Starter slots when compatible, and report no-match cases clearly.

Artifacts:

```text
analysis/mapping/presentation-mappings.draft.json
analysis/mapping/unsupported-patterns.json
Schemas/presentation-mappings.schema.json
Schemas/unsupported-patterns.schema.json
```

Mapping result fields:

- [ ] `sourceCandidateId`
- [ ] `presentationComponentId`
- [ ] `starterSlotId`
- [ ] `variant`
- [ ] `slotAssignments`
- [ ] `responsiveProperties`
- [ ] `tokenBindings`
- [ ] `interactionBindings`
- [ ] `dataRequirements`
- [ ] `behaviorOwnership`
- [ ] `confidence`
- [ ] `evidenceIds`
- [ ] `mappingReason`
- [ ] `alternativeMappings`
- [ ] `humanReviewRequired`

Unsupported groups:

- [ ] missing component
- [ ] missing variant
- [ ] missing slot
- [ ] missing responsive behavior
- [ ] missing interaction state
- [ ] missing composition pattern
- [ ] unsupported overlay/drawer/gallery/product option/content/shell behavior
- [ ] behavior boundary conflict

Implementation checklist:

- [ ] Add `PresentationMappingEngine`.
- [ ] Add exact rule match.
- [ ] Add alias/structural match.
- [ ] Validate required slots.
- [ ] Validate variant compatibility.
- [ ] Validate responsive capability.
- [ ] Validate interaction capability.
- [ ] Validate behavior ownership.
- [ ] Validate data contract compatibility.
- [ ] Emit no-match instead of forcing mapping.
- [ ] Add unsupported pattern severity and frequency.
- [ ] Add suggested resolution:
  - [ ] add Presentation variant
  - [ ] add Presentation component
  - [ ] compose existing components
  - [ ] treat as theme-only CSS
  - [ ] reject
  - [ ] require manual review

Tests:

- [ ] Exact mapping succeeds.
- [ ] Alternative mapping is emitted when multiple candidates are plausible.
- [ ] Slot mismatch blocks mapping.
- [ ] Behavior ownership conflict blocks mapping.
- [ ] Every no-match has unsupported reason.
- [ ] Critical no-match appears in readiness blocker.

Done when:

- [ ] No unsupported critical pattern silently passes to blueprint readiness.

## Phase 3B.11 - Confidence Scoring And Human Review

Goal: make every important conclusion reviewable, explainable, and auditable.

Artifacts:

```text
analysis/confidence/confidence-report.json
review/review-queue.json
review/review-decisions.json
review/review-pack.md
Schemas/confidence-report.schema.json
Schemas/review-queue.schema.json
Schemas/review-decisions.schema.json
```

Confidence dimensions:

- [ ] evidence completeness
- [ ] cross-viewport consistency
- [ ] repetition count
- [ ] rule strength
- [ ] structural similarity
- [ ] token consistency
- [ ] interaction evidence
- [ ] catalog compatibility
- [ ] ambiguity
- [ ] human override

Reviewable item types:

- [ ] semantic tokens
- [ ] page archetype
- [ ] sections
- [ ] component families
- [ ] component variants
- [ ] ecommerce roles
- [ ] Presentation mappings
- [ ] unsupported patterns
- [ ] originality restrictions
- [ ] generation readiness

Implementation checklist:

- [ ] Add `ConfidenceScorer`.
- [ ] Add per-token confidence.
- [ ] Add per-page confidence.
- [ ] Add per-section confidence.
- [ ] Add per-component confidence.
- [ ] Add per-region confidence.
- [ ] Add per-mapping confidence.
- [ ] Add project-level confidence summary.
- [ ] Add threshold configuration.
- [ ] Add low-confidence critical item queueing.
- [ ] Add `ReviewQueueBuilder`.
- [ ] Add Markdown review pack.
- [ ] Add `ReviewDecisionApplier`.
- [ ] Support `Approved`, `Modified`, `Rejected`, and `Deferred`.
- [ ] Preserve original proposal, original confidence, reviewer note, and timestamp.
- [ ] Do not require a web UI for MVP.

Tests:

- [ ] Score is deterministic.
- [ ] Human override does not erase original score.
- [ ] Low-confidence critical mapping enters review queue.
- [ ] Approve decision carries item into reviewed output.
- [ ] Modify decision preserves original proposal and records new value.
- [ ] Reject/defer prevents reviewed readiness when item is blocking.

Done when:

- [ ] Review workflow can close MVP without UI.

## Phase 3B.12 - Visual Blueprint v1 And Generation Readiness

Goal: assemble all approved analysis into a versioned blueprint and validate handoff readiness for Phase 4.

Artifacts:

```text
analysis/visual-blueprint.v1.draft.json
analysis/visual-blueprint.v1.reviewed.json
reports/generation-readiness.json
reports/generation-readiness.md
Schemas/visual-blueprint-v1.schema.json
Schemas/generation-readiness.schema.json
```

Blueprint sections:

- [ ] `projectMetadata`
- [ ] `sourceProvenance`
- [ ] `pages`
- [ ] `pageArchetypes`
- [ ] `tokens`
- [ ] `sections`
- [ ] `responsiveBehavior`
- [ ] `interactionModels`
- [ ] `componentDefinitions`
- [ ] `componentInstances`
- [ ] `ecommerceRegions`
- [ ] `presentationMappings`
- [ ] `unsupportedPatterns`
- [ ] `originalityRestrictions`
- [ ] `confidence`
- [ ] `reviewState`
- [ ] `generationRestrictions`

Generation readiness blocking conditions:

- [ ] Missing required page archetype.
- [ ] Invalid section segmentation.
- [ ] No semantic token baseline.
- [ ] Missing mapping for critical region.
- [ ] Required Presentation component unsupported.
- [ ] Slot mismatch.
- [ ] Behavior ownership conflict.
- [ ] Low-confidence critical mapping without review decision.
- [ ] Originality restriction unresolved.
- [ ] Missing review decisions.
- [ ] Invalid blueprint schema.
- [ ] Broken evidence references.
- [ ] Catalog version mismatch.

Generation readiness warning conditions:

- [ ] Cosmetic token ambiguity.
- [ ] Minor unsupported content section.
- [ ] Optional interaction not mapped.
- [ ] Low-confidence non-critical component.
- [ ] Inferred breakpoint.

Implementation checklist:

- [ ] Add `VisualBlueprintV1Builder`.
- [ ] Add draft blueprint builder.
- [ ] Add reviewed blueprint builder.
- [ ] Add schema validation.
- [ ] Add evidence reference validation.
- [ ] Add catalog version validation.
- [ ] Add originality restriction preservation.
- [ ] Add generation restriction preservation.
- [ ] Add `GenerationReadinessValidator`.
- [ ] Add Markdown readiness report.
- [ ] Return non-zero CLI exit code when blocking findings exist.
- [ ] Keep StorefrontBuilder consumption disabled.

Tests:

- [ ] Draft blueprint validates.
- [ ] Reviewed blueprint is created only after valid decisions.
- [ ] Broken evidence reference fails readiness.
- [ ] Blocking unsupported pattern fails readiness.
- [ ] Warning-only readiness passes.
- [ ] Catalog mismatch fails readiness.

Done when:

- [ ] Phase 4 can consume reviewed blueprint later without reinterpreting raw evidence.

## Phase 3B.13 - Multi-Page Fixtures

Goal: add realistic local fixtures so tests prove real ecommerce page recognition, not only smoke behavior.

Fixture requirements:

- [ ] Local only.
- [ ] Deterministic.
- [ ] No Commerce API calls.
- [ ] No external images/scripts/fonts.
- [ ] No copyrighted/brand assets.
- [ ] Desktop/tablet/mobile states.
- [ ] Interaction evidence where needed.

Fixtures:

- [ ] Home fixture:
  - [ ] header
  - [ ] hero
  - [ ] category cards
  - [ ] product grid
  - [ ] promo strip
  - [ ] newsletter
  - [ ] footer
  - [ ] mobile menu
- [ ] Product listing fixture:
  - [ ] breadcrumb
  - [ ] title
  - [ ] filter trigger/panel
  - [ ] sort
  - [ ] product grid
  - [ ] pagination
  - [ ] mobile filter drawer
- [ ] Product detail fixture:
  - [ ] product image gallery
  - [ ] title
  - [ ] price
  - [ ] option selector
  - [ ] quantity visual
  - [ ] add-to-cart visual
  - [ ] description accordion
  - [ ] reviews
  - [ ] related products
- [ ] Unsupported fixture:
  - [ ] irregular overlapping layout
  - [ ] unusual gallery
  - [ ] complex animated section
  - [ ] unsupported slot pattern
  - [ ] visual behavior conflicting with Presentation ownership

Implementation checklist:

- [ ] Extend `TestHttpFixtureServer` to serve multiple routes if needed.
- [ ] Keep single-page fixture tests intact for Phase 3A regression.
- [ ] Add route-specific fixture tests for Phase 3B.
- [ ] Add fixture expected-output snapshots where stable.

Done when:

- [ ] Phase 3B tests cover Home, PLP, PDP, and Unsupported cases.

## Phase 3B.14 - CLI, Inspect, Documentation, And Developer Experience

Goal: keep the tool usable and prevent generated artifacts from becoming hard to inspect.

Implementation checklist:

- [ ] Update CLI help.
- [ ] Add `inspect` output for Phase 3B artifacts:
  - [ ] evidence snapshot path/status
  - [ ] token status
  - [ ] archetype/section status
  - [ ] mapping status
  - [ ] review queue count
  - [ ] readiness status
  - [ ] latest blocking Phase 3B finding
- [ ] Add problem/cause/fix messages for common failures:
  - [ ] missing Phase 3A readiness
  - [ ] missing evidence snapshot
  - [ ] invalid token schema
  - [ ] catalog drift
  - [ ] unresolved blocking review item
  - [ ] unsupported critical pattern
- [ ] Update `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [ ] Update `docs/architecture/11-storefront-builder.md` only if artifact contract or boundary changes.
- [ ] Document that StorefrontBuilder still does not consume blueprint unless a later phase enables it.

Tests:

- [ ] `--help` includes new commands/options.
- [ ] `inspect` works without Playwright.
- [ ] Invalid Phase 3B project state reports problem/cause/fix.
- [ ] Docs commands are copy-paste valid.

Done when:

- [ ] A developer can run, inspect, review, and validate Phase 3B artifacts without guessing file locations.

## Phase 3B.15 - Release Gate

Goal: prove Phase 3B can be closed locally with deterministic evidence and no boundary leaks.

Create:

```text
scripts/qa/run-storefront-reverse-engineering-phase3b-gate.ps1
```

Gate steps:

- [ ] Build ReverseEngineering tool.
- [ ] Run Phase 3A regression gate or a documented fast subset when full gate is too slow.
- [ ] Run all ReverseEngineering tests.
- [ ] Run schema tests.
- [ ] Run evidence snapshot tests.
- [ ] Run token extraction tests.
- [ ] Run semantic token tests.
- [ ] Run page archetype tests.
- [ ] Run section segmentation tests.
- [ ] Run responsive tests.
- [ ] Run interaction model tests.
- [ ] Run component candidate tests.
- [ ] Run ecommerce region tests.
- [ ] Run Presentation catalog validation tests.
- [ ] Run mapping tests.
- [ ] Run unsupported pattern tests.
- [ ] Run confidence tests.
- [ ] Run review workflow tests.
- [ ] Run blueprint schema/reference tests.
- [ ] Run generation readiness tests.
- [ ] Run local multi-page fixture workflow.
- [ ] Run boundary scan:
  - [ ] production projects do not reference ReverseEngineering.
  - [ ] ReverseEngineering does not reference Storefront V2/backend/core/API projects.
  - [ ] ReverseEngineering does not write generated storefront project source.
  - [ ] StorefrontBuilder does not consume `visual-blueprint.v1.*.json` yet.
  - [ ] no Razor/CSS generation code is introduced in Phase 3B.
- [ ] Run StorefrontBuilder plan-only smoke.
- [ ] Write commit-linked report under `obj/storefront-reverse-engineering/reports`.

Report fields:

- [ ] Commit SHA.
- [ ] Branch.
- [ ] UTC timestamp.
- [ ] .NET version.
- [ ] Artifact project root.
- [ ] Fixture routes.
- [ ] Test summaries.
- [ ] Blueprint paths.
- [ ] Presentation catalog version.
- [ ] Readiness result.
- [ ] Unsupported pattern count.
- [ ] Review queue count.
- [ ] Known limitations.

Done when:

- [ ] Phase 3B gate passes locally.
- [ ] Gate failure report points to exact blocking artifact and fix.

## Rollout Plan

Recommended implementation order:

1. [x] Phase 3B.0 prerequisite closure check.
2. [x] Phase 3B.1 evidence snapshot.
3. [ ] Phase 3B.13 fixture expansion started early with Home/PLP/PDP/Unsupported routes.
4. [x] Phase 3B.2 raw design tokens.
5. [x] Phase 3B.3 semantic tokens.
6. [x] Phase 3B.4 page archetype.
7. [x] Phase 3B.5 section segmentation.
8. [x] Phase 3B.6 responsive and interaction model.
9. [ ] Phase 3B.7 visual component candidates.
10. [ ] Phase 3B.8 ecommerce region classification.
11. [ ] Phase 3B.9 Presentation component catalog.
12. [ ] Phase 3B.10 Presentation mapping and unsupported patterns.
13. [ ] Phase 3B.11 confidence and human review.
14. [ ] Phase 3B.12 Visual Blueprint v1 and generation readiness.
15. [ ] Phase 3B.14 CLI/docs/DX.
16. [ ] Phase 3B.15 release gate.

## Suggested Commit Slices

- [ ] Commit 1: evidence snapshot, schema, tests.
- [ ] Commit 2: multi-page fixtures and fixture server support.
- [ ] Commit 3: raw/semantic tokens and tests.
- [ ] Commit 4: archetype and section segmentation.
- [ ] Commit 5: responsive and interaction interpretation.
- [ ] Commit 6: component candidate and ecommerce region classifiers.
- [ ] Commit 7: Presentation catalog and drift tests.
- [ ] Commit 8: mapping, unsupported patterns, and confidence.
- [ ] Commit 9: review workflow and Visual Blueprint v1.
- [ ] Commit 10: readiness gate, docs, and closure report.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| CLI command explosion | Hard to maintain, inconsistent UX | Keep workflow-driven model and expose only high-value commands. |
| Token semantics overfit one website | Bad generated themes later | Preserve raw values, confidence, conflicts, and human review. |
| Component mapping invents unavailable Presentation support | Phase 4 produces unbuildable output | Build catalog from real `ViewSet` and Starter contract, add drift tests. |
| Unsupported patterns silently pass | Later generator creates broken UI | Make critical no-match a readiness blocker. |
| Phase 3B mutates generation behavior too early | StorefrontBuilder regression | Boundary scan must prove no active StorefrontBuilder blueprint consumption. |
| Analysis starts repairing Phase 3A capture | Scope creep and fragile fixes | Phase 3B.0 preflight requires Phase 3A readiness before analysis. |
| Human review overwrites draft evidence | Loss of auditability | Draft and reviewed artifacts must be separate. |
| Fixtures are too simple | Tests pass without proving ecommerce mapping | Add Home, PLP, PDP, and Unsupported multi-page fixtures. |

## Coverage Matrix Against Original Review

| Review item | Covered by phase |
| --- | --- |
| Evidence Aggregation Layer | 3B.1 |
| Raw Design Token Extraction | 3B.2 |
| Semantic Token Normalization | 3B.3 |
| Page Archetype Classification | 3B.4 |
| Section Segmentation | 3B.5 |
| Responsive Comparison Engine | 3B.6 |
| Interaction Interpretation | 3B.6 |
| Visual Component Candidate Detection | 3B.7 |
| Ecommerce Region Classification | 3B.8 |
| Presentation Component Catalog | 3B.9 |
| Presentation Mapping Engine | 3B.10 |
| Unsupported Pattern Detection | 3B.10 |
| Confidence Scoring | 3B.11 |
| Human Review Workflow | 3B.11 |
| Visual Blueprint v1 | 3B.12 |
| Generation Readiness Gate | 3B.12 and 3B.15 |
| CLI proposal | 3B.14, adjusted to avoid command explosion |
| Project structure proposal | Proposed Project Shape |
| Test matrix | 3B.15 and per-phase tests |
| Fixture requirements | 3B.13 |
| Definition of Done | Per-phase Done sections and final DoD |
| Phase 4 handoff | Final DoD and Handoff |

## Final Definition Of Done

- [ ] Existing Phase 3A regression tests remain green.
- [ ] Existing Phase 3A gate remains green or has a documented local-only environment skip.
- [ ] `analysis/evidence-snapshot.json` exists and validates.
- [ ] Raw tokens and semantic tokens exist and preserve evidence traces.
- [ ] Page archetypes include safe `unknown` fallback.
- [ ] Sections are ordered and non-overlapping.
- [ ] Responsive and interaction artifacts are based on observed evidence.
- [ ] Component candidates are neutral and framework-independent.
- [ ] Ecommerce regions mark visual role, data dependency, and behavior boundary.
- [ ] Presentation catalog reflects real Presentation/Starter contracts.
- [ ] Mapping never claims unsupported components as supported.
- [ ] Unsupported critical patterns block readiness.
- [ ] Confidence scoring is deterministic and explainable.
- [ ] Review workflow produces separate draft and reviewed outputs.
- [ ] `visual-blueprint.v1.draft.json` validates.
- [ ] `visual-blueprint.v1.reviewed.json` validates only after review decisions.
- [ ] `generation-readiness.json` validates and fails on blockers.
- [ ] StorefrontBuilder generation remains unchanged.
- [ ] ReverseEngineering remains development-time only.
- [ ] No Razor/CSS/storefront project generation is added.
- [ ] No production project references ReverseEngineering.
- [ ] Phase 3B gate writes a commit-linked local report.

## Handoff To Phase 4

Phase 4 may start only after:

- [ ] Reviewed Visual Blueprint v1 exists.
- [ ] Presentation Component Catalog version is recorded in the blueprint.
- [ ] Generation readiness passes.
- [ ] Originality restrictions are preserved.
- [ ] Unsupported pattern decisions are reviewed.
- [ ] StorefrontBuilder consumption is explicitly approved in a separate plan.

Phase 4 must not:

- [ ] Reinterpret raw website evidence.
- [ ] Ignore reviewed blueprint decisions.
- [ ] Ignore Presentation catalog constraints.
- [ ] Ignore originality restrictions.
- [ ] Auto-map unsupported patterns without a review decision.

## Autoplan Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Split Phase 3B into many smaller implementation phases instead of one large implementation | Auto-decided | Low blast radius | Codebase has only Phase 3A foundation; implementing all analysis layers at once would make review and QA weak. | One large Phase 3B PR. |
| 2 | Keep existing workflow-driven CLI and add only a few public commands | Auto-decided | DX and maintainability | Current CLI already supports `run`, `resume`, and `--force-step`; exposing every internal analyzer as a command would create unnecessary surface. | Add all proposed commands immediately. |
| 3 | Build Presentation catalog before mapping | Auto-decided | Correctness | Mapping must be validated against real `StorefrontFoundationViewSet` and Starter contract, not inferred component support. | Mapping directly from visual candidates to imagined components. |
| 4 | Keep StorefrontBuilder consumption disabled in Phase 3B | Auto-decided | Boundary safety | Architecture docs say later approved phase must wire consumption; Phase 3B should only prepare handoff artifacts. | Make StorefrontBuilder consume blueprint immediately. |
| 5 | Treat unknown/no-match as valid output | Auto-decided | Evidence-first | Forcing low-confidence evidence into archetypes or components would create broken generation later. | Force every page/section/component into a known role. |
| 6 | Use JSON/Markdown human review for MVP | Auto-decided | Practicality | Review auditability is required; a web UI is not required to close MVP. | Build a full review UI in Phase 3B. |

## GSTACK REVIEW REPORT

Autoplan review result: approved with scoped adjustments.

CEO lens:

- The goal is strategically correct: Phase 3B should transform trustworthy evidence into a reviewable blueprint before visual generation.
- The main scope correction is to avoid bundling analysis, mapping, review, readiness, and StorefrontBuilder consumption into one phase.
- StorefrontBuilder consumption remains a later explicit decision.

Engineering lens:

- The plan aligns with the current ReverseEngineering tool boundaries.
- The required first layer is evidence snapshot aggregation, because every later analyzer needs one stable input model.
- The Presentation catalog must be generated or validated from real contracts before mapping.
- The gate must include boundary scans proving no production/runtime coupling was added.

DX lens:

- Existing `run/resume/inspect/validate` workflow should remain the main developer path.
- New commands should be minimal and explain artifact paths clearly.
- Every failure should include problem, cause, and fix.

Design lens:

- Phase 3B should describe visual structure and behavior states, not produce final UI.
- Token and component interpretation must preserve ambiguity and require review for low-confidence decisions.

Final decision:

- Proceed with this scoped Phase 3B plan.
- Do not implement visual generation until reviewed blueprint readiness passes and a separate Phase 4 plan approves StorefrontBuilder consumption.

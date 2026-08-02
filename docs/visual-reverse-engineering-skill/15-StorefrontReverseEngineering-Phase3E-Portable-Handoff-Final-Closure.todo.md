# StorefrontReverseEngineering Phase 3E - Portable Handoff And Final HEAD Closure

Status: Proposed
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`
Target folder: `docs/visual-reverse-engineering-skill`
Depends on: Phase 3A, Phase 3B, Phase 3C, and Phase 3D closure being complete
Primary goal: make `analysis/agent-handoff/*` a self-contained, portable, schema-backed package that can be copied and validated without the original reverse-engineering project, then prove final closure on the same clean repository `HEAD`.

## Why This File Exists

Phase 3D closed the main correctness proof: reviewed decisions propagate into resolved artifacts and page compositions, exact Storefront slots are enforced, viewport-specific evidence is packaged, and real positive/negative proof suites run in the final closure gate.

The follow-up review found one remaining contract gap: Phase 3D proves consistency while the full source project still exists. It does not yet prove that a copied `analysis/agent-handoff/*` package can validate and load independently. Phase 3E is an additive correction for portability and final closure discipline. It must not rewrite Phase 3D history.

## Current Codebase Evidence

- `AgentHandoffAssembler` still copies several artifacts directly into `analysis/agent-handoff/*`, including the reviewed blueprint, instead of producing handoff-specific consumer contracts.
- `AgentHandoffContract.RequiredArtifacts` does not yet include every consumer artifact that the current validators and a future Phase 4 loader need, such as the Presentation catalog, reviewed mappings, component candidates/instances, responsive model, interaction model, originality restrictions, confidence, and review resolution.
- `AgentHandoffReadinessValidator` still reads project-root artifacts such as `project.json`, `reports/generation-readiness.json`, `review/*`, `analysis/resolved/*`, and `presentation-catalog/*`.
- `AgentHandoffEvidencePackager` still writes an authoritative-looking section slot from `InferSlot(node.Role)`.
- `agent-handoff-manifest.schema.json` still requires `sourceProjectPath`.
- Current readiness checks validate known handoff artifacts, but do not use a registry-driven scanner that classifies every path-like reference as consumer, diagnostic, external URL, generated target path, or ignored scalar.
- Phase 3D final closure is currently valid for the current `HEAD` according to the latest review handoff, so the old P0 about "proof does not match current HEAD" is not a current blocker. Phase 3E must instead require rerunning proof after all Phase 3E commits land.

## Locked Decisions

- Phase 4 may read only `analysis/agent-handoff/*` and registered handoff schemas.
- Phase 4 must not read `analysis/pages/*`, `analysis/resolved/*`, `analysis/mapping/*`, `analysis/tokens/*`, `analysis/components/*`, `presentation-catalog/*`, `review/*`, `captures/*`, or `reports/*` as fallback inputs.
- External source project paths may exist only as diagnostics provenance, never as consumer dependencies.
- StorefrontBuilder consumption remains disabled in Phase 3E.
- No Razor, CSS, JavaScript, generated storefront source, Starter writes, Commerce Node calls, Storefront V2 references, or production runtime references are introduced.
- Phase 3E closure is authoritative only when the runtime gate report records tested SHA equal to final `HEAD`, final working tree clean, and no source/docs commit after the gate.

## Codebase-Appropriate Corrections To The Original Proposal

- Normalize more than `visual-blueprint.json`; add handoff-specific contracts for page compositions, review resolution, design tokens, visual style, and all reference-bearing consumer artifacts.
- Package the Presentation catalog as a handoff-local consumer artifact, or embed an equivalent validated catalog contract. Do not make portable validation read `presentation-catalog/*`.
- Use a typed reference registry instead of scanning every string as a path. Generated target paths, diagnostic provenance, external URLs, and consumer dependencies have different rules.
- Add a separate portable validator that accepts only `handoffRoot` and `schemaRoot`. It must not use `ApprovedArtifactRootResolver` or the original project root.
- Extract shared authoritative slot resolution so slot validation and evidence packaging use the same reviewed mapping/catalog logic. Role text can produce only a diagnostic suggestion.
- Define package hashing over sorted file-level entries. Do not hash directory counts, timestamps, manifest self-hashes, absolute source paths, or diagnostic-only values.
- Extend canonical schema registration with required schema kinds and schema hashes, so portable packages can validate against the exact expected contract set.
- Keep Phase 3E CLI commands in ReverseEngineering only. Do not connect StorefrontBuilder generation or validation to handoff consumption.
- Label responsive and interaction artifacts as evidence-derived unless explicit confidence-review support is added for them.
- Avoid duplicate gate execution: Phase 3E should call the existing Phase 3D gate once, then run Phase 3E portable proofs and final `HEAD` checks.

## Scope

In scope:

- Handoff-specific portable contracts.
- Complete handoff-local consumer artifact set.
- Typed reference categories and containment validation.
- Portable manifest paths, schema records, and package hashes.
- Evidence slot provenance from reviewed mappings.
- Portable copy proof and negative portability mutations.
- Read-only Phase 4 consumer dry-run loader.
- Final Phase 3E no-skip clean-HEAD gate.
- Documentation alignment for Phase 3E and future Phase 4 input rules.

Out of scope:

- StorefrontBuilder handoff consumption.
- Visual generation, Razor generation, CSS generation, or functional JavaScript generation.
- AI provider integration.
- Reference-site visual repair loops.
- Phase 3A capture redesign.
- Phase 3B classifier redesign.
- Commerce Node, Control Plane, Storefront V2, Presentation runtime, Runtime, Client, Components, or Starter behavior changes.

## P0 Blockers

- [ ] P0-1: Handoff package is not portable because validators and manifests still depend on original project-root files.
- [ ] P0-2: `visual-blueprint.json` and other handoff artifacts can carry consumer-looking references outside `analysis/agent-handoff/*`.
- [ ] P0-3: Required consumer artifact set is incomplete for Phase 4 dry-run loading.
- [ ] P0-4: Evidence section slot provenance can still come from role text inference.
- [ ] P0-5: Manifest schema and package hash are not portable across copied package locations.
- [ ] P0-6: Final Phase 3 closure lacks a Phase 3E gate that proves isolated copy validation and no post-proof commits.

## Phase 3E.0 - Baseline And Contract Lock

Goal: start Phase 3E from a known Phase 3D closure state and lock the no-generation boundary before contract work.

Implementation checklist:

- [x] Record baseline branch and `HEAD` in `docs/qa/phase3e-final-closure.md`.
- [x] Verify `git status --short` is clean before implementation starts, or record unrelated dirty files explicitly.
- [x] Confirm the latest Phase 3D gate report passed at the current `HEAD`; if not, rerun Phase 3D before Phase 3E code work.
- [x] Confirm StorefrontBuilder still has no `analysis/agent-handoff` consumption.
- [x] Confirm ReverseEngineering still has no production runtime references.
- [x] Confirm no ReverseEngineering code writes Razor, CSS, JavaScript, generated storefront roots, or Starter files.
- [x] Add this plan file as the tracked Phase 3E execution checklist.

Tests and checks:

- [x] `git status --short`
- [x] `rg -n "analysis/agent-handoff|agent-handoff-readiness|visual-blueprint\\.v1" tools/BlazorShop.AI.StorefrontBuilder --glob "!bin/**" --glob "!obj/**"` returns no consumption matches.
- [x] `rg -n "ProjectReference|PackageReference" tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj`

Done when:

- [x] Phase 3E baseline and no-generation constraints are documented before code changes.

## Phase 3E.1 - Canonical Portable Package Contract

Goal: define one canonical handoff package contract before adding new artifacts or validators.

Implementation checklist:

- [x] Add contract models under `Analysis/Handoff`:
  - [x] `PortableHandoffPackageContract`
  - [x] `PortableHandoffArtifactKind`
  - [x] `PortableHandoffArtifactEntry`
  - [x] `PortableHandoffSchemaRequirement`
  - [x] `PortableHandoffReferencePolicy`
  - [x] `PortableHandoffPackageHash`
- [x] Extend `AgentHandoffContract.RequiredArtifacts` to include every Phase 4 consumer artifact planned in Phase 3E.
- [x] Add `RequiredSchemaKinds` beside `RequiredArtifacts`.
- [x] Record schema kind, schema version, and schema SHA-256 in the manifest.
- [x] Define canonical relative-path style: `/` separators, no drive letters, no UNC paths, no leading slash, no `..`, no symlink escape.
- [x] Define canonical `packageHash` over sorted file-level entries:
  - [x] relative path.
  - [x] artifact kind.
  - [x] schema kind and version.
  - [x] file SHA-256.
  - [x] file size.
- [x] Exclude from package hash:
  - [x] manifest self-hash fields.
  - [x] absolute source project paths.
  - [x] timestamps.
  - [x] directory entries represented only by file counts.
  - [x] diagnostic-only provenance values.

Tests:

- [x] Package hash is stable after copying package to a different temp folder.
- [x] Package hash changes when a consumer file changes.
- [x] Artifact ordering does not change package hash.
- [x] Directory file count changes without file-level entries cannot satisfy the hash rule.
- [x] Missing required schema record fails manifest validation.

Done when:

- [x] There is one canonical artifact/schema/hash contract used by assembler, manifest writer, validators, tests, docs, and final gate.

## Phase 3E.2 - Handoff-Specific Consumer Artifacts

Goal: stop copying reviewed project artifacts directly when their internal references are not portable.

Implementation checklist:

- [x] Replace copied `analysis/visual-blueprint.v1.reviewed.json` with `HandoffVisualBlueprint`.
- [x] Give the handoff blueprint artifact kind `agent-handoff-visual-blueprint`.
- [x] Add explicit `consumerReferences` for handoff-local artifacts only.
- [x] Add explicit `diagnosticProvenance` entries for original Phase 3 artifacts with `consumerReadable: false`.
- [ ] Add handoff-specific normalized contracts where needed:
  - [x] `HandoffPageCompositions`
  - [x] `HandoffDesignTokens`
  - [x] `HandoffVisualStyle`
  - [x] `HandoffReviewResolution`
  - [x] `HandoffPresentationCatalog`
  - [x] `HandoffResponsiveBehavior`
  - [x] `HandoffInteractionModels`
- [x] Preserve external source paths only as diagnostics provenance.
- [x] Do not allow `.draft.json` in any consumer reference.
- [x] Preserve reviewed source artifacts unchanged.

Required consumer artifacts:

- [x] `analysis/agent-handoff/presentation-catalog.json`
- [x] `analysis/agent-handoff/presentation-mappings.json`
- [x] `analysis/agent-handoff/component-candidates.json`
- [x] `analysis/agent-handoff/component-instances.json`
- [x] `analysis/agent-handoff/responsive-behavior.json`
- [x] `analysis/agent-handoff/interaction-models.json`
- [x] `analysis/agent-handoff/originality-restrictions.json`
- [x] `analysis/agent-handoff/confidence.json`
- [x] `analysis/agent-handoff/review-resolution.json`
- [x] Existing Phase 3D artifacts already listed in `AgentHandoffContract.RequiredArtifacts`.

Aggregation rules:

- [x] Responsive behavior is a site-level handoff document with page, section, viewport rule, evidence reference, and review status.
- [x] Interaction models are a site-level handoff document with page, section, state, trigger, protected behavior boundary, evidence reference, and review status.
- [x] Component instances must point to handoff-local component candidates or diagnostic-only provenance, not `analysis/components/*` as a consumer dependency.
- [x] Confidence must distinguish artifacts reviewed by `ConfidenceScorer` from evidence-derived responsive/interaction observations.

Tests:

- [x] Handoff blueprint has no `visual-blueprint-v1` artifact kind.
- [x] Every consumer reference starts with `analysis/agent-handoff/`.
- [x] Diagnostic provenance outside handoff is marked non-consumer.
- [x] Missing local consumer artifact blocks assembly.
- [x] Reviewed source blueprint remains unchanged after handoff assembly.
- [x] Responsive and interaction handoff docs contain no path list back to `analysis/pages/*`.

Done when:

- [x] All Phase 4 consumer inputs exist inside `analysis/agent-handoff/*` with handoff-specific contracts where direct copies would leak external dependencies.

## Phase 3E.3 - Typed Reference Registry And Containment Validator

Goal: validate consumer dependencies precisely without misclassifying diagnostic paths, target generated paths, or informational URLs.

Implementation checklist:

- [x] Add a reference registry under `Analysis/Handoff`:
  - [x] artifact kind.
  - [x] JSON pointer or markdown section.
  - [x] reference category.
  - [x] required/optional behavior.
  - [x] allowed target root.
  - [x] cycle policy.
- [ ] Reference categories:
  - [x] `consumer-dependency`
  - [x] `diagnostic-provenance`
  - [x] `generated-target-path`
  - [x] `external-informational-url`
  - [x] `opaque-id`
- [x] Add `HandoffReferenceScanner`.
- [ ] Scan registered fields in:
  - [x] manifest artifact list and entries.
  - [x] handoff visual blueprint.
  - [x] page compositions.
  - [x] design tokens and visual style.
  - [x] presentation catalog and mappings.
  - [x] component candidates and instances.
  - [x] responsive behavior and interaction models.
  - [x] evidence manifest.
  - [x] review resolution.
  - [x] task inputs when machine-readable.
- [x] Reject consumer references that are absolute, UNC, drive-letter paths, path escapes, missing, unregistered, `.draft.json`, or outside handoff.
- [x] Allow diagnostic provenance outside handoff only when explicitly marked diagnostics-only and not required.
- [x] Allow external URLs only for source/reference documentation context, never as required files.

Blocking codes:

- [x] `handoff-consumer-reference-escape`
- [x] `handoff-consumer-reference-missing`
- [x] `handoff-consumer-reference-absolute`
- [x] `handoff-consumer-reference-draft`
- [x] `handoff-consumer-reference-unregistered`
- [x] `handoff-diagnostic-reference-used-as-consumer`
- [x] `handoff-artifact-reference-cycle`
- [x] `handoff-reference-category-mismatch`

Tests:

- [x] `analysis/resolved/foo.json` in a consumer field fails.
- [x] `../foo.json` fails.
- [x] `C:/foo.json` fails.
- [x] `C:\\foo.json` fails.
- [x] `//server/share/foo.json` fails.
- [x] `/tmp/foo.json` fails.
- [x] Missing handoff-local consumer artifact fails.
- [x] Diagnostic provenance outside handoff passes.
- [x] Diagnostic path moved into a consumer field fails.
- [x] Generated target paths are validated as target paths, not file dependencies.
- [x] External URLs are accepted only in registered URL fields.
- [x] Valid portable reference graph passes.

Done when:

- [x] Readiness can prove that every consumer dependency resolves inside the handoff package and every external path is diagnostics-only.

## Phase 3E.4 - Portable Manifest And Schema Hardening

Goal: make `manifest.json` portable and useful as the package entry point.

Implementation checklist:

- [ ] Remove `sourceProjectPath` as a required manifest field.
- [ ] If original source root is kept, move it to `diagnostics.sourceProjectRoot` with `role: diagnostics-only`.
- [ ] Add manifest fields:
  - [ ] `packageVersion`
  - [ ] `handoffRoot`
  - [ ] `schemaRequirements`
  - [ ] `consumerReferencePolicy`
  - [ ] `artifactEntries`
  - [ ] `packageHash`
  - [ ] `diagnosticProvenancePolicy`
  - [ ] `portableValidationCommand`
- [ ] Replace directory-size artifact entries with file-level entries or explicit directory metadata that is excluded from package hash.
- [ ] Ensure `manifest.json` does not require access to `project.json`.
- [ ] Ensure `handoff-readiness.json` can report project ID from the handoff manifest, not the original project.
- [ ] Update `agent-handoff-manifest.schema.json`.
- [ ] Update `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas/README.md`.

Tests:

- [ ] Manifest validates without original project root.
- [ ] Copy package to another folder preserves `packageHash`.
- [ ] Absolute source path changes do not affect `packageHash`.
- [ ] Missing required artifact entry fails.
- [ ] Missing required schema entry fails.
- [ ] Changed artifact bytes fail hash validation.
- [ ] Manifest self-hash does not create nondeterministic hash drift.

Done when:

- [ ] The handoff manifest is the portable package index and no longer requires source-project context for consumer validation.

## Phase 3E.5 - Shared Authoritative Slot Resolution And Evidence Provenance

Goal: evidence packaging must use reviewed mapping/catalog slot provenance, not role inference.

Implementation checklist:

- [ ] Extract shared `SectionSlotResolver` used by:
  - [ ] `PageCompositionSlotValidator`
  - [ ] `AgentHandoffEvidencePackager`
  - [ ] portable consumer loader
- [ ] Resolver output:
  - [ ] exact `starterSlotId`.
  - [ ] `slotSource`.
  - [ ] mapping ID.
  - [ ] source section ID.
  - [ ] source page ID.
  - [ ] target path.
  - [ ] diagnostic `suggestedSlotId`.
  - [ ] problem/cause/fix suggestion when unresolved.
- [ ] Replace evidence manifest's ambiguous slot field with:
  - [ ] `starterSlotId`
  - [ ] `slotSource`
  - [ ] `mappingId`
  - [ ] `suggestedSlotId`
- [ ] Rename evidence packager heuristic to `SuggestSlotFromRole`.
- [ ] Ensure `SuggestSlotFromRole` is diagnostics-only.
- [ ] Validate `slotSource=reviewed-presentation-mapping` requires non-empty `starterSlotId` and a handoff-local mapping ID.
- [ ] Validate `starterSlotId` exists in `storefront-pattern.json`.
- [ ] Validate approved visual extension slot source is explicit and reviewed.

Tests:

- [ ] Product purchase crop gets `product.purchase` from reviewed mapping.
- [ ] Ambiguous role text does not create authoritative slot.
- [ ] Suggested slot is serialized only as diagnostics.
- [ ] Mapping ID missing from `presentation-mappings.json` fails.
- [ ] Evidence slot and mapping slot mismatch fails.
- [ ] Unknown starter slot fails.
- [ ] Approved visual extension records correct source.

Done when:

- [ ] No authoritative slot in handoff evidence can be produced from role text alone.

## Phase 3E.6 - Portable Validator And Inspect CLI

Goal: add tools that validate a copied handoff package without the original Phase 3 project.

Implementation checklist:

- [ ] Add `PortableHandoffValidator`.
- [ ] Constructor accepts schema registry/root only, not repo project root.
- [ ] Public API:
  - [ ] `ValidateAsync(string handoffRoot, string schemaRoot, CancellationToken cancellationToken)`
  - [ ] no `ApprovedArtifactRootResolver`.
  - [ ] no read of `project.json`, `reports/*`, `review/*`, `analysis/resolved/*`, `presentation-catalog/*`, or `captures/*` outside handoff.
- [ ] Add CLI command:
  - [ ] `validate-handoff --handoff-root <path> --schema-root <path>`
- [ ] Add CLI command:
  - [ ] `inspect-handoff --handoff-root <path> --schema-root <path>`
- [ ] CLI output includes:
  - [ ] project ID.
  - [ ] readiness.
  - [ ] package hash.
  - [ ] artifact count.
  - [ ] schema count.
  - [ ] consumer reference count.
  - [ ] diagnostic provenance count.
  - [ ] first blocking finding with problem/cause/fix.
- [ ] Keep these commands in ReverseEngineering only.

Tests:

- [ ] `validate-handoff` succeeds on a copied package.
- [ ] `inspect-handoff` does not read source project files.
- [ ] Missing schema root fails with clear problem/cause/fix.
- [ ] Missing handoff root fails with clear problem/cause/fix.
- [ ] Readiness false fails.
- [ ] Reference escape fails.
- [ ] Corrupt artifact fails.
- [ ] Command help lists both commands.

Done when:

- [ ] Operators and future Phase 4 agents can validate a portable package without knowing the original project layout.

## Phase 3E.7 - Phase 4 Consumer Dry-Run Loader

Goal: prove the package shape is actually loadable by a future consumer without enabling generation.

Implementation checklist:

- [ ] Add read-only `HandoffConsumerDryRunLoader`.
- [ ] Loader input is only:
  - [ ] `handoffRoot`
  - [ ] `schemaRoot`
  - [ ] cancellation token
- [ ] Loader output includes:
  - [ ] project metadata.
  - [ ] pages in deterministic order.
  - [ ] exact required slots.
  - [ ] allowed target files.
  - [ ] protected files.
  - [ ] design tokens.
  - [ ] visual style.
  - [ ] responsive rules.
  - [ ] interaction states.
  - [ ] evidence file paths.
  - [ ] unresolved regions.
  - [ ] readiness report.
- [ ] Loader refuses readiness `false`.
- [ ] Loader refuses missing required slots.
- [ ] Loader refuses any path outside package.
- [ ] Loader performs no writes and no generation.
- [ ] Loader has no StorefrontBuilder dependency.

Tests:

- [ ] Loader reads copied portable package.
- [ ] Loader never resolves outside `handoffRoot`.
- [ ] Loader refuses readiness false.
- [ ] Loader exposes protected paths and stop conditions.
- [ ] Loader returns deterministic page order.
- [ ] Loader does not create files.
- [ ] Loader does not call Commerce Node or StorefrontBuilder.

Done when:

- [ ] Phase 3E proves the final handoff is not just valid JSON, but a usable read-only Phase 4 input package.

## Phase 3E.8 - Isolated Copy Proof And Negative Portability Mutations

Goal: prove the package survives being copied away from its source project.

Positive test flow:

```text
create ready Phase 3D/3E fixture project
-> assemble handoff
-> copy only analysis/agent-handoff/* to a new temp root
-> copy registered schemas to a new schema root
-> withhold or delete original project root
-> run portable validator
-> run handoff consumer dry-run loader
```

Positive assertions:

- [ ] Manifest loads.
- [ ] Every required artifact loads.
- [ ] Every required schema loads.
- [ ] All hashes validate.
- [ ] All consumer references resolve inside the copied package.
- [ ] Diagnostics provenance is ignored by consumer loading.
- [ ] Required page slots remain available.
- [ ] Screenshots and section crops remain available.
- [ ] Original project access is not attempted.
- [ ] Package hash is stable across destination folders.

Negative mutations:

- [ ] Delete `presentation-catalog.json`.
- [ ] Delete `presentation-mappings.json`.
- [ ] Delete `responsive-behavior.json`.
- [ ] Delete `interaction-models.json`.
- [ ] Delete one section crop.
- [ ] Replace a consumer reference with `analysis/resolved/foo.json`.
- [ ] Replace a consumer reference with an absolute original project path.
- [ ] Replace a consumer reference with `../outside.json`.
- [ ] Move a diagnostics path into a consumer field.
- [ ] Remove a required schema file.
- [ ] Corrupt an artifact after copy.
- [ ] Mutate package hash ordering.

Tests:

- [ ] Each negative mutation returns the exact Phase 3E blocker code.
- [ ] Positive copy proof passes after original project is unavailable.

Done when:

- [ ] The isolated copy proof is the main Phase 3E portability evidence.

## Phase 3E.9 - Final Phase 3E Closure Gate

Goal: add a no-skip gate that proves Phase 3D closure plus Phase 3E portability on final clean `HEAD`.

Create:

```powershell
scripts/qa/run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1
```

Allowed parameter:

- [ ] `-CommandTimeoutSeconds`

Forbidden parameters:

- [ ] `-SkipPhase3AGate`
- [ ] `-SkipPhase3BGate`
- [ ] `-SkipPhase3CGate`
- [ ] `-SkipPhase3DGate`
- [ ] `-SkipPortableProof`
- [ ] `-SkipStorefrontBuilderSmoke`
- [ ] `-AllowDirtyTree`

Gate order:

```text
clean tree check
-> record tested HEAD
-> build ReverseEngineering
-> run Phase 3D final closure gate once
-> full ReverseEngineering tests
-> handoff-specific blueprint tests
-> portable artifact set tests
-> typed reference containment tests
-> manifest portability/hash tests
-> evidence slot provenance tests
-> portable validator CLI tests
-> isolated copy proof
-> Phase 4 dry-run loader proof
-> negative portability mutation tests
-> boundary scans
-> StorefrontBuilder plan-only smoke
-> final inspect-handoff proof
-> assert HEAD unchanged
-> assert working tree clean
-> write ignored runtime report under obj/storefront-reverse-engineering/reports
```

Boundary scans:

- [ ] No StorefrontBuilder handoff consumption.
- [ ] No Razor/CSS/JS storefront output from ReverseEngineering.
- [ ] No Starter writes.
- [ ] No generated storefront writes.
- [ ] No production project references ReverseEngineering.
- [ ] No consumer reference outside handoff in positive proof fixtures.
- [ ] No absolute consumer paths in positive proof fixtures.
- [ ] No `.draft.json` consumer references in positive proof fixtures.
- [ ] No direct Commerce Node browser calls.
- [ ] No generated route behavior.

Gate report must record:

- [ ] status.
- [ ] tested SHA.
- [ ] final `HEAD`.
- [ ] branch.
- [ ] working tree clean status.
- [ ] UTC timestamp.
- [ ] .NET version.
- [ ] full test count.
- [ ] Phase 3D gate result.
- [ ] portable package result.
- [ ] reference containment result.
- [ ] evidence slot provenance result.
- [ ] consumer dry-run result.
- [ ] negative mutation counts.
- [ ] StorefrontBuilder smoke result.
- [ ] GitHub Actions status.
- [ ] closure decision.

Done when:

- [ ] The Phase 3E gate can fail dirty tree, fail changed `HEAD`, prove isolated package validation, and write an ignored runtime report.

## Phase 3E.10 - Documentation Alignment And Final Candidate Procedure

Goal: prevent "gate passed, then docs changed" from invalidating final closure.

Files to create:

- [ ] `docs/qa/phase3e-final-closure.md`

Files to update:

- [ ] `docs/visual-reverse-engineering-skill/README.md`
- [ ] `docs/visual-reverse-engineering-skill/reference.md`
- [ ] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`
- [ ] `docs/architecture/11-storefront-builder.md`
- [ ] `docs/agents/storefront-builder.md`
- [ ] `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas/README.md`
- [ ] CLI help or README if a local ReverseEngineering command reference exists.

Required wording before final gate:

```text
Phase 3E remains in progress until the final Phase 3E runtime gate passes
on this same clean HEAD. The ignored gate report is authoritative final
proof; tracked docs must not require a post-gate source commit.
```

Final procedure:

- [ ] Complete code, tests, schemas, docs, and status text.
- [ ] Commit final candidate, suggested message: `phase 3e: finalize portable handoff closure`.
- [ ] Verify `git status --short` returns empty.
- [ ] Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1`.
- [ ] Do not edit source/docs after gate pass.
- [ ] Do not make a "record proof" commit after gate pass.
- [ ] Do not clean whitespace after gate pass.
- [ ] Do not change `.gitignore` after gate pass.
- [ ] If anything changes after gate pass, rerun the full Phase 3E gate on the new `HEAD`.

Done when:

- [ ] Documentation already describes the final proof procedure before the final candidate commit, and the runtime report supplies exact final closure evidence.

## Recommended Implementation Order

1. [x] Phase 3E.0 baseline and contract lock.
2. [x] Phase 3E.1 canonical package contract, schema list, reference registry, and hash rules.
3. [x] Phase 3E.2 handoff-specific consumer artifacts.
4. [x] Phase 3E.3 typed reference containment validator.
5. [ ] Phase 3E.4 portable manifest and schema hardening.
6. [ ] Phase 3E.5 shared authoritative slot resolver and evidence provenance.
7. [ ] Phase 3E.6 portable validator and inspect CLI.
8. [ ] Phase 3E.7 Phase 4 read-only dry-run loader.
9. [ ] Phase 3E.8 isolated copy proof and negative portability mutations.
10. [ ] Phase 3E.9 final Phase 3E closure gate.
11. [ ] Phase 3E.10 documentation alignment and final candidate procedure.
12. [ ] Final candidate commit.
13. [ ] Run final Phase 3E gate.
14. [ ] Make no further source/docs commits after the passing gate.

Suggested development commits:

1. [ ] `phase 3e: define portable handoff contract`
2. [x] `phase 3e: package handoff-local consumer artifacts`
3. [x] `phase 3e: enforce typed handoff references`
4. [ ] `phase 3e: harden manifest schemas and package hashes`
5. [ ] `phase 3e: share reviewed slot provenance`
6. [ ] `phase 3e: add portable validation commands`
7. [ ] `phase 3e: prove isolated handoff loading`
8. [ ] `phase 3e: add portability mutation proofs`
9. [ ] `phase 3e: add final clean-head gate`
10. [ ] `phase 3e: align closure docs`

## Test Matrix

Contract and schemas:

- [ ] `PortableHandoffContractTests`
- [ ] `PortableHandoffSchemaRequirementTests`
- [ ] `AgentHandoffManifestSchemaTests`
- [ ] `AgentHandoffRequiredArtifactTests`

Handoff artifact normalization:

- [x] `HandoffVisualBlueprintTests`
- [x] `HandoffPageCompositionsTests`
- [x] `HandoffPresentationCatalogTests`
- [x] `HandoffResponsiveBehaviorTests`
- [x] `HandoffInteractionModelTests`

Reference validation:

- [x] `HandoffReferenceScannerTests`
- [x] `HandoffReferenceContainmentTests`
- [x] `HandoffReferenceCategoryTests`
- [x] `HandoffReferenceCycleTests`

Manifest and hashing:

- [ ] `PortableHandoffManifestTests`
- [ ] `PortableHandoffPackageHashTests`
- [ ] `PortableHandoffSchemaHashTests`

Slot provenance:

- [ ] `SectionSlotResolverTests`
- [ ] `AgentHandoffEvidenceSlotProvenanceTests`
- [ ] `PageCompositionSlotValidatorSharedResolverTests`

Portable validation and loading:

- [ ] `PortableHandoffValidatorTests`
- [ ] `PortableHandoffCliTests`
- [ ] `HandoffConsumerDryRunLoaderTests`
- [ ] `PortableHandoffCopyProofTests`

Negative portability:

- [ ] `Phase3ENegativeReferenceMutationTests`
- [ ] `Phase3ENegativeArtifactMutationTests`
- [ ] `Phase3ENegativeSchemaMutationTests`
- [ ] `Phase3ENegativeHashMutationTests`

Gate:

- [ ] `Phase3EFinalClosureGate_IsNoSkipCleanHeadGate`
- [ ] `Phase3EFinalClosureGate_InvokesPhase3DOnce`
- [ ] `Phase3EFinalClosureGate_RecordsPortableProof`
- [ ] `Phase3EFinalClosureGate_FailsDirtyTree`

## Definition Of Done

Portable contract:

- [ ] Handoff manifest does not require absolute source project path.
- [ ] Required artifacts and required schemas are canonical and shared.
- [ ] Package hash is stable across copy locations.
- [ ] Directory entries are not hash substitutes for file-level entries.

Artifact completeness:

- [x] Presentation catalog is packaged or embedded inside handoff.
- [x] Reviewed mappings are packaged.
- [x] Component candidates and instances are packaged.
- [x] Responsive behavior is packaged.
- [x] Interaction models are packaged.
- [x] Originality restrictions are packaged.
- [x] Confidence report is packaged with correct review/evidence-derived labels.
- [x] Review resolution is packaged.

Reference validation:

- [x] Every registered consumer reference is scanned.
- [x] Consumer path escape is blocked.
- [x] Missing consumer target is blocked.
- [x] Absolute consumer path is blocked.
- [x] `.draft.json` consumer reference is blocked.
- [x] Diagnostic-as-consumer misuse is blocked.
- [x] External URLs are not treated as required file dependencies.

Evidence provenance:

- [ ] Authoritative slot comes from reviewed mapping, exact contract, or approved visual extension.
- [ ] Role inference is diagnostic-only.
- [ ] Mapping ID and evidence slot consistency is validated.
- [ ] Unknown slots are blocked.

Portability:

- [ ] Package copied to isolated temp root validates.
- [ ] Original Phase 3 project is not required.
- [ ] Consumer loader reads package without external access.
- [ ] Screenshots and section crops remain available inside copied package.
- [ ] Negative portability mutations fail with exact blocker codes.

Boundaries:

- [ ] StorefrontBuilder still does not consume handoff.
- [ ] No generation behavior is introduced.
- [ ] No Razor/CSS/JS writes are introduced.
- [ ] No Starter writes are introduced.
- [ ] No production project references ReverseEngineering.
- [ ] No direct Commerce Node browser calls are introduced.

Final gate:

- [ ] Phase 3D final closure gate passes as part of Phase 3E gate.
- [ ] Full ReverseEngineering tests pass.
- [ ] Portable copy proof passes.
- [ ] Phase 4 dry-run loader proof passes.
- [ ] Negative portability tests pass.
- [ ] StorefrontBuilder plan-only smoke passes.
- [ ] Working tree is clean.
- [ ] Tested SHA equals final `HEAD`.
- [ ] No commit is created after the passing gate.

## Final Phase 3 Closure Statement

Phase 3 can close after Phase 3E only when this statement is true:

```text
The reviewed storefront blueprint and every Phase 4 consumer dependency are
contained within analysis/agent-handoff; all consumer references resolve
inside that package; external Phase 3 paths are diagnostics-only; exact
Storefront slots remain backed by reviewed Presentation mappings, exact
contracts, or approved visual extensions; the package can be copied,
validated, and dry-run loaded independently of its source project; and the
full Phase 3E closure gate passes on the same clean repository HEAD with no
later source or documentation commit.
```

Approved Phase 4 input remains:

```text
analysis/agent-handoff/
registered handoff schemas
```

Next phase:

```text
Phase 4 - Agent-Assisted Storefront Visual Generation
```

## Autoplan Decision Audit

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | CEO | Keep Phase 3E as an additive portability correction after Phase 3D. | Auto-decided | Preserve reviewable history | Phase 3D correctness proof remains valid; Phase 3E addresses copied-package portability. | Rewrite Phase 3D closure history. |
| 2 | CEO | Reclassify the old current-HEAD blocker as a future proof rule. | Auto-decided | Evidence over stale assumptions | Current repo state indicates Phase 3D proof matches `HEAD`; Phase 3E still must rerun final proof after its own commits. | Treat P0-3 as still currently failing. |
| 3 | Eng | Build handoff-specific contracts instead of copying reference-bearing reviewed artifacts. | Auto-decided | Consumer contract clarity | Direct copies can retain dependencies on `analysis/resolved`, `presentation-catalog`, `reports`, or `captures`. | Normalize only `visual-blueprint.json`. |
| 4 | Eng | Add a typed reference registry. | Auto-decided | Precision | Consumer dependencies, diagnostics, URLs, and generated target paths require different validation rules. | Scan every path-looking string as a dependency. |
| 5 | Eng | Add a separate portable validator that accepts only handoff root and schema root. | Auto-decided | True portability | The existing validator proves source-project consistency, not copied-package independence. | Reuse `ApprovedArtifactRootResolver` in portable validation. |
| 6 | Eng | Share authoritative slot resolution between validation and evidence packaging. | Auto-decided | Single source of truth | Role inference can mislabel ecommerce slots; reviewed mapping/catalog logic must be shared. | Keep `InferSlot(node.Role)` authoritative in evidence. |
| 7 | QA | Hash sorted file-level package entries and exclude diagnostic values. | Auto-decided | Deterministic release evidence | Package hash must survive copy location changes and avoid self-referential manifest drift. | Hash absolute paths, timestamps, or directory counts. |
| 8 | DX | Add `validate-handoff` and `inspect-handoff` before Phase 4 generation work. | Auto-decided | Operator clarity | A future agent needs a simple read-only way to prove a package is consumable. | Make Phase 4 discover portability failures during generation. |
| 9 | QA | Phase 3E final gate invokes Phase 3D once, then runs portable proofs. | Auto-decided | Avoid duplicate expensive gates | Phase 3D already calls Phase 3A/B/C; Phase 3E should layer additional proof on top. | Re-run Phase 3A/B/C separately and duplicate Phase 3D behavior. |

## GSTACK Review Report

### Plan Summary

This plan converts the Phase 3E proposal into a codebase-specific portability phase. It keeps StorefrontBuilder consumption disabled, turns the handoff into a self-contained contract package, proves copied-package validation, and closes with a strict final `HEAD` gate.

### Review Scores

- CEO: Pass with scope control. Phase 3E protects the business value of Phase 3 by making the handoff usable outside the original project root.
- Design: Skipped. No UI or visual implementation is planned.
- Eng: Pass with required corrections. The plan addresses concrete current-code issues in handoff assembly, readiness validation, manifest schema, evidence slot provenance, and final proof.
- DX: Pass. The portable CLI and dry-run loader make future Phase 4 agent work easier to verify and harder to misuse.

### Cross-Phase Themes

- Package autonomy: Phase 4 must not need the source project as a hidden dependency.
- Source-of-truth discipline: reviewed handoff contracts and schemas must replace path copies of upstream analysis artifacts.
- Closure integrity: final proof must be on the same clean `HEAD`, with no post-proof source/docs commit.

### Implementation Tasks

- [ ] Implement Phase 3E.0 baseline and boundary lock.
- [ ] Implement Phase 3E.1 portable package contract.
- [x] Implement Phase 3E.2 handoff-specific consumer artifacts.
- [x] Implement Phase 3E.3 typed reference containment.
- [ ] Implement Phase 3E.4 manifest and schema hardening.
- [ ] Implement Phase 3E.5 shared slot provenance.
- [ ] Implement Phase 3E.6 portable validation commands.
- [ ] Implement Phase 3E.7 consumer dry-run loader.
- [ ] Implement Phase 3E.8 isolated copy proof and mutations.
- [ ] Implement Phase 3E.9 final gate.
- [ ] Implement Phase 3E.10 documentation alignment.

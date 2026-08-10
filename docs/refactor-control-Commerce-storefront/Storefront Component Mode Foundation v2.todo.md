# Storefront Component Mode Foundation v2

Status: planned
Owner: Storefront V2 architecture
Scope: H1 code/test/project-graph review after Hybrid clarification

## Goal

Re-evaluate the Storefront component mode implementation after `Storefront Hybrid Architecture Clarification.todo.md` corrected the H0 documentation source of truth.

H1 may touch code, tests, project references, and documentation only after this backlog is explicitly approved for implementation. Do not infer code movement from historical Hybrid shell wording.

## Required Reading

- [ ] Read `AGENTS.md`.
- [ ] Read `docs/architecture/README.md`.
- [ ] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [ ] Read `docs/architecture/03-runtime-boundaries.md`.
- [ ] Read `docs/architecture/05-project-and-folder-guide.md`.
- [ ] Read `docs/architecture/10-v2-contract-ownership.md`.
- [ ] Read `docs/refactor-control-Commerce-storefront/Storefront Hybrid Architecture Clarification.todo.md`.
- [ ] Read `docs/refactor-control-Commerce-storefront/Storefront Reference Components.todo.md`.

## H1 Review Queue

- [ ] Re-evaluate whether `BlazorShop.Storefront.Components.Hybrid` should remain, be narrowed, be renamed, or be retired.
- [ ] Re-evaluate whether public descriptors should stay tied to physical mode project assembly names.
- [ ] Re-evaluate `StorefrontComponentModeBoundaryValidator` profiles.
- [ ] Re-evaluate `StorefrontComponentModeDependencyTests` exact project reference assertions.
- [ ] Re-evaluate `StorefrontComponentDescriptorTests` descriptor/project mode consistency.
- [ ] Re-evaluate `StorefrontVisualOnlyBoundaryTests.F1_41_ReferenceComponentModeReferences_AreNarrowAndAdoptedOnlyByV2`.
- [ ] Re-evaluate V2 visible contact flow and decide whether the `Components.Hybrid` contact descriptor remains useful.
- [ ] Re-evaluate whether future shared interactive components should live in downloadable WASM assemblies directly instead of a server shell project.
- [ ] Preserve Browser/BFF data path and no direct Commerce Node calls.
- [ ] Preserve V2/Starter/generated visual ownership.
- [ ] Define browser QA required before closing any code-level change.

## Scope Guardrails

- [ ] Do not introduce `InteractiveAuto` for public Storefront V2 without a new architecture decision.
- [ ] Do not introduce `InteractiveServer` or SignalR/circuit-based public storefront interactivity without a new architecture decision.
- [ ] Do not rename route folders such as `Pages/Hybrid` unless an approved migration plan covers source references, docs, tests, and generated storefront implications.
- [ ] Do not remove `Components.Hybrid` while tests/code still reference it.
- [ ] Do not let browser/WASM code call Commerce Node APIs directly.
- [ ] Do not move V2 theme classes, CSS, final copy, or generated output into reusable mode libraries.

## Expected H1 Evidence

- [ ] Record baseline `git status --short`.
- [ ] Record all code/test/project-reference areas reviewed.
- [ ] Record any selected implementation direction with rationale.
- [ ] Run focused architecture tests for changed boundary validators and descriptor rules.
- [ ] Run focused build/test gates for every changed project.
- [ ] Run browser QA if V2 visible behavior, hydration, render-mode placement, or wrapper/component composition changes.
- [ ] Commit each H1 phase separately.

## Definition Of Done

- [ ] The physical component mode project graph matches the clarified Hybrid architecture.
- [ ] Descriptor mode ownership is semantic, tested, and no longer misleading.
- [ ] Boundary validators reflect actual intended ownership.
- [ ] V2/V2.WASM wrapper usage is either preserved as the documented pattern or replaced by a tested equivalent.
- [ ] Starter/generated storefront implications are documented and tested where relevant.
- [ ] Browser/BFF data path remains intact.
- [ ] Visual ownership remains host/generator-owned.
- [ ] Browser QA evidence exists for any visible runtime behavior change.

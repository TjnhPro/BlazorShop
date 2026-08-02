# Visual Reverse Engineering Skill Docs

This folder documents the StorefrontBuilder workflow for turning reference ecommerce storefront evidence into reviewable, generated Blazor storefront projects. Phase 3A introduced `BlazorShop.AI.StorefrontReverseEngineering`, a separate development-time executable that records reference-site evidence and neutral visual-blueprint drafts under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` or `obj/storefront-reverse-engineering/projects/{ProjectId}`. Phase 3B extends that executable with visual analysis, ecommerce mapping, confidence review, and Visual Blueprint v1 artifacts. Phase 3C turns the reviewed analysis into a strict site-level `analysis/agent-handoff/*` package with allowed/protected files, page compositions, Storefront pattern contracts, and final handoff readiness. Phase 3D is the completed final correctness and closure-proof phase for that handoff; D13-D19 prove resolved reviewed inputs, exact slot mapping, viewport-specific crops, real positive/negative mutation behavior, and clean-HEAD final gate closure. Phase 3E makes that handoff portable: manifests, schemas, hashes, canonical artifact/schema membership, manifest/readiness agreement, references, evidence slot provenance, CLI validation, dry-run loading, isolated copy proof, and the final Phase 3E gate all operate without the original source project. StorefrontBuilder remains the generation/regeneration tool; Phase 4.1 adds portable handoff preflight only, while handoff-driven generation remains gated behind later Phase 4 phases.

StorefrontReverseEngineering is the evidence/runtime foundation. It captures rendered browser evidence, workflow state, readiness reports, Phase 3B analysis artifacts, Phase 3C handoff artifacts, and conservative originality/provenance findings. Its final capture flow extracts rendered evidence before native screenshots, records explicit quality/fallback decisions, uses stitched fallback only with real segment artifacts, and keeps raw/normalized artifacts tied by capture correlation IDs. StorefrontBuilder is the generator. Phase 3C does not create Razor, CSS, generated projects, or active blueprint consumption.

## Read First

1. [StorefrontBuilder Architecture](../architecture/11-storefront-builder.md) - ownership, boundaries, artifact rules, and validation gates.
2. [Reference](reference.md) - commands, modes, generated artifacts, and gate expectations.
3. [How To Generate And Validate](how-to-generate-and-validate.md) - operator workflow for an existing or new generated storefront.
4. [Tutorial: Generated Proof](tutorial-generated-proof.md) - concrete walkthrough using the on-demand generated proof artifact.
5. [Explanation: Boundaries And Regeneration](explanation-boundaries-and-regeneration.md) - why generated storefronts stay isolated from Storefront V2 and backend projects.

## Historical Plans

The phase plans are retained as implementation history and checklist evidence:

- [18-StorefrontBuilder-Phase4-Agent-Assisted-Visual-Generation.todo.md](18-StorefrontBuilder-Phase4-Agent-Assisted-Visual-Generation.todo.md)
- [01-StorefrontBuilder-Foundation.todo.md](01-StorefrontBuilder-Foundation.todo.md)
- [02-StorefrontBuilder-Visual-Generation.todo.md](02-StorefrontBuilder-Visual-Generation.todo.md)
- [03-StorefrontBuilder-QA-Regeneration.todo.md](03-StorefrontBuilder-QA-Regeneration.todo.md)
- [08-StorefrontReverseEngineering-Engine-Foundation.todo.md](08-StorefrontReverseEngineering-Engine-Foundation.todo.md)
- [09-StorefrontReverseEngineering-Phase3A-Hardening.todo.md](09-StorefrontReverseEngineering-Phase3A-Hardening.todo.md)
- [StorefrontBuilder Architecture Note](StorefrontBuilder-architecture-note.md)

The architecture docs are the current source of truth when a historical plan conflicts with current code.

## Runtime Boundary Reminder

Generated storefront server/BFF projects consume `BlazorShop.Storefront.Presentation` for shared App/Routes/page services/BFF/SEO/media composition, then register generated visual components as Presentation view slots. Presentation composes Runtime internally, Runtime owns the direct generated-client transport dependency, and generated projects reference Presentation/Components directly while keeping Runtime/Client version metadata for compatibility proof. Generated visual files must not declare `@page` or add route assemblies. Browser and WASM code must use same-origin generated endpoints and browser-safe `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives, not Runtime or guessed Storefront API envelopes.

Use `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure` for generated package/boundary proof and lifecycle proof: post-regeneration build, deterministic no-op regeneration, and manual-edit conflict reporting. Use `.\scripts\qa\run-storefront-builder-regeneration-gate.ps1` for CI-friendly ownership/regeneration checks that do not require live Commerce Node data. Use `-ProofLevel FoundationFunctionalFast` for PR-safe generated browser action proof. Use `.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1` before release closure when fixture-backed generated browser behavior must be proven from a clean local/CI runtime.

Use the Phase 3A hardening gate for StorefrontReverseEngineering runtime, schema, workflow, browser evidence, interaction, readiness, boundary, and StorefrontBuilder compatibility changes:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
```

The gate is fixture-based and runs without an external reference website after Playwright Chromium has been installed locally. It writes commit-linked reports under `obj/storefront-reverse-engineering/reports/`; the tracked closure summary is `docs/qa/phase3a-final-fix-closure.md`.

Readiness is machine-readable in `reports/readiness-report.json`. It validates file existence, schemas, screenshot quality, evidence depth, correlation, originality, and latest workflow run state. Use `inspect --project <path>` before handoff to see latest run status, readiness status, blocking/warning counts, latest blocker, blueprint path, readiness report path, Phase 3B artifact status, review queue count, generation readiness, and step rows.

Phase 3B artifacts can be inspected without Playwright:

```powershell
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/fixturedemo
```

Rerun one Phase 3B step plus downstream steps with:

```powershell
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step aggregate-evidence
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step assemble-blueprint-v1
```

`inspect` reports problem/cause/fix guidance for missing Phase 3A readiness, missing evidence snapshots, invalid token schemas, Presentation catalog drift, unresolved blocking review items, unsupported critical patterns, and final handoff readiness blockers. StorefrontBuilder preflight may consume portable `analysis/agent-handoff/*` packages through `build-storefront.ps1 -Mode preflight-only -HandoffRoot <path>`; generation remains unchanged until the later Phase 4 generation-plan cutover.

Use the Phase 3B gate for StorefrontReverseEngineering visual analysis, mapping, review, blueprint, inspect, docs, and StorefrontBuilder boundary changes:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3b-gate.ps1
```

The gate uses local fixtures and a bounded `dotnet test --blame-hang-timeout 5m` configuration so long test runs fail with actionable evidence instead of hanging indefinitely.

Use the Phase 3C gate for final handoff fixture, mutation, schema, readiness, and boundary closure:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1
```

Phase 3C handoff readiness is machine-readable in `analysis/agent-handoff/handoff-readiness.json`. The handoff package under `analysis/agent-handoff/` is the only approved input shape for a Phase 4 consumer. Phase 4 may read those files and the registered schemas, must fail unless final handoff readiness passed, must not reinterpret raw evidence unless it runs a new ReverseEngineering pass, must not write into Starter, and must not change protected Storefront runtime behavior.

Use the Phase 3D gate only for final local closure proof after all Phase 3D commits are in place and the working tree is clean:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1
```

The Phase 3D gate has no skip flags. It remains the Phase 3D correctness proof with a clean tree and tested SHA equal to final `HEAD`; see `docs/qa/phase3d-final-closure.md` for the Phase 3D proof report. Phase 3 final closure after Phase 3E requires the Phase 3E gate below.

Use the Phase 3E gate only after the final candidate commit and a clean working tree:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1
```

The Phase 3E gate has no skip flags and is non-recursive: it does not invoke the Phase 3D gate or the historical Phase 3A/3B/3C gates. It restores once, builds once, runs later tests with `--no-build --no-restore`, groups closure proofs into the minimum test-host processes, reuses shared positive/portable baselines in the test host, runs one canonical boundary scan, runs StorefrontBuilder plan-only smoke once, records global timeout telemetry, cleans transient success artifacts, and verifies final `HEAD` equality. Portable validation checks copied-package canonical artifacts, schema requirements, package hashes, typed reference categories, and `manifest.json` readiness against `handoff-readiness.json`; source-aware slot validation blocks orphan reviewed mappings through `reviewed-slot-mapping-orphan`. GitHub Actions evidence is intentionally out of scope while Actions are disabled during development. Phase 3E remains in progress until the final Phase 3E runtime gate passes on this same clean HEAD. The ignored gate report is authoritative final proof; tracked docs must not require a post-gate source commit.

`BlazorShop.Storefront.Components.Features` is retired. StorefrontBuilder output should generate project-local visual templates from evidence while consuming shared `Contracts`, `Headless`, and `Browser` primitives.

Phase 3B through Phase 3E ReverseEngineering artifacts are handoff evidence only. StorefrontBuilder generation remains unchanged until a later approved phase wires the reviewed `analysis/agent-handoff/*` package into generation planning. Phase 4 may read only `analysis/agent-handoff/*` and registered handoff schemas, using `build-storefront.ps1 -Mode preflight-only`, `validate-handoff`, `inspect-handoff`, or the dry-run loader as portable preflight checks.

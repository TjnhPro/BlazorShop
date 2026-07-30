# Visual Reverse Engineering Skill Docs

This folder documents the StorefrontBuilder workflow for turning reference ecommerce storefront evidence into reviewable, generated Blazor storefront projects. Phase 3A introduced `BlazorShop.AI.StorefrontReverseEngineering`, a separate development-time executable that records reference-site evidence and neutral visual-blueprint drafts under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` or `obj/storefront-reverse-engineering/projects/{ProjectId}`. Phase 3B extends that executable with visual analysis, ecommerce mapping, confidence review, and Visual Blueprint v1 artifacts. StorefrontBuilder remains the generation/regeneration tool and does not consume those new artifacts until a later approved phase.

StorefrontReverseEngineering is the evidence/runtime foundation. It captures rendered browser evidence, workflow state, readiness reports, Phase 3B analysis artifacts, and conservative originality/provenance findings. Its final capture flow extracts rendered evidence before native screenshots, records explicit quality/fallback decisions, uses stitched fallback only with real segment artifacts, and keeps raw/normalized artifacts tied by capture correlation IDs. StorefrontBuilder is the generator. Phase 3B does not create Razor, CSS, generated projects, or active blueprint consumption.

## Read First

1. [StorefrontBuilder Architecture](../architecture/11-storefront-builder.md) - ownership, boundaries, artifact rules, and validation gates.
2. [Reference](reference.md) - commands, modes, generated artifacts, and gate expectations.
3. [How To Generate And Validate](how-to-generate-and-validate.md) - operator workflow for an existing or new generated storefront.
4. [Tutorial: Generated Proof](tutorial-generated-proof.md) - concrete walkthrough using the on-demand generated proof artifact.
5. [Explanation: Boundaries And Regeneration](explanation-boundaries-and-regeneration.md) - why generated storefronts stay isolated from Storefront V2 and backend projects.

## Historical Plans

The phase plans are retained as implementation history and checklist evidence:

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

`inspect` reports problem/cause/fix guidance for missing Phase 3A readiness, missing evidence snapshots, invalid token schemas, Presentation catalog drift, unresolved blocking review items, and unsupported critical patterns. StorefrontBuilder does not consume `analysis/visual-blueprint.v1.*.json` yet; generation remains unchanged until a later approved cutover.

`BlazorShop.Storefront.Components.Features` is retired. StorefrontBuilder output should generate project-local visual templates from evidence while consuming shared `Contracts`, `Headless`, and `Browser` primitives.

Phase 3B ReverseEngineering artifacts are future handoff evidence only. StorefrontBuilder generation remains unchanged until a later approved phase wires Visual Blueprint v1 into generation. Phase 3B consumes the trustworthy evidence foundation and should not repair capture fallback, readiness depth, or StorefrontBuilder generation behavior.

# How To Generate And Validate A Storefront

Use this workflow when creating or updating a generated storefront from the Storefront Starter.

## Prerequisites

- .NET SDK from `global.json`.
- PowerShell.
- Node dependencies installed in `tools/BlazorShop.AI.StorefrontBuilder` when browser QA is required.
- Current Storefront API client/runtime/presentation/components packages build successfully.

Install Node dependencies:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm ci
Pop-Location
```

## Generate

Create a generated storefront:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Url https://reference.example `
  -Name Demo `
  -StoreKey sample `
  -OutputRoot artifacts/storefront-builder/generated `
  -Mode generate
```

For manual project output, `-OutputRoot artifacts/storefront-builder` is also approved. That writes the project as `artifacts/storefront-builder/BlazorShop.Storefront.{Name}` instead of under the default `generated` subfolder.

Use a full project name only when the folder must already include the `BlazorShop.Storefront.` prefix:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Name BlazorShop.Storefront.Demo `
  -StoreKey sample `
  -OutputRoot artifacts/storefront-builder/generated `
  -Mode generate
```

Generate from a portable Phase 3E handoff package:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Mode preflight-only `
  -HandoffRoot <portable-handoff-root> `
  -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas

.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Mode plan-only `
  -Name Demo `
  -StoreKey sample `
  -HandoffRoot <portable-handoff-root> `
  -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas

.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Mode generate `
  -Name Demo `
  -StoreKey sample `
  -OutputRoot obj/storefront-builder/generated `
  -HandoffRoot <portable-handoff-root> `
  -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas `
  -Force
```

The handoff generation path writes a Starter-based `BlazorShop.Storefront.{Name}` project and stores `generation-plan.json`, `handoff-generation-summary.md`, `handoff-placeholders.json`, and `agent-task-package/` under `docs/storefront-analysis/`. It consumes only the portable `analysis/agent-handoff/*` package and schemas; it does not read raw captures or mutate Starter.

## Plan, Implement, And QA Visuals

Use the visual skills only after StorefrontBuilder has created a handoff-generated project. StorefrontBuilder remains the source of project creation; `tools/BlazorShop.AI.Visual` only defines agent instructions, references, schemas, examples, and report contracts.

1. Run `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`.
   - Read `docs/storefront-analysis/generation-plan.json`.
   - Read `docs/storefront-analysis/agent-task-package/manifest.json`.
   - Emit `docs/storefront-analysis/visual-plan.json`.
   - Emit `docs/storefront-analysis/visual-implementation-checklist.todo.md`.
2. Run `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`.
   - Edit only generated visual files listed by the task package.
   - Preserve Presentation semantic descriptors such as product purchase and cart commands.
   - Emit `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json`.
   - Run `record-agent-visual-writes.mjs`.
   - Build the generated project.
   - Emit `docs/storefront-analysis/visual-implementation-report.json` and `.md`.
3. Run `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`.
   - Run browser evidence with `run-visual-qa.mjs`.
   - Inspect screenshots for blank states, missing slots, overflow, broken assets, and descriptor loss.
   - Repair only generated-owned visual defects, then rerun recorder and QA.
   - Emit `docs/storefront-analysis/visual-qa-report.json` and `.md`.

Record visual writes:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --workspace-root <generated-workspace-root> --written-files <comma-separated-generated-visual-paths>
```

Run visual QA:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --workspace-root <generated-workspace-root> --fixture-root <fixture-root> --screenshot-root obj/storefront-builder/visual-qa-screens
```

This fixture-root command is skeleton/static proof for fast feedback. It can prove generated shell coverage, planned placeholders, required slots, and artifact wiring before a generated host is running, but it is not final release closure.

Run target MVP closure after plan, implementation, recorder, build, QA, and optional repair evidence exist:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -WorkspaceRoot <generated-workspace-root> -FixtureRoot <fixture-root> -HandoffRoot <portable-handoff-root> -CommandTimeoutSeconds 600
```

Run runtime MVP closure when the generated host should be started and proved end to end:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -WorkspaceRoot <generated-workspace-root> -ProofMode Runtime -BaseUrl http://127.0.0.1:18620 -StartRuntimeHost -HandoffRoot <portable-handoff-root> -CommandTimeoutSeconds 600
```

Run final closure only after the candidate commit is complete and the working tree is clean. Do not seed `obj` manually for this gate; it validates the tracked portable handoff fixture, removes stale pilot output, regenerates fresh disposable output through `build-storefront.ps1 -Mode generate -HandoffRoot ... -HandoffSchemaRoot ...`, records changed-file evidence, runs runtime visual QA, materializes the Reference QA JSON from the current `visual-qa-runtime-summary.json`, runs the MVP gate, runs `FoundationFunctionalFast`, runs regeneration ownership proof, and verifies the same clean `HEAD` at the end.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
```

Failure output identifies the broken evidence link and writes a report under `obj/storefront-builder/reports/phase4-final-closure-gate-*.md` plus JSON beside it. Inspect the failed step name, command, problem, likely cause, and rerun command in that report. Generated pilot output, screenshots, MVP reports, and closure reports under `obj` are disposable local artifacts and should not be committed unless a later plan explicitly asks for tracked evidence.

## Update

Regenerate all generated visual/composition output:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope all
```

Regenerate a narrower target:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope page `
  -Target Home
```

Use `-Scope conflicts` before manual edits to generated files when you need to confirm idempotency state.

Preview before applying:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope all `
  -WhatIf
```

`-WhatIf` runs the same fresh-candidate planning pipeline as apply mode and exits before copying target changes. Read the `WhatIf report:` path printed by the command; by default it is written under the output root `.regeneration-reports/` folder and contains create/update/conflict/obsolete/platform metadata actions. Use `-WhatIfReportPath <path>` only when you need a custom approved report path under the output report folder, repo `obj`, or `artifacts/storefront-builder`.

For handoff-generated projects, regeneration preserves handoff metadata and reapplies stored `docs/storefront-analysis/generation-plan.json` in the candidate. Handoff package/readiness hash drift fails with a re-plan/update requirement; Starter contract drift fails with a foundation upgrade requirement.

Require validation and build after applying:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope all `
  -ValidateAfterApply `
  -BuildAfterApply
```

Refresh platform metadata, package compatibility versions, and the copied Starter contract only with the explicit foundation scope:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope foundation `
  -ValidateAfterApply `
  -BuildAfterApply
```

Manual edits to generated/managed files are not overwritten automatically. They are recorded in `docs/storefront-analysis/generated-files.yaml` and summarized by `docs/storefront-analysis/regeneration-report.md`; resolve the file intentionally, then rerun `-Scope conflicts`.

After constrained agent visual edits in a handoff-generated project, record the files:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --workspace-root <generated-workspace-root> --written-files <comma-separated-generated-visual-paths>
```

If visual proof fails in generated-owned CSS/markup, run bounded repair:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\repair-visual-generation.mjs --workspace-root <generated-workspace-root> --failure-report <report.md> --max-attempts 2
```

Generated `metadata.yaml` and `generated-files.yaml` share the StorefrontBuilder `generatorVersion` from `tools/BlazorShop.AI.StorefrontBuilder/version.json`. Validation fails if those artifact versions drift.

ReverseEngineering Phase 3A can create reference evidence and `analysis/visual-blueprint.draft.json`. Phase 3B can add reviewed visual analysis and Visual Blueprint v1. Phase 3C can assemble a strict final handoff package under `analysis/agent-handoff/`. Phase 3D is the final correctness and no-skip closure proof for that package. Phase 3E makes the package portable with handoff-local artifacts, canonical artifact/schema membership, hashes, reference containment, manifest/readiness agreement, portable validation commands, dry-run loading, isolated copy proof, source-aware slot provenance, and a final clean-HEAD gate. Phase 4 StorefrontBuilder commands consume only the portable package and schemas through preflight, deterministic planning, generated project skeletons, constrained visual writes, visual QA, repair, and safe regeneration.

The approved Phase 4 input root is only `analysis/agent-handoff/*` plus the registered schemas. A Phase 4 consumer must not read draft artifacts such as `analysis/pages/*`, raw `captures/*`, `analysis/visual-blueprint.draft.json`, `analysis/visual-blueprint.v1.draft.json`, or any unresolved reviewed-source file as generation input.

Run a production Phase 3A evidence smoke against Kindred Coast:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -CommandTimeoutSeconds 900
```

Read `artifacts/storefront-reverse-engineering/projects/kindredcoast/reports/readiness-report.json` as the Phase 3A source of truth. Offscreen skip links, visually-hidden/sr-only helpers, offscreen ARIA live helpers, and horizontally off-canvas carousel items should not become rendered visual evidence. Visible visual elements with bad dimensions or coordinates should still produce `invalid-element-box`; do not weaken readiness validation to make a production run pass.

The production script may finish as `completed-with-blockers` after Phase 3A passes because Phase 3B still requires review decisions and a reviewed blueprint before final handoff. Resolve those review/handoff blockers separately; they are not capture readiness failures when `readiness-report.json` has `passed: true` and no findings.

To run the Phase 3A fixture evidence workflow:

```powershell
$fixture = (Resolve-Path 'tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\static-storefront.html').Path
$fixtureUrl = [Uri]::new($fixture).AbsoluteUri
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url $fixtureUrl --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --no-ai --force
```

Inspect final handoff readiness:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/fixturedemo
```

Validate or inspect a copied portable handoff package without the original project root:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root obj/storefront-reverse-engineering/projects/fixturedemo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect-handoff --handoff-root obj/storefront-reverse-engineering/projects/fixturedemo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
```

Apply review decisions and rerun final handoff validation:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step apply-review-decisions
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step validate-agent-handoff-readiness
```

Run the Phase 3C local gate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1
```

Run the Phase 3D final closure gate only from a clean working tree:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1
```

The Phase 3D gate has no skip flags. It records clean-tree proof, tested SHA, final `HEAD`, focused fixture results, boundary scans, and the final handoff readiness path `analysis/agent-handoff/handoff-readiness.json`.

Run the Phase 3E final closure gate only after the final candidate commit and a clean working tree:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1
```

Phase 3E remains in progress until the final Phase 3E runtime gate passes on this same clean HEAD. The ignored gate report is authoritative final proof; tracked docs must not require a post-gate source commit.

The Phase 3E gate is non-recursive: it does not call the Phase 3D gate as a child process. It restores/builds once, runs later tests with `--no-build --no-restore`, relies on the grouped closure proof bucket for CLI/browser/portable coverage, uses shared positive and portable baselines, runs one StorefrontBuilder plan-only smoke, runs one canonical boundary scan, records timeout/process/slow-step telemetry, and cleans transient success artifacts. Portable validation checks copied-package canonical artifacts, schema requirements, package hashes, typed reference categories, and `manifest.json` readiness agreement with `handoff-readiness.json`. GitHub Actions evidence remains intentionally outside this development closure path while Actions are disabled.

Phase 4 may read only `analysis/agent-handoff/*` and schemas as input. Use `build-storefront.ps1 -Mode preflight-only -HandoffRoot <path>` for StorefrontBuilder preflight. It must fail unless `analysis/agent-handoff/handoff-readiness.json` passed and agrees with `manifest.json` readiness, must not reinterpret raw captures unless running a new ReverseEngineering pass, must not write into Starter, and must not change protected Storefront runtime behavior. Reviewed mappings are authoritative for slot proof only when their source page and section belong to the reviewed page composition; orphan reviewed mappings are blocked by `reviewed-slot-mapping-orphan`.

## Validate

Generated storefront workspaces build from the solution at the workspace root:

```powershell
dotnet restore artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo.sln --no-cache --force-evaluate
dotnet build artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo.sln --no-restore
dotnet run --project artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo.csproj
```

Run the static gate:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 `
  -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Name BlazorShop.Storefront.Demo `
  -StoreKey sample
```

Run tests:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Run isolation:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo -Name BlazorShop.Storefront.Demo
```

Run the CI-friendly regeneration ownership gate:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

Run the canonical generated proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Run the PR-safe browser proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

Run the self-contained full fixture proof before release closure:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

## Compatibility Rules

- Generated storefronts use Runtime-backed Presentation contexts and BFF contracts instead of direct generated-client references in visual source.
- Generated storefronts use `BlazorShop.Storefront.Presentation` for shared App/Routes/page services/BFF/SEO/media composition and provide project-local views/assets/copy.
- Generated storefronts register project-local views as Presentation view slots; generated files must not declare `@page` routes or add route assemblies.
- Generated storefronts use Storefront Presentation for server-side storefront application registration. Presentation composes Runtime internally for generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Generated storefronts may use `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives for reusable browser-safe UI components; generated project-local components are allowed for store-specific presentation.
- `BlazorShop.Storefront.Components.Features` is retired; generated storefronts should consume `Contracts`, `Headless`, and `Browser` primitives and own their visual templates locally.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, generated pages, and analysis artifacts.
- StorefrontBuilder may replace product card/grid/gallery/purchase/cart/checkout/account visual templates without changing shared behavior contracts.
- Protected browser actions go through same-origin BFF endpoints.
- Do not generate route/BFF/SEO/media application logic from scratch when Presentation already owns it.
- Do not reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Do not use Storefront V2 visual markup as the generated/custom storefront presentation source.
- Do not copy Components `Features` wrappers as generated visual templates or stable presentation contracts.
- Do not guess API response shapes; use generated package contracts through Runtime, Presentation BFF contracts, or explicitly documented host-local extensions.

## Browser QA

Start the generated storefront:

```powershell
dotnet run --no-build --project artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo.csproj --urls http://127.0.0.1:18991
```

Run visual and commerce checks from another PowerShell session:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --workspace-root artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --workspace-root artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo
```

For handoff skeleton proof with seeded/mock fixture pages:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --workspace-root <generated-workspace-root> --fixture-root <fixture-root> --screenshot-root obj/storefront-builder/visual-qa-screens --allow-planned-placeholders
```

For runtime visual proof against a generated host:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --workspace-root <generated-workspace-root> --screenshot-root obj/storefront-builder/visual-qa-screens
```

Do not mix `--fixture-root` with runtime visual proof. Runtime proof is the closure path; skeleton proof is early feedback only.

Review the resulting reports under the generated artifact's `docs/storefront-analysis/`. Do not commit the generated artifact by default.

## Before Commit

Check these points before promoting generated storefront output or committing tooling changes:

- `BlazorShop.Storefront.Starter` has no store-specific visual output.
- `BlazorShop.Storefront.Starter` owns neutral visual templates and does not copy Storefront V2 visual components.
- The generated project references `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components` as direct packages, and keeps `BlazorShop.Storefront.Runtime` plus `BlazorShop.Storefront.Client` version metadata because Presentation/Runtime own the application and generated transport dependencies.
- Browser code uses same-origin BFF routes for protected actions.
- Generated visual files contain no `@page` route directives.
- Required analysis artifacts exist.
- Static gate, focused tests, and isolation gate pass.
- Regeneration ownership gate passes when generated ownership, manifest, or regeneration behavior changed.
- Generated proof `Structure` passes before release closure because it recreates the proof, builds it, validates package/reference boundaries, proves safe regeneration, proves no-op determinism, and proves manual-edit conflict reporting.
- Generated proof `FoundationFunctionalFast` passes for PR-safe browser action behavior.
- Phase 4.12 final closure passes from tracked portable handoff fixture input and fresh generated output; it does not depend on pre-existing `obj` artifacts or GitHub Actions.
- Runtime visual proof passes for final closure; skeleton/static fixture proof is early feedback only.
- Generated proof `FoundationFunctionalFast` passes as the minimum closure functional proof.
- Self-contained full fixture proof passes before release-level commerce closure when the fixture runtime is available; it starts V2 fixture runtime, verifies store/category/product/page/payment data, runs `FoundationFunctionalFull`, collects reports, and tears down.
- Storefront client regeneration gate passes before package proof if the canonical Storefront contract or generated client changed.
- Browser QA reports are current when page behavior changed.
- Generated storefront artifacts remain out of `BlazorShop.sln` unless a separate architecture decision promotes them.

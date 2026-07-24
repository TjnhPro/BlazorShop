# Storefront AI Generator Plan

Date: 2026-07-24
Status: Planning only

This plan records the constraints for a future AI-assisted storefront generator. It does not build the generator.

## Prerequisites

The AI generator may start only after these inputs exist and remain green:

- Starter architecture: `docs/architecture/adr/2026-07-24-storefront-starter-foundation.md`.
- Starter source: `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter`.
- Deterministic proof artifact: `artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof`.
- Generation workflow: `scripts/generate-storefront-sample.ps1`.
- Generated proof workflow: `scripts/qa/run-storefront-builder-generated-proof.ps1`.
- Package gate: `scripts/qa/run-storefront-starter-isolation-gate.ps1`.
- Generated storefront release gate: `scripts/qa/run-storefront-sample-release-gate.ps1`.
- Feature map: `Features/feature-manifest.json` in Starter/generated proof artifacts.
- Capability map: `StarterFeatureManifest` and `StarterFeatureActivationService`.
- Route map: `Pages/README.md` and the `Pages/Ssr`, `Pages/Hybrid`, `Pages/WasmHost` folders.
- Protected file rules: this file, the Starter ADR, and `docs/architecture/05-project-and-folder-guide.md`.
- Package/version manifest: `StorefrontPackageVersions.props` and `docs/storefront-platform/storefront-package-compatibility.md`.
- QA checklist: `docs/refactor-control-Commerce-storefront/Storefront Starter Foundation.todo.md`.

## Allowed AI Edit Areas

AI may propose and edit only storefront presentation and composition surfaces:

- design system tokens and CSS under generated storefront `wwwroot`.
- layout and composition components.
- presentation-only Blazor components.
- store-specific assets.
- store-specific public pages.
- feature placement in the generated storefront manifest.
- copy/text content that does not alter commerce contracts or security behavior.

## Protected Areas

AI must not edit these areas unless a human explicitly changes the task scope and the release gate is updated first:

- generated client source and generated API DTOs.
- Runtime security primitives.
- auth/session cookie behavior.
- antiforgery handling.
- return URL validation.
- same-origin BFF transport.
- cart commands.
- checkout commands.
- error contract and error-code branching.
- package/version manifests.
- backend/core/API project references.

## Required Workflow

1. Generate or refresh the proof artifact deterministically from Starter:

   ```powershell
   .\scripts\qa\run-storefront-builder-generated-proof.ps1
   ```

2. Run package and generated storefront release gates before AI changes:

   ```powershell
   .\scripts\qa\run-storefront-starter-isolation-gate.ps1
   .\scripts\qa\run-storefront-sample-release-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
   ```

3. Apply AI changes only in allowed edit areas.

4. Diff-review AI output. Any change under a protected area fails review unless explicitly approved in the task.

5. Re-run the same gates after AI changes. The generated client, Runtime security behavior, BFF transport, package boundaries, and generated storefront route smoke must remain green.

## Failure Conditions

The AI generator output is rejected if it:

- copies Storefront V2 source, CSS, assets, or route composition.
- adds `ProjectReference` to backend/core/API/V2 projects.
- defines handwritten duplicates of generated Storefront API contracts.
- exposes Commerce Node base URL, access tokens, refresh tokens, raw cart tokens, store secrets, or provider credentials to browser output.
- moves pricing, sellability, cart validation, checkout state, order placement, payment rules, or authorization into the generated storefront.
- edits protected package/version manifests without a human-approved release plan.
- removes the feature/capability gate for conditional UI.
- bypasses same-origin `/api/*` for browser commands.

## Done Criteria For Future Generator Work

Future generator implementation is ready to start when a task names the target store/design inputs and confirms the protected areas above. Generator output is ready to review only when deterministic generation, AI presentation edits, package gates, release gates, and diff review all pass.

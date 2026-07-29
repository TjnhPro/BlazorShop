# Tutorial: Validate Generated Proof

This walkthrough validates the StorefrontBuilder proof as an on-demand generated artifact. It does not require a committed generated storefront project.

## Generate And Validate

Run the canonical structure proof workflow:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

The command:

- cleans `artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof`;
- packs `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Presentation`, and `BlazorShop.Storefront.Components`;
- generates `BlazorShop.Storefront.GeneratedProof` from `BlazorShop.Storefront.Starter`;
- writes StorefrontBuilder review, asset, CSS, composition, and generated-file artifacts;
- restores and builds the generated proof;
- runs the static StorefrontBuilder validation gate;
- runs the package/reference isolation gate;
- runs the shared visual consumer boundary validator;
- runs a post-regeneration validate/build proof;
- runs a deterministic no-op regeneration proof;
- runs a manual-edit conflict fixture proof and restores the generated proof.

Expected final line:

```text
StorefrontBuilder generated proof completed at artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof.
```

## Fast Foundation Functional Proof

Run the PR-safe generated browser proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

This proof uses mocked same-origin Presentation BFF routes in Playwright, checks generated product purchase descriptors, selection preview, add-to-cart, cart badge, cart page, checkout route, consent save/revoke, and rejects direct Commerce Node browser calls.

## Regeneration Ownership Gate

Run the CI-friendly ownership gate when regeneration behavior or generated-file ownership changes:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

This gate uses ignored output under `obj/storefront-builder/generated`, proves no-op determinism, scoped CSS/page/component updates, manual generated-file conflicts, user-owned preservation, protected-file rejection, and obsolete-file reporting without live Commerce Node data.

## Full Foundation Functional Proof

Run the fixture-backed generated browser proof before release closure:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull
```

This proof checks fixture store/category/product/page/payment data, starts the generated host, runs visual smoke QA, and runs commerce regression checks for same-origin add-to-cart, cart badge, cart, checkout entry, account route, SEO, consent, missing slug, and direct Commerce Node browser-call rejection.

## Manual Browser QA

For browser QA, start the generated proof:

```powershell
dotnet run --no-build --project artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.csproj --urls http://127.0.0.1:18991
```

Then run:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof --category-slug apparel --product-slug qa-simple-product-100
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof --category-slug apparel --product-slug qa-simple-product-100 --page-slug customer-service
```

The generated reports are artifact-local and ignored by git by default.

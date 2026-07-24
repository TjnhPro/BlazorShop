# Tutorial: Validate Generated Proof

This walkthrough validates the StorefrontBuilder proof as an on-demand generated artifact. It does not require a committed generated storefront project.

## Generate And Validate

Run the canonical proof workflow:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1
```

The command:

- cleans `artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof`;
- packs `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime`;
- generates `BlazorShop.Storefront.GeneratedProof` from `BlazorShop.Storefront.Starter`;
- writes StorefrontBuilder review, asset, CSS, composition, and generated-file artifacts;
- restores and builds the generated proof;
- runs the static StorefrontBuilder validation gate;
- runs the package/reference isolation gate.

Expected final line:

```text
StorefrontBuilder generated proof completed at artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof.
```

## Browser QA

For browser QA, start the generated proof:

```powershell
dotnet run --no-build --project artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.csproj --urls http://127.0.0.1:18991
```

Then run:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
```

The generated reports are artifact-local and ignored by git by default.

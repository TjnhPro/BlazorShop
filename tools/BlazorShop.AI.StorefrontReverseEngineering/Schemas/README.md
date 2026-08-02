# Storefront Reverse Engineering Schemas

Phase 3A keeps schema registrations in `Validation/VisualSchemaRegistry.cs` and validates all first-class JSON artifacts before filesystem writes and after reads. The registry currently enforces shared provenance metadata for every artifact kind.

Phase 3E makes `analysis/agent-handoff/manifest.json` the portable package index. The manifest schema no longer requires a root-level `sourceProjectPath`; original source roots live under diagnostics-only metadata. Portable package validation depends on file-level `artifactEntries`, `schemaRequirements`, `consumerReferencePolicy`, and `packageHash`. The package hash is computed from sorted consumer artifact entries and required schema hashes, excluding manifest self-hash fields, timestamps, directory counts, absolute source paths, and diagnostic-only provenance values.

Copy the matching schema files beside any copied handoff package and validate with:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root <path> --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect-handoff --handoff-root <path> --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
```

The portable validator rejects missing schema roots, missing required schema files, schema hash drift, artifact hash drift, consumer references outside `analysis/agent-handoff/*`, diagnostic paths used as consumer dependencies, failed handoff readiness, and non-canonical order in package-hash-bearing manifest arrays. Manifest `artifactList` keeps the contract order from `AgentHandoffContract.RequiredArtifacts`; `artifactEntries` and `schemaRequirements` are the canonical sorted arrays used for portable package validation.

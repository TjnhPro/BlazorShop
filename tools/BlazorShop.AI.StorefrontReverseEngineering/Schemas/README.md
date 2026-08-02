# Storefront Reverse Engineering Schemas

Phase 3A keeps schema registrations in `Validation/VisualSchemaRegistry.cs` and validates all first-class JSON artifacts before filesystem writes and after reads. The registry currently enforces shared provenance metadata for every artifact kind.

Phase 3E makes `analysis/agent-handoff/manifest.json` the portable package index. The manifest schema no longer requires a root-level `sourceProjectPath`; original source roots live under diagnostics-only metadata. Portable package validation depends on file-level `artifactEntries`, `schemaRequirements`, `consumerReferencePolicy`, and `packageHash`. The package hash is computed from sorted consumer artifact entries and required schema hashes, excluding manifest self-hash fields, timestamps, directory counts, absolute source paths, and diagnostic-only provenance values.

# Storefront Reverse Engineering Schemas

Phase 3A keeps schema registrations in `Validation/VisualSchemaRegistry.cs` and validates all first-class JSON artifacts before filesystem writes and after reads. The registry currently enforces shared provenance metadata for every artifact kind; later phases can add richer per-kind JSON Schema files without changing artifact-store callers.

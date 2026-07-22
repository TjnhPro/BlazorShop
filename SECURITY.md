# Security Policy

## Supported Surface

BlazorShop is still evolving toward the active V2 architecture. Until the first versioned V2 release, security fixes target the current `master` branch.

| Surface | Status |
| --- | --- |
| Active V2 projects under `BlazorShop.PresentationV2` | Supported for current development. |
| Shared core projects: `Domain`, `Application`, `Infrastructure`, `ServiceDefaults` | Supported when used by active V2 runtime paths. |
| Legacy projects under `BlazorShop.Presentation` | Migration/reference only unless a vulnerability is reachable from an active V2 path or the maintainer explicitly supports that legacy deployment. |
| `Smartstore/` reference source | Not part of BlazorShop runtime support. |

## Reporting A Vulnerability

Do not open a public GitHub issue for a vulnerability that includes exploit details, secrets, personal data, or unpublished attack paths.

Preferred reporting path:

1. Use GitHub private vulnerability reporting for `TjnhPro/BlazorShop` if it is enabled.
2. If private vulnerability reporting is not available, contact a repository maintainer through a private GitHub or maintainer-provided channel.

Include:

- A short description of the vulnerability.
- Affected runtime and route/page/API.
- Exact reproduction steps.
- Expected impact.
- Relevant logs, request/response samples, or screenshots with secrets redacted.
- Suggested mitigation if you have one.

## Handling Expectations

The maintainer will triage the report, confirm whether it affects the supported surface, and coordinate a fix. Public disclosure should wait until a fix or mitigation is available.

This repository does not publish a guaranteed response SLA yet. If you need a contractual SLA, define it outside this open-source policy.

## Security Rules For Contributors

- Do not commit real secrets, credentials, tokens, private keys, production connection strings, or customer data.
- Keep local development credentials in local environment files or user secrets. Treat checked-in examples as placeholders only.
- Do not expose Commerce Node node keys or node secrets to WebAssembly clients.
- Keep Control Plane Web calling Control Plane API only; Commerce Node credentials stay behind Control Plane API.
- Keep Storefront V2 scoped through `api/storefront/stores/{storeKey}/*`; Storefront APIs must not require node credentials.
- Do not add side-effecting `GET` endpoints.
- Update OpenAPI security metadata and contract tests when protected endpoints change.
- Keep generated logs, screenshots, Swagger snapshots, and QA artifacts free of secrets before committing them.

## Local Security Testing

Recommended focused checks before committing auth, payment, checkout, admin, or configuration changes:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release
```

For API contract changes, verify the active Swagger documents still expose response schemas, error schemas, security requirements, and generator-safe operation IDs:

- `http://localhost:5180/swagger/commerce-admin/swagger.json`
- `http://localhost:5180/swagger/storefront/swagger.json`

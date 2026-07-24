# API Contract Standards

This page defines the minimum contract quality bar for every new or changed HTTP API in the active V2 runtime.

The goal is simple: Swagger/OpenAPI must contain enough truth for humans, client generators, and AI agents to build clients without guessing.

## Applies To

These rules apply to active V2 API boundaries:

- `BlazorShop.ControlPlane.API` under `api/control-plane/*`.
- `BlazorShop.CommerceNode.API` under `api/commerce/*`.
- `BlazorShop.CommerceNode.API` under `api/storefront/stores/{storeKey}/*`.

Do not add or extend legacy `api/internal/*`, `api/admin/*`, `api/public/*`, or legacy `api/[controller]` routes for V2 work.

## Required Operation Metadata

Every operation must declare:

- Stable `operationId`.
- Short `Summary`.
- Explicit request model when a request body exists.
- Explicit response model for every success response.
- Standard error response model for every expected error status.
- Security requirement when the endpoint is protected.
- Required request body metadata for POST, PUT, PATCH, and any action that reads a body.

Do not rely on inferred anonymous object schemas, domain entities, or controller return types as the public contract.

## Request And Response DTO Rules

Use public API DTOs for transport contracts. Domain entities and EF entities must not appear in public OpenAPI schemas.

Request DTOs must:

- Include DataAnnotations or validators for required fields, length, range, and format.
- Publish numeric bounds such as `minimum` and `maximum` in OpenAPI.
- Publish email fields as email format.
- Publish password requirements such as minimum length.
- Publish shipping address requirements explicitly when checkout or fulfillment data is collected.
- Use named string values for client-facing sort/filter enums unless there is a strong compatibility reason not to.
- Hide server-owned fields from Storefront/client input. Examples: authenticated `userId`, order `status`, `IsPublished`, audit fields, store ownership, tenant/store identifiers that must come from route/query/server context.

Response DTOs must:

- Be explicit and generator-friendly.
- Use the boundary's standard response envelope where that boundary already uses one.
- Return safe error messages and a standard error response shape.
- Avoid leaking domain-only fields, admin-only fields, secrets, credentials, internal row ids when public ids are expected, and raw exception details.

## Store Scope And Security Rules

Store scope must match the route ownership:

- Control Plane API owns `api/control-plane/*`.
- Commerce Admin/control owns `api/commerce/*`; store-scoped admin endpoints use required query `storeKey`.
- Storefront owns `api/storefront/stores/{storeKey}/*`; store scope comes from the route value.
- In Commerce Node API, the Presentation boundary resolves store scope into `StoreExecutionContext` before Application/Infrastructure services run. Infrastructure must not parse public HTTP route/query/header/host details to infer normal store scope.

Do not use `X-Store-Key` for active V2 Storefront APIs. Do not require node credentials on Storefront endpoints.

Protected endpoints must declare and enforce the matching security scheme:

- Bearer/JWT for customer or platform identity.
- Cookie/refresh-cookie scheme where refresh-cookie behavior is part of the contract.
- Node key and node secret only for Commerce Admin/control endpoints that are called by Control Plane API.

## HTTP Method Rules

Use HTTP methods according to side effects:

- `GET` reads only and must not mutate state or trigger payment/order side effects.
- `POST` creates resources or triggers commands/callback captures.
- `PUT` replaces or updates an existing resource when the operation is idempotent.
- `DELETE` removes or deactivates resources.

Payment captures, checkout submits, logout/revocation, imports, task commands, and other side-effecting actions must not be `GET`.

## OpenAPI Document Rules

Swagger/OpenAPI is part of the product surface. Every active API document must:

- Include only routes owned by that document.
- Expose required route/query/header parameters.
- Include schemas for 100% of declared success and error responses.
- Include security schemes and per-operation security requirements.
- Avoid operations that declare only HTTP 200.
- Mark request bodies required when the endpoint requires a body.
- Validate as an OpenAPI document.
- Be stable enough for generated clients and AI agents.

Commerce Node Storefront document ownership:

- `/swagger/storefront/swagger.json` is the frontend/client Storefront API contract and must exclude provider callback/webhook operations.
- `/swagger/storefront-provider/swagger.json` is the provider integration contract for payment callback/webhook routes when those operations need Swagger coverage.
- Runtime provider callback/webhook routes may stay under `api/storefront/stores/{storeKey}/payments/*`, but they are not frontend SDK operations.

## Contract Tests

When adding or changing an API surface, add or update focused contract tests. Business logic tests are not enough.

Minimum contract test coverage:

- Swagger/OpenAPI can be fetched and parsed.
- OpenAPI validation passes.
- Every operation has stable `operationId` and summary metadata.
- Every declared response has a JSON schema.
- No operation declares only HTTP 200.
- Protected endpoints have security metadata.
- Request bodies that are required are marked required.
- Public schemas do not expose domain entities or unsafe fields.
- Important validation metadata is present, such as `minimum`, `maximum`, email format, password length, and required shipping fields.
- Side-effecting operations do not use `GET`.
- A C# or TypeScript client can be generated, or a smoke equivalent proves generator-safe operation names and schemas.
- Swagger snapshots are stored for breaking-change detection when the API is public or consumed by another V2 runtime.

## Change Checklist

Before committing any API change:

1. Confirm route ownership and store scope.
2. Define request and response DTOs.
3. Add validation metadata.
4. Add or update Swagger metadata.
5. Add or update error responses.
6. Add or update security metadata.
7. Add or update contract tests and snapshots.
8. Run focused build/test verification.
9. Update the relevant QA todo file.

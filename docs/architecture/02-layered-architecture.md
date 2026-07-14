# Layered Architecture

BlazorShop follows a pragmatic Layered Architecture style. The active V2 boundary reuses the shared core layers while keeping legacy Presentation projects out of new runtime paths.

## Domain Layer

Project: `BlazorShop.Domain`

Responsibilities:

- Domain entities.
- Domain contracts used by repositories/services.
- Shared identity entities where both Control Plane and Commerce Node need identity behavior.

Rules:

- Keep persistence-specific decisions out unless the existing project already models them there.
- Do not add UI, API, or infrastructure dependencies.
- Prefer expanding existing domain entities only when the capability is truly core to the ecommerce model.

## Application Layer

Project: `BlazorShop.Application`

Responsibilities:

- DTOs and request/response contracts.
- Application service contracts.
- Business services that coordinate repositories and domain behavior.
- Validation.
- Shared options and lightweight utilities.
- Control Plane interfaces under `ControlPlane/*`.
- Commerce Node interfaces under `CommerceNode/*`.

Rules:

- Search for an existing service contract before adding a new one.
- Keep API transport details out of application services.
- Reuse existing DTO and response patterns where possible.
- When migrating from legacy, preserve behavior first; refactor later only when the user asks.

## Infrastructure Layer

Project: `BlazorShop.Infrastructure`

Responsibilities:

- EF DbContexts and migrations.
- Repository implementations.
- Identity/auth infrastructure adapters.
- Email, payment, logging, transaction, seed, and health implementations.
- Control Plane infrastructure under `Data/ControlPlane`.
- Commerce Node infrastructure under `Data/CommerceNode`.

Rules:

- New Control Plane persistence belongs in `ControlPlaneDbContext`.
- New Commerce Node persistence belongs in `CommerceNodeDbContext`.
- Do not add new V2 persistence to legacy `AppDbContext`.
- Use context-specific repositories/services when a behavior must be scoped to Control Plane or Commerce Node.

## Presentation Layer

Legacy project family: `BlazorShop.Presentation`

Status:

- Legacy only.
- Do not extend for new V2 features unless explicitly requested.
- Safe for comparison during migration and QA.

Active project family: `BlazorShop.PresentationV2`

Responsibilities:

- Host active V2 API/UI boundaries.
- Keep Control Plane, Commerce Node, Storefront V2, and shared UI helpers separate.
- Preserve clear API route ownership.

Rules:

- Control Plane Web is a UI client and must call only Control Plane API.
- Control Plane API is the gateway to Commerce Node API.
- Storefront V2 calls Commerce Node Storefront APIs under `api/storefront/stores/{storeKey}/*`.
- Commerce Node API owns node-local ecommerce runtime and deployment tasks.

## Reuse Policy

Before adding a new abstraction:

1. Search existing services, repositories, DTOs, validators, options, response helpers, and clients.
2. Reuse existing application logic when it can be adapted without mixing legacy Presentation dependencies.
3. If Smartstore is relevant, use it for business understanding, not code copying.
4. Keep the first implementation narrow and testable.
5. Update the relevant QA todo file when behavior changes.

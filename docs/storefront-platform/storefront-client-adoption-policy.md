# Storefront Generated Client Adoption Policy

## Policy

- New `BlazorShop.Storefront.Starter` code uses generated `BlazorShop.Storefront.Client` contracts and typed clients by default.
- Manual `HttpClient` transport is forbidden unless the capability is listed in `storefront-client-exception-registry.md`.
- Manual request/response DTOs are forbidden when generated DTOs exist for the same Commerce Node Storefront API schema.
- Presentation view models are allowed only when they transform generated API data for rendering or composition.
- Browser code calls same-origin `/api/*` endpoints only. SSR/BFF code may call Commerce Node through Runtime/generated client wiring.

## Allowed Exception Candidates

These candidates still require registry approval before use:

- auth flow needing `Set-Cookie` or refresh-cookie behavior;
- streaming or file download;
- media proxy;
- multipart upload;
- provider redirect callback handling;
- generator limitation with explicit tracking issue.

## Review Rule

Every exception entry must include capability, exception, reason, owner, test, and revisit trigger. A missing test or revisit trigger means the exception is not approved.

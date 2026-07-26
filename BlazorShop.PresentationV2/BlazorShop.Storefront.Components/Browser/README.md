# Storefront Browser Primitives

`Browser` contains behavior-only primitives for interactive storefront components that call same-origin BFF endpoints.

- `StorefrontLocalApiClient` accepts only relative local routes and rejects absolute or protocol-relative URLs.
- Browser models describe BFF request and response data only; they must not contain theme, layout, admin, or backend-internal fields.
- Antiforgery abstractions belong here because protected browser mutations stay behind the host storefront BFF.
- Visual ownership stays with the host storefront project, not this folder.

Do not add Commerce Node base URLs, Control Plane URLs, node credentials, access tokens, route ownership policy, or CSS/theme decisions here.

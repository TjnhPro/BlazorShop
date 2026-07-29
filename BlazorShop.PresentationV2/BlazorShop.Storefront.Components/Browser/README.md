# Storefront Browser Contracts

`Browser` contains shared browser-facing contracts for interactive storefront components and same-origin BFF endpoints.

- Browser models describe BFF request and response data only; they must not contain theme, layout, admin, or backend-internal fields.
- API clients, antiforgery readers, mutation orchestration, browser state services, and same-origin route validation belong to `BlazorShop.Storefront.Browser`.
- Visual ownership stays with the host storefront project, not this folder.

Do not add Commerce Node base URLs, Control Plane URLs, node credentials, access tokens, route ownership policy, or CSS/theme decisions here.

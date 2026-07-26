# Storefront OpenAPI Contract

`storefront.openapi.json` is the canonical committed Storefront frontend/client OpenAPI contract.

Commerce Node runtime still produces the live Storefront Swagger document at `/swagger/storefront/swagger.json`. Tests compare that live document to this canonical contract and keep the test snapshots as guardrails.

`BlazorShop.Storefront.Client` generation must consume this contract file, not files under `BlazorShop.Tests.V2`.

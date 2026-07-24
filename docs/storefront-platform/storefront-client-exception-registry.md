# Storefront Client Exception Registry

| Capability | Exception | Reason | Owner | Test | Revisit trigger |
| --- | --- | --- | --- | --- | --- |
| none | none | Starter currently has no manual transport exceptions. | Storefront Platform | `StorefrontStarterFoundationBoundaryTests.StarterClientPolicy_HasExceptionRegistryAndNoSilentManualContracts` | First Starter manual `HttpClient` or duplicate DTO proposal. |

Allowed exception candidates are documented in `storefront-client-adoption-policy.md`, but they are not active exceptions until added to this registry with a test and revisit trigger.

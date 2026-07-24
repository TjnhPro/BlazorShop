# Storefront Package Compatibility

| Storefront API | Client package | Compatibility |
| --- | --- | --- |
| v1 | 1.x | compatible |
| v2 | 2.x | breaking API changes |

| Runtime package | Compatible client package | Notes |
| --- | --- | --- |
| 1.x | 1.x | Runtime registration/error/capability primitives expect the v1 generated client package surface. |

Starter and generated storefront projects pin Storefront package versions explicitly. Breaking Storefront API changes must update this matrix, the package version pin, and the generated-client compatibility tests in the same phase.

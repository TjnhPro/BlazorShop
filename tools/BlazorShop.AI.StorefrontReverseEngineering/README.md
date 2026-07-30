# BlazorShop.AI.StorefrontReverseEngineering

Development-time executable for Phase 3A visual evidence capture and neutral blueprint drafting.

This tool is independent from StorefrontBuilder generation. It writes reverse-engineering project state under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` for manual work or `obj/storefront-reverse-engineering/projects/{ProjectId}` for automated tests.

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
```


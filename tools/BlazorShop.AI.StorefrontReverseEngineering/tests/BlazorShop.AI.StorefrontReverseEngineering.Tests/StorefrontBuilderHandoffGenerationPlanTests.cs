using System.Diagnostics;
using System.Text.Json.Nodes;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffGenerationPlan")]
public sealed class StorefrontBuilderHandoffGenerationPlanTests
{
    [Fact]
    public async Task HandoffPlan_IsByteStableAcrossRuns()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Stable");
        fixture.DeleteSourceProject();
        var outputRoot = CreateOutputRoot();

        var first = await RunPlanAsync(fixture.PortableRoot, outputRoot, "BlazorShop.Storefront.Phase4PlanStable", "a");
        var second = await RunPlanAsync(fixture.PortableRoot, outputRoot, "BlazorShop.Storefront.Phase4PlanStable", "b");

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(await File.ReadAllTextAsync(first.JsonPath), await File.ReadAllTextAsync(second.JsonPath));
        Assert.Contains("Generation plan:", first.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffPlan_MapsEcommerceCompositionsToPresentationSlots()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Slot Mapping");
        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanSlots", "slots");
        var plan = ReadPlan(result);

        AssertPlanHasSlot(plan, "category", "catalog.product-card", "Components/Catalog/ProductSummaryCard.razor");
        AssertPlanHasSlot(plan, "product", "product.gallery", "Components/Catalog/ProductGalleryPlaceholder.razor");
        AssertPlanHasSlot(plan, "product", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor");
        AssertPlanHasSlot(plan, "maintenance", "system.error", "Components/States/ErrorState.razor");
        AssertPlanHasFile(plan, "Components/Layout/MainLayout.razor", "generated", "replace");
    }

    [Fact]
    public async Task HandoffPlan_UsesReviewedSharedLayoutMappingWhenFooterSectionIsNotPageLocal()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Shared Footer");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/page-compositions.json", json =>
        {
            var home = json["compositions"]!.AsArray().First(item => item!["pageId"]!.GetValue<string>() == "home")!.AsObject();
            var sections = home["sectionTree"]!.AsArray();
            foreach (var section in sections.OfType<JsonObject>()
                         .Where(section => section["role"]?.GetValue<string>().Contains("footer", StringComparison.OrdinalIgnoreCase) == true)
                         .ToArray())
            {
                sections.Remove(section);
            }
        });
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/presentation-mappings.json", json =>
        {
            var mappings = json["mappings"]!.AsArray();
            foreach (var mapping in mappings.OfType<JsonObject>()
                         .Where(mapping =>
                             mapping["sourcePageId"]?.GetValue<string>() == "home" &&
                             mapping["starterSlotId"]?.GetValue<string>() == "layout.footer")
                         .ToArray())
            {
                mappings.Remove(mapping);
            }

            mappings.Add(new JsonObject
            {
                ["sourceCandidateId"] = "shared-footer-fallback",
                ["presentationComponentId"] = "layout.footer",
                ["starterSlotId"] = "layout.footer",
                ["variant"] = "default",
                ["slotAssignments"] = new JsonArray(),
                ["responsiveProperties"] = new JsonArray(),
                ["tokenBindings"] = new JsonArray(),
                ["interactionBindings"] = new JsonArray(),
                ["dataRequirements"] = new JsonArray(),
                ["behaviorOwnership"] = "presentation",
                ["confidence"] = 0.78,
                ["evidenceIds"] = new JsonArray(),
                ["mappingReason"] = "reviewed-shared-layout-fallback",
                ["alternativeMappings"] = new JsonArray(),
                ["humanReviewRequired"] = false,
                ["sourcePageId"] = "unknown",
                ["sourceSectionId"] = "unknown",
                ["ecommerceRegionId"] = "unknown",
                ["pageArchetype"] = "unknown",
                ["targetGeneratedPath"] = "Components/Layout/MainLayout.razor",
                ["generatedZone"] = "layout-components",
                ["routeOwnership"] = "Storefront Presentation owns route declarations; generated visuals register view slots only",
                ["reasonCodes"] = new JsonArray(),
                ["reviewState"] = "Approved"
            });
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanSharedFooter", "shared-footer");
        var plan = ReadPlan(result);

        AssertPlanHasSlot(plan, "home", "layout.footer", "Components/Layout/MainLayout.razor");
        Assert.Contains(plan["slots"]!.AsArray(), item =>
            item!["slotId"]!.GetValue<string>() == "layout.footer" &&
            item["sourceHandoffArtifacts"]!.AsArray().Any(artifact => artifact!.GetValue<string>() == "analysis/agent-handoff/presentation-mappings.json"));
    }

    [Fact]
    public async Task HandoffPlan_MapsCartCheckoutAccountToVisualShellOnly()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Shells");
        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanShells", "shells");
        var plan = ReadPlan(result);

        AssertVisualShellFile(plan, "Pages/Hybrid/Commerce/CartPage.razor", "cart.page");
        AssertVisualShellFile(plan, "Pages/Hybrid/Commerce/CheckoutPage.razor", "checkout.page");
        AssertVisualShellFile(plan, "Pages/WasmHost/Account/AccountHostPage.razor", "account.shell");
    }

    [Fact]
    public async Task HandoffPlan_ForbiddenTargetPathFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Forbidden Target");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/page-compositions.json", json =>
        {
            var section = json["compositions"]!.AsArray().First(item => item!["pageId"]!.GetValue<string>() == "product")!["sectionTree"]!.AsArray()[0]!.AsObject();
            section["targetFilePath"] = "../outside.razor";
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanForbidden", "forbidden");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-PLAN-006", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffPlan_ProtectedTargetPathFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Protected Target");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/page-compositions.json", json =>
        {
            var section = json["compositions"]!.AsArray().First(item => item!["pageId"]!.GetValue<string>() == "product")!["sectionTree"]!.AsArray()[0]!.AsObject();
            section["targetFilePath"] = "StorefrontPackageVersions.props";
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanProtected", "protected");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-PLAN-007", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffPlan_MissingRequiredSlotFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Missing Slot");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/page-compositions.json", json =>
        {
            var product = json["compositions"]!.AsArray().First(item => item!["pageId"]!.GetValue<string>() == "product")!.AsObject();
            var sections = product["sectionTree"]!.AsArray();
            var purchase = sections.First(item => item!["presentationMappingId"]!.GetValue<string>() == "product-product.purchase");
            sections.Remove(purchase);
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanMissingSlot", "missing-slot");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-PLAN-004", result.Output, StringComparison.Ordinal);
        Assert.Contains("product.purchase", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffPlan_UnsupportedInteractionBecomesManualBlocker()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Interaction Blocker");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/interaction-models.json", json =>
        {
            var product = json["pages"]!.AsArray().First(item => item!["pageId"]!.GetValue<string>() == "product")!.AsObject();
            product["interactions"]!.AsArray().Add(new JsonObject
            {
                ["interactionId"] = "product-direct-fetch",
                ["requiresBusinessLogic"] = true,
                ["description"] = "direct-commerce fetch"
            });
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanInteraction", "interaction");
        var plan = ReadPlan(result);

        AssertPlanHasBlocker(plan, "unsupported-functional-interaction");
    }

    [Fact]
    public async Task HandoffPlan_RestrictedCopiedAssetIsReplacementRequired()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Restricted Asset");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/originality-restrictions.json", json =>
        {
            json["decisions"]!.AsArray().Add(new JsonObject
            {
                ["assetId"] = "brand-logo",
                ["usage"] = "reference-only",
                ["handoffPath"] = "analysis/agent-handoff/section-screenshots/home/section-01.desktop-1440.png"
            });
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanAsset", "asset");
        var plan = ReadPlan(result);
        var asset = plan["assets"]!.AsArray().First(item => item!["assetId"]!.GetValue<string>() == "brand-logo")!;

        Assert.True(asset["replacementRequired"]!.GetValue<bool>());
        Assert.False(asset["copyAllowed"]!.GetValue<bool>());
        AssertPlanHasBlocker(plan, "restricted-copied-asset");
    }

    [Fact]
    public async Task HandoffPlan_RawEvidencePathFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Plan Raw Evidence");
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/evidence-manifest.json", json =>
        {
            var product = json["pages"]!.AsArray().First(item => item!["pageId"]!.GetValue<string>() == "product")!.AsObject();
            var purchase = product["sections"]!.AsArray().First(item => item!["starterSlotId"]!.GetValue<string>() == "product.purchase")!.AsObject();
            purchase["handoffPath"] = "captures/product/raw.png";
        });

        var result = await RunPlanAsync(fixture.PortableRoot, CreateOutputRoot(), "BlazorShop.Storefront.Phase4PlanRawEvidence", "raw");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-PLAN-008", result.Output, StringComparison.Ordinal);
    }

    private static JsonObject ReadPlan(PlanRunResult result)
    {
        Assert.Equal(0, result.ExitCode);
        return JsonNode.Parse(File.ReadAllText(result.JsonPath))!.AsObject();
    }

    private static void AssertPlanHasSlot(JsonObject plan, string pageId, string slotId, string targetPath)
    {
        Assert.Contains(plan["slots"]!.AsArray(), item =>
            item!["pageId"]!.GetValue<string>() == pageId &&
            item["slotId"]!.GetValue<string>() == slotId &&
            item["targetPath"]!.GetValue<string>() == targetPath);
    }

    private static void AssertPlanHasFile(JsonObject plan, string targetPath, string ownership, string action)
    {
        Assert.Contains(plan["files"]!.AsArray(), item =>
            item!["targetPath"]!.GetValue<string>() == targetPath &&
            item["ownership"]!.GetValue<string>() == ownership &&
            item["action"]!.GetValue<string>() == action);
    }

    private static void AssertVisualShellFile(JsonObject plan, string targetPath, string slotId)
    {
        Assert.Contains(plan["files"]!.AsArray(), item =>
            item!["targetPath"]!.GetValue<string>() == targetPath &&
            item["ownership"]!.GetValue<string>() == "managed" &&
            item["action"]!.GetValue<string>() == "patch" &&
            item["visualShellOnly"]!.GetValue<bool>() &&
            item["slots"]!.AsArray().Any(slot => slot!.GetValue<string>() == slotId));
    }

    private static void AssertPlanHasBlocker(JsonObject plan, string code)
    {
        Assert.Contains(plan["blockedItems"]!.AsArray(), item => item!["code"]!.GetValue<string>() == code);
    }

    private static async Task<PlanRunResult> RunPlanAsync(string handoffRoot, string outputRoot, string projectName, string suffix)
    {
        var repoRoot = GetRepoRoot();
        Directory.CreateDirectory(outputRoot);
        var jsonPath = Path.Combine(outputRoot, $"generation-plan-{suffix}.json");
        var yamlPath = Path.Combine(outputRoot, $"generation-plan-{suffix}.yaml");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var argument in new[]
        {
            Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "generate", "plan-generation-files.mjs"),
            "--project-name", projectName,
            "--store-key", "sample",
            "--output-root", outputRoot,
            "--repo-root", repoRoot,
            "--handoff-root", handoffRoot,
            "--output", yamlPath,
            "--json-output", jsonPath,
            "--dry-run"
        })
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await process.WaitForExitAsync(timeout.Token);
        var output = (await stdoutTask) + (await stderrTask);
        return new PlanRunResult(process.ExitCode, output, jsonPath, yamlPath);
    }

    private static string CreateOutputRoot() =>
        Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generation-plan-tests", Guid.NewGuid().ToString("N"));

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record PlanRunResult(int ExitCode, string Output, string JsonPath, string YamlPath);
}

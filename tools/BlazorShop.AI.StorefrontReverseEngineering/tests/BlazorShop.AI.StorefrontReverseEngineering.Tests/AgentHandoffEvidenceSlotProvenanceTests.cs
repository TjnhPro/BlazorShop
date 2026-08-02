using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "PortableProof")]
public sealed class AgentHandoffEvidenceSlotProvenanceTests
{
    [Fact]
    public async Task ProductPurchaseCropUsesReviewedMappingSlot()
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3E Evidence Product Purchase Slot");
        var evidence = await ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json");
        var product = Assert.Single(evidence.Pages, page => page.PageId == "product");
        var purchase = Assert.Single(product.Sections, section => section.SectionId == "section-04" && section.ViewportId == "desktop-1440");

        Assert.Equal("product.purchase", purchase.StarterSlotId);
        Assert.Equal(SectionSlotResolver.ReviewedPresentationMappingSource, purchase.SlotSource);
        Assert.Equal("product-product.purchase", purchase.MappingId);
        Assert.Equal("product.purchase", purchase.SuggestedSlotId);
    }

    [Fact]
    public void SuggestedSlotSerializesSeparatelyFromAuthoritativeSlot()
    {
        var section = new AgentHandoffSectionEvidence(
            "role-only-purchase",
            null,
            SectionSlotResolver.UnresolvedSource,
            null,
            "product.purchase",
            "desktop-1440",
            "analysis/agent-handoff/section-screenshots/product/role-only-purchase.desktop-1440.png",
            "captures/product/desktop-1440/full-page.png",
            "sha",
            "x=0;y=0;width=100;height=100",
            "default",
            ["evidence-only"]);

        var json = JsonNode.Parse(JsonSerializer.Serialize(section, VisualJson.Options))!.AsObject();

        Assert.Null(json["starterSlotId"]);
        Assert.Equal(SectionSlotResolver.UnresolvedSource, json["slotSource"]!.GetValue<string>());
        Assert.Null(json["mappingId"]);
        Assert.Equal("product.purchase", json["suggestedSlotId"]!.GetValue<string>());
    }

    [Fact]
    public async Task ReadinessBlocksEvidenceSlotProvenanceMutations()
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3E Evidence Slot Mutations");

        var missingMappingRoot = CopyProject(projectRoot, "missing-mapping");
        await MutateFirstReviewedEvidenceSectionAsync(missingMappingRoot, section => section["mappingId"] = "missing-mapping-id");
        var missingMapping = await new AgentHandoffReadinessValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).ValidateAsync(missingMappingRoot, CancellationToken.None);
        Assert.Contains(missingMapping.Findings, finding => finding.Code == "evidence-slot-mapping-missing");

        var mismatchRoot = CopyProject(projectRoot, "slot-mismatch");
        await MutateFirstReviewedEvidenceSectionAsync(mismatchRoot, section => section["starterSlotId"] = "layout.footer");
        var mismatch = await new AgentHandoffReadinessValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).ValidateAsync(mismatchRoot, CancellationToken.None);
        Assert.Contains(mismatch.Findings, finding => finding.Code == "evidence-slot-mapping-mismatch");

        var unknownRoot = CopyProject(projectRoot, "unknown-slot");
        await MutateFirstReviewedEvidenceSectionAsync(unknownRoot, section => section["starterSlotId"] = "unknown.slot");
        var unknown = await new AgentHandoffReadinessValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).ValidateAsync(unknownRoot, CancellationToken.None);
        Assert.Contains(unknown.Findings, finding => finding.Code == "unknown-slot");
    }

    private static async Task MutateFirstReviewedEvidenceSectionAsync(string projectRoot, Action<JsonObject> mutate)
    {
        await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/evidence-manifest.json", json =>
        {
            var section = json["pages"]!.AsArray()
                .SelectMany(page => page!["sections"]!.AsArray().OfType<JsonObject>())
                .First(item => item["slotSource"]?.GetValue<string>() == SectionSlotResolver.ReviewedPresentationMappingSource);
            mutate(section);
        });
    }

    private static async Task<T> ReadAsync<T>(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<T>(json, VisualJson.Options)
            ?? throw new InvalidOperationException($"Artifact '{relativePath}' did not deserialize.");
    }

    private static string CopyProject(string sourceRoot, string label)
    {
        var destination = Path.Combine(
            Phase3DNegativeReviewMutationTests.GetRepoRoot(),
            "obj",
            "storefront-reverse-engineering",
            "projects",
            "phase3e-slot-" + label + "-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(Path.GetFullPath(sourceRoot), destination);
        return destination;
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file)), overwrite: true);
        }
    }
}

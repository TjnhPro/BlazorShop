using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "PortableProof")]
public sealed class HandoffReferenceScannerTests
{
    [Fact]
    public void HandoffReferenceScanner_ValidPortableReferenceGraphPasses()
    {
        var root = CreatePackage();

        var result = new HandoffReferenceScanner().Scan(root);

        Assert.Empty(result.Findings);
        Assert.Contains(result.Observations, reference => reference.Category == PortableHandoffReferenceCategories.ConsumerDependency);
        Assert.Contains(result.Observations, reference => reference.Category == PortableHandoffReferenceCategories.DiagnosticProvenance);
        Assert.Contains(result.Observations, reference => reference.Category == PortableHandoffReferenceCategories.GeneratedTargetPath);
    }

    [Theory]
    [InlineData("analysis/resolved/foo.json", "handoff-diagnostic-reference-used-as-consumer")]
    [InlineData("../foo.json", "handoff-consumer-reference-escape")]
    [InlineData("C:/foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("C:\\foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("//server/share/foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("/tmp/foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("analysis/agent-handoff/visual-blueprint.draft.json", "handoff-consumer-reference-draft")]
    public void HandoffReferenceScanner_InvalidConsumerReferencesFailWithExactCode(string reference, string expectedCode)
    {
        var root = CreatePackage(reference);

        var result = new HandoffReferenceScanner().Scan(root);

        Assert.Contains(result.Findings, finding => finding.Code == expectedCode);
    }

    [Fact]
    public void HandoffReferenceScanner_MissingConsumerReferenceFails()
    {
        var root = CreatePackage("analysis/agent-handoff/missing.json");

        var result = new HandoffReferenceScanner().Scan(root);

        Assert.Contains(result.Findings, finding => finding.Code == "handoff-consumer-reference-missing");
    }

    [Fact]
    public void HandoffReferenceScanner_DiagnosticProvenanceOutsideHandoffPasses()
    {
        var root = CreatePackage();
        Rewrite(root, "analysis/agent-handoff/visual-blueprint.json", json =>
        {
            json["diagnosticProvenance"] = new JsonArray(new JsonObject
            {
                ["path"] = "analysis/resolved/page-compositions.reviewed.json",
                ["role"] = "diagnostics-only",
                ["consumerReadable"] = false
            });
        });

        var result = new HandoffReferenceScanner().Scan(root);

        Assert.Empty(result.Findings);
    }

    [Fact]
    public void HandoffReferenceScanner_GeneratedTargetPathsAreNotFileDependencies()
    {
        var root = CreatePackage();
        Rewrite(root, "analysis/agent-handoff/allowed-files.json", json =>
        {
            json["paths"] = new JsonArray("Components/Generated/ProductCard.razor");
        });

        var result = new HandoffReferenceScanner().Scan(root);

        Assert.Empty(result.Findings);
        Assert.Contains(result.Observations, reference => reference.Category == PortableHandoffReferenceCategories.GeneratedTargetPath);
    }

    [Fact]
    public void HandoffReferenceScanner_ExternalUrlsAreAcceptedOnlyInRegisteredUrlFields()
    {
        var root = CreatePackage();
        Rewrite(root, "analysis/agent-handoff/page-compositions.json", json =>
        {
            json["site"] = new JsonObject
            {
                ["sourceUrls"] = new JsonArray("https://reference.example/products")
            };
        });

        var valid = new HandoffReferenceScanner().Scan(root);
        Assert.Empty(valid.Findings);

        Rewrite(root, "analysis/agent-handoff/visual-blueprint.json", json =>
        {
            json["consumerReferences"]!.AsObject()["pageCompositions"] = "https://reference.example/page-compositions.json";
        });

        var invalid = new HandoffReferenceScanner().Scan(root);
        Assert.Contains(invalid.Findings, finding => finding.Code == "handoff-reference-category-mismatch");
    }

    [Fact]
    public void HandoffReferenceScanner_ConsumerReferenceCycleFails()
    {
        var root = CreatePackage();
        Write(root, "analysis/agent-handoff/a.json", new JsonObject { ["ref"] = "analysis/agent-handoff/b.json" });
        Write(root, "analysis/agent-handoff/b.json", new JsonObject { ["ref"] = "analysis/agent-handoff/a.json" });
        var registry = new[]
        {
            new HandoffReferenceRegistryEntry("analysis/agent-handoff/a.json", "/ref", PortableHandoffReferenceCategories.ConsumerDependency, true, AgentHandoffContract.HandoffRoot, "acyclic"),
            new HandoffReferenceRegistryEntry("analysis/agent-handoff/b.json", "/ref", PortableHandoffReferenceCategories.ConsumerDependency, true, AgentHandoffContract.HandoffRoot, "acyclic")
        };

        var result = new HandoffReferenceScanner(registry).Scan(root);

        Assert.Contains(result.Findings, finding => finding.Code == "handoff-artifact-reference-cycle");
    }

    [Fact]
    public void HandoffReferenceScanner_UnregisteredPathLikeReferenceFails()
    {
        var root = CreatePackage();
        Rewrite(root, "analysis/agent-handoff/visual-blueprint.json", json =>
        {
            json["unexpectedPath"] = "analysis/agent-handoff/page-compositions.json";
        });

        var result = new HandoffReferenceScanner().Scan(root);

        Assert.Contains(result.Findings, finding => finding.Code == "handoff-consumer-reference-unregistered");
    }

    private static string CreatePackage(string pageCompositionReference = "analysis/agent-handoff/page-compositions.json")
    {
        var root = Path.Combine(Path.GetTempPath(), "sre-reference-scan-" + Guid.NewGuid().ToString("N"));
        var handoff = Path.Combine(root, "analysis", "agent-handoff");
        Directory.CreateDirectory(handoff);
        Write(root, "analysis/agent-handoff/page-compositions.json", new JsonObject
        {
            ["diagnosticProvenance"] = new JsonArray(new JsonObject
            {
                ["path"] = "analysis/resolved/page-compositions.reviewed.json",
                ["role"] = "diagnostics-only",
                ["consumerReadable"] = false
            }),
            ["pages"] = new JsonArray(new JsonObject
            {
                ["pageId"] = "home",
                ["targetGeneratedFilePath"] = "Components/Generated/Home.razor"
            })
        });
        Write(root, "analysis/agent-handoff/visual-blueprint.json", new JsonObject
        {
            ["consumerReferences"] = new JsonObject
            {
                ["pageCompositions"] = pageCompositionReference
            },
            ["diagnosticProvenance"] = new JsonArray(new JsonObject
            {
                ["path"] = "analysis/visual-blueprint.v1.reviewed.json",
                ["role"] = "diagnostics-only",
                ["consumerReadable"] = false
            })
        });
        Write(root, "analysis/agent-handoff/allowed-files.json", new JsonObject
        {
            ["paths"] = new JsonArray("Components/Generated/Home.razor")
        });
        return root;
    }

    private static void Write(string root, string relativePath, JsonObject json)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json.ToJsonString(VisualJson.Options) + Environment.NewLine);
    }

    private static void Rewrite(string root, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        mutate(json);
        File.WriteAllText(path, json.ToJsonString(VisualJson.Options) + Environment.NewLine);
    }
}

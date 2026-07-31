using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3DNegativeReviewMutationTests
{
    [Theory]
    [InlineData("stale", "decision-source-hash-mismatch")]
    [InlineData("unknown-status", "SRE-WORKFLOW-REVIEW-DECISIONS-INVALID")]
    [InlineData("modified-without-value", "SRE-WORKFLOW-REVIEW-DECISIONS-INVALID")]
    [InlineData("duplicate", "SRE-WORKFLOW-REVIEW-DECISIONS-INVALID")]
    public async Task ReviewMutations_FailWithExactCode(string mutation, string expectedCode)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Review " + mutation);
        await MutateJsonAsync(projectRoot, "review/review-decisions.json", json =>
        {
            var decisions = json["decisions"]!.AsArray();
            var first = decisions[0]!.AsObject();
            if (mutation == "stale")
            {
                first["sourceArtifactHash"] = "stale";
            }
            else if (mutation == "unknown-status")
            {
                first["status"] = "Done";
            }
            else if (mutation == "modified-without-value")
            {
                first["status"] = "Modified";
                first["modifiedValue"] = null;
            }
            else if (mutation == "duplicate")
            {
                var duplicate = first.DeepClone().AsObject();
                duplicate["decisionId"] = "duplicate-decision";
                decisions.Add(duplicate);
            }
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));

        Assert.Contains(expectedCode, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Deferred")]
    [InlineData("Rejected")]
    public async Task CriticalReviewDisposition_BlocksReviewedBlueprint(string status)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Review " + status);
        await MutateJsonAsync(projectRoot, "review/review-decisions.json", json =>
        {
            json["decisions"]!.AsArray()[0]!.AsObject()["status"] = status;
        });

        var assembled = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Null(assembled.Reviewed);
        Assert.Contains(assembled.Readiness.Findings, finding => finding.Code == "reviewed-blueprint-not-resolved");
    }

    internal static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))?.AsObject()
            ?? throw new InvalidOperationException("Artifact did not parse: " + relativePath);
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }

    internal static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

public sealed class Phase3DNegativeSlotMutationTests
{
    [Theory]
    [InlineData("product.purchase")]
    [InlineData("product.gallery")]
    public async Task RemoveReviewedProductMapping_BlocksRequiredSlot(string slotId)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Slot " + slotId);
        await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json", json =>
        {
            RemoveMappings(json, mapping => mapping["sourcePageId"]?.GetValue<string>() == "product" && mapping["starterSlotId"]?.GetValue<string>() == slotId);
        });
        await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", json =>
        {
            var composition = json["compositions"]!.AsArray().OfType<JsonObject>().First(item => item["pageId"]?.GetValue<string>() == "product");
            composition["targetViewSlot"] = null;
            var node = composition["sectionTree"]!.AsArray().OfType<JsonObject>().First(item => item["componentMappingRef"]?.GetValue<string>()?.Contains(slotId, StringComparison.Ordinal) == true);
            node["componentMappingRef"] = null;
            node["presentationMappingId"] = null;
            node["targetFilePath"] = null;
        });

        var findings = new PageCompositionSlotValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => (finding.Code == "missing-required-slot" || finding.Code == "required-slot-unmapped") && finding.Message.Contains(slotId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CloneGalleryNode_BlocksDuplicateNonRepeatableSlot()
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Slot Duplicate");
        await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", json =>
        {
            var tree = ProductTree(json);
            var gallery = tree.OfType<JsonObject>().First(node => node["componentMappingRef"]?.GetValue<string>()?.Contains("product.gallery", StringComparison.Ordinal) == true);
            var clone = gallery.DeepClone().AsObject();
            clone["nodeId"] = "duplicate-gallery";
            tree.Add(clone);
        });

        var findings = new PageCompositionSlotValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "duplicate-non-repeatable-slot" && finding.Message.Contains("product.gallery", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("runtime", "slot-behavior-ownership-conflict")]
    [InlineData("protected-path", "protected-path-target")]
    [InlineData("missing-target", "slot-target-path-mismatch")]
    [InlineData("invalid-catalog", "invalid-section-slot-mapping")]
    [InlineData("extra-section", "unapproved-extra-section")]
    public async Task SlotMutations_BlockWithExactCode(string mutation, string expectedCode)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Slot " + mutation);
        if (mutation == "runtime" || mutation == "invalid-catalog")
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json", json =>
            {
                var mapping = json["mappings"]!.AsArray().OfType<JsonObject>().First(item => item["starterSlotId"]?.GetValue<string>() == "product.purchase");
                if (mutation == "runtime")
                {
                    mapping["behaviorOwnership"] = "runtime";
                }
                else
                {
                    mapping["presentationComponentId"] = "missing.component";
                }
            });
        }
        else
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", json =>
            {
                var tree = ProductTree(json);
                var node = tree.OfType<JsonObject>().First(item => item["componentMappingRef"]?.GetValue<string>()?.Contains("product.purchase", StringComparison.Ordinal) == true);
                if (mutation == "protected-path")
                {
                    node["targetFilePath"] = "starter-generation.contract.yaml";
                }
                else if (mutation == "missing-target")
                {
                    node["targetFilePath"] = null;
                }
                else
                {
                    var extra = node.DeepClone().AsObject();
                    extra["nodeId"] = "unapproved-extra-section";
                    extra["componentMappingRef"] = null;
                    extra["presentationMappingId"] = null;
                    extra["targetFilePath"] = "Pages/Ssr/Product/Extra.razor";
                    extra["role"] = "unmapped visual extra";
                    tree.Add(extra);
                }
            });
        }

        var findings = new PageCompositionSlotValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == expectedCode);
    }

    private static JsonArray ProductTree(JsonObject json) =>
        json["compositions"]!.AsArray().OfType<JsonObject>().First(item => item["pageId"]?.GetValue<string>() == "product")["sectionTree"]!.AsArray();

    private static void RemoveMappings(JsonObject json, Func<JsonObject, bool> predicate)
    {
        var mappings = json["mappings"]!.AsArray();
        foreach (var mapping in mappings.OfType<JsonObject>().Where(predicate).ToArray())
        {
            mappings.Remove(mapping);
        }
    }
}

public sealed class Phase3DNegativeEvidenceMutationTests
{
    [Theory]
    [InlineData("remove-mobile-bounds", "missing-section-viewport-bounds")]
    [InlineData("invalid-bounds", "invalid-section-viewport-bounds")]
    [InlineData("zero-bounds", "invalid-section-viewport-bounds")]
    public async Task EvidenceBoundsMutations_BlockPackaging(string mutation, string expectedCode)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Evidence " + mutation);
        await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", json =>
        {
            var node = json["compositions"]!.AsArray().OfType<JsonObject>().First()["sectionTree"]!.AsArray().OfType<JsonObject>().First();
            var bounds = node["viewportBoundingBoxes"]!.AsObject();
            if (mutation == "remove-mobile-bounds")
            {
                bounds.Remove("mobile-390");
            }
            else if (mutation == "invalid-bounds")
            {
                bounds["desktop-1440"] = "x=bad;y=0;width=100;height=100";
            }
            else
            {
                bounds["desktop-1440"] = "x=0;y=0;width=0;height=0";
            }
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new AgentHandoffAssembler(Phase3DNegativeReviewMutationTests.GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None));

        Assert.Contains(expectedCode, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("delete-crop", "missing-section-screenshot")]
    [InlineData("corrupt-crop", "evidence-hash-mismatch")]
    [InlineData("path-escape", "handoff-path-escape")]
    public async Task EvidenceManifestMutations_BlockReadiness(string mutation, string expectedCode)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Evidence " + mutation);
        var evidence = await ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json");
        var section = evidence.Pages.SelectMany(page => page.Sections).First();
        if (mutation == "delete-crop")
        {
            File.Delete(Path.Combine(projectRoot, section.HandoffPath.Replace('/', Path.DirectorySeparatorChar)));
        }
        else if (mutation == "corrupt-crop")
        {
            await File.WriteAllBytesAsync(Path.Combine(projectRoot, section.HandoffPath.Replace('/', Path.DirectorySeparatorChar)), [1, 2, 3]);
        }
        else
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/evidence-manifest.json", json =>
            {
                json["pages"]!.AsArray()[0]!["sections"]!.AsArray()[0]!.AsObject()["handoffPath"] = "../outside.png";
            });
        }

        var report = await new AgentHandoffReadinessValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == expectedCode);
    }

    internal static async Task<T> ReadAsync<T>(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<T>(json, VisualJson.Options) ?? throw new InvalidOperationException("Artifact did not deserialize: " + relativePath);
    }
}

public sealed class Phase3DNegativeHandoffMutationTests
{
    [Theory]
    [InlineData("task.md")]
    [InlineData("design-tokens.json")]
    [InlineData("evidence-manifest.json")]
    public async Task MissingRequiredHandoffArtifact_BlocksReadiness(string fileName)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Handoff Missing " + fileName);
        File.Delete(Path.Combine(projectRoot, "analysis", "agent-handoff", fileName));

        var report = await new AgentHandoffReadinessValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "missing-agent-handoff-artifact");
    }

    [Theory]
    [InlineData("path-escape", "handoff-path-escape")]
    [InlineData("missing-manifest-entry", "missing-agent-handoff-artifact")]
    [InlineData("absolute-path", "handoff-path-escape")]
    [InlineData("allowed-protected-overlap", "allowed-protected-overlap")]
    [InlineData("draft-blueprint", "reviewed-blueprint-references-draft")]
    [InlineData("artifact-kind", "schema-validation-failed")]
    [InlineData("project-id", "project-id-mismatch")]
    [InlineData("hash", "handoff-hash-mismatch")]
    public async Task HandoffMutations_BlockReadiness(string mutation, string expectedCode)
    {
        var projectRoot = await Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync("Phase 3D Negative Handoff " + mutation);
        if (mutation is "path-escape" or "absolute-path")
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/manifest.json", json =>
            {
                json["artifactList"]!.AsArray().Add(mutation == "path-escape" ? "../outside.json" : "C:/outside.json");
            });
        }
        else if (mutation == "missing-manifest-entry")
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/manifest.json", json =>
            {
                var entries = json["artifactEntries"]!.AsArray();
                var target = entries.OfType<JsonObject>().First(entry => entry["path"]?.GetValue<string>() == "analysis/agent-handoff/design-tokens.json");
                entries.Remove(target);
                var list = json["artifactList"]!.AsArray();
                var listTarget = list.First(item => item?.GetValue<string>() == "analysis/agent-handoff/design-tokens.json");
                list.Remove(listTarget);
            });
        }
        else if (mutation == "allowed-protected-overlap")
        {
            var allowed = await Phase3DNegativeEvidenceMutationTests.ReadAsync<AgentHandoffFileManifest>(projectRoot, "analysis/agent-handoff/allowed-files.json");
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/protected-files.json", json =>
            {
                json["paths"]!.AsArray().Add(allowed.Paths.First());
            });
        }
        else if (mutation == "draft-blueprint")
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/visual-blueprint.json", json => json["tokens"] = "analysis/tokens/semantic-tokens.draft.json");
        }
        else if (mutation == "artifact-kind")
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/design-tokens.json", json => json["artifactKind"] = "wrong-kind");
        }
        else if (mutation == "project-id")
        {
            await Phase3DNegativeReviewMutationTests.MutateJsonAsync(projectRoot, "analysis/agent-handoff/design-tokens.json", json => json["projectId"] = "wrong-project");
        }
        else
        {
            await File.AppendAllTextAsync(Path.Combine(projectRoot, "analysis", "agent-handoff", "design-tokens.json"), " ");
        }

        var report = await new AgentHandoffReadinessValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == expectedCode);
    }
}

public sealed class Phase3DNegativeBoundaryMutationTests
{
    [Theory]
    [InlineData("@page \"/generated\"", "generated-route-ownership")]
    [InlineData("fetch('/api/storefront/stores/demo/cart')", "unsafe-browser-action")]
    [InlineData("CommerceNode direct client", "unsafe-browser-action")]
    [InlineData("checkout.payment.capture()", "unsafe-browser-action")]
    [InlineData("route reimplementation", "generated-route-ownership")]
    [InlineData("BFF reimplementation", "slot-behavior-ownership-conflict")]
    [InlineData("SEO media reimplementation", "slot-behavior-ownership-conflict")]
    public void BoundaryMarkers_MapToExactBlockers(string marker, string expectedCode)
    {
        Assert.Equal(expectedCode, DetectBoundaryBlocker(marker));
    }

    private static string DetectBoundaryBlocker(string marker)
    {
        if (marker.Contains("@page", StringComparison.OrdinalIgnoreCase) || marker.Contains("route reimplementation", StringComparison.OrdinalIgnoreCase))
        {
            return "generated-route-ownership";
        }

        if (marker.Contains("api/storefront", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("CommerceNode", StringComparison.OrdinalIgnoreCase) ||
            marker.Contains("checkout.payment", StringComparison.OrdinalIgnoreCase))
        {
            return "unsafe-browser-action";
        }

        return "slot-behavior-ownership-conflict";
    }
}

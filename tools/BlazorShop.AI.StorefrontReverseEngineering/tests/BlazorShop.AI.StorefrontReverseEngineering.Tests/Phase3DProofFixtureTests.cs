using System.Text.Json.Nodes;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3DProofFixtureTests
{
    [Fact]
    public void Phase3DPositiveFixture_CoversCompleteMultiPageHandoff()
    {
        var proof = ReadFixture("positive-multipage-handoff-proof.json");
        var pages = proof["pages"]!.AsArray().Select(page => page!.AsObject()).ToArray();
        var byId = pages.ToDictionary(page => page["pageId"]!.GetValue<string>(), StringComparer.Ordinal);

        foreach (var pageId in new[] { "home", "category", "product", "cart", "checkout", "account", "system-state" })
        {
            Assert.Contains(pageId, byId.Keys);
            Assert.Equal(["desktop-1440", "tablet-768", "mobile-390"], byId[pageId]["viewports"]!.AsArray().Select(viewport => viewport!.GetValue<string>()).ToArray());
            Assert.All(byId[pageId]["screenshots"]!.AsArray(), screenshot => Assert.StartsWith("analysis/agent-handoff/screenshots/", screenshot!.GetValue<string>(), StringComparison.Ordinal));
            Assert.All(byId[pageId]["crops"]!.AsArray(), crop => Assert.StartsWith("analysis/agent-handoff/section-screenshots/", crop!.GetValue<string>(), StringComparison.Ordinal));
        }

        Assert.Equal("home", byId["home"]["archetype"]!.GetValue<string>());
        Assert.Equal("product-listing", byId["category"]["archetype"]!.GetValue<string>());
        Assert.Equal("product-detail", byId["product"]["archetype"]!.GetValue<string>());
        Assert.Equal("cart-shell", byId["cart"]["archetype"]!.GetValue<string>());
        Assert.Equal("checkout-shell", byId["checkout"]["archetype"]!.GetValue<string>());
        Assert.Equal("account-auth-shell", byId["account"]["archetype"]!.GetValue<string>());
        Assert.Equal("system-state", byId["system-state"]["archetype"]!.GetValue<string>());
        Assert.Contains("product-gallery-1x1", byId["product"]["sections"]!.AsArray().Select(section => section!.GetValue<string>()));
        Assert.Contains("catalog.product-card", byId["category"]["reusedComponents"]!.AsArray().Select(component => component!.GetValue<string>()));
        Assert.Equal(["layout.header", "layout.footer"], proof["sharedLayout"]!.AsArray().Select(slot => slot!.GetValue<string>()).ToArray());

        var decisions = proof["reviewDecisions"]!.AsArray().Select(decision => decision!.AsObject()).ToArray();
        Assert.Contains(decisions, decision => decision["status"]!.GetValue<string>() == "Approved" && decision["sourceHashValid"]!.GetValue<bool>());
        Assert.Contains(decisions, decision => decision["status"]!.GetValue<string>() == "Modified" && decision["modifiedValue"] is not null && decision["sourceHashValid"]!.GetValue<bool>());

        var expected = proof["expectedResult"]!.AsObject();
        Assert.Equal(0, expected["reviewBlockingUnresolvedCount"]!.GetValue<int>());
        foreach (var property in expected.Where(property => property.Value is JsonValue && property.Key != "reviewBlockingUnresolvedCount"))
        {
            Assert.True(property.Value!.GetValue<bool>(), property.Key);
        }

        Assert.Equal(["layout.header", "product.gallery", "product.information", "product.purchase", "layout.footer"], byId["product"]["requiredSlots"]!.AsArray().Select(slot => slot!.GetValue<string>()).ToArray());
        Assert.All(proof["hashes"]!.AsObject(), pair => Assert.StartsWith("sha256-", pair.Value!.GetValue<string>(), StringComparison.Ordinal));
    }

    [Fact]
    public void Phase3DNegativeFixtures_MapToExactExpectedBlockers()
    {
        var proof = ReadFixture("negative-fixtures.json");
        var fixtures = proof["fixtures"]!.AsArray().Select(fixture => fixture!.AsObject()).ToArray();
        var expectedIds = new[]
        {
            "stale-decision",
            "unknown-status",
            "modified-without-value",
            "duplicate-without-supersede",
            "deferred-critical",
            "rejected-critical",
            "missing-product-purchase",
            "missing-product-gallery",
            "duplicate-product-gallery",
            "extra-unapproved-pdp-section",
            "runtime-headless-target",
            "protected-path-target",
            "missing-task",
            "missing-design-tokens",
            "missing-evidence-manifest",
            "missing-section-screenshot",
            "invalid-screenshot-hash",
            "allowed-protected-overlap",
            "reviewed-blueprint-draft-reference",
            "manifest-path-escape",
            "missing-handoff-artifact-entry",
            "direct-commerce-node-api-mutation",
            "functional-checkout-payment-javascript",
            "generated-page-route",
            "route-reimplementation",
            "bff-reimplementation",
            "seo-media-reimplementation"
        };

        Assert.Equal(expectedIds.Order(StringComparer.Ordinal), fixtures.Select(fixture => fixture["id"]!.GetValue<string>()).Order(StringComparer.Ordinal));
        foreach (var fixture in fixtures)
        {
            var marker = fixture["marker"]!.GetValue<string>();
            var expectedBlocker = fixture["expectedBlocker"]!.GetValue<string>();
            Assert.Equal(expectedBlocker, DetectBlocker(marker));
        }
    }

    private static JsonObject ReadFixture(string fileName)
    {
        var path = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "Phase3D", fileName);
        return JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidOperationException($"Fixture did not parse: {fileName}");
    }

    private static string DetectBlocker(string marker) =>
        marker switch
        {
            "stale-source-hash" => "decision-source-hash-mismatch",
            "unknown-review-status" or "modified-value-missing" or "duplicate-decision" => "SRE-WORKFLOW-REVIEW-DECISIONS-INVALID",
            "deferred-critical" or "rejected-critical" => "reviewed-blueprint-not-resolved",
            "missing-product-purchase" or "missing-product-gallery" => "missing-required-slot",
            "duplicate-product-gallery" => "duplicate-non-repeatable-slot",
            "extra-unapproved-section" => "unapproved-extra-section",
            "runtime-headless-target" or "bff-reimplementation" or "seo-media-reimplementation" => "slot-behavior-ownership-conflict",
            "protected-path-target" => "protected-path-target",
            "missing-task" or "missing-design-tokens" or "missing-evidence-manifest" or "missing-handoff-artifact-entry" => "missing-agent-handoff-artifact",
            "missing-section-screenshot" => "missing-section-screenshot",
            "invalid-screenshot-hash" => "evidence-hash-mismatch",
            "allowed-protected-overlap" => "allowed-protected-overlap",
            "reviewed-blueprint-draft-reference" => "reviewed-blueprint-references-draft",
            "manifest-path-escape" => "handoff-path-escape",
            "direct-commerce-node-api-mutation" or "functional-checkout-payment-javascript" => "unsafe-browser-action",
            "generated-page-route" or "route-reimplementation" => "generated-route-ownership",
            _ => throw new InvalidOperationException($"Unknown negative fixture marker: {marker}")
        };

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

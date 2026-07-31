using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class BlueprintV1ReadinessTests
{
    [Fact]
    public async Task BlueprintV1_AssemblesDraftReviewedAndReadinessArtifacts()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint V1 Artifacts");

        var blueprint = await ReadBlueprintAsync(projectRoot, "analysis/visual-blueprint.v1.draft.json");
        var reviewed = await ReadBlueprintAsync(projectRoot, "analysis/visual-blueprint.v1.reviewed.json");

        Assert.Contains("analysis/evidence-snapshot.json", blueprint.SourceProvenance);
        Assert.Contains("analysis/resolved/page-compositions.reviewed.json", blueprint.SourceProvenance);
        Assert.NotEmpty(blueprint.Pages);
        Assert.Equal("analysis/tokens/semantic-tokens.draft.json", blueprint.Tokens);
        Assert.Equal("analysis/resolved/semantic-tokens.reviewed.json", reviewed.Tokens);
        Assert.Equal("analysis/resolved/presentation-mappings.reviewed.json", reviewed.PresentationMappings);
        Assert.Contains("analysis/resolved/review-resolution-manifest.json", reviewed.SourceProvenance);
        Assert.DoesNotContain(BlueprintReferences(reviewed), reference => reference.Contains(".draft.json", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(reviewed.ProjectMetadata["reviewBundleHash"]));
        Assert.False(string.IsNullOrWhiteSpace(reviewed.ProjectMetadata["storefrontPatternHash"]));
        Assert.False(string.IsNullOrWhiteSpace(reviewed.ProjectMetadata["presentationCatalogHash"]));
        Assert.False(string.IsNullOrWhiteSpace(reviewed.ProjectMetadata["pageContractHash"]));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "resolved", "page-compositions.reviewed.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "visual-blueprint.v1.reviewed.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "reports", "generation-readiness.md")));
    }

    [Fact]
    public async Task GenerationReadiness_MissingSemanticTokenBaselineBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Missing Tokens");
        File.Delete(Path.Combine(projectRoot, "analysis", "tokens", "semantic-tokens.draft.json"));

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Readiness.Passed);
        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "missing-required-artifact" && finding.ArtifactPath == "analysis/tokens/semantic-tokens.draft.json");
        Assert.False(File.Exists(Path.Combine(projectRoot, "analysis", "visual-blueprint.v1.reviewed.json")));
    }

    [Fact]
    public async Task ReviewedBlueprint_IsDeletedWhenCriticalReviewIsDeferred()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Deferred Review");
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "visual-blueprint.v1.reviewed.json")));
        await RewriteFirstReviewDecisionStatusAsync(projectRoot, "Deferred");

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Null(result.Reviewed);
        Assert.False(File.Exists(Path.Combine(projectRoot, "analysis", "visual-blueprint.v1.reviewed.json")));
        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "reviewed-blueprint-not-resolved");
    }

    [Fact]
    public async Task ReviewedBlueprint_IsDeletedWhenCriticalReviewIsRejected()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Rejected Review");
        await RewriteFirstReviewDecisionStatusAsync(projectRoot, "Rejected");

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Null(result.Reviewed);
        Assert.False(File.Exists(Path.Combine(projectRoot, "analysis", "visual-blueprint.v1.reviewed.json")));
        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "reviewed-blueprint-not-resolved");
    }

    [Fact]
    public async Task GenerationReadiness_ReviewedBlueprintDraftReferenceBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Draft Reference");
        await MutateJsonAsync(projectRoot, "analysis/visual-blueprint.v1.reviewed.json", json =>
        {
            json["tokens"] = "analysis/tokens/semantic-tokens.draft.json";
        });

        var readiness = InvokeReviewedBlueprintValidation(projectRoot);

        Assert.Contains(readiness.Findings, finding => finding.Code == "reviewed-blueprint-references-draft");
    }

    [Fact]
    public async Task GenerationReadiness_StaleReviewBundleHashBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Stale Hash");
        await MutateJsonAsync(projectRoot, "analysis/visual-blueprint.v1.reviewed.json", json =>
        {
            json["projectMetadata"]!.AsObject()["reviewBundleHash"] = "stale";
        });

        var readiness = InvokeReviewedBlueprintValidation(projectRoot);

        Assert.Contains(readiness.Findings, finding => finding.Code == "reviewed-blueprint-hash-stale");
    }

    [Fact]
    public async Task PageCompositions_MultiPageFixtureProducesOneSiteBlueprint()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Multi Page");
        await CloneHomePageAsync(projectRoot, "category", "https://example.test/category/women", "category-listing", null);
        await CloneHomePageAsync(projectRoot, "product", "https://example.test/product/linen-jacket", "product-detail", "product detail media gallery");

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var compositions = await ReadPageCompositionsAsync(projectRoot);

        Assert.DoesNotContain(result.Readiness.Findings, finding => finding.Code == "missing-page-evidence");
        Assert.Equal(3, compositions.Pages.Count);
        Assert.Equal(compositions.ProjectId, compositions.Site.SiteId);
        Assert.Contains("home", result.Draft.Pages);
        Assert.Contains("category", result.Draft.Pages);
        Assert.Contains("product", result.Draft.Pages);
        Assert.Contains(compositions.Pages, page => page.PageId == "product" && page.CompositionTree.Any(node => node.Role == "product detail media gallery"));
        Assert.Contains(compositions.Pages, page => page.PageId == "product" && page.TargetViewSlot == "product.gallery" && page.TargetGeneratedFilePath?.Contains("Components/Catalog/", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task PageCompositions_MissingEvidenceForRequiredPageCreatesPageScopedBlocker()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Missing Page Evidence");
        await AddEvidenceOnlyPageAsync(projectRoot, "product", "https://example.test/product/missing", "product-detail", withEvidence: false);

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Readiness.Passed);
        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "missing-page-evidence" && finding.ArtifactPath == "analysis/resolved/page-compositions.reviewed.json");
    }

    [Fact]
    public async Task PageCompositions_UnknownPageArchetypeBlocksReadiness()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Unknown Archetype");
        await CloneHomePageAsync(projectRoot, "lookbook", "https://example.test/lookbook", "experimental-showroom", null);

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Readiness.Passed);
        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "unknown-page-archetype");
    }

    [Fact]
    public async Task PageCompositions_SharedTokensAreDedupedAtSiteLevel()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Shared Tokens");
        await CloneHomePageAsync(projectRoot, "category", "https://example.test/category/women", "category-listing", null);

        await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var compositions = await ReadPageCompositionsAsync(projectRoot);

        Assert.NotEmpty(compositions.Site.SharedVisualLanguage);
        Assert.All(compositions.Site.SharedVisualLanguage, pair =>
        {
            var values = pair.Value.Split(", ", StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(values.Distinct(StringComparer.Ordinal).Count(), values.Length);
        });
    }

    [Fact]
    public void BlueprintAssembler_DoesNotHardcodeHomeCaptureInput()
    {
        var repoRoot = GetRepoRoot();
        var assembler = File.ReadAllText(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Analysis", "Blueprint", "BlueprintV1Assembler.cs"));

        Assert.DoesNotContain("captures/home", assembler, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReviewedComposition_DoesNotReadDraftInputs()
    {
        var repoRoot = GetRepoRoot();
        var assembler = File.ReadAllText(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Analysis", "Blueprint", "BlueprintV1Assembler.cs"));
        var start = assembler.IndexOf("private static ReviewedPageCompositionsDocument BuildReviewedPageCompositions", StringComparison.Ordinal);
        var end = assembler.IndexOf("private static ReviewedPageCompositionsDocument BuildDraftPageCompositions", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var reviewedBuilder = assembler[start..end];

        Assert.DoesNotContain("sections.draft.json", reviewedBuilder, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("semantic-tokens.draft.json", reviewedBuilder, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("presentation-mappings.draft.json", reviewedBuilder, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ecommerce-regions.json", reviewedBuilder, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReviewedComposition_RecordsResolvedInputProvenance()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Reviewed Provenance");
        var compositions = await ReadPageCompositionsAsync(projectRoot);

        Assert.Equal("analysis/resolved/review-resolution-manifest.json", compositions.Provenance.ReviewResolutionManifestPath);
        Assert.False(string.IsNullOrWhiteSpace(compositions.Provenance.ReviewBundleHash));
        Assert.Contains("analysis/resolved/page-sections.reviewed.json", compositions.Provenance.ReviewedInputArtifactPaths);
        Assert.Contains("analysis/resolved/presentation-mappings.reviewed.json", compositions.Provenance.ReviewedInputArtifactPaths);
        Assert.Equal("reviewed-page-sections", compositions.Provenance.ReviewedInputArtifactKinds["analysis/resolved/page-sections.reviewed.json"]);
        Assert.False(string.IsNullOrWhiteSpace(compositions.Provenance.SourceResolvedArtifactHashes["analysis/resolved/page-sections.reviewed.json"]));
    }

    [Fact]
    public async Task ReviewedComposition_ModifiedDecisionsPropagateToHandoff()
    {
        var projectRoot = await CreateProjectWithModifiedReviewDecisionsAsync("Blueprint Modified Propagation");
        var targetPath = "Components/Catalog/ProductSummaryCardModified.razor";

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        Assert.True(result.Readiness.Passed, string.Join(Environment.NewLine, result.Readiness.Findings.Select(finding => finding.Code + ":" + finding.Message)));
        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        var resolvedMappings = ReadNode(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json").ToJsonString();
        var resolvedSections = ReadNode(projectRoot, "analysis/resolved/page-sections.reviewed.json").ToJsonString();
        var resolvedTokens = ReadNode(projectRoot, "analysis/resolved/semantic-tokens.reviewed.json").ToJsonString();
        var reviewedCompositions = ReadNode(projectRoot, "analysis/resolved/page-compositions.reviewed.json").ToJsonString();
        var handoffCompositions = ReadNode(projectRoot, "analysis/agent-handoff/page-compositions.json").ToJsonString();
        var allowedFiles = ReadNode(projectRoot, "analysis/agent-handoff/allowed-files.json").ToJsonString();
        var task = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "agent-handoff", "task.md"));
        var designTokens = ReadNode(projectRoot, "analysis/agent-handoff/design-tokens.json").ToJsonString();
        var visualStyle = ReadNode(projectRoot, "analysis/agent-handoff/visual-style.json").ToJsonString();

        Assert.Contains(targetPath, resolvedMappings, StringComparison.Ordinal);
        Assert.Contains(targetPath, reviewedCompositions, StringComparison.Ordinal);
        Assert.Contains(targetPath, handoffCompositions, StringComparison.Ordinal);
        Assert.Contains(targetPath, allowedFiles, StringComparison.Ordinal);
        Assert.Contains("featured product card", resolvedSections, StringComparison.Ordinal);
        Assert.Contains("featured product card", reviewedCompositions, StringComparison.Ordinal);
        Assert.Contains("featured product card", handoffCompositions, StringComparison.Ordinal);
        Assert.Contains("featured product card", task, StringComparison.Ordinal);
        Assert.Contains("17px", resolvedTokens, StringComparison.Ordinal);
        Assert.Contains("17px", reviewedCompositions, StringComparison.Ordinal);
        Assert.Contains("17px", designTokens, StringComparison.Ordinal);
        Assert.Contains("17px", visualStyle, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PageCompositions_SectionTreeIsStableAcrossDeterministicRuns()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Stable Composition");
        var first = await ReadPageCompositionsAsync(projectRoot);

        await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var second = await ReadPageCompositionsAsync(projectRoot);

        Assert.Equal(
            first.Compositions.SelectMany(composition => composition.SectionTree.Select(section => (composition.PageId, section.NodeId, section.StableFingerprint))),
            second.Compositions.SelectMany(composition => composition.SectionTree.Select(section => (composition.PageId, section.NodeId, section.StableFingerprint))));
    }

    [Fact]
    public async Task PageCompositions_RepeatedProductCardSectionsAreGrouped()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Repeated Product Cards");
        await MutateSectionsAsync(projectRoot, "home", sections =>
        {
            var clone = sections[0]!.DeepClone();
            clone["sectionId"] = "section-product-card-duplicate";
            clone["sectionType"] = "product card";
            sections.Add(clone);
        });

        await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var compositions = await ReadPageCompositionsAsync(projectRoot);

        Assert.Contains(compositions.Compositions, composition =>
            composition.PageId == "home" &&
            composition.RepeatedGroups.Any(group => group.SemanticRole == "product card" && group.SectionIds.Contains("section-product-card-duplicate")));
    }

    [Fact]
    public async Task PageCompositions_MissingSectionEvidenceBlocksReadiness()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Missing Section Evidence");
        await MutateSectionsAsync(projectRoot, "home", sections => sections[0]!["evidenceIds"] = new JsonArray());

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "missing-section-evidence");
    }

    [Fact]
    public async Task PageCompositions_OptionalMissingSectionDoesNotBlockReadiness()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Optional Missing Section");
        await MutateSectionsAsync(projectRoot, "home", sections =>
        {
            var footer = sections.OfType<JsonObject>().FirstOrDefault(section => section["sectionType"]?.GetValue<string>() == "footer");
            if (footer is not null)
            {
                sections.Remove(footer);
            }
        });

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.DoesNotContain(result.Readiness.Findings, finding => finding.Code == "missing-section-evidence");
    }

    [Fact]
    public async Task PageCompositions_SectionCannotTargetProtectedPath()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Protected Section Target");
        var evidenceId = "ev-protected-target";
        await MutateSectionsAsync(projectRoot, "home", sections => sections[0]!["evidenceIds"] = new JsonArray(evidenceId));
        await AddDraftMappingAsync(projectRoot, evidenceId, "starter-generation.contract.yaml", "unknown");

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "protected-path-target");
    }

    [Fact]
    public async Task PageCompositions_PdpMissingPurchaseSlotBlocksReadiness()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Missing PDP Purchase");
        await CloneHomePageAsync(projectRoot, "product", "https://example.test/product/missing-purchase", "product-detail", "product detail media gallery");

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "missing-required-slot" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PageCompositions_PdpPurchaseWithoutEvidenceBlocksAsMissingSectionEvidence()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint PDP Purchase Evidence");
        await CloneHomePageAsync(projectRoot, "product", "https://example.test/product/no-purchase-evidence", "product-detail", "product detail media gallery");
        await MutateSectionsAsync(projectRoot, "product", sections =>
        {
            var purchase = sections[0]!.DeepClone();
            purchase["sectionId"] = "product-purchase-no-evidence";
            purchase["sectionType"] = "product purchase";
            purchase["evidenceIds"] = new JsonArray();
            sections.Add(purchase);
        });

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "missing-section-evidence" && finding.Message.Contains("product-purchase-no-evidence", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PageCompositions_ProductListingAllowsRepeatedProductCards()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Listing Repeatable Slots");
        await CloneHomePageAsync(projectRoot, "category", "https://example.test/category/repeatable", "product-listing", null);
        await MutateSectionsAsync(projectRoot, "category", sections =>
        {
            var first = sections[0]!.DeepClone();
            first["sectionId"] = "category-card-1";
            first["sectionType"] = "product card";
            var second = first.DeepClone();
            second["sectionId"] = "category-card-2";
            sections.Add(first);
            sections.Add(second);
        });

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.DoesNotContain(result.Readiness.Findings, finding => finding.Code == "duplicate-non-repeatable-slot" && finding.Message.Contains("catalog.product-card", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PageCompositions_CartShellCannotOwnCheckoutBehavior()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Cart Behavior Ownership");
        await CloneHomePageAsync(projectRoot, "cart", "https://example.test/cart", "cart-shell", "cart line items");
        await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        await MutateFirstCompositionNodeAsync(projectRoot, "cart", node => node["allowedOperations"] = new JsonArray("checkout.place-order"));

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "slot-behavior-ownership-conflict");
    }

    [Fact]
    public async Task PageCompositions_CheckoutShellCannotOwnPaymentProviderBehavior()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Checkout Behavior Ownership");
        await CloneHomePageAsync(projectRoot, "checkout", "https://example.test/checkout", "checkout-shell", "checkout form");
        await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        await MutateFirstCompositionNodeAsync(projectRoot, "checkout", node => node["allowedOperations"] = new JsonArray("payment.capture"));

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "slot-behavior-ownership-conflict");
    }

    [Fact]
    public async Task PageCompositions_AccountShellCannotOwnAuthenticationBehavior()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Account Behavior Ownership");
        await CloneHomePageAsync(projectRoot, "account", "https://example.test/account", "account-auth-shell", "account shell");
        await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        await MutateFirstCompositionNodeAsync(projectRoot, "account", node => node["allowedOperations"] = new JsonArray("auth.token"));

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "slot-behavior-ownership-conflict");
    }

    [Fact]
    public async Task SlotValidation_RoleSuggestionWithoutMappingDoesNotSatisfyRequiredSlot()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Role Suggestion");
        await AddPageContractSlotAsync(projectRoot, "home", "requiredSlotIds", "product.purchase");
        await AddPageContractSlotAsync(projectRoot, "home", "requiredSlotIds", "product.gallery");
        await AddCompositionNodeAsync(projectRoot, "home", "role-only-purchase", "purchase panel", null, null, "pages");
        await AddCompositionNodeAsync(projectRoot, "home", "role-only-gallery", "gallery", null, null, "pages");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "required-slot-unmapped" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Code == "required-slot-unmapped" && finding.Message.Contains("product.gallery", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Code == "section-slot-suggestion-unreviewed" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Code == "section-slot-suggestion-unreviewed" && finding.Message.Contains("product.gallery", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SlotValidation_ReviewedMappingSatisfiesRequiredSlot()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Reviewed Mapping");
        await AddPageContractSlotAsync(projectRoot, "home", "requiredSlotIds", "product.purchase");
        await AddReviewedMappingAsync(projectRoot, "reviewed-product-purchase", "home", "reviewed-product-purchase-node", "product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor");
        await AddCompositionNodeAsync(projectRoot, "home", "reviewed-product-purchase-node", "purchase panel", "reviewed-product-purchase", "Components/Catalog/PurchasePanelPlaceholder.razor", "catalog-components");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.DoesNotContain(findings, finding => (finding.Code == "missing-required-slot" || finding.Code == "required-slot-unmapped") && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "invalid-section-slot-mapping" && finding.Message.Contains("reviewed-product-purchase", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "unapproved-extra-section" && finding.Message.Contains("reviewed-product-purchase-node", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SlotValidation_DuplicateNonRepeatableSlotFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Duplicate Gallery");
        await AddPageContractSlotAsync(projectRoot, "home", "allowedAdditionalSlotIds", "product.gallery");
        await AddReviewedMappingAsync(projectRoot, "gallery-one", "home", "gallery-one-node", "product.gallery", "product.gallery", "Components/Catalog/ProductGalleryPlaceholder.razor");
        await AddReviewedMappingAsync(projectRoot, "gallery-two", "home", "gallery-two-node", "product.gallery", "product.gallery", "Components/Catalog/ProductGalleryPlaceholder.razor");
        await AddCompositionNodeAsync(projectRoot, "home", "gallery-one-node", "gallery", "gallery-one", "Components/Catalog/ProductGalleryPlaceholder.razor", "catalog-components");
        await AddCompositionNodeAsync(projectRoot, "home", "gallery-two-node", "gallery", "gallery-two", "Components/Catalog/ProductGalleryPlaceholder.razor", "catalog-components");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "duplicate-non-repeatable-slot" && finding.Message.Contains("product.gallery", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SlotValidation_RepeatableProductCardsPass()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Repeatable Product Cards");
        await AddPageContractSlotAsync(projectRoot, "home", "allowedAdditionalSlotIds", "catalog.product-card");
        await AddPageContractSlotAsync(projectRoot, "home", "repeatableSlotIds", "catalog.product-card");
        await AddReviewedMappingAsync(projectRoot, "card-one", "home", "card-one-node", "catalog.product-card", "catalog.product-card", "Components/Catalog/ProductSummaryCard.razor");
        await AddReviewedMappingAsync(projectRoot, "card-two", "home", "card-two-node", "catalog.product-card", "catalog.product-card", "Components/Catalog/ProductSummaryCard.razor");
        await AddCompositionNodeAsync(projectRoot, "home", "card-one-node", "product card", "card-one", "Components/Catalog/ProductSummaryCard.razor", "catalog-components");
        await AddCompositionNodeAsync(projectRoot, "home", "card-two-node", "product card", "card-two", "Components/Catalog/ProductSummaryCard.razor", "catalog-components");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.DoesNotContain(findings, finding => finding.Code == "duplicate-non-repeatable-slot" && finding.Message.Contains("catalog.product-card", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "unapproved-extra-section" && (finding.Message.Contains("card-one-node", StringComparison.Ordinal) || finding.Message.Contains("card-two-node", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SlotValidation_UnknownUnmappedSectionFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Unknown Extra Section");
        await AddCompositionNodeAsync(projectRoot, "home", "unknown-extra-node", "editorial promo", null, "Pages/Ssr/Home/Promo.razor", "pages");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "unapproved-extra-section" && finding.Message.Contains("unknown-extra-node", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SlotValidation_ApprovedVisualExtensionPasses()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Approved Extension");
        await AddPageContractSlotAsync(projectRoot, "home", "allowedAdditionalSlotIds", "product.related-products");
        await AddCompositionNodeAsync(
            projectRoot,
            "home",
            "approved-extension-node",
            "visual recommendations",
            null,
            "Components/Catalog/RelatedProductsPlaceholder.razor",
            "product.related-products",
            approvedVisualExtensionId: "visual-extension-related-products",
            approvedVisualExtensionReason: "Approved visual-only merchandising extension.");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.DoesNotContain(findings, finding => finding.Code == "unapproved-extra-section" && finding.Message.Contains("approved-extension-node", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "slot-behavior-ownership-conflict" && finding.Message.Contains("approved-extension-node", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SlotValidation_RuntimeOwnershipFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Runtime Ownership");
        await MutateFirstReviewedMappingAsync(projectRoot, mapping => mapping["behaviorOwnership"] = "runtime");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "slot-behavior-ownership-conflict");
    }

    [Fact]
    public async Task SlotValidation_MissingTargetPathFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Missing Target Path");
        await MutateFirstCompositionNodeAsync(projectRoot, "home", node => node["targetFilePath"] = null);

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "slot-target-path-mismatch");
    }

    [Fact]
    public async Task SlotValidation_InvalidCatalogTargetFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Slot Invalid Catalog Target");
        await MutateFirstReviewedMappingAsync(projectRoot, mapping => mapping["presentationComponentId"] = "missing.component");

        var findings = new PageCompositionSlotValidator(GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "invalid-section-slot-mapping");
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "blueprint-v1-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "blueprint-v1-fixture");

        Assert.True(summary.ReadinessPassed);
        await ApproveAllReviewDecisionsAsync(summary.ArtifactRoot);
        var assembled = await new BlueprintV1Assembler(repoRoot).AssembleAsync(summary.ArtifactRoot, CancellationToken.None);
        Assert.True(assembled.Readiness.Passed, string.Join(Environment.NewLine, assembled.Readiness.Findings.Select(finding => finding.Code + ":" + finding.Message)));
        return summary.ArtifactRoot;
    }

    private static async Task<string> CreateProjectWithModifiedReviewDecisionsAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "blueprint-v1-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "blueprint-v1-modified-fixture");

        Assert.True(summary.ReadinessPassed);
        await AddAllowedCatalogTargetAsync(summary.ArtifactRoot, "catalog.product-card", "Components/Catalog/ProductSummaryCardModified.razor");
        await AddAllowedHomeSlotAsync(summary.ArtifactRoot, "catalog.product-card");
        await WriteModifiedReviewDecisionsAsync(summary.ArtifactRoot);
        return summary.ArtifactRoot;
    }

    private static async Task<VisualBlueprintV1> ReadBlueprintAsync(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<VisualBlueprintV1>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Blueprint artifact did not deserialize.");
    }

    private static async Task<ReviewedPageCompositionsDocument> ReadPageCompositionsAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "resolved", "page-compositions.reviewed.json"));
        return JsonSerializer.Deserialize<ReviewedPageCompositionsDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Page compositions artifact did not deserialize.");
    }

    private static async Task RewriteFirstReviewDecisionStatusAsync(string projectRoot, string status)
    {
        await MutateJsonAsync(projectRoot, "review/review-decisions.json", json =>
        {
            var decisions = json["decisions"]?.AsArray() ?? throw new InvalidOperationException("Review decisions has no decisions array.");
            if (decisions.Count == 0)
            {
                throw new InvalidOperationException("Review decisions is empty.");
            }

            decisions[0]!["status"] = status;
            decisions[0]!["reviewerNote"] = status + " for lifecycle test.";
        });
    }

    private static async Task ApproveAllReviewDecisionsAsync(string projectRoot)
    {
        var queuePath = Path.Combine(projectRoot, "review", "review-queue.json");
        var queue = JsonSerializer.Deserialize<ReviewQueue>(await File.ReadAllTextAsync(queuePath), VisualJson.Options)
            ?? throw new InvalidOperationException("Review queue did not deserialize.");
        var decisions = queue.Items.Select(item => new ReviewDecision(
            item.ItemId,
            "Approved",
            null,
            "Approved by deterministic test fixture.",
            DateTimeOffset.UtcNow,
            "reviewer@example.test",
            item.SourceArtifactId,
            item.SourceArtifactHash,
            "decision-" + item.ItemId)).ToArray();
        var document = new ReviewDecisions(
            "1.0",
            "review-decisions",
            "review-decisions-" + queue.ProjectId,
            DateTimeOffset.UtcNow,
            queue.ProjectId,
            decisions);
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "review", "review-decisions.json"), JsonSerializer.Serialize(document, VisualJson.Options) + Environment.NewLine);
    }

    private static async Task WriteModifiedReviewDecisionsAsync(string projectRoot)
    {
        var queuePath = Path.Combine(projectRoot, "review", "review-queue.json");
        var queue = JsonSerializer.Deserialize<ReviewQueue>(await File.ReadAllTextAsync(queuePath), VisualJson.Options)
            ?? throw new InvalidOperationException("Review queue did not deserialize.");
        queue = await EnsurePropagationReviewItemsAsync(projectRoot, queue);
        await File.WriteAllTextAsync(queuePath, JsonSerializer.Serialize(queue, VisualJson.Options) + Environment.NewLine);
        var decisions = queue.Items.Select(item =>
        {
            object? modified = item.ItemId switch
            {
                var id when id.StartsWith("mapping:", StringComparison.Ordinal) => new
                {
                    targetGeneratedPath = "Components/Catalog/ProductSummaryCardModified.razor",
                    generatedZone = "catalog-components",
                    variant = "modified-proof"
                },
                var id when id.StartsWith("section:", StringComparison.Ordinal) => new
                {
                    sectionType = "featured product card"
                },
                var id when id.StartsWith("token:", StringComparison.Ordinal) => new
                {
                    value = "17px"
                },
                _ => null
            };
            return new ReviewDecision(
                item.ItemId,
                modified is null ? "Approved" : "Modified",
                modified,
                modified is null ? "Approved by deterministic test fixture." : "Modified by propagation proof.",
                DateTimeOffset.UtcNow,
                "reviewer@example.test",
                item.SourceArtifactId,
                item.SourceArtifactHash,
                "decision-" + item.ItemId);
        }).ToArray();
        var document = new ReviewDecisions(
            "1.0",
            "review-decisions",
            "review-decisions-" + queue.ProjectId,
            DateTimeOffset.UtcNow,
            queue.ProjectId,
            decisions);
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "review", "review-decisions.json"), JsonSerializer.Serialize(document, VisualJson.Options) + Environment.NewLine);
    }

    private static async Task<ReviewQueue> EnsurePropagationReviewItemsAsync(string projectRoot, ReviewQueue queue)
    {
        var items = queue.Items.ToList();
        var mapping = ReadNode(projectRoot, "analysis/mapping/presentation-mappings.draft.json")["mappings"]?.AsArray().OfType<JsonObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("No draft mapping exists for propagation proof.");
        var mappingId = mapping["sourceCandidateId"]?.GetValue<string>() ?? throw new InvalidOperationException("Draft mapping has no sourceCandidateId.");
        AddIfMissing(items, "mapping:" + mappingId, "Presentation mappings", mapping.DeepClone(), ["mapping-proof"], "analysis/mapping/presentation-mappings.draft.json", "phase3d-mapping-hash");

        var section = ReadNode(projectRoot, "analysis/pages/home/sections.draft.json")["sections"]?.AsArray().OfType<JsonObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("No draft section exists for propagation proof.");
        var sectionId = section["sectionId"]?.GetValue<string>() ?? throw new InvalidOperationException("Draft section has no sectionId.");
        AddIfMissing(items, "section:home:" + sectionId, "sections", section.DeepClone(), ["section-proof"], "analysis/pages/*/sections.draft.json", "phase3d-section-hash");

        var token = ReadNode(projectRoot, "analysis/tokens/semantic-tokens.draft.json")["tokens"]?.AsArray().OfType<JsonObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("No draft token exists for propagation proof.");
        var tokenRole = token["role"]?.GetValue<string>() ?? token["tokenId"]?.GetValue<string>() ?? throw new InvalidOperationException("Draft token has no role.");
        AddIfMissing(items, "token:" + tokenRole, "semantic tokens", token.DeepClone(), ["token-proof"], "analysis/tokens/semantic-tokens.draft.json", "phase3d-token-hash");

        return queue with { Items = items.OrderBy(item => item.ItemId, StringComparer.Ordinal).ToArray() };

        static void AddIfMissing(
            List<ReviewQueueItem> items,
            string itemId,
            string itemType,
            JsonNode proposal,
            IReadOnlyList<string> evidenceIds,
            string sourceArtifactId,
            string sourceArtifactHash)
        {
            if (items.Any(item => string.Equals(item.ItemId, itemId, StringComparison.Ordinal)))
            {
                return;
            }

            items.Add(new ReviewQueueItem(itemId, itemType, 0.50m, proposal, evidenceIds, true, sourceArtifactId, sourceArtifactHash));
        }
    }

    private static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))?.AsObject()
            ?? throw new InvalidOperationException("Artifact is not a JSON object: " + relativePath);
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }

    private static JsonObject ReadNode(string projectRoot, string relativePath)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidOperationException("Artifact is not a JSON object: " + relativePath);
    }

    private static async Task AddAllowedCatalogTargetAsync(string projectRoot, string componentId, string targetPath)
    {
        await MutateJsonAsync(projectRoot, "presentation-catalog/presentation-component-catalog.json", json =>
        {
            var components = json["components"]?.AsArray() ?? throw new InvalidOperationException("Catalog has no components array.");
            var component = components.OfType<JsonObject>().First(candidate => candidate["componentId"]?.GetValue<string>() == componentId);
            var patterns = component["allowedFilePatterns"]?.AsArray() ?? throw new InvalidOperationException("Catalog component has no allowedFilePatterns array.");
            patterns.Add(targetPath);
        });
    }

    private static async Task AddAllowedHomeSlotAsync(string projectRoot, string slotId)
    {
        await MutateJsonAsync(projectRoot, "analysis/storefront-pattern/page-contracts.json", json =>
        {
            var pages = json["pages"]?.AsArray() ?? throw new InvalidOperationException("Page contracts has no pages array.");
            var home = pages.OfType<JsonObject>().First(page => page["pageId"]?.GetValue<string>() == "home");
            var allowed = home["allowedAdditionalSlotIds"]?.AsArray() ?? throw new InvalidOperationException("Home page has no allowedAdditionalSlotIds array.");
            allowed.Add(slotId);
        });
    }

    private static async Task AddPageContractSlotAsync(string projectRoot, string pageId, string arrayName, string slotId)
    {
        await MutateJsonAsync(projectRoot, "analysis/storefront-pattern/page-contracts.json", json =>
        {
            var pages = json["pages"]?.AsArray() ?? throw new InvalidOperationException("Page contracts has no pages array.");
            var page = pages.OfType<JsonObject>().First(candidate => candidate["pageId"]?.GetValue<string>() == pageId);
            var slots = page[arrayName]?.AsArray() ?? throw new InvalidOperationException("Page contract has no " + arrayName + " array.");
            if (!slots.Any(slot => slot?.GetValue<string>() == slotId))
            {
                slots.Add(slotId);
            }
        });
    }

    private static async Task AddReviewedMappingAsync(
        string projectRoot,
        string mappingId,
        string pageId,
        string sectionId,
        string componentId,
        string slotId,
        string targetPath)
    {
        await MutateJsonAsync(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json", json =>
        {
            var mappings = json["mappings"]?.AsArray() ?? throw new InvalidOperationException("Mappings artifact has no mappings array.");
            mappings.Add(new JsonObject
            {
                ["sourceCandidateId"] = mappingId,
                ["presentationComponentId"] = componentId,
                ["starterSlotId"] = slotId,
                ["variant"] = "default",
                ["slotAssignments"] = new JsonArray(),
                ["responsiveProperties"] = new JsonArray(),
                ["tokenBindings"] = new JsonArray(),
                ["interactionBindings"] = new JsonArray(),
                ["dataRequirements"] = new JsonArray(),
                ["behaviorOwnership"] = "presentation",
                ["confidence"] = 0.95,
                ["evidenceIds"] = new JsonArray("ev-desktop-1440-001"),
                ["mappingReason"] = "test reviewed mapping",
                ["alternativeMappings"] = new JsonArray(),
                ["humanReviewRequired"] = false,
                ["sourcePageId"] = pageId,
                ["sourceSectionId"] = sectionId,
                ["ecommerceRegionId"] = sectionId,
                ["pageArchetype"] = "home",
                ["targetGeneratedPath"] = targetPath,
                ["generatedZone"] = "catalog-components",
                ["routeOwnership"] = "presentation",
                ["reasonCodes"] = new JsonArray("test"),
                ["reviewState"] = "Approved"
            });
        });
    }

    private static async Task AddCompositionNodeAsync(
        string projectRoot,
        string pageId,
        string nodeId,
        string role,
        string? mappingId,
        string? targetPath,
        string targetZone,
        string? approvedVisualExtensionId = null,
        string? approvedVisualExtensionReason = null)
    {
        await MutateJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", json =>
        {
            var compositions = json["compositions"]?.AsArray() ?? throw new InvalidOperationException("Compositions artifact has no compositions array.");
            var composition = compositions.OfType<JsonObject>().First(item => item["pageId"]?.GetValue<string>() == pageId);
            var tree = composition["sectionTree"]?.AsArray() ?? throw new InvalidOperationException("Composition has no sectionTree array.");
            tree.Add(new JsonObject
            {
                ["nodeId"] = nodeId,
                ["role"] = role,
                ["presentationMappingId"] = mappingId,
                ["evidenceIds"] = new JsonArray("ev-desktop-1440-001"),
                ["children"] = new JsonArray(),
                ["stableFingerprint"] = nodeId + "-fingerprint",
                ["ecommerceRole"] = role,
                ["parentNodeId"] = null,
                ["childNodeIds"] = new JsonArray(),
                ["viewportBoundingBoxes"] = new JsonObject
                {
                    ["base"] = "x=0;y=0;width=100;height=100"
                },
                ["visualStyleTokenRefs"] = new JsonArray("surface-page"),
                ["componentMappingRef"] = mappingId,
                ["targetFilePath"] = targetPath,
                ["targetGeneratedZone"] = targetZone,
                ["allowedOperations"] = new JsonArray("visual-markup", "css", "responsive-layout"),
                ["protectedBehaviorMarkers"] = new JsonArray(),
                ["screenshotReferences"] = new JsonArray("captures/home/capture-manifest.json"),
                ["cropReferences"] = new JsonArray(),
                ["approvedVisualExtensionId"] = approvedVisualExtensionId,
                ["approvedVisualExtensionReason"] = approvedVisualExtensionReason,
                ["repeatedGroupId"] = null,
                ["stateExpectations"] = new JsonArray(),
                ["responsiveTransformationRules"] = new JsonArray(),
                ["unresolvedIssues"] = new JsonArray()
            });
        });
    }

    private static async Task MutateFirstReviewedMappingAsync(string projectRoot, Action<JsonObject> mutate)
    {
        await MutateJsonAsync(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json", json =>
        {
            var mappings = json["mappings"]?.AsArray() ?? throw new InvalidOperationException("Mappings artifact has no mappings array.");
            var first = mappings.OfType<JsonObject>().FirstOrDefault()
                ?? throw new InvalidOperationException("Mappings artifact has no mapping entries.");
            mutate(first);
        });
    }

    private static async Task MutateFirstCompositionNodeAsync(string projectRoot, string pageId, Action<JsonObject> mutate)
    {
        await MutateJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", json =>
        {
            var compositions = json["compositions"]?.AsArray() ?? throw new InvalidOperationException("Compositions artifact has no compositions array.");
            var composition = compositions.OfType<JsonObject>().First(item => item["pageId"]?.GetValue<string>() == pageId);
            var first = composition["sectionTree"]?.AsArray().OfType<JsonObject>().FirstOrDefault()
                ?? throw new InvalidOperationException("Composition has no section nodes.");
            mutate(first);
        });
    }

    private static GenerationReadinessReport InvokeReviewedBlueprintValidation(string projectRoot)
    {
        var method = typeof(BlueprintV1Assembler).GetMethod("ValidateReviewedBlueprint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("ValidateReviewedBlueprint method was not found.");
        var manifest = JsonSerializer.Deserialize<ReviewResolutionManifest>(File.ReadAllText(Path.Combine(projectRoot, "analysis", "resolved", "review-resolution-manifest.json")), VisualJson.Options);
        var findings = new List<GenerationReadinessFinding>();
        method.Invoke(null, [projectRoot, manifest, findings]);
        return new GenerationReadinessReport("1.0", "generation-readiness", "generation-readiness-test", DateTimeOffset.UtcNow, "test", findings.All(finding => finding.Severity != "blocking"), findings);
    }

    private static IEnumerable<string> BlueprintReferences(VisualBlueprintV1 blueprint)
    {
        foreach (var reference in blueprint.SourceProvenance) yield return reference;
        foreach (var reference in blueprint.PageArchetypes) yield return reference;
        yield return blueprint.Tokens;
        foreach (var reference in blueprint.Sections) yield return reference;
        foreach (var reference in blueprint.ResponsiveBehavior) yield return reference;
        foreach (var reference in blueprint.InteractionModels) yield return reference;
        yield return blueprint.ComponentDefinitions;
        yield return blueprint.ComponentInstances;
        foreach (var reference in blueprint.EcommerceRegions) yield return reference;
        yield return blueprint.PresentationMappings;
        yield return blueprint.UnsupportedPatterns;
        yield return blueprint.OriginalityRestrictions;
        yield return blueprint.Confidence;
        yield return blueprint.ReviewState;
    }

    private static async Task CloneHomePageAsync(string projectRoot, string pageId, string sourceUrl, string archetype, string? extraSectionRole)
    {
        CopyDirectory(Path.Combine(projectRoot, "analysis", "pages", "home"), Path.Combine(projectRoot, "analysis", "pages", pageId));
        var homeCaptures = Path.Combine(projectRoot, "captures", "home");
        if (Directory.Exists(homeCaptures))
        {
            CopyDirectory(homeCaptures, Path.Combine(projectRoot, "captures", pageId));
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(projectRoot, "analysis", "pages", pageId), "*.json", SearchOption.AllDirectories))
        {
            var node = JsonNode.Parse(await File.ReadAllTextAsync(file))!;
            ReplaceStrings(node, "/home/", $"/{pageId}/");
            ReplaceStrings(node, "home-", $"{pageId}-");
            if (Path.GetFileName(file).Equals("page-archetype.json", StringComparison.Ordinal))
            {
                node["pageId"] = pageId;
                node["primaryArchetype"] = archetype;
            }

            if (extraSectionRole is not null &&
                Path.GetFileName(file).Equals("sections.draft.json", StringComparison.Ordinal) &&
                node["sections"] is JsonArray sections &&
                sections.Count > 0)
            {
                var clone = sections[0]!.DeepClone();
                clone["sectionId"] = $"{pageId}-specific-composition";
                clone["sectionType"] = extraSectionRole;
                sections.Add(clone);
            }

            await File.WriteAllTextAsync(file, node.ToJsonString(VisualJson.Options));
        }

        await AddEvidenceOnlyPageAsync(projectRoot, pageId, sourceUrl, archetype, withEvidence: true);
    }

    private static async Task AddEvidenceOnlyPageAsync(string projectRoot, string pageId, string sourceUrl, string archetype, bool withEvidence)
    {
        var evidencePath = Path.Combine(projectRoot, "analysis", "evidence-snapshot.json");
        var evidence = JsonNode.Parse(await File.ReadAllTextAsync(evidencePath))!;
        var pages = evidence["pages"]?.AsArray() ?? throw new InvalidOperationException("Evidence snapshot has no pages array.");
        var clone = pages[0]!.DeepClone();
        ReplaceStrings(clone, "/home/", $"/{pageId}/");
        ReplaceStrings(clone, "home-", $"{pageId}-");
        clone["pageId"] = pageId;
        clone["sourceUrl"] = sourceUrl;
        clone["primaryArchetype"] = archetype;
        if (!withEvidence)
        {
            clone["sourceArtifactPaths"] = new JsonArray();
            clone["sourceEvidenceIds"] = new JsonArray();
            clone["viewports"] = new JsonArray();
        }

        pages.Add(clone);
        await File.WriteAllTextAsync(evidencePath, evidence.ToJsonString(VisualJson.Options));
    }

    private static async Task MutateSectionsAsync(string projectRoot, string pageId, Action<JsonArray> mutate)
    {
        var path = Path.Combine(projectRoot, "analysis", "pages", pageId, "sections.draft.json");
        var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var sections = node["sections"]?.AsArray() ?? throw new InvalidOperationException("Sections artifact has no sections array.");
        mutate(sections);
        await File.WriteAllTextAsync(path, node.ToJsonString(VisualJson.Options));
    }

    private static async Task<string> FirstSectionEvidenceIdAsync(string projectRoot, string pageId)
    {
        var path = Path.Combine(projectRoot, "analysis", "pages", pageId, "sections.draft.json");
        var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        return node["sections"]?[0]?["evidenceIds"]?[0]?.GetValue<string>()
            ?? throw new InvalidOperationException("First section has no evidence ID.");
    }

    private static async Task AddDraftMappingAsync(string projectRoot, string evidenceId, string targetGeneratedPath, string generatedZone)
    {
        var path = Path.Combine(projectRoot, "analysis", "mapping", "presentation-mappings.draft.json");
        var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var mappings = node["mappings"]?.AsArray() ?? throw new InvalidOperationException("Mappings artifact has no mappings array.");
        mappings.Add(new JsonObject
        {
            ["sourceCandidateId"] = "section-protected-target",
            ["presentationComponentId"] = "catalog.product-card",
            ["starterSlotId"] = "catalog.product-card",
            ["variant"] = "default",
            ["slotAssignments"] = new JsonArray(),
            ["responsiveProperties"] = new JsonArray(),
            ["tokenBindings"] = new JsonArray(),
            ["interactionBindings"] = new JsonArray(),
            ["dataRequirements"] = new JsonArray(),
            ["behaviorOwnership"] = "presentation",
            ["confidence"] = 0.8,
            ["evidenceIds"] = new JsonArray(evidenceId),
            ["mappingReason"] = "test",
            ["alternativeMappings"] = new JsonArray(),
            ["humanReviewRequired"] = false,
            ["targetGeneratedPath"] = targetGeneratedPath,
            ["generatedZone"] = generatedZone
        });
        await File.WriteAllTextAsync(path, node.ToJsonString(VisualJson.Options));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private static void ReplaceStrings(JsonNode node, string oldValue, string newValue)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    obj[property.Key] = text.Replace(oldValue, newValue, StringComparison.Ordinal);
                }
                else if (property.Value is not null)
                {
                    ReplaceStrings(property.Value, oldValue, newValue);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    array[index] = text.Replace(oldValue, newValue, StringComparison.Ordinal);
                }
                else if (array[index] is not null)
                {
                    ReplaceStrings(array[index]!, oldValue, newValue);
                }
            }
        }
    }

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

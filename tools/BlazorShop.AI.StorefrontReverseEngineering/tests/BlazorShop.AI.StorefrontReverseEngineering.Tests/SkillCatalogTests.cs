using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Skills;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class SkillCatalogTests
{
    private static readonly string[] RequiredSkills =
    [
        "storefront-reference-reconnaissance",
        "stabilize-reference-page",
        "capture-stable-full-page",
        "capture-responsive-evidence",
        "discover-visual-interactions",
        "extract-visual-evidence",
        "analyze-page-topology",
        "create-visual-specification-draft",
        "audit-reference-originality",
        "validate-visual-evidence"
    ];

    [Fact]
    public void SkillCatalog_IsComplete()
    {
        var catalog = ReadCatalog();
        var names = catalog.Skills.Select(skill => skill.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Equal("1.0", catalog.SchemaVersion);
        foreach (var requiredSkill in RequiredSkills)
        {
            Assert.Contains(requiredSkill, names);
        }

        Assert.All(catalog.Skills, skill =>
        {
            Assert.False(string.IsNullOrWhiteSpace(skill.Version));
            Assert.False(string.IsNullOrWhiteSpace(skill.Category));
            Assert.False(string.IsNullOrWhiteSpace(skill.Purpose));
            Assert.NotEmpty(skill.Inputs);
            Assert.NotEmpty(skill.Outputs);
            Assert.NotEmpty(skill.CompletionCriteria);
            Assert.NotEmpty(skill.ForbiddenActions);
        });
    }

    [Fact]
    public void SkillCatalog_IsManifestNotRuntimeMagic()
    {
        var catalog = ReadCatalog();

        Assert.Contains(catalog.Skills, skill => skill.ExecutionType == SkillExecutionType.DocumentationOnly || skill.ExecutionType == SkillExecutionType.Hybrid);
        Assert.DoesNotContain(catalog.Skills, skill => skill.Dependencies.Any(dependency => dependency.EndsWith("SKILL.md", StringComparison.OrdinalIgnoreCase)));
        Assert.All(catalog.Skills, skill => Assert.DoesNotContain("React", string.Join(' ', skill.Outputs.Concat(skill.Dependencies)), StringComparison.OrdinalIgnoreCase));
    }

    private static SkillCatalog ReadCatalog()
    {
        var path = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Skills", "reverse-engineering-skills.json");
        return JsonSerializer.Deserialize<SkillCatalog>(File.ReadAllText(path), VisualJson.Options)
            ?? throw new InvalidOperationException("Skill catalog could not be parsed.");
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

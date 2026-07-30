namespace BlazorShop.AI.StorefrontReverseEngineering.Skills;

public sealed record SkillCatalog(
    string SchemaVersion,
    IReadOnlyList<SkillDefinition> Skills);

public sealed record SkillDefinition(
    string Name,
    string Version,
    string Category,
    string Purpose,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<string> Dependencies,
    SkillExecutionType ExecutionType,
    bool HumanReviewRequired,
    IReadOnlyList<string> CompletionCriteria,
    IReadOnlyList<string> ForbiddenActions);

public enum SkillExecutionType
{
    Deterministic,
    AIAssisted,
    Hybrid,
    DocumentationOnly
}

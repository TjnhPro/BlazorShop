using BlazorShop.AI.StorefrontReverseEngineering.Contracts;

namespace BlazorShop.AI.StorefrontReverseEngineering.Domain;

public static class VisualProjectStatusTransitions
{
    private static readonly IReadOnlyDictionary<VisualProjectStatus, VisualProjectStatus[]> AllowedTransitions =
        new Dictionary<VisualProjectStatus, VisualProjectStatus[]>
        {
            [VisualProjectStatus.Created] = [VisualProjectStatus.Discovering, VisualProjectStatus.Failed],
            [VisualProjectStatus.Discovering] = [VisualProjectStatus.Discovered, VisualProjectStatus.ValidationFailed, VisualProjectStatus.Failed],
            [VisualProjectStatus.Discovered] = [VisualProjectStatus.Capturing, VisualProjectStatus.Failed],
            [VisualProjectStatus.Capturing] = [VisualProjectStatus.Captured, VisualProjectStatus.ValidationFailed, VisualProjectStatus.Failed],
            [VisualProjectStatus.Captured] = [VisualProjectStatus.Analyzing, VisualProjectStatus.ValidationFailed, VisualProjectStatus.Failed],
            [VisualProjectStatus.Analyzing] = [VisualProjectStatus.DraftReady, VisualProjectStatus.ValidationFailed, VisualProjectStatus.Failed],
            [VisualProjectStatus.DraftReady] = [VisualProjectStatus.Analyzing, VisualProjectStatus.ValidationFailed, VisualProjectStatus.Failed],
            [VisualProjectStatus.ValidationFailed] = [VisualProjectStatus.Discovering, VisualProjectStatus.Capturing, VisualProjectStatus.Analyzing, VisualProjectStatus.Failed],
            [VisualProjectStatus.Failed] = [VisualProjectStatus.Discovering, VisualProjectStatus.Capturing, VisualProjectStatus.Analyzing]
        };

    public static VisualProject MoveTo(VisualProject project, VisualProjectStatus status, bool recoveryMode = false)
    {
        if (project.Status == status)
        {
            return project with { UpdatedUtc = DateTimeOffset.UtcNow };
        }

        if (!recoveryMode && (!AllowedTransitions.TryGetValue(project.Status, out var allowed) || !allowed.Contains(status)))
        {
            throw new InvalidOperationException($"[SRE-LIFECYCLE-001] Invalid project status transition. Problem: '{project.Status}' cannot move to '{status}'. Cause: workflow commands must preserve deterministic lifecycle order. Fix: resume from the expected command or use explicit recovery mode.");
        }

        return project with
        {
            Status = status,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }
}

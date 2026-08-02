using System.Text.Json;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record HandoffReferenceRegistryEntry(
    string ArtifactPath,
    string Pointer,
    string Category,
    bool Required,
    string AllowedTargetRoot,
    string CyclePolicy);

public sealed record HandoffReferenceObservation(
    string ArtifactPath,
    string Pointer,
    string Category,
    string CyclePolicy,
    string Value);

public sealed record HandoffReferenceScanFinding(
    string Code,
    string Message,
    string ArtifactPath,
    string Pointer,
    string Value);

public static class HandoffReferenceRegistry
{
    public static IReadOnlyList<HandoffReferenceRegistryEntry> Entries { get; } =
    [
        PackageIndex("analysis/agent-handoff/manifest.json", "/artifactList/*"),
        PackageIndex("analysis/agent-handoff/manifest.json", "/artifactEntries/*/path"),
        Diagnostic("analysis/agent-handoff/manifest.json", "/diagnostics/sourceProjectRoot"),
        Diagnostic("analysis/agent-handoff/manifest.json", "/handoffRoot"),
        Diagnostic("analysis/agent-handoff/manifest.json", "/requiredConsumerContract"),
        Opaque("analysis/agent-handoff/manifest.json", "/schemaRequirements/*/schemaFileName"),
        Opaque("analysis/agent-handoff/manifest.json", "/consumerReferencePolicy/handoffRoot"),
        Consumer("analysis/agent-handoff/visual-blueprint.json", "/consumerReferences/*"),
        Diagnostic("analysis/agent-handoff/visual-blueprint.json", "/diagnosticProvenance/*/path"),
        Diagnostic("analysis/agent-handoff/visual-blueprint.json", "/generationRestrictions/*"),
        ExternalUrl("analysis/agent-handoff/page-compositions.json", "$..sourceUrl"),
        ExternalUrl("analysis/agent-handoff/page-compositions.json", "$..sourceUrls"),
        Diagnostic("analysis/agent-handoff/page-compositions.json", "/diagnosticProvenance/*/path"),
        Diagnostic("analysis/agent-handoff/page-compositions.json", "$..captureArtifactPaths"),
        Diagnostic("analysis/agent-handoff/page-compositions.json", "$..sourceEvidenceLinks"),
        Diagnostic("analysis/agent-handoff/page-compositions.json", "$..sharedResponsiveRules"),
        Diagnostic("analysis/agent-handoff/page-compositions.json", "$..screenshotReferences"),
        Diagnostic("analysis/agent-handoff/page-compositions.json", "$..cropReferences"),
        GeneratedTarget("analysis/agent-handoff/page-compositions.json", "$..targetGeneratedFilePath"),
        GeneratedTarget("analysis/agent-handoff/page-compositions.json", "$..targetFilePath"),
        GeneratedTarget("analysis/agent-handoff/allowed-files.json", "/paths/*"),
        GeneratedTarget("analysis/agent-handoff/protected-files.json", "/paths/*"),
        Diagnostic("analysis/agent-handoff/presentation-catalog.json", "/diagnosticProvenance/*/path"),
        Diagnostic("analysis/agent-handoff/presentation-catalog.json", "$..sourceFiles"),
        GeneratedTarget("analysis/agent-handoff/presentation-catalog.json", "$..allowedFilePatterns"),
        GeneratedTarget("analysis/agent-handoff/presentation-catalog.json", "$..protectedFilePatterns"),
        GeneratedTarget("analysis/agent-handoff/presentation-mappings.json", "$..targetGeneratedPath"),
        Opaque("analysis/agent-handoff/presentation-mappings.json", "$..evidenceIds"),
        Opaque("analysis/agent-handoff/presentation-mappings.json", "$..sourceCandidateId"),
        Opaque("analysis/agent-handoff/presentation-mappings.json", "$..sourcePageId"),
        Opaque("analysis/agent-handoff/presentation-mappings.json", "$..sourceSectionId"),
        Opaque("analysis/agent-handoff/presentation-mappings.json", "$..ecommerceRegionId"),
        Opaque("analysis/agent-handoff/component-candidates.json", "$..instanceIds"),
        Opaque("analysis/agent-handoff/component-candidates.json", "$..tokenReferences"),
        Opaque("analysis/agent-handoff/component-candidates.json", "$..localOverrideIds"),
        Opaque("analysis/agent-handoff/component-candidates.json", "$..responsiveBehaviorRefs"),
        Opaque("analysis/agent-handoff/component-candidates.json", "$..interactionBehaviorRefs"),
        Opaque("analysis/agent-handoff/component-candidates.json", "$..evidenceIds"),
        Opaque("analysis/agent-handoff/component-instances.json", "$..evidenceIds"),
        Opaque("analysis/agent-handoff/responsive-behavior.json", "$..evidenceIds"),
        Opaque("analysis/agent-handoff/interaction-models.json", "$..evidenceIds"),
        Diagnostic("analysis/agent-handoff/design-tokens.json", "/diagnosticProvenance/*/path"),
        Opaque("analysis/agent-handoff/design-tokens.json", "$..evidenceIds"),
        Diagnostic("analysis/agent-handoff/visual-style.json", "/diagnosticProvenance/*/path"),
        Opaque("analysis/agent-handoff/visual-style.json", "$..evidenceIds"),
        Opaque("analysis/agent-handoff/confidence.json", "$..evidenceIds"),
        GeneratedTarget("analysis/agent-handoff/confidence.json", "$..targetGeneratedPath"),
        GeneratedTarget("analysis/agent-handoff/confidence.json", "$..targetFilePath"),
        Diagnostic("analysis/agent-handoff/review-resolution.json", "/resolvedArtifactReferences/*"),
        Diagnostic("analysis/agent-handoff/review-resolution.json", "/diagnosticProvenance/*/path"),
        Opaque("analysis/agent-handoff/originality-restrictions.json", "$..itemId"),
        Consumer("analysis/agent-handoff/evidence-manifest.json", "$..handoffPath"),
        Diagnostic("analysis/agent-handoff/evidence-manifest.json", "$..sourcePath"),
        ExternalUrl("analysis/agent-handoff/evidence-manifest.json", "$..sourceUrl"),
        Diagnostic("analysis/agent-handoff/interaction-models.json", "$..beforeStylesPath"),
        Diagnostic("analysis/agent-handoff/interaction-models.json", "$..afterStylesPath")
    ];

    private static HandoffReferenceRegistryEntry Consumer(string artifactPath, string pointer) =>
        new(artifactPath, pointer, PortableHandoffReferenceCategories.ConsumerDependency, Required: true, AgentHandoffContract.HandoffRoot, "acyclic");

    private static HandoffReferenceRegistryEntry PackageIndex(string artifactPath, string pointer) =>
        new(artifactPath, pointer, PortableHandoffReferenceCategories.ConsumerDependency, Required: true, AgentHandoffContract.HandoffRoot, "index-only");

    private static HandoffReferenceRegistryEntry Diagnostic(string artifactPath, string pointer) =>
        new(artifactPath, pointer, PortableHandoffReferenceCategories.DiagnosticProvenance, Required: false, string.Empty, "ignored");

    private static HandoffReferenceRegistryEntry GeneratedTarget(string artifactPath, string pointer) =>
        new(artifactPath, pointer, PortableHandoffReferenceCategories.GeneratedTargetPath, Required: false, string.Empty, "ignored");

    private static HandoffReferenceRegistryEntry ExternalUrl(string artifactPath, string pointer) =>
        new(artifactPath, pointer, PortableHandoffReferenceCategories.ExternalInformationalUrl, Required: false, string.Empty, "ignored");

    private static HandoffReferenceRegistryEntry Opaque(string artifactPath, string pointer) =>
        new(artifactPath, pointer, PortableHandoffReferenceCategories.OpaqueId, Required: false, string.Empty, "ignored");
}

public sealed class HandoffReferenceScanner
{
    private readonly IReadOnlyList<HandoffReferenceRegistryEntry> registry;

    public HandoffReferenceScanner(IReadOnlyList<HandoffReferenceRegistryEntry>? registry = null)
    {
        this.registry = registry ?? HandoffReferenceRegistry.Entries;
    }

    public (IReadOnlyList<HandoffReferenceObservation> Observations, IReadOnlyList<HandoffReferenceScanFinding> Findings) Scan(string handoffRoot)
    {
        var observations = new List<HandoffReferenceObservation>();
        var findings = new List<HandoffReferenceScanFinding>();
        var registeredByArtifact = registry.GroupBy(entry => entry.ArtifactPath, StringComparer.Ordinal);
        foreach (var group in registeredByArtifact)
        {
            var artifactPath = Path.Combine(handoffRoot, group.Key.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(artifactPath))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
            foreach (var entry in group)
            {
                var values = Extract(document.RootElement, entry.Pointer).ToArray();
                foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
                {
                    var observation = new HandoffReferenceObservation(entry.ArtifactPath, entry.Pointer, entry.Category, entry.CyclePolicy, value);
                    observations.Add(observation);
                    findings.AddRange(Validate(handoffRoot, entry, observation));
                }
            }

            var registeredPointers = group.Select(entry => entry.Pointer).ToArray();
            foreach (var (pointer, value) in AllStringValues(document.RootElement))
            {
                if (IsRegistered(pointer, registeredPointers) || !LooksLikePath(value))
                {
                    continue;
                }

                findings.Add(new HandoffReferenceScanFinding(
                    "handoff-consumer-reference-unregistered",
                    $"Path-like reference is not registered for portable validation: {value}",
                    group.Key,
                    pointer,
                    value));
            }
        }

        findings.AddRange(FindConsumerReferenceCycles(observations));
        return (observations, findings);
    }

    private static IEnumerable<HandoffReferenceScanFinding> FindConsumerReferenceCycles(IReadOnlyList<HandoffReferenceObservation> observations)
    {
        var graph = observations
            .Where(observation => observation.Category == PortableHandoffReferenceCategories.ConsumerDependency)
            .Where(observation => string.Equals(observation.CyclePolicy, "acyclic", StringComparison.Ordinal))
            .Select(observation => (From: Normalize(observation.ArtifactPath), To: Normalize(observation.Value), Observation: observation))
            .GroupBy(edge => edge.From, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in graph.Keys.Order(StringComparer.Ordinal))
        {
            var finding = Visit(node, graph, visiting, visited);
            if (finding is not null)
            {
                yield return finding;
                yield break;
            }
        }
    }

    private static HandoffReferenceScanFinding? Visit(
        string node,
        IReadOnlyDictionary<string, (string From, string To, HandoffReferenceObservation Observation)[]> graph,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(node))
        {
            return null;
        }

        if (!visiting.Add(node))
        {
            return new HandoffReferenceScanFinding("handoff-artifact-reference-cycle", "Consumer artifact references contain a cycle.", node, "", node);
        }

        if (graph.TryGetValue(node, out var edges))
        {
            foreach (var edge in edges)
            {
                if (!graph.ContainsKey(edge.To))
                {
                    continue;
                }

                if (visiting.Contains(edge.To))
                {
                    return new HandoffReferenceScanFinding("handoff-artifact-reference-cycle", "Consumer artifact references contain a cycle.", edge.Observation.ArtifactPath, edge.Observation.Pointer, edge.Observation.Value);
                }

                var finding = Visit(edge.To, graph, visiting, visited);
                if (finding is not null)
                {
                    return finding;
                }
            }
        }

        visiting.Remove(node);
        visited.Add(node);
        return null;
    }

    private static IEnumerable<HandoffReferenceScanFinding> Validate(
        string handoffRoot,
        HandoffReferenceRegistryEntry entry,
        HandoffReferenceObservation observation)
    {
        var value = StripDiagnosticMarker(observation.Value);
        if (entry.Category == PortableHandoffReferenceCategories.ConsumerDependency)
        {
            if (IsAbsolutePath(value))
            {
                yield return Finding("handoff-consumer-reference-absolute", "Consumer reference is absolute and not portable.", observation);
                yield break;
            }

            if (IsExternalUrl(value))
            {
                yield return Finding("handoff-reference-category-mismatch", "External URL cannot be used as a consumer file dependency.", observation);
                yield break;
            }

            if (value.Contains(".draft.json", StringComparison.OrdinalIgnoreCase))
            {
                yield return Finding("handoff-consumer-reference-draft", "Consumer reference points at a draft artifact.", observation);
                yield break;
            }

            var normalized = Normalize(value);
            if (normalized.Split('/').Contains("..", StringComparer.Ordinal) ||
                !normalized.StartsWith(entry.AllowedTargetRoot + "/", StringComparison.Ordinal))
            {
                var code = normalized.StartsWith("analysis/", StringComparison.Ordinal) && !normalized.StartsWith(AgentHandoffContract.HandoffRoot + "/", StringComparison.Ordinal)
                    ? "handoff-diagnostic-reference-used-as-consumer"
                    : "handoff-consumer-reference-escape";
                yield return Finding(code, "Consumer reference escapes the handoff package.", observation);
                yield break;
            }

            var targetPath = Path.Combine(handoffRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(targetPath) &&
                !Directory.Exists(targetPath) &&
                !string.Equals(normalized, "analysis/agent-handoff/handoff-readiness.json", StringComparison.Ordinal))
            {
                yield return Finding("handoff-consumer-reference-missing", "Consumer reference target is missing from the handoff package.", observation);
            }
        }
        else if (entry.Category == PortableHandoffReferenceCategories.ExternalInformationalUrl && !IsExternalUrl(value))
        {
            yield return Finding("handoff-reference-category-mismatch", "Registered external informational URL field contains a non-URL value.", observation);
        }
    }

    private static HandoffReferenceScanFinding Finding(string code, string message, HandoffReferenceObservation observation) =>
        new(code, message, observation.ArtifactPath, observation.Pointer, observation.Value);

    private static IEnumerable<string> Extract(JsonElement element, string pointer)
    {
        if (pointer.StartsWith("$..", StringComparison.Ordinal))
        {
            var propertyName = pointer[3..];
            foreach (var value in DescendantPropertyValues(element, propertyName))
            {
                foreach (var scalar in ScalarStrings(value))
                {
                    yield return scalar;
                }
            }

            yield break;
        }

        foreach (var value in ExtractPointer(element, pointer.Split('/', StringSplitOptions.RemoveEmptyEntries), 0))
        {
            foreach (var scalar in ScalarStrings(value))
            {
                yield return scalar;
            }
        }
    }

    private static IEnumerable<JsonElement> ExtractPointer(JsonElement element, string[] segments, int index)
    {
        if (index >= segments.Length)
        {
            yield return element;
            yield break;
        }

        var segment = segments[index];
        if (segment == "*" && element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in ExtractPointer(item, segments, index + 1))
                {
                    yield return value;
                }
            }
        }
        else if (segment == "*" && element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var value in ExtractPointer(property.Value, segments, index + 1))
                {
                    yield return value;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(segment, out var child))
        {
            foreach (var value in ExtractPointer(child, segments, index + 1))
            {
                yield return value;
            }
        }
    }

    private static IEnumerable<JsonElement> DescendantPropertyValues(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                {
                    yield return property.Value;
                }

                foreach (var child in DescendantPropertyValues(property.Value, propertyName))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in DescendantPropertyValues(item, propertyName))
                {
                    yield return child;
                }
            }
        }
    }

    private static IEnumerable<string> ScalarStrings(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            yield return element.GetString() ?? string.Empty;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in ScalarStrings(item))
                {
                    yield return value;
                }
            }
        }
    }

    private static IEnumerable<(string Pointer, string Value)> AllStringValues(JsonElement element, string pointer = "")
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            yield return (pointer, element.GetString() ?? string.Empty);
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var value in AllStringValues(property.Value, pointer + "/" + property.Name))
                {
                    yield return value;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in AllStringValues(item, pointer + "/*"))
                {
                    yield return value;
                }

                index++;
            }
        }
    }

    private static bool IsRegistered(string pointer, IReadOnlyList<string> registeredPointers) =>
        registeredPointers.Any(registered =>
            string.Equals(pointer, registered, StringComparison.Ordinal) ||
            (registered.StartsWith("$..", StringComparison.Ordinal) && PointerContainsProperty(pointer, registered[3..])) ||
            PointerPatternMatches(pointer, registered));

    private static bool PointerContainsProperty(string pointer, string propertyName) =>
        pointer.Split('/', StringSplitOptions.RemoveEmptyEntries).Contains(propertyName, StringComparer.Ordinal);

    private static bool PointerPatternMatches(string pointer, string pattern)
    {
        var pointerSegments = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var patternSegments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return pointerSegments.Length == patternSegments.Length &&
            pointerSegments.Zip(patternSegments).All(pair => pair.Second == "*" || string.Equals(pair.First, pair.Second, StringComparison.Ordinal));
    }

    private static bool LooksLikePath(string value)
    {
        var stripped = StripDiagnosticMarker(value);
        if (stripped.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return stripped.Contains('/', StringComparison.Ordinal) ||
            stripped.Contains('\\', StringComparison.Ordinal) ||
            stripped.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            stripped.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            stripped.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            IsAbsolutePath(stripped);
    }

    private static bool IsAbsolutePath(string value) =>
        Path.IsPathRooted(value) ||
        value.StartsWith("//", StringComparison.Ordinal) ||
        (value.Length >= 3 && char.IsLetter(value[0]) && value[1] == ':' && (value[2] == '/' || value[2] == '\\'));

    private static bool IsExternalUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out _);

    private static string Normalize(string value) => StripDiagnosticMarker(value).Replace('\\', '/');

    private static string StripDiagnosticMarker(string value) =>
        value.StartsWith("diagnostics-only:", StringComparison.Ordinal) ? value["diagnostics-only:".Length..] : value;
}

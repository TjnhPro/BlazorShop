using System.Security.Cryptography;
using System.Text;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record PortableHandoffPackageContract(
    string PackageVersion,
    string HandoffRoot,
    IReadOnlyList<PortableHandoffArtifactEntry> ArtifactEntries,
    IReadOnlyList<PortableHandoffSchemaRequirement> SchemaRequirements,
    PortableHandoffReferencePolicy ConsumerReferencePolicy,
    string PackageHash);

public sealed record PortableHandoffArtifactEntry(
    string RelativePath,
    string ArtifactKind,
    string SchemaKind,
    string SchemaVersion,
    string Sha256,
    long SizeBytes,
    bool Required,
    bool IncludeInPackageHash);

public sealed record PortableHandoffSchemaRequirement(
    string SchemaKind,
    string ArtifactKind,
    string SchemaVersion,
    string SchemaFileName,
    string Sha256,
    bool Required);

public sealed record PortableHandoffReferencePolicy(
    string HandoffRoot,
    IReadOnlyList<HandoffReferenceCategory> Categories,
    bool RejectAbsoluteConsumerPaths,
    bool RejectConsumerPathEscape,
    bool RejectDraftConsumerReferences);

public sealed record HandoffReferenceCategory(
    string Category,
    string Description,
    bool RequiredFileDependency,
    bool MustStayInsideHandoffRoot);

public static class PortableHandoffReferenceCategories
{
    public const string ConsumerDependency = "consumer-dependency";
    public const string DiagnosticProvenance = "diagnostic-provenance";
    public const string GeneratedTargetPath = "generated-target-path";
    public const string ExternalInformationalUrl = "external-informational-url";
    public const string OpaqueId = "opaque-id";

    public static IReadOnlyList<HandoffReferenceCategory> All { get; } =
    [
        new(ConsumerDependency, "Required file dependency read by a Phase 4 consumer.", RequiredFileDependency: true, MustStayInsideHandoffRoot: true),
        new(DiagnosticProvenance, "Original Phase 3 source path kept for audit only.", RequiredFileDependency: false, MustStayInsideHandoffRoot: false),
        new(GeneratedTargetPath, "Future generated storefront target path, not a package file dependency.", RequiredFileDependency: false, MustStayInsideHandoffRoot: false),
        new(ExternalInformationalUrl, "Reference URL or documentation URL, not a package file dependency.", RequiredFileDependency: false, MustStayInsideHandoffRoot: false),
        new(OpaqueId, "Stable identifier that must not be interpreted as a filesystem path.", RequiredFileDependency: false, MustStayInsideHandoffRoot: false)
    ];
}

public static class PortableHandoffPackageHasher
{
    public static string ComputePackageHash(
        IEnumerable<PortableHandoffArtifactEntry> artifactEntries,
        IEnumerable<PortableHandoffSchemaRequirement> schemaRequirements)
    {
        var lines = artifactEntries
            .Where(entry => entry.IncludeInPackageHash)
            .Select(entry => string.Join('\t',
                "artifact",
                NormalizePath(entry.RelativePath),
                entry.ArtifactKind,
                entry.SchemaKind,
                entry.SchemaVersion,
                entry.Sha256,
                entry.SizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                entry.Required ? "required" : "optional"))
            .Concat(schemaRequirements
                .Where(schema => schema.Required)
                .Select(schema => string.Join('\t',
                    "schema",
                    NormalizePath(schema.SchemaFileName),
                    schema.SchemaKind,
                    schema.ArtifactKind,
                    schema.SchemaVersion,
                    schema.Sha256)))
            .Order(StringComparer.Ordinal);

        var payload = string.Join('\n', lines) + "\n";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public static string ComputeFileHash(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}

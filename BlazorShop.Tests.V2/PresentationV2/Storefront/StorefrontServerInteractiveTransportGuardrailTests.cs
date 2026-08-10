namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontServerInteractiveTransportGuardrailTests
{
    private static readonly string[] StorefrontUiRoots =
    [
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Browser",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
        "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
    ];

    private static readonly string[] ForbiddenServerInteractiveTransportTokens =
    [
        "HubConnection",
        "AddSignalR",
        "MapHub",
        "ClientWebSocket",
        "WebSocket.CreateFromStream",
    ];

    [Fact]
    public void StorefrontUiSourceDoesNotIntroduceServerInteractiveOrManualWebSocketTransport()
    {
        var violations = FindForbiddenTransportViolations(EnumerateStorefrontUiSourceFiles());

        Assert.Empty(violations);
    }

    [Fact]
    public void TransportGuardrailRejectsSignalRHubAndManualWebSocketTokens()
    {
        var fixtureFiles = new[]
        {
            new SourceFile("Program.cs", "builder.Services.AddSignalR();"),
            new SourceFile("Routes.cs", "endpoints.MapHub<CartHub>(\"/cart\");"),
            new SourceFile("Browser.cs", "private HubConnection? connection;"),
            new SourceFile("Socket.cs", "var socket = new ClientWebSocket();"),
            new SourceFile("Factory.cs", "WebSocket.CreateFromStream(stream, true, null, TimeSpan.Zero);"),
        };

        var violations = FindForbiddenTransportViolations(fixtureFiles);

        foreach (var token in ForbiddenServerInteractiveTransportTokens)
        {
            Assert.Contains(violations, violation => violation.Contains(token, StringComparison.Ordinal));
        }
    }

    private static IReadOnlyList<string> FindForbiddenTransportViolations(IEnumerable<SourceFile> files)
    {
        return files
            .SelectMany(file => ForbiddenServerInteractiveTransportTokens
                .Where(token => file.Source.Contains(token, StringComparison.Ordinal))
                .Select(token => $"{file.RelativePath}: {token}"))
            .ToArray();
    }

    private static IEnumerable<SourceFile> EnumerateStorefrontUiSourceFiles()
    {
        return StorefrontUiRoots
            .Select(RepositoryPath)
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(IsSourceFile)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Select(file => new SourceFile(
                Path.GetRelativePath(RepositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllText(file)));
    }

    private static bool IsSourceFile(string file)
    {
        return Path.GetExtension(file) is ".cs" or ".razor" or ".js";
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed record SourceFile(string RelativePath, string Source);
}

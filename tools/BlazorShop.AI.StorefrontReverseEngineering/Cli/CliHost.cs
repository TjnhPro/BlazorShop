namespace BlazorShop.AI.StorefrontReverseEngineering.Cli;

public static class CliHost
{
    private static readonly string[] KnownCommands =
    [
        "init",
        "discover",
        "capture",
        "inspect",
        "validate"
    ];

    public static Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            WriteHelp(output);
            return Task.FromResult(0);
        }

        var command = args[0].Trim().ToLowerInvariant();
        if (!KnownCommands.Contains(command, StringComparer.Ordinal))
        {
            error.WriteLine($"[SRE-CLI-001] Unknown command '{args[0]}'. Problem: command is not supported. Cause: Phase 3A only exposes known workflow commands. Fix: run with --help and choose a listed command.");
            return Task.FromResult(2);
        }

        output.WriteLine($"StorefrontReverseEngineering command '{command}' is available. Implementation is added by later Phase 3A lifecycle phases.");
        return Task.FromResult(0);
    }

    public static void WriteHelp(TextWriter output)
    {
        output.WriteLine("BlazorShop.AI.StorefrontReverseEngineering");
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering -- <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");
        foreach (var command in KnownCommands)
        {
            output.WriteLine($"  {command}");
        }
    }
}

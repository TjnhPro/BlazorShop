namespace BlazorShop.Tests.Architecture
{
    using System.Diagnostics;

    using Xunit;

    public sealed class LegacyRemovalGuardrailTests
    {
        [Fact]
        public void Phase0_LegacyRemovalGuardrailScript_SupportsInventoryAndActiveStrictModes()
        {
            var repositoryRoot = FindRepositoryRoot().FullName;
            var scriptPath = RepositoryPath("scripts/verify-no-active-legacy-reference.ps1");
            var allowListPath = RepositoryPath("docs/refactor-control-Commerce-storefront/legacy-removal-allowlist.json");

            Assert.True(File.Exists(scriptPath));
            Assert.True(File.Exists(allowListPath));

            var script = File.ReadAllText(scriptPath);
            var allowList = File.ReadAllText(allowListPath);

            Assert.Contains("ValidateSet(\"Inventory\", \"ActiveStrict\")", script, StringComparison.Ordinal);
            Assert.Contains("activeStrictIgnoreFiles", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.sln", script, StringComparison.Ordinal);
            Assert.Contains("compose.v2.production.yml", script, StringComparison.Ordinal);
            Assert.Contains("scripts/run-v2-local.ps1", script, StringComparison.Ordinal);
            Assert.Contains("Inventory is intentionally noisy", allowList, StringComparison.Ordinal);

            var inventory = RunProcess(
                "powershell",
                [
                    "-NoLogo",
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    scriptPath,
                    "-Mode",
                    "Inventory",
                ],
                repositoryRoot);

            Assert.Equal(0, inventory.ExitCode);
            Assert.Contains("Inventory: legacy references found.", inventory.StandardOutput, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Presentation", inventory.StandardOutput, StringComparison.Ordinal);

            var activeStrict = RunProcess(
                "powershell",
                [
                    "-NoLogo",
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    scriptPath,
                    "-Mode",
                    "ActiveStrict",
                ],
                repositoryRoot);

            Assert.Equal(0, activeStrict.ExitCode);
            Assert.Contains("ActiveStrict: no active legacy references found.", activeStrict.StandardOutput, StringComparison.Ordinal);
        }

        private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ProcessResult(process.ExitCode, standardOutput, standardError);
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot().FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
            {
                current = current.Parent;
            }

            Assert.NotNull(current);
            return current!;
        }

        private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
    }
}

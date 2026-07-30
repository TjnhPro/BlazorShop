namespace BlazorShop.AI.StorefrontReverseEngineering.Cli;

public sealed class CommandOptions
{
    private readonly Dictionary<string, string?> values;

    private CommandOptions(Dictionary<string, string?> values)
    {
        this.values = values;
    }

    public static CommandOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[++index];
            }
            else
            {
                values[key] = null;
            }
        }

        return new CommandOptions(values);
    }

    public bool HasFlag(string key) => values.ContainsKey(key);

    public string? GetOptional(string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public string GetRequired(string key, string errorCode)
    {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"[{errorCode}] Missing required option. Problem: --{key} was not provided. Cause: this command needs explicit project inputs. Fix: pass --{key} <value>.");
    }
}

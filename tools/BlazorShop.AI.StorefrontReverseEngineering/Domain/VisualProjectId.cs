using System.Text;

namespace BlazorShop.AI.StorefrontReverseEngineering.Domain;

public readonly record struct VisualProjectId(string Value)
{
    public static VisualProjectId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder = new StringBuilder();
        var pendingSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                pendingSeparator = false;
                continue;
            }

            pendingSeparator = builder.Length > 0;
        }

        var normalized = builder.ToString().Trim('-');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Visual project id must contain at least one ASCII letter or digit.", nameof(value));
        }

        return new VisualProjectId(normalized.Length <= 80 ? normalized : normalized[..80].Trim('-'));
    }

    public override string ToString() => Value;
}

using System.Text.RegularExpressions;

namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public static partial class SensitiveValueRedactor
{
    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = AuthorizationRegex().Replace(value, "$1[redacted]");
        redacted = CookieRegex().Replace(redacted, "$1[redacted]");
        redacted = TokenRegex().Replace(redacted, "$1=[redacted]");
        return redacted;
    }

    [GeneratedRegex("(Authorization\\s*[:=]\\s*)([^\\r\\n]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AuthorizationRegex();

    [GeneratedRegex("(Cookie\\s*[:=]\\s*)([^\\r\\n]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CookieRegex();

    [GeneratedRegex("(?i)(access_token|refresh_token|api_key|password|secret)=([^\\s&]+)", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}

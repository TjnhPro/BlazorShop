namespace BlazorShop.Application.CommerceNode.Messages
{
    using System.Net;
    using System.Text.RegularExpressions;

    public sealed class MessageTokenRenderer : IMessageTokenRenderer
    {
        private static readonly Regex TokenRegex = new(
            @"\{\{\s*(?<name>[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9]*)*)\s*\}\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        public MessageTokenRenderResult Render(MessageTokenRenderRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var tokens = new Dictionary<string, string?>(
                request.Tokens ?? new Dictionary<string, string?>(),
                StringComparer.Ordinal);
            var safeHtmlTokens = request.SafeHtmlTokens is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(request.SafeHtmlTokens, StringComparer.Ordinal);
            var requiredTokens = request.RequiredTokens is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(request.RequiredTokens, StringComparer.Ordinal);
            var unknownTokens = new SortedSet<string>(StringComparer.Ordinal);
            var warnings = new List<MessageTokenRenderWarning>();

            foreach (var requiredToken in requiredTokens)
            {
                if (!tokens.TryGetValue(requiredToken, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    warnings.Add(new MessageTokenRenderWarning(
                        "message_token.required_missing",
                        requiredToken,
                        $"Required message token '{requiredToken}' is missing."));
                }
            }

            var rendered = TokenRegex.Replace(
                request.Template ?? string.Empty,
                match =>
                {
                    var tokenName = match.Groups["name"].Value;
                    if (!tokens.TryGetValue(tokenName, out var value))
                    {
                        if (unknownTokens.Add(tokenName))
                        {
                            warnings.Add(new MessageTokenRenderWarning(
                                "message_token.unknown",
                                tokenName,
                                $"Message token '{tokenName}' is not available."));
                        }

                        return match.Value;
                    }

                    return safeHtmlTokens.Contains(tokenName)
                        ? value ?? string.Empty
                        : WebUtility.HtmlEncode(value ?? string.Empty);
                });

            return new MessageTokenRenderResult(
                rendered,
                warnings,
                unknownTokens.ToArray(),
                warnings
                    .Where(warning => string.Equals(warning.Code, "message_token.required_missing", StringComparison.Ordinal))
                    .Select(warning => warning.TokenName)
                    .ToArray());
        }
    }
}

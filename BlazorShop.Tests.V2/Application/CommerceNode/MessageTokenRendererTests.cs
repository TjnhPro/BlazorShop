namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;

    using Xunit;

    public sealed class MessageTokenRendererTests
    {
        [Fact]
        public void Render_ReplacesKnownTokensDeterministically()
        {
            var renderer = new MessageTokenRenderer();

            var result = renderer.Render(new MessageTokenRenderRequest(
                "Order {{Order.Reference}} total {{Order.Total}} {{Order.Currency}}.",
                new Dictionary<string, string?>
                {
                    ["Order.Currency"] = "USD",
                    ["Order.Reference"] = "ORD-1",
                    ["Order.Total"] = "25.00",
                }));

            Assert.Equal("Order ORD-1 total 25.00 USD.", result.Rendered);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Render_EncodesUnsafeUserInputByDefault()
        {
            var renderer = new MessageTokenRenderer();

            var result = renderer.Render(new MessageTokenRenderRequest(
                "<p>{{Contact.Message}}</p>",
                new Dictionary<string, string?>
                {
                    ["Contact.Message"] = "<script>alert(1)</script>",
                }));

            Assert.Equal("<p>&lt;script&gt;alert(1)&lt;/script&gt;</p>", result.Rendered);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Render_AllowsCodeMarkedSafeTokensWithoutEncoding()
        {
            var renderer = new MessageTokenRenderer();

            var result = renderer.Render(new MessageTokenRenderRequest(
                "<a href=\"{{Account.ActivationUrl}}\">Confirm</a>",
                new Dictionary<string, string?>
                {
                    ["Account.ActivationUrl"] = "https://store.example/confirm?token=abc&email=a@example.test",
                },
                SafeHtmlTokens: new HashSet<string>(StringComparer.Ordinal)
                {
                    "Account.ActivationUrl",
                }));

            Assert.Equal("<a href=\"https://store.example/confirm?token=abc&email=a@example.test\">Confirm</a>", result.Rendered);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Render_ReportsUnknownTokensAndLeavesThemVisible()
        {
            var renderer = new MessageTokenRenderer();

            var result = renderer.Render(new MessageTokenRenderRequest(
                "Hello {{Customer.FullName}} {{Customer.Secret}}",
                new Dictionary<string, string?>
                {
                    ["Customer.FullName"] = "Customer One",
                }));

            Assert.Equal("Hello Customer One {{Customer.Secret}}", result.Rendered);
            Assert.Equal(["Customer.Secret"], result.UnknownTokens);
            var warning = Assert.Single(result.Warnings);
            Assert.Equal("message_token.unknown", warning.Code);
            Assert.Equal("Customer.Secret", warning.TokenName);
        }

        [Fact]
        public void Render_ReportsMissingRequiredTokens()
        {
            var renderer = new MessageTokenRenderer();

            var result = renderer.Render(new MessageTokenRenderRequest(
                "Reset {{Account.PasswordResetUrl}}",
                new Dictionary<string, string?>
                {
                    ["Customer.Email"] = "customer@example.test",
                },
                RequiredTokens: new HashSet<string>(StringComparer.Ordinal)
                {
                    "Account.PasswordResetUrl",
                    "Customer.Email",
                }));

            Assert.Equal(["Account.PasswordResetUrl"], result.MissingRequiredTokens);
            Assert.Contains(result.Warnings, warning => warning.Code == "message_token.required_missing"
                && warning.TokenName == "Account.PasswordResetUrl");
        }

        [Theory]
        [InlineData("{{Customer.GetType()}}")]
        [InlineData("{{#each Orders}}")]
        [InlineData("{{Customer['Email']}}")]
        [InlineData("{{ System.Environment }}")]
        public void Render_DoesNotExecuteOrResolveExpressions(string expression)
        {
            var renderer = new MessageTokenRenderer();

            var result = renderer.Render(new MessageTokenRenderRequest(
                $"Value {expression}",
                new Dictionary<string, string?>
                {
                    ["Customer.Email"] = "customer@example.test",
                }));

            Assert.Equal($"Value {expression}", result.Rendered);
        }
    }
}

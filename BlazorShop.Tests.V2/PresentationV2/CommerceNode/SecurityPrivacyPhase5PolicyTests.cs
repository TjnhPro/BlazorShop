namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;

    using Xunit;

    public sealed class SecurityPrivacyPhase5PolicyTests
    {
        [Fact]
        public void SecurityPrivacyOptions_DefaultsDeclareRetentionPolicy()
        {
            var options = new SecurityPrivacyOptions();

            Assert.Equal(30, options.RefreshTokenIpRetentionDays);
            Assert.Equal(30, options.RefreshTokenUserAgentRetentionDays);
            Assert.Equal(365, options.ConsentEventRetentionDays);
            Assert.Equal(30, options.CaptchaVerificationLogRetentionDays);
            Assert.Equal(365, options.NewsletterConsentEvidenceRetentionDays);
            Assert.True(options.AnonymizeIpAfterRetentionWindow);
        }

        [Theory]
        [InlineData(" 203.0.113.42 ", "203.0.113.42")]
        [InlineData("", null)]
        [InlineData("   ", null)]
        public void NormalizeIpAddress_TrimsAndDropsEmptyValues(string? value, string? expected)
        {
            Assert.Equal(expected, PrivacyDataSanitizer.NormalizeIpAddress(value));
        }

        [Fact]
        public void NormalizeUserAgent_TruncatesLongValues()
        {
            var userAgent = new string('a', PrivacyDataSanitizer.DefaultMaxUserAgentLength + 20);

            var normalized = PrivacyDataSanitizer.NormalizeUserAgent(userAgent);

            Assert.NotNull(normalized);
            Assert.Equal(PrivacyDataSanitizer.DefaultMaxUserAgentLength, normalized!.Length);
        }

        [Theory]
        [InlineData("203.0.113.42", "203.0.113.0")]
        [InlineData("2001:db8:abcd:12:3456:789a:1:2", "2001:db8:abcd:12::")]
        [InlineData("not-an-ip", "not-an-ip")]
        public void AnonymizeIpAddress_RemovesHostPortionWhenParseable(string value, string expected)
        {
            Assert.Equal(expected, PrivacyDataSanitizer.AnonymizeIpAddress(value));
        }

        [Fact]
        public void TokenManagers_UseSanitizerBeforePersistingIpAndUserAgent()
        {
            var commerceTokenManager = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Repositories/CommerceNodeAppTokenManager.cs");

            Assert.Contains("PrivacyDataSanitizer.NormalizeIpAddress(createdByIp)", commerceTokenManager, StringComparison.Ordinal);
            Assert.Contains("PrivacyDataSanitizer.NormalizeUserAgent(userAgent)", commerceTokenManager, StringComparison.Ordinal);
            Assert.Contains("PrivacyDataSanitizer.NormalizeIpAddress(revokedByIp)", commerceTokenManager, StringComparison.Ordinal);
            Assert.False(RepositoryFileExists("BlazorShop.Infrastructure/Repositories/Authentication/AppTokenManager.cs"));
        }

        [Fact]
        public void AuthenticationService_LoginUsesGenericInvalidCredentialMessages()
        {
            var authenticationService = ReadRepositoryFile("BlazorShop.Application/Services/Authentication/AuthenticationService.cs");

            Assert.Contains("Invalid credentials.", authenticationService, StringComparison.Ordinal);
            Assert.DoesNotContain("User does not exist", authenticationService, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Email not found", authenticationService, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password is incorrect", authenticationService, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
        }

        private static bool RepositoryFileExists(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return true;
                }

                directory = directory.Parent;
            }

            return false;
        }
    }
}

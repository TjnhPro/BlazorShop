namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Extensions.DependencyInjection;

    using Xunit;

    public sealed class StoreEmailSecretProtectorTests
    {
        [Fact]
        public void Protect_RoundTripsSecretWithoutReturningPlaintext()
        {
            var services = new ServiceCollection()
                .AddDataProtection()
                .UseEphemeralDataProtectionProvider()
                .Services
                .BuildServiceProvider();
            var protector = new DataProtectionStoreEmailSecretProtector(
                services.GetRequiredService<IDataProtectionProvider>());

            var protectedSecret = protector.Protect("smtp-secret-value");
            var roundTrip = protector.Unprotect(protectedSecret);

            Assert.NotEqual("smtp-secret-value", protectedSecret);
            Assert.DoesNotContain("smtp-secret-value", protectedSecret, StringComparison.Ordinal);
            Assert.Equal("smtp-secret-value", roundTrip);
        }

        [Fact]
        public void Interface_ExposesOnlyProtectAndUnprotectOperations()
        {
            var methodNames = typeof(IStoreEmailSecretProtector)
                .GetMethods()
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(["Protect", "Unprotect"], methodNames);
        }
    }
}

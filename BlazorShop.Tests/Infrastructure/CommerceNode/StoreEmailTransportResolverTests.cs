namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    using Xunit;

    public sealed class StoreEmailTransportResolverTests
    {
        [Fact]
        public async Task ResolveTransportAsync_UsesStoreSpecificSmtpSettings()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeA, "a");
            SeedStore(context, storeB, "b");
            context.StoreEmailSettings.Add(CreateSettings(
                storeA,
                "smtp-a.example.test",
                "sender-a@example.test",
                "protected:password-a"));
            context.StoreEmailSettings.Add(CreateSettings(
                storeB,
                "smtp-b.example.test",
                "sender-b@example.test",
                "protected:password-b"));
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context);

            var resultA = await resolver.ResolveTransportAsync(storeA);
            var resultB = await resolver.ResolveTransportAsync(storeB);

            Assert.True(resultA.Success, resultA.Message);
            Assert.True(resultB.Success, resultB.Message);
            Assert.Equal("smtp-a.example.test", resultA.Transport!.SmtpHost);
            Assert.Equal("sender-a@example.test", resultA.Transport.FromEmail);
            Assert.Equal("password-a", resultA.Transport.Password);
            Assert.Equal("smtp-b.example.test", resultB.Transport!.SmtpHost);
            Assert.Equal("sender-b@example.test", resultB.Transport.FromEmail);
            Assert.Equal("password-b", resultB.Transport.Password);
        }

        [Fact]
        public async Task ResolveSenderProfileAsync_PrefersStoreSender()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, "default");
            context.StoreEmailSettings.Add(CreateSettings(
                storeId,
                "smtp.example.test",
                " sender@example.test ",
                "protected:password"));
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context);

            var sender = await resolver.ResolveSenderProfileAsync(storeId);

            Assert.True(sender.FromStoreSettings);
            Assert.Equal("sender@example.test", sender.FromEmail);
            Assert.Equal("Store Sender", sender.FromName);
            Assert.Equal("reply@example.test", sender.ReplyToEmail);
        }

        [Fact]
        public async Task ResolveTransportAsync_WhenMissingStoreSettingsAndFallbackDisabled_ReturnsNotConfigured()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, "default");
            await context.SaveChangesAsync();
            var resolver = CreateResolver(
                context,
                options: new StoreEmailTransportOptions { AllowGlobalEmailSettingsFallback = false },
                global: new EmailSettings
                {
                    From = "global@example.test",
                    SmtpServer = "smtp-global.example.test",
                    Port = 587,
                });

            var result = await resolver.ResolveTransportAsync(storeId);

            Assert.False(result.Success);
            Assert.Equal("message_delivery.smtp_not_configured", result.ErrorCode);
        }

        [Fact]
        public async Task ResolveTransportAsync_WhenFallbackEnabled_UsesGlobalEmailSettings()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, "default");
            await context.SaveChangesAsync();
            var resolver = CreateResolver(
                context,
                options: new StoreEmailTransportOptions { AllowGlobalEmailSettingsFallback = true },
                global: new EmailSettings
                {
                    From = "global@example.test",
                    DisplayName = "Global Sender",
                    SmtpServer = "smtp-global.example.test",
                    Port = 2525,
                    UseSsl = false,
                });

            var result = await resolver.ResolveTransportAsync(storeId);

            Assert.True(result.Success, result.Message);
            Assert.Equal("global@example.test", result.Transport!.FromEmail);
            Assert.Equal("smtp-global.example.test", result.Transport.SmtpHost);
            Assert.Equal(2525, result.Transport.SmtpPort);
        }

        private static StoreEmailTransportResolver CreateResolver(
            CommerceNodeDbContext context,
            StoreEmailTransportOptions? options = null,
            EmailSettings? global = null)
        {
            return new StoreEmailTransportResolver(
                context,
                new FakeSecretProtector(),
                Options.Create(global ?? new EmailSettings()),
                Options.Create(options ?? new StoreEmailTransportOptions()));
        }

        private static StoreEmailSettings CreateSettings(
            Guid storeId,
            string smtpHost,
            string fromEmail,
            string protectedPassword)
        {
            return new StoreEmailSettings
            {
                StoreId = storeId,
                Enabled = true,
                SmtpHost = smtpHost,
                SmtpPort = 587,
                UseSsl = true,
                Username = "smtp-user",
                ProtectedPassword = protectedPassword,
                FromEmail = fromEmail,
                FromDisplayName = "Store Sender",
                ReplyToEmail = "reply@example.test",
                DeliveryMode = StoreEmailDeliveryModes.Smtp,
            };
        }

        private static void SeedStore(CommerceNodeDbContext context, Guid storeId, string key)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = key,
                Name = key,
                SupportEmail = $"support-{key}@example.test",
            });
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"store-email-transport-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class FakeSecretProtector : IStoreEmailSecretProtector
        {
            public string Protect(string secret)
            {
                return $"protected:{secret}";
            }

            public string Unprotect(string protectedSecret)
            {
                return protectedSecret.Replace("protected:", string.Empty, StringComparison.Ordinal);
            }
        }
    }
}

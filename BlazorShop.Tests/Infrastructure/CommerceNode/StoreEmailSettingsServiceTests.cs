namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StoreEmailSettingsServiceTests
    {
        [Fact]
        public async Task UpdateAsync_RotatesPasswordWithoutEchoingSecret()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore { Id = storeId, StoreKey = "default", Name = "Default" });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateAsync(
                storeId,
                new UpdateStoreEmailSettingsRequest
                {
                    Enabled = true,
                    DeliveryMode = StoreEmailDeliveryModes.Smtp,
                    SmtpHost = " smtp.example.test ",
                    SmtpPort = 587,
                    UseSsl = true,
                    Username = " smtp-user ",
                    Password = "smtp-secret-value",
                    UseExistingPassword = false,
                    FromEmail = " sender@example.test ",
                    FromDisplayName = " Store Sender ",
                    ReplyToEmail = " reply@example.test ",
                },
                "actor-1",
                captureModeAllowed: false);

            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Payload);
            Assert.True(result.Payload!.SecretsConfigured);
            Assert.Equal("smtp.example.test", result.Payload.SmtpHost);
            Assert.Equal("smtp-user", result.Payload.Username);
            Assert.Equal("sender@example.test", result.Payload.FromEmail);
            Assert.Equal("actor-1", result.Payload.UpdatedByUserId);

            var settings = await context.StoreEmailSettings.SingleAsync();
            Assert.Equal("protected:smtp-secret-value", settings.ProtectedPassword);
            Assert.NotNull(settings.PasswordUpdatedAtUtc);

            var responseText = System.Text.Json.JsonSerializer.Serialize(result.Payload);
            Assert.DoesNotContain("smtp-secret-value", responseText, StringComparison.Ordinal);
            Assert.DoesNotContain("protected:smtp-secret-value", responseText, StringComparison.Ordinal);
        }

        [Fact]
        public async Task UpdateAsync_ClearPasswordRemovesSecretAndPasswordTimestamp()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore { Id = storeId, StoreKey = "default", Name = "Default" });
            context.StoreEmailSettings.Add(new StoreEmailSettings
            {
                StoreId = storeId,
                Enabled = true,
                SmtpHost = "smtp.example.test",
                FromEmail = "sender@example.test",
                ProtectedPassword = "protected:old-secret",
                PasswordUpdatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateAsync(
                storeId,
                new UpdateStoreEmailSettingsRequest
                {
                    Enabled = false,
                    DeliveryMode = StoreEmailDeliveryModes.Smtp,
                    SmtpHost = "smtp.example.test",
                    SmtpPort = 587,
                    FromEmail = "sender@example.test",
                    ClearPassword = true,
                },
                "actor-2",
                captureModeAllowed: false);

            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Payload);
            Assert.False(result.Payload!.SecretsConfigured);
            Assert.Null(result.Payload.PasswordUpdatedAtUtc);

            var settings = await context.StoreEmailSettings.SingleAsync();
            Assert.Null(settings.ProtectedPassword);
            Assert.Null(settings.PasswordUpdatedAtUtc);
            Assert.Equal("actor-2", settings.UpdatedByUserId);
        }

        [Fact]
        public async Task UpdateAsync_BlocksCaptureModeUnlessAllowed()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore { Id = storeId, StoreKey = "default", Name = "Default" });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateAsync(
                storeId,
                new UpdateStoreEmailSettingsRequest
                {
                    Enabled = true,
                    DeliveryMode = StoreEmailDeliveryModes.Capture,
                    FromEmail = "sender@example.test",
                },
                "actor-1",
                captureModeAllowed: false);

            Assert.False(result.Success);
            Assert.Contains("Capture delivery mode is not allowed", result.Message, StringComparison.Ordinal);
            Assert.Empty(context.StoreEmailSettings);
        }

        private static StoreEmailSettingsService CreateService(CommerceNodeDbContext context)
        {
            return new StoreEmailSettingsService(context, new FakeSecretProtector());
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"store-email-settings-{Guid.NewGuid():N}")
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

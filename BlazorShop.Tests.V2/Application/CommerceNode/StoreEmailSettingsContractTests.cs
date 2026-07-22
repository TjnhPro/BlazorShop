namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Xunit;

    public sealed class StoreEmailSettingsContractTests
    {
        [Fact]
        public void StoreEmailSettingsResponse_DoesNotExposeRawSmtpPassword()
        {
            var propertyNames = typeof(StoreEmailSettingsResponse)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.Contains("SecretsConfigured", propertyNames);
            Assert.Contains("PasswordUpdatedAtUtc", propertyNames);
            Assert.DoesNotContain("Password", propertyNames);
            Assert.DoesNotContain("SmtpPassword", propertyNames);
            Assert.DoesNotContain("ProtectedPassword", propertyNames);
        }

        [Fact]
        public void EmailSettingsOperationResponses_DoNotExposeSmtpPassword()
        {
            var responseTypes = new[]
            {
                typeof(StoreEmailSettingsResponse),
                typeof(SendStoreEmailTestResponse),
            };

            foreach (var responseType in responseTypes)
            {
                var propertyNames = responseType
                    .GetProperties()
                    .Select(property => property.Name)
                    .ToArray();

                Assert.DoesNotContain("Password", propertyNames);
                Assert.DoesNotContain("SmtpPassword", propertyNames);
                Assert.DoesNotContain("ProtectedPassword", propertyNames);
            }
        }

        [Fact]
        public void UpdateStoreEmailSettingsRequest_KeepsPasswordWriteOnlyRequestSide()
        {
            var responsePropertyNames = typeof(StoreEmailSettingsResponse)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();
            var requestPropertyNames = typeof(UpdateStoreEmailSettingsRequest)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.Contains("Password", requestPropertyNames);
            Assert.Contains("ClearPassword", requestPropertyNames);
            Assert.Contains("UseExistingPassword", requestPropertyNames);
            Assert.Contains("Password", typeof(RotateStoreEmailPasswordRequest).GetProperties().Select(property => property.Name));
            Assert.DoesNotContain("Password", responsePropertyNames);
        }

        [Fact]
        public void Validator_BlocksIncompleteEnabledSmtpConfig()
        {
            var request = new UpdateStoreEmailSettingsRequest
            {
                Enabled = true,
                DeliveryMode = StoreEmailDeliveryModes.Smtp,
                SmtpPort = 587,
            };

            var result = StoreEmailSettingsRequestValidator.Validate(
                request,
                new StoreEmailSettingsValidationContext(ExistingSecretConfigured: false));

            Assert.False(result.Success);
            Assert.Contains("SMTP host is required when store email is enabled.", result.Errors);
            Assert.Contains("From email is required when store email is enabled.", result.Errors);
            Assert.Contains("SMTP password is required when SMTP delivery is enabled.", result.Errors);
        }

        [Fact]
        public void Validator_AllowsEnabledSmtpWithExistingPassword()
        {
            var request = new UpdateStoreEmailSettingsRequest
            {
                Enabled = true,
                DeliveryMode = StoreEmailDeliveryModes.Smtp,
                SmtpHost = "smtp.example.test",
                SmtpPort = 587,
                FromEmail = "sender@example.test",
                UseExistingPassword = true,
            };

            var result = StoreEmailSettingsRequestValidator.Validate(
                request,
                new StoreEmailSettingsValidationContext(ExistingSecretConfigured: true));

            Assert.True(result.Success);
        }

        [Fact]
        public void Validator_BlocksCaptureModeWhenNotAllowed()
        {
            var request = new UpdateStoreEmailSettingsRequest
            {
                Enabled = true,
                DeliveryMode = StoreEmailDeliveryModes.Capture,
                FromEmail = "sender@example.test",
            };

            var result = StoreEmailSettingsRequestValidator.Validate(
                request,
                new StoreEmailSettingsValidationContext(CaptureModeAllowed: false));

            Assert.False(result.Success);
            Assert.Contains("Capture delivery mode is not allowed in this environment.", result.Errors);
        }
    }
}

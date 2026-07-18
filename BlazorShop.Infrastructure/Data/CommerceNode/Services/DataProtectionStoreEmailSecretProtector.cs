namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;

    using Microsoft.AspNetCore.DataProtection;

    public sealed class DataProtectionStoreEmailSecretProtector : IStoreEmailSecretProtector
    {
        private const string Purpose = "BlazorShop.CommerceNode.StoreEmailSettings.SmtpPassword.v1";

        private readonly IDataProtector protector;

        public DataProtectionStoreEmailSecretProtector(IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProtectionProvider);
            this.protector = dataProtectionProvider.CreateProtector(Purpose);
        }

        public string Protect(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException("Secret is required.", nameof(secret));
            }

            return this.protector.Protect(secret);
        }

        public string Unprotect(string protectedSecret)
        {
            if (string.IsNullOrWhiteSpace(protectedSecret))
            {
                throw new ArgumentException("Protected secret is required.", nameof(protectedSecret));
            }

            return this.protector.Unprotect(protectedSecret);
        }
    }
}

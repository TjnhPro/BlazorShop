namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Identity
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<string>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserToken<string>> entity)
        {
            entity.Property(token => token.LoginProvider).HasMaxLength(128);
            entity.Property(token => token.Name).HasMaxLength(128);
        }
    }
}

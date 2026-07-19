namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Identity
{
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> entity)
        {
            entity.Property(token => token.TokenHash).HasMaxLength(64);
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property(token => token.CreatedByIp).HasMaxLength(64);
            entity.Property(token => token.RevokedByIp).HasMaxLength(64);
            entity.Property(token => token.UserAgent).HasMaxLength(512);

            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.RevokedAtUtc });

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

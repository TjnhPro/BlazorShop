namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Identity
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class IdentityRoleSeedConfiguration : IEntityTypeConfiguration<IdentityRole>
    {
        public void Configure(EntityTypeBuilder<IdentityRole> entity)
        {
            entity.HasData(
                new IdentityRole
                {
                    Id = "93f5cdac-43de-4895-8426-2048c228e76d",
                    ConcurrencyStamp = "02d86d56-8e63-4d2e-92f8-81b154ba0532",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                },
                new IdentityRole
                {
                    Id = "b7af6842-02fa-4af4-8f61-ae04a49644a2",
                    ConcurrencyStamp = "75e8afa8-8df5-4431-a220-ac56b1fd0cda",
                    Name = "User",
                    NormalizedName = "USER",
                });
        }
    }
}

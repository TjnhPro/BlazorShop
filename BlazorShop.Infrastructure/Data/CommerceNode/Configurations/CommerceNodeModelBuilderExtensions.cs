namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations
{
    using Microsoft.EntityFrameworkCore;

    internal static class CommerceNodeModelBuilderExtensions
    {
        private const string CommerceNodeConfigurationNamespace = "BlazorShop.Infrastructure.Data.CommerceNode.Configurations";

        public static ModelBuilder ApplyCommerceNodeConfigurations(this ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CommerceNodeDbContext).Assembly,
                type => type.Namespace?.StartsWith(CommerceNodeConfigurationNamespace, StringComparison.Ordinal) == true);

            return modelBuilder;
        }
    }
}

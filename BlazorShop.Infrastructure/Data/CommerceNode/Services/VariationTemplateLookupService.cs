namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.EntityFrameworkCore;

    public sealed class VariationTemplateLookupService : IVariationTemplateLookupService
    {
        private readonly CommerceNodeDbContext context;

        public VariationTemplateLookupService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> IsActiveTemplateInStoreAsync(
            Guid templateId,
            Guid? storeId,
            CancellationToken cancellationToken = default)
        {
            if (templateId == Guid.Empty || !storeId.HasValue || storeId.Value == Guid.Empty)
            {
                return false;
            }

            return await this.context.VariationTemplates
                .AsNoTracking()
                .AnyAsync(
                    template => template.Id == templateId
                        && template.StoreId == storeId.Value
                        && template.IsActive,
                    cancellationToken);
        }
    }
}

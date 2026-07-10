namespace BlazorShop.Application.CommerceNode.VariationTemplates
{
    public interface IVariationTemplateLookupService
    {
        Task<bool> IsActiveTemplateInStoreAsync(Guid templateId, Guid? storeId, CancellationToken cancellationToken = default);
    }
}

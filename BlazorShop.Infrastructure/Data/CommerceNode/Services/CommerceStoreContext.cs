namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    public sealed class CommerceStoreContext : ICommerceStoreContext
    {
        private readonly IStoreExecutionContextAccessor contextAccessor;

        public CommerceStoreContext(IStoreExecutionContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
            CancellationToken cancellationToken = default)
        {
            var context = this.contextAccessor.Current;
            if (context is null)
            {
                return Task.FromResult(Failed<CommerceCurrentStore>("Store execution context is required."));
            }

            return Task.FromResult(ApplicationResult<CommerceCurrentStore>.Succeeded(
                context.CurrentStore,
                "Current store resolved."));
        }

        public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(
            CancellationToken cancellationToken = default)
        {
            var context = this.contextAccessor.Current;
            if (context is null)
            {
                return Task.FromResult(Failed<Guid>("Store execution context is required."));
            }

            if (!context.IsActive
                || !string.Equals(context.Status, CommerceStoreStatuses.Active, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(ApplicationResult<Guid>.Failed(
                    ApplicationError.NotFound("store.not_found", "Store was not found.")));
            }

            return Task.FromResult(ApplicationResult<Guid>.Succeeded(context.StoreId, "Current store id resolved."));
        }

        private static ApplicationResult<TPayload> Failed<TPayload>(string message)
        {
            return ApplicationResult<TPayload>.Failed(ApplicationError.Validation("store.validation", message));
        }
    }
}

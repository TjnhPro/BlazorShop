namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodePaymentMethodRepository : IPaymentMethod
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodePaymentMethodRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync()
        {
            return await this.context.PaymentMethods.AsNoTracking().ToListAsync();
        }
    }
}

namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCustomerService : IStorefrontCustomerService
    {
        private readonly CommerceNodeDbContext context;

        public StorefrontCustomerService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<ServiceResponse<StorefrontCustomerProfile>> ResolveOrCreateAsync(
            StorefrontCustomerResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            var email = NormalizeEmail(request.Email);
            if (email is null)
            {
                return Failed(ServiceResponseType.ValidationError, "Customer email is required.");
            }

            var normalizedEmail = NormalizeEmailKey(email);
            var existing = await this.LoadCustomerAsync(request.StoreId, normalizedEmail, cancellationToken);
            if (existing is not null)
            {
                var changed = ApplyProfileUpdate(existing, request, email, DateTimeOffset.UtcNow);
                if (changed)
                {
                    await this.context.SaveChangesAsync(cancellationToken);
                }

                return Succeeded("Customer resolved.", Map(existing));
            }

            var now = DateTimeOffset.UtcNow;
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = request.StoreId,
                Email = email,
                NormalizedEmail = normalizedEmail,
                FullName = NormalizeNullable(request.FullName) ?? email,
                Phone = NormalizeNullable(request.Phone),
                AppUserId = NormalizeNullable(request.AppUserId),
                CreatedAt = now,
                UpdatedAt = now,
                LastCheckoutAt = now,
            };

            this.context.CommerceCustomers.Add(customer);

            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
                return Succeeded("Customer created.", Map(customer));
            }
            catch (DbUpdateException)
            {
                this.context.Entry(customer).State = EntityState.Detached;
                var reloaded = await this.LoadCustomerAsync(request.StoreId, normalizedEmail, cancellationToken);
                if (reloaded is null)
                {
                    return Failed(ServiceResponseType.Conflict, "Customer could not be created.");
                }

                ApplyProfileUpdate(reloaded, request, email, DateTimeOffset.UtcNow);
                await this.context.SaveChangesAsync(cancellationToken);
                return Succeeded("Customer resolved.", Map(reloaded));
            }
        }

        private async Task<CommerceCustomer?> LoadCustomerAsync(
            Guid storeId,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return await this.context.CommerceCustomers.FirstOrDefaultAsync(
                customer => customer.StoreId == storeId && customer.NormalizedEmail == normalizedEmail,
                cancellationToken);
        }

        private static bool ApplyProfileUpdate(
            CommerceCustomer customer,
            StorefrontCustomerResolutionRequest request,
            string email,
            DateTimeOffset now)
        {
            if (!string.Equals(customer.Email, email, StringComparison.Ordinal))
            {
                customer.Email = email;
            }

            var fullName = NormalizeNullable(request.FullName);
            if (fullName is not null && !string.Equals(customer.FullName, fullName, StringComparison.Ordinal))
            {
                customer.FullName = fullName;
            }

            var phone = NormalizeNullable(request.Phone);
            if (phone is not null && !string.Equals(customer.Phone, phone, StringComparison.Ordinal))
            {
                customer.Phone = phone;
            }

            var appUserId = NormalizeNullable(request.AppUserId);
            if (appUserId is not null && string.IsNullOrWhiteSpace(customer.AppUserId))
            {
                customer.AppUserId = appUserId;
            }

            customer.LastCheckoutAt = now;
            customer.UpdatedAt = now;
            return true;
        }

        private static StorefrontCustomerProfile Map(CommerceCustomer customer)
        {
            return new StorefrontCustomerProfile(
                customer.Id,
                customer.StoreId,
                customer.AppUserId,
                customer.Email,
                customer.NormalizedEmail,
                customer.FullName,
                customer.Phone,
                customer.CreatedAt,
                customer.UpdatedAt,
                customer.LastCheckoutAt);
        }

        private static string? NormalizeEmail(string? email)
        {
            var normalized = NormalizeNullable(email);
            return normalized is null ? null : normalized.ToLowerInvariant();
        }

        private static string NormalizeEmailKey(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<StorefrontCustomerProfile> Succeeded(
            string message,
            StorefrontCustomerProfile profile)
        {
            return new ServiceResponse<StorefrontCustomerProfile>(true, message, profile.Id)
            {
                Payload = profile,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StorefrontCustomerProfile> Failed(
            ServiceResponseType responseType,
            string message)
        {
            return new ServiceResponse<StorefrontCustomerProfile>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}

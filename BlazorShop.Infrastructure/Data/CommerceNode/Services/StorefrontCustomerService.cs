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
            var firstName = NormalizeNullable(request.FirstName);
            var lastName = NormalizeNullable(request.LastName);
            var customer = new CommerceCustomer
            {
                Id = Guid.NewGuid(),
                StoreId = request.StoreId,
                Email = email,
                NormalizedEmail = normalizedEmail,
                FullName = ResolveFullName(request.FullName, firstName, lastName, email),
                FirstName = firstName,
                LastName = lastName,
                Company = NormalizeNullable(request.Company),
                Phone = NormalizeNullable(request.Phone),
                PreferredLanguage = NormalizeNullable(request.PreferredLanguage),
                PreferredCurrencyCode = NormalizeCurrencyCode(request.PreferredCurrencyCode),
                IsActive = true,
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

        public async Task<ServiceResponse<StorefrontCustomerProfile>> GetOrCreateAuthenticatedProfileAsync(
            StorefrontAuthenticatedCustomerProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateAuthenticatedRequest(request.StoreId, request.AppUserId, request.Email);
            if (validation is not null)
            {
                return validation;
            }

            var email = NormalizeEmail(request.Email)!;
            var appUserId = NormalizeNullable(request.AppUserId)!;
            var normalizedEmail = NormalizeEmailKey(email);
            var customer = await this.LoadAuthenticatedCustomerAsync(request.StoreId, appUserId, normalizedEmail, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            if (customer is null)
            {
                customer = new CommerceCustomer
                {
                    Id = Guid.NewGuid(),
                    StoreId = request.StoreId,
                    AppUserId = appUserId,
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    FullName = ResolveFullName(request.FullName, null, null, email),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastActivityAtUtc = now,
                };
                this.context.CommerceCustomers.Add(customer);
            }
            else
            {
                if (!customer.IsActive)
                {
                    return Failed(ServiceResponseType.Conflict, "Customer account is disabled.");
                }

                if (string.IsNullOrWhiteSpace(customer.AppUserId))
                {
                    customer.AppUserId = appUserId;
                }

                if (!string.Equals(customer.Email, email, StringComparison.Ordinal))
                {
                    customer.Email = email;
                    customer.NormalizedEmail = normalizedEmail;
                }

                var fullName = NormalizeNullable(request.FullName);
                if (fullName is not null && !string.Equals(customer.FullName, fullName, StringComparison.Ordinal))
                {
                    customer.FullName = fullName;
                }

                customer.UpdatedAt = now;
                customer.LastActivityAtUtc = now;
            }

            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Failed(ServiceResponseType.Conflict, "Customer profile could not be resolved.");
            }

            return Succeeded("Customer profile returned.", Map(customer));
        }

        public async Task<ServiceResponse<StorefrontCustomerProfile>> UpdateAuthenticatedProfileAsync(
            StorefrontCustomerProfileUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateAuthenticatedRequest(request.StoreId, request.AppUserId, request.Email);
            if (validation is not null)
            {
                return validation;
            }

            var fullName = NormalizeNullable(request.FullName);
            if (fullName is null)
            {
                return Failed(ServiceResponseType.ValidationError, "Full name is required.");
            }

            var email = NormalizeEmail(request.Email)!;
            var appUserId = NormalizeNullable(request.AppUserId)!;
            var normalizedEmail = NormalizeEmailKey(email);
            var customer = await this.LoadAuthenticatedCustomerAsync(request.StoreId, appUserId, normalizedEmail, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            if (customer is null)
            {
                customer = new CommerceCustomer
                {
                    Id = Guid.NewGuid(),
                    StoreId = request.StoreId,
                    AppUserId = appUserId,
                    CreatedAt = now,
                    IsActive = true,
                };
                this.context.CommerceCustomers.Add(customer);
            }
            else if (!customer.IsActive)
            {
                return Failed(ServiceResponseType.Conflict, "Customer account is disabled.");
            }

            customer.Email = email;
            customer.NormalizedEmail = normalizedEmail;
            customer.FullName = fullName;
            customer.FirstName = NormalizeNullable(request.FirstName);
            customer.LastName = NormalizeNullable(request.LastName);
            customer.Company = NormalizeNullable(request.Company);
            customer.Phone = NormalizeNullable(request.Phone);
            customer.PreferredLanguage = NormalizeNullable(request.PreferredLanguage);
            customer.PreferredCurrencyCode = NormalizeCurrencyCode(request.PreferredCurrencyCode);
            customer.UpdatedAt = now;
            customer.LastActivityAtUtc = now;

            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Failed(ServiceResponseType.Conflict, "Customer profile could not be updated.");
            }

            return Succeeded("Customer profile updated.", Map(customer));
        }

        public async Task<ServiceResponse<StorefrontCustomerProfile>> TouchLastActivityAsync(
            Guid storeId,
            string appUserId,
            DateTimeOffset? activityAtUtc = null,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            var normalizedAppUserId = NormalizeNullable(appUserId);
            if (normalizedAppUserId is null)
            {
                return Failed(ServiceResponseType.ValidationError, "Authenticated user is required.");
            }

            var customer = await this.context.CommerceCustomers.FirstOrDefaultAsync(
                item => item.StoreId == storeId && item.AppUserId == normalizedAppUserId,
                cancellationToken);

            if (customer is null)
            {
                return Failed(ServiceResponseType.NotFound, "Customer was not found.");
            }

            var now = activityAtUtc ?? DateTimeOffset.UtcNow;
            customer.LastActivityAtUtc = now;
            customer.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded("Customer activity updated.", Map(customer));
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

        private async Task<CommerceCustomer?> LoadAuthenticatedCustomerAsync(
            Guid storeId,
            string appUserId,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return await this.context.CommerceCustomers.FirstOrDefaultAsync(
                customer => customer.StoreId == storeId
                    && (customer.AppUserId == appUserId || customer.NormalizedEmail == normalizedEmail),
                cancellationToken);
        }

        private static ServiceResponse<StorefrontCustomerProfile>? ValidateAuthenticatedRequest(
            Guid storeId,
            string appUserId,
            string email)
        {
            if (storeId == Guid.Empty)
            {
                return Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (NormalizeNullable(appUserId) is null)
            {
                return Failed(ServiceResponseType.ValidationError, "Authenticated user is required.");
            }

            if (NormalizeEmail(email) is null)
            {
                return Failed(ServiceResponseType.ValidationError, "Customer email is required.");
            }

            return null;
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
            var firstName = NormalizeNullable(request.FirstName);
            var lastName = NormalizeNullable(request.LastName);
            var resolvedFullName = fullName ?? ResolveFullName(null, firstName, lastName, null);
            if (resolvedFullName is not null && !string.Equals(customer.FullName, resolvedFullName, StringComparison.Ordinal))
            {
                customer.FullName = resolvedFullName;
            }

            if (firstName is not null && !string.Equals(customer.FirstName, firstName, StringComparison.Ordinal))
            {
                customer.FirstName = firstName;
            }

            if (lastName is not null && !string.Equals(customer.LastName, lastName, StringComparison.Ordinal))
            {
                customer.LastName = lastName;
            }

            var company = NormalizeNullable(request.Company);
            if (company is not null && !string.Equals(customer.Company, company, StringComparison.Ordinal))
            {
                customer.Company = company;
            }

            var phone = NormalizeNullable(request.Phone);
            if (phone is not null && !string.Equals(customer.Phone, phone, StringComparison.Ordinal))
            {
                customer.Phone = phone;
            }

            var preferredLanguage = NormalizeNullable(request.PreferredLanguage);
            if (preferredLanguage is not null && !string.Equals(customer.PreferredLanguage, preferredLanguage, StringComparison.Ordinal))
            {
                customer.PreferredLanguage = preferredLanguage;
            }

            var preferredCurrencyCode = NormalizeCurrencyCode(request.PreferredCurrencyCode);
            if (preferredCurrencyCode is not null && !string.Equals(customer.PreferredCurrencyCode, preferredCurrencyCode, StringComparison.Ordinal))
            {
                customer.PreferredCurrencyCode = preferredCurrencyCode;
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
                customer.FirstName,
                customer.LastName,
                customer.Company,
                customer.Phone,
                customer.PreferredLanguage,
                customer.PreferredCurrencyCode,
                customer.IsActive,
                customer.LastActivityAtUtc,
                customer.CreatedAt,
                customer.UpdatedAt,
                customer.LastCheckoutAt);
        }

        private static string ResolveFullName(
            string? fullName,
            string? firstName,
            string? lastName,
            string? fallback)
        {
            var normalizedFullName = NormalizeNullable(fullName);
            if (normalizedFullName is not null)
            {
                return normalizedFullName;
            }

            var combinedName = string.Join(
                " ",
                new[] { firstName, lastName }.Where(item => !string.IsNullOrWhiteSpace(item)));
            return string.IsNullOrWhiteSpace(combinedName) ? fallback ?? string.Empty : combinedName;
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToUpperInvariant();
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

namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCustomerAddressService : IStorefrontCustomerAddressService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontCustomerService customerService;
        private readonly IAddressValidationService validationService;

        public StorefrontCustomerAddressService(
            CommerceNodeDbContext context,
            IStorefrontCustomerService customerService,
            IAddressValidationService validationService)
        {
            this.context = context;
            this.customerService = customerService;
            this.validationService = validationService;
        }

        public async Task<ServiceResponse<IReadOnlyList<CustomerAddressDto>>> ListAsync(
            StorefrontCustomerAddressContext context,
            CancellationToken cancellationToken = default)
        {
            var customer = await this.ResolveExistingCustomerAsync(context, cancellationToken);
            if (customer is null)
            {
                return Succeeded<IReadOnlyList<CustomerAddressDto>>("Customer addresses loaded.", []);
            }

            var addresses = await this.context.CommerceCustomerAddresses
                .AsNoTracking()
                .Where(address =>
                    address.StoreId == context.StoreId
                    && address.CustomerId == customer.Id
                    && address.DeletedAtUtc == null)
                .OrderByDescending(address => address.IsDefaultShipping)
                .ThenByDescending(address => address.IsDefaultBilling)
                .ThenBy(address => address.CreatedAtUtc)
                .Select(address => Map(address))
                .ToArrayAsync(cancellationToken);

            return Succeeded<IReadOnlyList<CustomerAddressDto>>("Customer addresses loaded.", addresses);
        }

        public async Task<ServiceResponse<CustomerAddressDto>> CreateAsync(
            StorefrontCustomerAddressContext context,
            CustomerAddressCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var customerResponse = await this.ResolveOrCreateCustomerAsync(context, cancellationToken);
            if (!customerResponse.Success || customerResponse.Payload is null)
            {
                return Failed<CustomerAddressDto>(customerResponse.ResponseType, customerResponse.Message ?? "Customer could not be resolved.");
            }

            var validation = this.validationService.ValidateAndNormalize(request);
            if (!validation.IsValid)
            {
                return Failed<CustomerAddressDto>(ServiceResponseType.ValidationError, FormatValidationMessage(validation.Issues));
            }

            var normalized = validation.Address;
            var now = DateTimeOffset.UtcNow;
            var activeAddresses = await this.LoadActiveAddressesAsync(context.StoreId, customerResponse.Payload.Id, cancellationToken);
            var firstAddress = activeAddresses.Count == 0;
            var address = new CommerceCustomerAddress
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = context.StoreId,
                CustomerId = customerResponse.Payload.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                IsDefaultShipping = normalized.IsDefaultShipping || firstAddress,
                IsDefaultBilling = normalized.IsDefaultBilling || firstAddress,
            };

            ApplyAddress(address, normalized, now);
            await this.ClearDefaultSelectionAsync(activeAddresses, Guid.Empty, address.IsDefaultShipping, address.IsDefaultBilling, now, cancellationToken);
            this.context.CommerceCustomerAddresses.Add(address);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded("Customer address created.", Map(address));
        }

        public async Task<ServiceResponse<CustomerAddressDto>> UpdateAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CustomerAddressCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var customer = await this.ResolveExistingCustomerAsync(context, cancellationToken);
            if (customer is null)
            {
                return Failed<CustomerAddressDto>(ServiceResponseType.NotFound, "Customer address was not found.");
            }

            var address = await this.LoadOwnedAddressAsync(context.StoreId, customer.Id, addressPublicId, cancellationToken);
            if (address is null)
            {
                return Failed<CustomerAddressDto>(ServiceResponseType.NotFound, "Customer address was not found.");
            }

            var validation = this.validationService.ValidateAndNormalize(new CustomerAddressUpdateRequest(
                addressPublicId,
                request.FirstName,
                request.LastName,
                request.Company,
                request.Address1,
                request.Address2,
                request.City,
                request.PostalCode,
                request.CountryCode,
                request.StateProvinceCode,
                request.StateProvinceName,
                request.Phone,
                request.Email,
                request.IsDefaultShipping,
                request.IsDefaultBilling));

            if (!validation.IsValid)
            {
                return Failed<CustomerAddressDto>(ServiceResponseType.ValidationError, FormatValidationMessage(validation.Issues));
            }

            var normalized = validation.Address;
            var now = DateTimeOffset.UtcNow;
            var activeAddresses = await this.LoadActiveAddressesAsync(context.StoreId, customer.Id, cancellationToken);
            await this.ClearDefaultSelectionAsync(activeAddresses, address.Id, normalized.IsDefaultShipping, normalized.IsDefaultBilling, now, cancellationToken);
            ApplyAddress(address, normalized, now);
            address.IsDefaultShipping = normalized.IsDefaultShipping;
            address.IsDefaultBilling = normalized.IsDefaultBilling;
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded("Customer address updated.", Map(address));
        }

        public async Task<ServiceResponse> DeleteAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CancellationToken cancellationToken = default)
        {
            var customer = await this.ResolveExistingCustomerAsync(context, cancellationToken);
            if (customer is null)
            {
                return Failed(ServiceResponseType.NotFound, "Customer address was not found.");
            }

            var address = await this.LoadOwnedAddressAsync(context.StoreId, customer.Id, addressPublicId, cancellationToken);
            if (address is null)
            {
                return Failed(ServiceResponseType.NotFound, "Customer address was not found.");
            }

            var wasDefaultShipping = address.IsDefaultShipping;
            var wasDefaultBilling = address.IsDefaultBilling;
            address.DeletedAtUtc = DateTimeOffset.UtcNow;
            address.UpdatedAtUtc = address.DeletedAtUtc.Value;
            address.IsDefaultShipping = false;
            address.IsDefaultBilling = false;

            if (wasDefaultShipping || wasDefaultBilling)
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }

            await PromoteDefaultsAfterDeleteAsync(
                context.StoreId,
                customer.Id,
                address.Id,
                wasDefaultShipping,
                wasDefaultBilling,
                cancellationToken);

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Customer address deleted.");
        }

        public async Task<ServiceResponse<CustomerAddressDto>> SetDefaultShippingAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CancellationToken cancellationToken = default)
        {
            return await this.SetDefaultAsync(context, addressPublicId, shipping: true, cancellationToken);
        }

        public async Task<ServiceResponse<CustomerAddressDto>> SetDefaultBillingAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CancellationToken cancellationToken = default)
        {
            return await this.SetDefaultAsync(context, addressPublicId, shipping: false, cancellationToken);
        }

        private async Task<ServiceResponse<CustomerAddressDto>> SetDefaultAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            bool shipping,
            CancellationToken cancellationToken)
        {
            var customer = await this.ResolveExistingCustomerAsync(context, cancellationToken);
            if (customer is null)
            {
                return Failed<CustomerAddressDto>(ServiceResponseType.NotFound, "Customer address was not found.");
            }

            var address = await this.LoadOwnedAddressAsync(context.StoreId, customer.Id, addressPublicId, cancellationToken);
            if (address is null)
            {
                return Failed<CustomerAddressDto>(ServiceResponseType.NotFound, "Customer address was not found.");
            }

            var activeAddresses = await this.LoadActiveAddressesAsync(context.StoreId, customer.Id, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            await this.ClearDefaultSelectionAsync(activeAddresses, address.Id, clearShipping: shipping, clearBilling: !shipping, now, cancellationToken);
            address.UpdatedAtUtc = now;
            if (shipping)
            {
                address.IsDefaultShipping = true;
            }
            else
            {
                address.IsDefaultBilling = true;
            }

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded(shipping ? "Default shipping address updated." : "Default billing address updated.", Map(address));
        }

        private async Task<CommerceCustomer?> ResolveExistingCustomerAsync(
            StorefrontCustomerAddressContext context,
            CancellationToken cancellationToken)
        {
            if (context.StoreId == Guid.Empty || string.IsNullOrWhiteSpace(context.AppUserId))
            {
                return null;
            }

            var customer = await this.context.CommerceCustomers.FirstOrDefaultAsync(
                candidate => candidate.StoreId == context.StoreId && candidate.AppUserId == context.AppUserId,
                cancellationToken);

            if (customer is not null || string.IsNullOrWhiteSpace(context.Email))
            {
                return customer;
            }

            var normalizedEmail = NormalizeEmailKey(context.Email);
            customer = await this.context.CommerceCustomers.FirstOrDefaultAsync(
                candidate => candidate.StoreId == context.StoreId && candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);

            if (customer is not null && string.IsNullOrWhiteSpace(customer.AppUserId))
            {
                customer.AppUserId = context.AppUserId;
                customer.UpdatedAt = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return customer;
        }

        private async Task<ServiceResponse<StorefrontCustomerProfile>> ResolveOrCreateCustomerAsync(
            StorefrontCustomerAddressContext context,
            CancellationToken cancellationToken)
        {
            var existing = await this.ResolveExistingCustomerAsync(context, cancellationToken);
            if (existing is not null)
            {
                return new ServiceResponse<StorefrontCustomerProfile>(true, "Customer resolved.", existing.Id)
                {
                    Payload = MapCustomer(existing),
                    ResponseType = ServiceResponseType.Success,
                };
            }

            if (context.StoreId == Guid.Empty || string.IsNullOrWhiteSpace(context.AppUserId))
            {
                return Failed<StorefrontCustomerProfile>(ServiceResponseType.ValidationError, "Customer identity was not found.");
            }

            if (string.IsNullOrWhiteSpace(context.Email))
            {
                return Failed<StorefrontCustomerProfile>(ServiceResponseType.ValidationError, "Customer email claim is required.");
            }

            return await this.customerService.ResolveOrCreateAsync(
                new StorefrontCustomerResolutionRequest(
                    context.StoreId,
                    context.Email,
                    context.FullName,
                    AppUserId: context.AppUserId),
                cancellationToken);
        }

        private async Task<List<CommerceCustomerAddress>> LoadActiveAddressesAsync(
            Guid storeId,
            Guid customerId,
            CancellationToken cancellationToken)
        {
            return await this.context.CommerceCustomerAddresses
                .Where(address =>
                    address.StoreId == storeId
                    && address.CustomerId == customerId
                    && address.DeletedAtUtc == null)
                .OrderBy(address => address.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        private async Task<CommerceCustomerAddress?> LoadOwnedAddressAsync(
            Guid storeId,
            Guid customerId,
            Guid addressPublicId,
            CancellationToken cancellationToken)
        {
            return await this.context.CommerceCustomerAddresses.FirstOrDefaultAsync(
                address =>
                    address.StoreId == storeId
                    && address.CustomerId == customerId
                    && address.PublicId == addressPublicId
                    && address.DeletedAtUtc == null,
                cancellationToken);
        }

        private static void ApplyAddress(
            CommerceCustomerAddress address,
            CustomerAddressCreateRequest request,
            DateTimeOffset now)
        {
            address.FirstName = request.FirstName;
            address.LastName = request.LastName;
            address.Company = request.Company;
            address.Address1 = request.Address1;
            address.Address2 = request.Address2;
            address.City = request.City;
            address.PostalCode = request.PostalCode;
            address.CountryCode = request.CountryCode;
            address.StateProvinceCode = request.StateProvinceCode;
            address.StateProvinceName = request.StateProvinceName;
            address.Phone = request.Phone;
            address.Email = request.Email;
            address.UpdatedAtUtc = now;
        }

        private async Task ClearDefaultSelectionAsync(
            IEnumerable<CommerceCustomerAddress> addresses,
            Guid selectedAddressId,
            bool clearShipping,
            bool clearBilling,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var changed = false;
            foreach (var existing in addresses)
            {
                if (existing.Id == selectedAddressId)
                {
                    continue;
                }

                if (clearShipping && existing.IsDefaultShipping)
                {
                    existing.IsDefaultShipping = false;
                    existing.UpdatedAtUtc = now;
                    changed = true;
                }

                if (clearBilling && existing.IsDefaultBilling)
                {
                    existing.IsDefaultBilling = false;
                    existing.UpdatedAtUtc = now;
                    changed = true;
                }
            }

            if (changed)
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task PromoteDefaultsAfterDeleteAsync(
            Guid storeId,
            Guid customerId,
            Guid deletedAddressId,
            bool promoteShipping,
            bool promoteBilling,
            CancellationToken cancellationToken)
        {
            if (!promoteShipping && !promoteBilling)
            {
                return;
            }

            var remaining = await this.context.CommerceCustomerAddresses
                .Where(address =>
                    address.StoreId == storeId
                    && address.CustomerId == customerId
                    && address.Id != deletedAddressId
                    && address.DeletedAtUtc == null)
                .OrderBy(address => address.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (promoteShipping && remaining.All(address => !address.IsDefaultShipping))
            {
                var nextShipping = remaining.FirstOrDefault();
                if (nextShipping is not null)
                {
                    nextShipping.IsDefaultShipping = true;
                    nextShipping.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            if (promoteBilling && remaining.All(address => !address.IsDefaultBilling))
            {
                var nextBilling = remaining.FirstOrDefault();
                if (nextBilling is not null)
                {
                    nextBilling.IsDefaultBilling = true;
                    nextBilling.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }
        }

        private static CustomerAddressDto Map(CommerceCustomerAddress address)
        {
            return new CustomerAddressDto(
                address.PublicId,
                address.FirstName,
                address.LastName,
                address.Company,
                address.Address1,
                address.Address2,
                address.City,
                address.PostalCode,
                address.CountryCode,
                address.StateProvinceCode,
                address.StateProvinceName,
                address.Phone,
                address.Email,
                address.IsDefaultShipping,
                address.IsDefaultBilling,
                address.CreatedAtUtc,
                address.UpdatedAtUtc);
        }

        private static StorefrontCustomerProfile MapCustomer(CommerceCustomer customer)
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

        private static string NormalizeEmailKey(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private static string FormatValidationMessage(IReadOnlyList<AddressValidationIssue> issues)
        {
            return issues.Count == 0
                ? "Address validation failed."
                : string.Join(" ", issues.Select(issue => $"{issue.Field}:{issue.Code}"));
        }

        private static ServiceResponse Succeeded(string message)
        {
            return new ServiceResponse(true, message);
        }

        private static ServiceResponse<TPayload> Succeeded<TPayload>(string message, TPayload payload)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failed<TPayload>(ServiceResponseType responseType, string message)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static ServiceResponse Failed(ServiceResponseType responseType, string message)
        {
            return new ServiceResponse(false, message)
            {
                Payload = responseType,
            };
        }
    }
}

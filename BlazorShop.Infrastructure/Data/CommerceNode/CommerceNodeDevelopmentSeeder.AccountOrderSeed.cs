namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed partial class CommerceNodeDevelopmentSeeder
    {
        private async Task<CommerceCustomer> EnsureCustomerAsync(
            Guid storeId,
            Guid customerId,
            Guid shippingAddressId,
            Guid? billingAddressId,
            string email,
            string password,
            string fullName,
            CancellationToken cancellationToken)
        {
            var user = await this.userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    FullName = fullName,
                    CreatedOn = DateTime.UtcNow,
                };

                var result = await this.userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var message = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not seed storefront QA user {email}: {message}");
                }
            }
            else
            {
                user.EmailConfirmed = true;
                user.FullName = fullName;
                user.UserName = email;
                user.Email = email;
                await this.userManager.UpdateAsync(user);
            }

            if (!await this.userManager.CheckPasswordAsync(user, password))
            {
                var resetToken = await this.userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await this.userManager.ResetPasswordAsync(user, resetToken, password);
                if (!resetResult.Succeeded)
                {
                    var message = string.Join("; ", resetResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not reset storefront QA user password for {email}: {message}");
                }
            }

            if (!await this.userManager.IsInRoleAsync(user, StorefrontQaUserRole))
            {
                var roleResult = await this.userManager.AddToRoleAsync(user, StorefrontQaUserRole);
                if (!roleResult.Succeeded)
                {
                    var message = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not assign storefront QA user role for {email}: {message}");
                }
            }

            var normalizedEmail = email.ToUpperInvariant();
            var customer = await this.dbContext.CommerceCustomers.FirstOrDefaultAsync(
                item => item.StoreId == storeId && item.NormalizedEmail == normalizedEmail,
                cancellationToken);
            if (customer is null)
            {
                customer = new CommerceCustomer
                {
                    Id = customerId,
                    StoreId = storeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.CommerceCustomers.Add(customer);
            }

            customer.AppUserId = user.Id;
            customer.Email = email;
            customer.NormalizedEmail = normalizedEmail;
            customer.FullName = fullName;
            customer.FirstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;
            customer.LastName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "Customer";
            customer.Company = "QA Synthetic";
            customer.Phone = "+1 555 0101";
            customer.PreferredLanguage = "en-US";
            customer.PreferredCurrencyCode = "EUR";
            customer.IsActive = true;
            customer.LastActivityAtUtc = DateTimeOffset.UtcNow;
            customer.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureCustomerAddressAsync(
                shippingAddressId,
                storeId,
                customer.Id,
                "QA",
                "Customer",
                isDefaultShipping: true,
                isDefaultBilling: billingAddressId is null,
                email,
                cancellationToken);

            if (billingAddressId is { } secondaryAddressId)
            {
                await this.EnsureCustomerAddressAsync(
                    secondaryAddressId,
                    storeId,
                    customer.Id,
                    "QA",
                    "Billing",
                    isDefaultShipping: false,
                    isDefaultBilling: true,
                    email,
                    cancellationToken);
            }

            return customer;
        }

        private async Task EnsureCustomerAddressAsync(
            Guid id,
            Guid storeId,
            Guid customerId,
            string firstName,
            string lastName,
            bool isDefaultShipping,
            bool isDefaultBilling,
            string email,
            CancellationToken cancellationToken)
        {
            var address = await this.dbContext.CommerceCustomerAddresses
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (address is null)
            {
                address = new CommerceCustomerAddress
                {
                    Id = id,
                    PublicId = id,
                    StoreId = storeId,
                    CustomerId = customerId,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                };
                this.dbContext.CommerceCustomerAddresses.Add(address);
            }

            address.StoreId = storeId;
            address.CustomerId = customerId;
            address.FirstName = firstName;
            address.LastName = lastName;
            address.Company = "QA Synthetic";
            address.Address1 = "1 QA Street";
            address.Address2 = isDefaultBilling ? "Billing Suite" : "Shipping Suite";
            address.City = "San Francisco";
            address.StateProvinceCode = "CA";
            address.StateProvinceName = "California";
            address.PostalCode = "94105";
            address.CountryCode = "US";
            address.Phone = "+1 555 0101";
            address.Email = email;
            address.IsDefaultShipping = isDefaultShipping;
            address.IsDefaultBilling = isDefaultBilling;
            address.DeletedAtUtc = null;
            address.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureSampleOrderAsync(CommerceStore store, CommerceCustomer customer, CancellationToken cancellationToken)
        {
            var reference = store.StoreKey == DefaultStoreKey ? "QA-CATALOG-SNAPSHOT" : $"QA-{store.StoreKey.ToUpperInvariant()}-SNAPSHOT";
            await this.EnsureSampleOrderAsync(store, customer, reference, cancellationToken);
        }

        private async Task EnsureSampleOrderAsync(
            CommerceStore store,
            CommerceCustomer customer,
            string reference,
            CancellationToken cancellationToken)
        {
            if (await this.dbContext.Orders.AnyAsync(order => order.Reference == reference, cancellationToken))
            {
                return;
            }

            this.dbContext.Orders.Add(new Order
            {
                StoreId = store.Id,
                StorePublicId = store.PublicId,
                StoreKeySnapshot = store.StoreKey,
                StoreNameSnapshot = store.Name,
                StoreBaseUrlSnapshot = store.BaseUrl,
                StoreCompanyNameSnapshot = store.CompanyName,
                StoreCompanyEmailSnapshot = store.CompanyEmail,
                StoreCompanyPhoneSnapshot = store.CompanyPhone,
                StoreCompanyAddressSnapshot = store.CompanyAddress,
                UserId = customer.AppUserId ?? "qa-seed-user",
                CustomerId = customer.Id,
                Reference = reference,
                OrderStatus = OrderStatuses.Complete,
                PaymentStatus = PaymentStatuses.Paid,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                PaymentAt = DateTime.UtcNow,
                PaymentMetadataJson = JsonSerializer.Serialize(new { handler = PaymentMethodKeys.Cod, mode = "seed" }),
                CurrencyCode = "EUR",
                TotalAmount = 19.99m,
                SubtotalAmount = 19.99m,
                ShippingTotalAmount = 0m,
                TaxTotalAmount = 0m,
                DiscountTotalAmount = 0m,
                GrandTotalAmount = 19.99m,
                BaseCurrencyCode = "EUR",
                BaseTotalAmount = 19.99m,
                BaseSubtotalAmount = 19.99m,
                BaseShippingTotalAmount = 0m,
                BaseTaxTotalAmount = 0m,
                BaseDiscountTotalAmount = 0m,
                BaseGrandTotalAmount = 19.99m,
                CreatedOn = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CustomerName = customer.FullName,
                CustomerEmail = customer.Email,
                ShippingFullName = customer.FullName,
                ShippingEmail = customer.Email,
                ShippingAddress1 = "1 QA Street",
                ShippingCity = "QA City",
                ShippingPostalCode = "10000",
                ShippingCountryCode = "US",
                ShippingStatus = ShippingStatuses.NotYetShipped,
                ShippingMethodKey = "free-standard",
                ShippingProviderSystemName = "internal",
                ShippingMethodCode = "free-standard",
                ShippingMethodName = "Free standard shipping",
                ShippingTotal = 0m,
                ShippingCurrencyCode = "EUR",
                ShippingDeliveryEstimateText = "3-5 business days",
                Lines =
                [
                    new OrderLine
                    {
                        ProductId = TshirtProductId,
                        ProductName = "Catalog QA T-Shirt",
                        Sku = "QA-TSHIRT-RED-M",
                        Image = "/images/banner-bg.jpg",
                        ProductVariantId = TshirtRedMVariantId,
                        VariantAttributesJson = JsonSerializer.Serialize(
                            new[]
                            {
                                new VariantAttributeSeed("Color", "Red"),
                                new VariantAttributeSeed("Size", "M"),
                            }),
                        Quantity = 1,
                        UnitPrice = 19.99m,
                    },
                ],
            });

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

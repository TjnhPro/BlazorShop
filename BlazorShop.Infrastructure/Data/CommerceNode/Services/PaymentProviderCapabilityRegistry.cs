namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;

    public sealed class PaymentProviderCapabilityRegistry : IPaymentProviderCapabilityRegistry
    {
        private readonly IReadOnlyDictionary<string, PaymentProviderDescriptor> descriptors;

        public PaymentProviderCapabilityRegistry(IEnumerable<IStorefrontPaymentProvider> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            var providerDescriptors = providers.Select(ValidateDescriptor).ToArray();
            var duplicate = providerDescriptors
                .GroupBy(descriptor => Normalize(descriptor.SystemName), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Duplicate payment provider descriptor '{duplicate.Key}'.");
            }

            this.descriptors = providerDescriptors.ToDictionary(
                descriptor => Normalize(descriptor.SystemName),
                StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<PaymentProviderCapabilityDto> List()
        {
            return this.descriptors.Values
                .OrderBy(descriptor => descriptor.DefaultDisplayOrder)
                .ThenBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(ToCapability)
                .ToArray();
        }

        public ServiceResponse<PaymentProviderCapabilityDto> Get(string systemName)
        {
            var normalized = Normalize(systemName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Failure("Payment provider is required.");
            }

            return this.descriptors.TryGetValue(normalized, out var descriptor)
                ? new ServiceResponse<PaymentProviderCapabilityDto>(true, "Payment provider capability loaded.")
                {
                    Payload = ToCapability(descriptor),
                    ResponseType = ServiceResponseType.Success,
                }
                : Failure("Payment provider is not supported.");
        }

        private static PaymentProviderDescriptor ValidateDescriptor(IStorefrontPaymentProvider provider)
        {
            var descriptor = provider.Descriptor;
            var providerKey = Normalize(provider.ProviderKey);
            var descriptorKey = Normalize(descriptor.SystemName);
            if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(descriptorKey))
            {
                throw new InvalidOperationException("Payment provider key and descriptor system name are required.");
            }

            if (!string.Equals(providerKey, descriptorKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Payment provider '{provider.ProviderKey}' descriptor system name '{descriptor.SystemName}' does not match.");
            }

            return descriptor;
        }

        private static PaymentProviderCapabilityDto ToCapability(PaymentProviderDescriptor descriptor)
        {
            return new PaymentProviderCapabilityDto(
                descriptor.SystemName,
                Installed: true,
                Active: descriptor.ActiveByDefault,
                descriptor.DisplayName,
                descriptor.Description,
                descriptor.IconUrl,
                descriptor.DefaultDisplayOrder,
                SupportedStoreIds: [],
                descriptor.SupportedCurrencyCodes,
                descriptor.SupportedCountryCodes,
                descriptor.MinOrderTotal,
                descriptor.MaxOrderTotal,
                descriptor.MethodType,
                descriptor.RecurringCapable,
                descriptor.SupportsAuthorize,
                descriptor.SupportsCapture,
                descriptor.SupportsVoid,
                descriptor.SupportsRefund,
                descriptor.SupportsPartialRefund,
                descriptor.RequiresWebhookSignature);
        }

        private static ServiceResponse<PaymentProviderCapabilityDto> Failure(string message)
        {
            return new ServiceResponse<PaymentProviderCapabilityDto>(false, message)
            {
                ResponseType = ServiceResponseType.ValidationError,
            };
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}

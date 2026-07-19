namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Domain.Entities.CommerceNode;

    internal static class CheckoutSessionStateRules
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static bool IsActive(string? state)
        {
            return string.Equals(state, CheckoutSessionStates.Draft, StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, CheckoutSessionStates.Ready, StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, CheckoutSessionStates.OrderPending, StringComparison.OrdinalIgnoreCase);
        }

        public static void Touch(
            CheckoutSession session,
            string state,
            string currentStep,
            DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(session);

            session.State = state;
            session.CurrentStep = currentStep;
            session.CheckoutVersion = Math.Max(1, session.CheckoutVersion) + 1;
            session.UpdatedAtUtc = now;
        }

        public static void MarkExpired(CheckoutSession session, DateTimeOffset now)
        {
            Touch(session, CheckoutSessionStates.Expired, CheckoutSteps.Entry, now);
            session.NextAction = "expired";
        }

        public static string ResolveNextRequiredStep(
            bool hasBillingAddress,
            bool hasShippingAddress,
            bool hasSelectedShippingOption,
            bool hasSelectedPaymentMethod,
            bool shippingRequired = true)
        {
            if (!hasBillingAddress)
            {
                return CheckoutSteps.BillingAddress;
            }

            if (shippingRequired && !hasShippingAddress)
            {
                return CheckoutSteps.ShippingAddress;
            }

            if (shippingRequired && !hasSelectedShippingOption)
            {
                return CheckoutSteps.ShippingMethod;
            }

            return hasSelectedPaymentMethod ? CheckoutSteps.Review : CheckoutSteps.PaymentMethod;
        }

        public static IReadOnlyList<string> ParseCompletedSteps(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions)
                    ?.Where(step => CheckoutSteps.All.Contains(step))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray() ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}

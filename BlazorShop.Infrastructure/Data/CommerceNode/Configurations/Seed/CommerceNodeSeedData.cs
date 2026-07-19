namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Seed
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Domain.Entities.CommerceNode;

    internal static class CommerceNodeSeedData
    {
        public static IReadOnlyList<MessageTemplate> CreateDefaultMessageTemplates()
        {
            var createdAt = new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.Zero);
            return
            [
                CreateMessageTemplate(
                    "11111111-1111-1111-1111-000000000001",
                    "11111111-1111-1111-1111-100000000001",
                    TransactionalMessageTemplateSystemNames.AccountActivation,
                    "Confirm your {{Store.Name}} account",
                    "<p>Hello {{Customer.FullName}},</p><p>Confirm your account: <a href=\"{{Account.ActivationUrl}}\">Confirm email</a></p>",
                    "Default customer account activation template.",
                    createdAt),
                CreateMessageTemplate(
                    "11111111-1111-1111-1111-000000000002",
                    "11111111-1111-1111-1111-100000000002",
                    TransactionalMessageTemplateSystemNames.PasswordRecovery,
                    "Reset your {{Store.Name}} password",
                    "<p>Hello {{Customer.FullName}},</p><p>Reset your password: <a href=\"{{Account.PasswordResetUrl}}\">Reset password</a></p>",
                    "Default customer password recovery template.",
                    createdAt),
                CreateMessageTemplate(
                    "11111111-1111-1111-1111-000000000003",
                    "11111111-1111-1111-1111-100000000003",
                    TransactionalMessageTemplateSystemNames.OrderPlaced,
                    "{{Store.Name}} order {{Order.Reference}} confirmed",
                    "<p>Thanks for your order from {{Store.Name}}.</p><p>Order: {{Order.Reference}}</p><p>Total: {{Order.Total}} {{Order.Currency}}</p><p>View your order: <a href=\"{{Order.DetailUrl}}\">{{Order.DetailUrl}}</a></p>",
                    "Default order placed confirmation template.",
                    createdAt),
                CreateMessageTemplate(
                    "11111111-1111-1111-1111-000000000004",
                    "11111111-1111-1111-1111-100000000004",
                    TransactionalMessageTemplateSystemNames.OrderPaymentStatusChanged,
                    "Payment update for {{Order.Reference}}",
                    "<p>Your payment status is now {{Order.PaymentStatus}}.</p>",
                    "Default order payment status notification template.",
                    createdAt),
                CreateMessageTemplate(
                    "11111111-1111-1111-1111-000000000005",
                    "11111111-1111-1111-1111-100000000005",
                    TransactionalMessageTemplateSystemNames.OrderFulfillmentStatusChanged,
                    "Shipping update for {{Order.Reference}}",
                    "<p>Your shipping status is now {{Order.ShippingStatus}}.</p><p>Tracking: {{Shipment.TrackingNumber}}</p>",
                    "Default order fulfillment status notification template.",
                    createdAt),
                CreateMessageTemplate(
                    "11111111-1111-1111-1111-000000000006",
                    "11111111-1111-1111-1111-100000000006",
                    TransactionalMessageTemplateSystemNames.StorefrontContactForm,
                    "Contact form: {{Contact.Subject}}",
                    "<p>From: {{Contact.Name}} ({{Contact.Email}})</p><p>{{Contact.Message}}</p>",
                    "Default storefront contact form delivery template.",
                    createdAt),
            ];
        }

        private static MessageTemplate CreateMessageTemplate(
            string id,
            string publicId,
            string systemName,
            string subject,
            string body,
            string description,
            DateTimeOffset createdAt)
        {
            return new MessageTemplate
            {
                Id = Guid.Parse(id),
                PublicId = Guid.Parse(publicId),
                SystemName = systemName,
                StoreId = null,
                LanguageCode = null,
                SubjectTemplate = subject,
                BodyHtmlTemplate = body,
                IsActive = true,
                Description = description,
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt,
            };
        }
    }
}

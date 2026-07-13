namespace BlazorShop.Domain.Constants
{
    public static class PaymentStatuses
    {
        public const string Pending = "pending";
        public const string Authorized = "authorized";
        public const string Paid = "paid";
        public const string PartiallyRefunded = "partially_refunded";
        public const string Refunded = "refunded";
        public const string Voided = "voided";
    }
}

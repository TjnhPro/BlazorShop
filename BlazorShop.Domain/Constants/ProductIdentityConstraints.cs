namespace BlazorShop.Domain.Constants
{
    public static class ProductIdentityConstraints
    {
        public const int GtinMaxLength = 32;
        public const int BarcodeMaxLength = 64;
        public const int ManufacturerPartNumberMaxLength = 128;
        public const int ConditionMaxLength = 32;

        public const string New = "new";
        public const string Used = "used";
        public const string Refurbished = "refurbished";

        public static readonly string[] Conditions =
        [
            New,
            Used,
            Refurbished,
        ];
    }
}

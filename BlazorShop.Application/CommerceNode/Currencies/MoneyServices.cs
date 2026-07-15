namespace BlazorShop.Application.CommerceNode.Currencies
{
    public sealed record CurrencyMetadata(
        string CurrencyCode,
        int DecimalDigits);

    public interface ICurrencyMetadataService
    {
        CurrencyMetadata Get(string currencyCode);
    }

    public interface IMoneyRoundingService
    {
        decimal RoundUnitPrice(decimal amount, string currencyCode);

        decimal RoundLineTotal(decimal amount, string currencyCode);

        decimal RoundOrderTotal(decimal amount, string currencyCode);

        decimal RoundPaymentAmount(decimal amount, string currencyCode);
    }

    public interface IPaymentMinorUnitConverter
    {
        long ToMinorUnits(decimal amount, string currencyCode);
    }

    public sealed class CurrencyMetadataService : ICurrencyMetadataService
    {
        private static readonly IReadOnlyDictionary<string, int> DecimalDigitsByCode =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["BHD"] = 3,
                ["CLP"] = 0,
                ["EUR"] = 2,
                ["ISK"] = 0,
                ["JPY"] = 0,
                ["KRW"] = 0,
                ["KWD"] = 3,
                ["OMR"] = 3,
                ["TWD"] = 0,
                ["USD"] = 2,
                ["VND"] = 0,
            };

        public CurrencyMetadata Get(string currencyCode)
        {
            var normalized = NormalizeCurrencyCode(currencyCode);
            return new CurrencyMetadata(
                normalized,
                DecimalDigitsByCode.TryGetValue(normalized, out var decimalDigits) ? decimalDigits : 2);
        }

        private static string NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } ? normalized : "USD";
        }
    }

    public sealed class MoneyRoundingService : IMoneyRoundingService
    {
        private readonly ICurrencyMetadataService currencyMetadataService;

        public MoneyRoundingService(ICurrencyMetadataService currencyMetadataService)
        {
            this.currencyMetadataService = currencyMetadataService;
        }

        public decimal RoundUnitPrice(decimal amount, string currencyCode)
        {
            return this.Round(amount, currencyCode);
        }

        public decimal RoundLineTotal(decimal amount, string currencyCode)
        {
            return this.Round(amount, currencyCode);
        }

        public decimal RoundOrderTotal(decimal amount, string currencyCode)
        {
            return this.Round(amount, currencyCode);
        }

        public decimal RoundPaymentAmount(decimal amount, string currencyCode)
        {
            return this.Round(amount, currencyCode);
        }

        private decimal Round(decimal amount, string currencyCode)
        {
            var metadata = this.currencyMetadataService.Get(currencyCode);
            return decimal.Round(amount, metadata.DecimalDigits, MidpointRounding.AwayFromZero);
        }
    }

    public sealed class PaymentMinorUnitConverter : IPaymentMinorUnitConverter
    {
        private readonly ICurrencyMetadataService currencyMetadataService;
        private readonly IMoneyRoundingService moneyRoundingService;

        public PaymentMinorUnitConverter(
            ICurrencyMetadataService currencyMetadataService,
            IMoneyRoundingService moneyRoundingService)
        {
            this.currencyMetadataService = currencyMetadataService;
            this.moneyRoundingService = moneyRoundingService;
        }

        public long ToMinorUnits(decimal amount, string currencyCode)
        {
            var metadata = this.currencyMetadataService.Get(currencyCode);
            var roundedAmount = this.moneyRoundingService.RoundPaymentAmount(amount, metadata.CurrencyCode);
            var multiplier = Pow10(metadata.DecimalDigits);
            return decimal.ToInt64(decimal.Round(roundedAmount * multiplier, 0, MidpointRounding.AwayFromZero));
        }

        private static decimal Pow10(int exponent)
        {
            var result = 1m;
            for (var index = 0; index < exponent; index++)
            {
                result *= 10m;
            }

            return result;
        }
    }
}

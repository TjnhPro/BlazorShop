namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Analytics;
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/metrics")]
    public sealed class MetricsController : CommerceAdminControllerBase
    {
        private readonly IMetricsService metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            this.metricsService = metricsService;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> GetSales([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? granularity)
        {
            var range = ResolveRange(from, to);
            if (range is null)
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Invalid date range supplied.");
            }

            var result = await this.metricsService.GetSalesAsync(range.Value.FromUtc, range.Value.ToUtc, ParseGranularity(granularity));
            return this.Success(result, "Sales metrics retrieved successfully.");
        }

        [HttpGet("traffic")]
        public async Task<IActionResult> GetTraffic([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? granularity)
        {
            var range = ResolveRange(from, to);
            if (range is null)
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Invalid date range supplied.");
            }

            var result = await this.metricsService.GetTrafficAsync(range.Value.FromUtc, range.Value.ToUtc, ParseGranularity(granularity));
            return this.Success(result, "Traffic metrics retrieved successfully.");
        }

        private static (DateTime FromUtc, DateTime ToUtc)? ResolveRange(DateTime? from, DateTime? to)
        {
            var toValue = to ?? DateTime.UtcNow;
            var fromValue = from ?? toValue.AddDays(-29);
            var normalizedFrom = EnsureUtc(fromValue);
            var normalizedTo = EnsureUtc(toValue);

            if (normalizedTo < normalizedFrom)
            {
                return null;
            }

            return (normalizedFrom, normalizedTo);
        }

        private static MetricsGranularity ParseGranularity(string? value)
        {
            if (Enum.TryParse<MetricsGranularity>(value, true, out var parsed) && Enum.IsDefined(typeof(MetricsGranularity), parsed))
            {
                return parsed;
            }

            return MetricsGranularity.Day;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }
    }
}

namespace BlazorShop.ControlPlane.Web.Services.Dashboard
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;

    public interface IControlPlaneDashboardClient
    {
        Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneDashboardClient : IControlPlaneDashboardClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneDashboardClient(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();
            using var response = await client.GetAsync("api/control-plane/dashboard/summary", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DashboardSummary>(SerializerOptions, cancellationToken)
                       ?? new DashboardSummary(0, 0, 0, 0, 0);
            }

            throw new InvalidOperationException(await ResolveErrorMessageAsync(response, "Unable to load dashboard summary."));
        }

        private static async Task<string> ResolveErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Sign in with a Control Plane account that can read dashboard data.";
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return "Your Control Plane account does not have permission for this action.";
            }

            if (response.Content is null)
            {
                return defaultMessage;
            }

            try
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (document.RootElement.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(messageElement.GetString()))
                {
                    return messageElement.GetString()!;
                }
            }
            catch (JsonException)
            {
            }

            return defaultMessage;
        }
    }

    public sealed record DashboardSummary(
        int TotalNodes,
        int HealthyNodes,
        int WarningNodes,
        int DownNodes,
        int TotalStores);
}

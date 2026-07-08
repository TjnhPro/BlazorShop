namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Health;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public sealed class ControlPlaneProbeBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private readonly ILogger<ControlPlaneProbeBackgroundService> logger;

        public ControlPlaneProbeBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<ControlPlaneProbeBackgroundService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!this.configuration.GetValue("ControlPlane:Probes:ScheduledEnabled", false))
            {
                return;
            }

            var intervalSeconds = Math.Max(30, this.configuration.GetValue("ControlPlane:Probes:IntervalSeconds", 300));
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = this.scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IControlPlaneHealthService>();
                    var count = await service.ProbeAllActiveAsync(stoppingToken);
                    this.logger.LogInformation("Control Plane scheduled probe completed for {NodeCount} nodes.", count);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Control Plane scheduled probe failed.");
                }
            }
        }
    }
}

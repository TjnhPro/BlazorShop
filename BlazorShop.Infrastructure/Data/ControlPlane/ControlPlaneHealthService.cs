namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.ControlPlane.Health;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public sealed class ControlPlaneHealthService : IControlPlaneHealthService
    {
        private const string ControlApiEndpointKind = "control_api";
        private readonly ControlPlaneDbContext dbContext;
        private readonly ICommerceNodeControlClient controlClient;
        private readonly ILogger<ControlPlaneHealthService>? logger;

        public ControlPlaneHealthService(
            ControlPlaneDbContext dbContext,
            ICommerceNodeControlClient controlClient,
            ILogger<ControlPlaneHealthService>? logger = null)
        {
            this.dbContext = dbContext;
            this.controlClient = controlClient;
            this.logger = logger;
        }

        public async Task<ControlPlaneHealthListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            var nodes = await this.dbContext.Nodes
                .AsNoTracking()
                .Include(node => node.HealthSnapshots)
                .Include(node => node.CapabilitySnapshots)
                .OrderBy(node => node.NodeKey)
                .ToListAsync(cancellationToken);

            return new ControlPlaneHealthListResponse(nodes.Select(MapSummary).ToArray());
        }

        public async Task<ControlPlaneHealthOperationResult<ControlPlaneHealthDetail>> GetDetailAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default)
        {
            var node = await this.dbContext.Nodes
                .AsNoTracking()
                .Include(candidate => candidate.HealthSnapshots)
                .Include(candidate => candidate.CapabilitySnapshots)
                .FirstOrDefaultAsync(candidate => candidate.PublicId == nodePublicId, cancellationToken);

            return node is null
                ? NotFound<ControlPlaneHealthDetail>("Node was not found.")
                : Succeeded(MapDetail(node));
        }

        public async Task<ControlPlaneHealthOperationResult<ControlPlaneProbeResult>> ProbeAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default)
        {
            var node = await this.dbContext.Nodes
                .Include(candidate => candidate.Endpoints)
                .Include(candidate => candidate.CapabilitySnapshots)
                .FirstOrDefaultAsync(candidate => candidate.PublicId == nodePublicId, cancellationToken);

            if (node is null)
            {
                return NotFound<ControlPlaneProbeResult>("Node was not found.");
            }

            if (node.Status == "disabled")
            {
                return ValidationFailed<ControlPlaneProbeResult>("Disabled nodes cannot be probed.");
            }

            var endpoint = node.Endpoints.FirstOrDefault(candidate =>
                candidate.Kind == ControlApiEndpointKind
                && candidate.IsPrimary
                && candidate.DisabledAt is null);

            if (endpoint is null)
            {
                return ValidationFailed<ControlPlaneProbeResult>("Node does not have an active primary Control API endpoint.");
            }

            this.logger?.LogInformation(
                "Starting Control Plane health probe for node {NodePublicId} at {ControlApiUrl}.",
                node.PublicId,
                endpoint.Url);

            var probe = await this.controlClient.ProbeAsync(endpoint.Url, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var health = new NodeHealthSnapshot
            {
                NodeId = node.Id,
                Status = probe.HealthStatus,
                HttpStatusCode = probe.HttpStatusCode,
                DurationMs = probe.DurationMs,
                DependencyStatusJson = probe.DependencyStatusJson,
                ErrorCode = probe.HealthErrorCode,
                ErrorMessage = probe.HealthErrorMessage,
                CheckedAt = now
            };

            this.dbContext.NodeHealthSnapshots.Add(health);
            node.Status = MapNodeStatus(probe.HealthStatus);
            node.LastSeenAt = probe.HealthStatus == "healthy" || probe.HealthStatus == "warning" ? now : node.LastSeenAt;
            node.UpdatedAt = now;

            var capabilityChanged = false;
            NodeCapabilitySnapshot? capability = null;

            if (!string.IsNullOrWhiteSpace(probe.CapabilityJson))
            {
                var checksum = ComputeChecksum(probe.CapabilityJson);
                var currentCapability = node.CapabilitySnapshots.FirstOrDefault(snapshot => snapshot.IsCurrent);

                if (currentCapability is null || currentCapability.Checksum != checksum)
                {
                    if (currentCapability is not null)
                    {
                        currentCapability.IsCurrent = false;
                    }

                    capability = new NodeCapabilitySnapshot
                    {
                        NodeId = node.Id,
                        SchemaVersion = string.IsNullOrWhiteSpace(probe.CapabilitySchemaVersion) ? "unknown" : probe.CapabilitySchemaVersion,
                        Checksum = checksum,
                        CapabilitiesJson = probe.CapabilityJson,
                        IsCurrent = true,
                        CapturedAt = now
                    };

                    this.dbContext.NodeCapabilitySnapshots.Add(capability);
                    capabilityChanged = true;
                }
                else
                {
                    capability = currentCapability;
                }
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);

            if (probe.HealthStatus is "healthy" or "warning")
            {
                this.logger?.LogInformation(
                    "Completed Control Plane health probe for node {NodePublicId} with status {HealthStatus} in {DurationMs} ms. CapabilityChanged={CapabilityChanged}.",
                    node.PublicId,
                    probe.HealthStatus,
                    probe.DurationMs,
                    capabilityChanged);
            }
            else
            {
                this.logger?.LogWarning(
                    "Control Plane health probe for node {NodePublicId} ended with status {HealthStatus}, error {ErrorCode}, duration {DurationMs} ms.",
                    node.PublicId,
                    probe.HealthStatus,
                    probe.HealthErrorCode,
                    probe.DurationMs);
            }

            return Succeeded(new ControlPlaneProbeResult(
                MapHealth(health),
                capability is null ? null : MapCapability(capability),
                capabilityChanged));
        }

        public async Task<int> ProbeAllActiveAsync(CancellationToken cancellationToken = default)
        {
            var publicIds = await this.dbContext.Nodes
                .Where(node => node.Status != "disabled")
                .OrderBy(node => node.Id)
                .Select(node => node.PublicId)
                .ToListAsync(cancellationToken);

            var probed = 0;
            foreach (var publicId in publicIds)
            {
                var result = await this.ProbeAsync(publicId, cancellationToken);
                if (result.Success)
                {
                    probed++;
                }
            }

            return probed;
        }

        private static ControlPlaneHealthNodeSummary MapSummary(CommerceNode node)
        {
            return new ControlPlaneHealthNodeSummary(
                node.PublicId,
                node.NodeKey,
                node.Name,
                node.Status,
                node.LastSeenAt,
                node.HealthSnapshots.OrderByDescending(snapshot => snapshot.CheckedAt).Select(MapHealth).FirstOrDefault(),
                node.CapabilitySnapshots.Where(snapshot => snapshot.IsCurrent).Select(MapCapability).FirstOrDefault());
        }

        private static ControlPlaneHealthDetail MapDetail(CommerceNode node)
        {
            return new ControlPlaneHealthDetail(
                node.PublicId,
                node.NodeKey,
                node.Name,
                node.Status,
                node.LastSeenAt,
                node.HealthSnapshots
                    .OrderByDescending(snapshot => snapshot.CheckedAt)
                    .Take(25)
                    .Select(MapHealth)
                    .ToArray(),
                node.CapabilitySnapshots
                    .Where(snapshot => snapshot.IsCurrent)
                    .Select(MapCapability)
                    .FirstOrDefault());
        }

        private static ControlPlaneHealthSnapshotDto MapHealth(NodeHealthSnapshot snapshot)
        {
            return new ControlPlaneHealthSnapshotDto(
                snapshot.PublicId,
                snapshot.Status,
                snapshot.HttpStatusCode,
                snapshot.DurationMs,
                snapshot.DependencyStatusJson,
                snapshot.ErrorCode,
                snapshot.ErrorMessage,
                snapshot.CheckedAt);
        }

        private static ControlPlaneCapabilitySnapshotDto MapCapability(NodeCapabilitySnapshot snapshot)
        {
            return new ControlPlaneCapabilitySnapshotDto(
                snapshot.PublicId,
                snapshot.SchemaVersion,
                snapshot.Checksum,
                snapshot.CapabilitiesJson,
                snapshot.IsCurrent,
                snapshot.CapturedAt);
        }

        private static string MapNodeStatus(string healthStatus)
        {
            return healthStatus switch
            {
                "healthy" => "healthy",
                "warning" => "warning",
                "down" => "down",
                "timeout" => "down",
                "malformed" => "down",
                _ => "unknown"
            };
        }

        private static string ComputeChecksum(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        private static ControlPlaneHealthOperationResult<TPayload> Succeeded<TPayload>(TPayload payload)
        {
            return new ControlPlaneHealthOperationResult<TPayload>(true, Payload: payload);
        }

        private static ControlPlaneHealthOperationResult<TPayload> ValidationFailed<TPayload>(string message)
        {
            return new ControlPlaneHealthOperationResult<TPayload>(false, message, Failure: ControlPlaneHealthOperationFailure.Validation);
        }

        private static ControlPlaneHealthOperationResult<TPayload> NotFound<TPayload>(string message)
        {
            return new ControlPlaneHealthOperationResult<TPayload>(false, message, Failure: ControlPlaneHealthOperationFailure.NotFound);
        }
    }
}

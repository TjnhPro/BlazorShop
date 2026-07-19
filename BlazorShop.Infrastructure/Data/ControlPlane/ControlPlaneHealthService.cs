namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.ControlPlane.Common;
    using BlazorShop.Application.ControlPlane.Health;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public sealed class ControlPlaneHealthService : IControlPlaneHealthService
    {
        private const string ControlApiEndpointKind = "control_api";
        private readonly ControlPlaneDbContext dbContext;
        private readonly ICommerceNodeControlClient controlClient;
        private readonly ILogger<ControlPlaneHealthService> logger;

        public ControlPlaneHealthService(
            ControlPlaneDbContext dbContext,
            ICommerceNodeControlClient controlClient,
            ILogger<ControlPlaneHealthService> logger)
        {
            this.dbContext = dbContext;
            this.controlClient = controlClient;
            this.logger = logger;
        }

        public async Task<ControlPlaneHealthListResponse> ListAsync(
            ControlPlaneHealthListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var nodesQuery = this.dbContext.Nodes
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLowerInvariant();
                nodesQuery = nodesQuery.Where(node =>
                    node.NodeKey.ToLower().Contains(search)
                    || node.Name.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                nodesQuery = nodesQuery.Where(node => node.Status == status);
            }

            var page = ControlPlanePaging.Normalize(query.PageNumber, query.PageSize);
            var totalCount = await nodesQuery.CountAsync(cancellationToken);
            var nodes = await nodesQuery
                .OrderBy(node => node.NodeKey)
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToListAsync(cancellationToken);

            var nodeIds = nodes.Select(node => node.Id).ToArray();
            var latestHealthByNodeId = await this.dbContext.NodeHealthSnapshots
                .AsNoTracking()
                .Where(snapshot => nodeIds.Contains(snapshot.NodeId))
                .OrderByDescending(snapshot => snapshot.CheckedAt)
                .ToListAsync(cancellationToken);
            var currentCapabilityByNodeId = await this.dbContext.NodeCapabilitySnapshots
                .AsNoTracking()
                .Where(snapshot => nodeIds.Contains(snapshot.NodeId) && snapshot.IsCurrent)
                .ToListAsync(cancellationToken);

            var latestHealth = latestHealthByNodeId
                .GroupBy(snapshot => snapshot.NodeId)
                .ToDictionary(group => group.Key, group => group.First());
            var currentCapabilities = currentCapabilityByNodeId.ToDictionary(snapshot => snapshot.NodeId);

            return new ControlPlaneHealthListResponse(
                nodes.Select(node => MapSummary(node, latestHealth.GetValueOrDefault(node.Id), currentCapabilities.GetValueOrDefault(node.Id))).ToArray(),
                totalCount,
                page.PageNumber,
                page.PageSize,
                ControlPlanePaging.GetTotalPages(totalCount, page.PageSize));
        }

        public async Task<ApplicationResult<ControlPlaneHealthDetail>> GetDetailAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default)
        {
            var node = await this.dbContext.Nodes
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.PublicId == nodePublicId, cancellationToken);

            if (node is null)
            {
                return NotFound<ControlPlaneHealthDetail>("Node was not found.");
            }

            var latestHealth = await this.dbContext.NodeHealthSnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.NodeId == node.Id)
                .OrderByDescending(snapshot => snapshot.CheckedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var currentCapability = await this.dbContext.NodeCapabilitySnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.NodeId == node.Id && snapshot.IsCurrent)
                .FirstOrDefaultAsync(cancellationToken);

            return Succeeded(MapDetail(node, latestHealth, currentCapability));
        }

        public async Task<ApplicationResult<ControlPlaneHealthTimelineResponse>> GetTimelineAsync(
            Guid nodePublicId,
            ControlPlaneHealthTimelineQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var nodeId = await this.dbContext.Nodes
                .AsNoTracking()
                .Where(node => node.PublicId == nodePublicId)
                .Select(node => (long?)node.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nodeId is null)
            {
                return NotFound<ControlPlaneHealthTimelineResponse>("Node was not found.");
            }

            var page = ControlPlanePaging.Normalize(query.PageNumber, query.PageSize);
            var timelineQuery = this.dbContext.NodeHealthSnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.NodeId == nodeId.Value);
            var totalCount = await timelineQuery.CountAsync(cancellationToken);
            var snapshotEntities = await timelineQuery
                .OrderByDescending(snapshot => snapshot.CheckedAt)
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToListAsync(cancellationToken);

            return Succeeded(new ControlPlaneHealthTimelineResponse(
                snapshotEntities.Select(MapHealth).ToArray(),
                totalCount,
                page.PageNumber,
                page.PageSize,
                ControlPlanePaging.GetTotalPages(totalCount, page.PageSize)));
        }

        public async Task<ApplicationResult<ControlPlaneProbeResult>> ProbeAsync(
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

            if (string.IsNullOrWhiteSpace(node.NodeSecret))
            {
                return ValidationFailed<ControlPlaneProbeResult>("Node does not have a Commerce Node secret configured.");
            }

            this.logger.LogInformation(
                "Starting Control Plane health probe for node {NodePublicId} at {ControlApiUrl}.",
                node.PublicId,
                endpoint.Url);

            var probe = await this.controlClient.ProbeAsync(
                endpoint.Url,
                node.NodeKey,
                node.NodeSecret,
                cancellationToken);
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
                this.logger.LogInformation(
                    "Completed Control Plane health probe for node {NodePublicId} with status {HealthStatus} in {DurationMs} ms. CapabilityChanged={CapabilityChanged}.",
                    node.PublicId,
                    probe.HealthStatus,
                    probe.DurationMs,
                    capabilityChanged);
            }
            else
            {
                this.logger.LogWarning(
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

        private static ControlPlaneHealthNodeSummary MapSummary(
            CommerceNode node,
            NodeHealthSnapshot? latestHealth,
            NodeCapabilitySnapshot? currentCapability)
        {
            return new ControlPlaneHealthNodeSummary(
                node.PublicId,
                node.NodeKey,
                node.Name,
                node.Status,
                node.LastSeenAt,
                latestHealth is null ? null : MapHealth(latestHealth),
                currentCapability is null ? null : MapCapability(currentCapability));
        }

        private static ControlPlaneHealthDetail MapDetail(
            CommerceNode node,
            NodeHealthSnapshot? latestHealth,
            NodeCapabilitySnapshot? currentCapability)
        {
            return new ControlPlaneHealthDetail(
                node.PublicId,
                node.NodeKey,
                node.Name,
                node.Status,
                node.LastSeenAt,
                latestHealth is null ? null : MapHealth(latestHealth),
                currentCapability is null ? null : MapCapability(currentCapability));
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

        private static ApplicationResult<TPayload> Succeeded<TPayload>(TPayload payload)
        {
            return new ApplicationResult<TPayload>(true, Payload: payload);
        }

        private static ApplicationResult<TPayload> ValidationFailed<TPayload>(string message)
        {
            return new ApplicationResult<TPayload>(false, message, Failure: ApplicationErrorKind.Validation);
        }

        private static ApplicationResult<TPayload> NotFound<TPayload>(string message)
        {
            return new ApplicationResult<TPayload>(false, message, Failure: ApplicationErrorKind.NotFound);
        }
    }
}

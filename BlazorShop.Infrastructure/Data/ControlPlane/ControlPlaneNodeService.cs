namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Text;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed partial class ControlPlaneNodeService : IControlPlaneNodeService
    {
        private const int DefaultLimit = 25;
        private const int MaxLimit = 100;
        private const string ControlApiEndpointKind = "control_api";

        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneNodeService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ControlPlaneNodeListResponse> ListAsync(
            ControlPlaneNodeListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var limit = Math.Clamp(query.Limit <= 0 ? DefaultLimit : query.Limit, 1, MaxLimit);
            var cursorId = DecodeCursor(query.Cursor);
            var nodes = this.dbContext.Nodes
                .AsNoTracking()
                .Include(node => node.Endpoints)
                .AsQueryable();

            if (cursorId is not null)
            {
                nodes = nodes.Where(node => node.Id < cursorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                nodes = nodes.Where(node => node.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLowerInvariant();
                nodes = nodes.Where(node =>
                    node.NodeKey.ToLower().Contains(search)
                    || node.Name.ToLower().Contains(search));
            }

            var fetchedNodes = await nodes
                .OrderByDescending(node => node.Id)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);

            var items = fetchedNodes.Take(limit).Select(MapSummary).ToArray();
            var nextCursor = fetchedNodes.Count > limit ? EncodeCursor(fetchedNodes[limit - 1].Id) : null;

            return new ControlPlaneNodeListResponse(items, nextCursor);
        }

        public async Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> GetByPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var node = await this.LoadNodeAsync(publicId, cancellationToken);

            return node is null
                ? NotFound()
                : Succeeded(MapDetail(node));
        }

        public async Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> CreateAsync(
            CreateControlPlaneNodeRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateCreateRequest(request);
            if (validation is not null)
            {
                return ValidationFailed(validation);
            }

            var nodeKey = request.NodeKey.Trim().ToLowerInvariant();
            var duplicateExists = await this.dbContext.Nodes.AnyAsync(
                node => node.NodeKey == nodeKey && node.DisabledAt == null,
                cancellationToken);

            if (duplicateExists)
            {
                return Conflict("An active node with this key already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var node = new CommerceNode
            {
                NodeKey = nodeKey,
                NodeSecret = request.NodeSecret.Trim(),
                NodeSecretUpdatedAt = now,
                Name = request.Name.Trim(),
                Description = NormalizeOptionalText(request.Description),
                Status = "unknown",
                CreatedAt = now,
                UpdatedAt = now,
                Endpoints =
                [
                    new CommerceNodeEndpoint
                    {
                        Kind = ControlApiEndpointKind,
                        Url = request.ControlApiUrl.Trim(),
                        IsPrimary = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                ]
            };

            this.dbContext.Nodes.Add(node);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(MapDetail(node));
        }

        public async Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> UpdateAsync(
            Guid publicId,
            UpdateControlPlaneNodeRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateUpdateRequest(request);
            if (validation is not null)
            {
                return ValidationFailed(validation);
            }

            var node = await this.LoadNodeAsync(publicId, cancellationToken);
            if (node is null)
            {
                return NotFound();
            }

            if (node.Status == "disabled")
            {
                return ValidationFailed("Disabled nodes cannot be updated.");
            }

            var now = DateTimeOffset.UtcNow;
            node.Name = request.Name.Trim();
            node.Description = NormalizeOptionalText(request.Description);
            node.UpdatedAt = now;

            if (!string.IsNullOrWhiteSpace(request.NodeSecret))
            {
                node.NodeSecret = request.NodeSecret.Trim();
                node.NodeSecretUpdatedAt = now;
            }

            var endpoint = node.Endpoints.FirstOrDefault(endpoint =>
                endpoint.Kind == ControlApiEndpointKind
                && endpoint.IsPrimary
                && endpoint.DisabledAt == null);

            if (endpoint is null)
            {
                node.Endpoints.Add(new CommerceNodeEndpoint
                {
                    Kind = ControlApiEndpointKind,
                    Url = request.ControlApiUrl.Trim(),
                    IsPrimary = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                endpoint.Url = request.ControlApiUrl.Trim();
                endpoint.UpdatedAt = now;
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(MapDetail(node));
        }

        public async Task<ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>> DisableAsync(
            Guid publicId,
            CancellationToken cancellationToken = default)
        {
            var node = await this.LoadNodeAsync(publicId, cancellationToken);
            if (node is null)
            {
                return NotFound();
            }

            if (node.Status == "disabled")
            {
                return Succeeded(MapDetail(node));
            }

            var now = DateTimeOffset.UtcNow;
            node.Status = "disabled";
            node.DisabledAt = now;
            node.UpdatedAt = now;

            foreach (var endpoint in node.Endpoints.Where(endpoint => endpoint.DisabledAt is null))
            {
                endpoint.DisabledAt = now;
                endpoint.UpdatedAt = now;
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(MapDetail(node));
        }

        private async Task<CommerceNode?> LoadNodeAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Nodes
                .Include(node => node.Endpoints)
                .FirstOrDefaultAsync(node => node.PublicId == publicId, cancellationToken);
        }

        private static string? ValidateCreateRequest(CreateControlPlaneNodeRequest request)
        {
            if (request is null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.NodeKey) || !NodeKeyRegex().IsMatch(request.NodeKey.Trim()))
            {
                return "Node key must be 3-64 characters and contain only lowercase letters, digits, and hyphens.";
            }

            var secretValidation = ValidateNodeSecret(request.NodeSecret, required: true);
            return secretValidation ?? ValidateCommonFields(request.Name, request.ControlApiUrl);
        }

        private static string? ValidateUpdateRequest(UpdateControlPlaneNodeRequest request)
        {
            if (request is null)
            {
                return "Request body is required.";
            }

            var secretValidation = ValidateNodeSecret(request.NodeSecret, required: false);
            return secretValidation ?? ValidateCommonFields(request.Name, request.ControlApiUrl);
        }

        private static string? ValidateNodeSecret(string? nodeSecret, bool required)
        {
            if (string.IsNullOrWhiteSpace(nodeSecret))
            {
                return required ? "Node secret is required." : null;
            }

            var trimmedSecret = nodeSecret.Trim();
            if (trimmedSecret.Length < 8 || trimmedSecret.Length > 512)
            {
                return "Node secret must be between 8 and 512 characters.";
            }

            return null;
        }

        private static string? ValidateCommonFields(string name, string controlApiUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Node name is required.";
            }

            if (name.Trim().Length > 120)
            {
                return "Node name must be 120 characters or fewer.";
            }

            if (!Uri.TryCreate(controlApiUrl?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return "Control API URL must be an absolute HTTP or HTTPS URL.";
            }

            return null;
        }

        private static ControlPlaneNodeSummary MapSummary(CommerceNode node)
        {
            return new ControlPlaneNodeSummary(
                node.PublicId,
                node.NodeKey,
                node.Name,
                node.Status,
                node.Description,
                GetPrimaryControlApiUrl(node),
                !string.IsNullOrWhiteSpace(node.NodeSecret),
                node.NodeSecretUpdatedAt,
                node.LastSeenAt,
                node.CreatedAt,
                node.UpdatedAt,
                node.DisabledAt);
        }

        private static ControlPlaneNodeDetail MapDetail(CommerceNode node)
        {
            return new ControlPlaneNodeDetail(
                node.PublicId,
                node.NodeKey,
                node.Name,
                node.Status,
                node.Description,
                GetPrimaryControlApiUrl(node),
                !string.IsNullOrWhiteSpace(node.NodeSecret),
                node.NodeSecretUpdatedAt,
                node.LastSeenAt,
                node.CreatedAt,
                node.UpdatedAt,
                node.DisabledAt,
                node.Endpoints
                    .OrderByDescending(endpoint => endpoint.IsPrimary)
                    .ThenBy(endpoint => endpoint.Kind, StringComparer.Ordinal)
                    .Select(endpoint => new ControlPlaneNodeEndpointDto(
                        endpoint.Id,
                        endpoint.Kind,
                        endpoint.Url,
                        endpoint.IsPrimary,
                        endpoint.DisabledAt))
                    .ToArray());
        }

        private static string? GetPrimaryControlApiUrl(CommerceNode node)
        {
            return node.Endpoints.FirstOrDefault(endpoint =>
                endpoint.Kind == ControlApiEndpointKind
                && endpoint.IsPrimary
                && endpoint.DisabledAt == null)?.Url;
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? EncodeCursor(long id)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static long? DecodeCursor(string? cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor))
            {
                return null;
            }

            try
            {
                var value = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                return long.TryParse(value, out var id) && id > 0 ? id : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static ControlPlaneNodeOperationResult<ControlPlaneNodeDetail> Succeeded(ControlPlaneNodeDetail payload)
        {
            return new ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>(true, Payload: payload);
        }

        private static ControlPlaneNodeOperationResult<ControlPlaneNodeDetail> ValidationFailed(string message)
        {
            return new ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>(false, message, Failure: ControlPlaneNodeOperationFailure.Validation);
        }

        private static ControlPlaneNodeOperationResult<ControlPlaneNodeDetail> Conflict(string message)
        {
            return new ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>(false, message, Failure: ControlPlaneNodeOperationFailure.Conflict);
        }

        private static ControlPlaneNodeOperationResult<ControlPlaneNodeDetail> NotFound()
        {
            return new ControlPlaneNodeOperationResult<ControlPlaneNodeDetail>(false, "Node was not found.", Failure: ControlPlaneNodeOperationFailure.NotFound);
        }

        [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{1,62}[a-z0-9])$")]
        private static partial Regex NodeKeyRegex();
    }
}

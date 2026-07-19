namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeAdminGatewayTransport : ICommerceNodeAdminGatewayTransport
    {
        private const string ControlApiEndpointKind = "control_api";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ControlPlaneDbContext dbContext;
        private readonly HttpClient httpClient;

        public CommerceNodeAdminGatewayTransport(ControlPlaneDbContext dbContext, HttpClient httpClient)
        {
            this.dbContext = dbContext;
            this.httpClient = httpClient;
        }

        public async Task<CommerceNodeAdminGatewayResult<TPayload>> SendAsync<TPayload>(
            Guid storePublicId,
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken = default)
        {
            var context = await this.ResolveContextAsync(storePublicId, cancellationToken);
            if (!context.Success)
            {
                return ToResult<TPayload>(context);
            }

            try
            {
                using var request = this.CreateRequest(context.Payload!, method, path);
                if (body is not null)
                {
                    request.Content = JsonContent.Create(body, options: SerializerOptions);
                }

                return await this.SendEnvelopeAsync<TPayload>(request, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
        }

        public async Task<CommerceNodeAdminGatewayResult<TPayload>> SendProductImportMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            ProductImportUploadRequest upload,
            CancellationToken cancellationToken = default)
        {
            var context = await this.ResolveContextAsync(storePublicId, cancellationToken);
            if (!context.Success)
            {
                return ToResult<TPayload>(context);
            }

            try
            {
                using var request = this.CreateRequest(context.Payload!, HttpMethod.Post, path);
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(upload.Content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(upload.FileName) ? "products.csv" : upload.FileName);
                form.Add(new StringContent(string.IsNullOrWhiteSpace(upload.Mode) ? ProductImportModes.CreateOnly : upload.Mode), "mode");
                request.Content = form;

                return await this.SendEnvelopeAsync<TPayload>(request, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
        }

        public async Task<CommerceNodeAdminGatewayResult<TPayload>> SendMediaAssetMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            CommerceMediaAssetUploadRequest upload,
            CancellationToken cancellationToken = default)
        {
            var context = await this.ResolveContextAsync(storePublicId, cancellationToken);
            if (!context.Success)
            {
                return ToResult<TPayload>(context);
            }

            try
            {
                using var request = this.CreateRequest(context.Payload!, HttpMethod.Post, path);
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(upload.Content);
                if (!string.IsNullOrWhiteSpace(upload.ContentType))
                {
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(upload.ContentType);
                }

                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(upload.FileName) ? "media-asset" : upload.FileName);
                request.Content = form;

                return await this.SendEnvelopeAsync<TPayload>(request, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
        }

        public async Task<CommerceNodeAdminMediaGatewayResult> SendMediaAsync(
            Guid storePublicId,
            string path,
            CancellationToken cancellationToken = default)
        {
            var context = await this.ResolveContextAsync(storePublicId, cancellationToken);
            if (!context.Success)
            {
                return ToMediaResult(context);
            }

            try
            {
                using var request = this.CreateRequest(context.Payload!, HttpMethod.Get, path);
                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return new CommerceNodeAdminMediaGatewayResult(
                        false,
                        bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : "Commerce Node media preview request failed.",
                        Failure: ToFailure(response.StatusCode),
                        HttpStatusCode: (int)response.StatusCode);
                }

                return new CommerceNodeAdminMediaGatewayResult(
                    true,
                    "Product media preview loaded.",
                    bytes,
                    response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return new CommerceNodeAdminMediaGatewayResult(false, "Commerce Node media preview request timed out.", Failure: CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return new CommerceNodeAdminMediaGatewayResult(false, ex.Message, Failure: CommerceNodeAdminGatewayFailure.RemoteFailure);
            }
        }

        public async Task<CommerceNodeAdminGatewayResult<string>> ResolveStoreKeyAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            var context = await this.ResolveContextAsync(storePublicId, cancellationToken);
            return context.Success
                ? new CommerceNodeAdminGatewayResult<string>(true, Payload: context.Payload!.StoreKey)
                : ToResult<string>(context);
        }

        private async Task<CommerceNodeAdminGatewayResult<GatewayStoreContext>> ResolveContextAsync(
            Guid storePublicId,
            CancellationToken cancellationToken)
        {
            var store = await this.dbContext.Stores
                .AsNoTracking()
                .Include(item => item.Node)
                    .ThenInclude(node => node!.Endpoints)
                .FirstOrDefaultAsync(item => item.PublicId == storePublicId, cancellationToken);

            if (store is null)
            {
                return Failure<GatewayStoreContext>(CommerceNodeAdminGatewayFailure.NotFound, "Store was not found.");
            }

            if (store.Status == "archived")
            {
                return Failure<GatewayStoreContext>(CommerceNodeAdminGatewayFailure.Validation, "Archived stores cannot be managed.");
            }

            if (store.Node is null || store.Node.Status == "disabled")
            {
                return Failure<GatewayStoreContext>(CommerceNodeAdminGatewayFailure.Validation, "Store node is missing or disabled.");
            }

            if (string.IsNullOrWhiteSpace(store.Node.NodeSecret))
            {
                return Failure<GatewayStoreContext>(CommerceNodeAdminGatewayFailure.Validation, "Store node does not have a node secret configured.");
            }

            var controlApiUrl = GetControlApiUrl(store.Node);
            if (string.IsNullOrWhiteSpace(controlApiUrl))
            {
                return Failure<GatewayStoreContext>(CommerceNodeAdminGatewayFailure.Validation, "Store node does not have an active Control API endpoint.");
            }

            return new CommerceNodeAdminGatewayResult<GatewayStoreContext>(
                true,
                Payload: new GatewayStoreContext(store.StoreKey, store.Node.NodeKey, store.Node.NodeSecret, controlApiUrl));
        }

        private HttpRequestMessage CreateRequest(GatewayStoreContext context, HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, AppendPath(context.ControlApiUrl, AppendStoreKeyQuery(path, context.StoreKey)));
            request.Headers.TryAddWithoutValidation("X-Node-Key", context.NodeKey);
            request.Headers.TryAddWithoutValidation("X-Node-Secret", context.NodeSecret);
            return request;
        }

        private async Task<CommerceNodeAdminGatewayResult<TPayload>> SendEnvelopeAsync<TPayload>(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var response = await this.httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return Failure<TPayload>(
                    "Commerce Node returned an empty response.",
                    CommerceNodeAdminGatewayFailure.RemoteFailure,
                    (int)response.StatusCode);
            }

            var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
            if (envelope is null)
            {
                return Failure<TPayload>(
                    "Commerce Node returned a malformed response envelope.",
                    CommerceNodeAdminGatewayFailure.RemoteFailure,
                    (int)response.StatusCode);
            }

            if (!response.IsSuccessStatusCode || !envelope.Success)
            {
                return new CommerceNodeAdminGatewayResult<TPayload>(
                    false,
                    string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node catalog request failed." : envelope.Message,
                    envelope.Data,
                    ToFailure(response.StatusCode),
                    (int)response.StatusCode);
            }

            return new CommerceNodeAdminGatewayResult<TPayload>(
                true,
                envelope.Message,
                envelope.Data,
                HttpStatusCode: (int)response.StatusCode);
        }

        private static string GetControlApiUrl(CommerceNode node)
        {
            return node.Endpoints.FirstOrDefault(endpoint =>
                endpoint.Kind == ControlApiEndpointKind &&
                endpoint.IsPrimary &&
                endpoint.DisabledAt is null)?.Url ?? string.Empty;
        }

        private static Uri AppendPath(string baseUrl, string path)
        {
            return new Uri(baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'), UriKind.Absolute);
        }

        private static string AppendStoreKeyQuery(string path, string storeKey)
        {
            var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return path + separator + "storeKey=" + Uri.EscapeDataString(storeKey);
        }

        private static CommerceNodeAdminGatewayFailure ToFailure(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.NotFound => CommerceNodeAdminGatewayFailure.NotFound,
                System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict => CommerceNodeAdminGatewayFailure.Validation,
                _ => CommerceNodeAdminGatewayFailure.RemoteFailure,
            };
        }

        private static CommerceNodeAdminGatewayResult<TPayload> Failure<TPayload>(
            string message,
            CommerceNodeAdminGatewayFailure failure,
            int? httpStatusCode = null)
        {
            return new CommerceNodeAdminGatewayResult<TPayload>(
                false,
                message,
                Failure: failure,
                HttpStatusCode: httpStatusCode);
        }

        private static CommerceNodeAdminGatewayResult<TPayload> Failure<TPayload>(
            CommerceNodeAdminGatewayFailure failure,
            string message)
        {
            return Failure<TPayload>(message, failure);
        }

        private static CommerceNodeAdminGatewayResult<TPayload> ToResult<TPayload>(
            CommerceNodeAdminGatewayResult<GatewayStoreContext> context)
        {
            return new CommerceNodeAdminGatewayResult<TPayload>(
                false,
                context.Message,
                Failure: context.Failure,
                HttpStatusCode: context.HttpStatusCode);
        }

        private static CommerceNodeAdminMediaGatewayResult ToMediaResult(
            CommerceNodeAdminGatewayResult<GatewayStoreContext> context)
        {
            return new CommerceNodeAdminMediaGatewayResult(
                false,
                context.Message,
                Failure: context.Failure,
                HttpStatusCode: context.HttpStatusCode);
        }

        private sealed record GatewayStoreContext(
            string StoreKey,
            string NodeKey,
            string NodeSecret,
            string ControlApiUrl);

        private sealed record CommerceNodeEnvelope<TPayload>(
            bool Success,
            string? Message,
            TPayload? Data);
    }
}

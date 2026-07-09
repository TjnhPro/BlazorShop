namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.ControlPlane.Stores;

    public sealed class CommerceNodeTaskClient : ICommerceNodeTaskClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient httpClient;

        public CommerceNodeTaskClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public Task<CommerceNodeTaskClientResult<CommerceTaskSummary>> EnqueueAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            EnqueueCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceTaskSummary>(
                HttpMethod.Post,
                controlApiBaseUrl,
                nodeKey,
                nodeSecret,
                "api/commerce/tasks",
                request,
                cancellationToken);
        }

        public Task<CommerceNodeTaskClientResult<CommerceTaskDetail>> GetAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            Guid taskPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceTaskDetail>(
                HttpMethod.Get,
                controlApiBaseUrl,
                nodeKey,
                nodeSecret,
                $"api/commerce/tasks/{taskPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<CommerceNodeTaskClientResult<CommerceTaskDetail>> CancelAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            Guid taskPublicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceTaskDetail>(
                HttpMethod.Post,
                controlApiBaseUrl,
                nodeKey,
                nodeSecret,
                $"api/commerce/tasks/{taskPublicId:D}/cancel",
                request,
                cancellationToken);
        }

        public Task<CommerceNodeTaskClientResult<CommerceTaskDetail>> RetryAsync(
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            Guid taskPublicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceTaskDetail>(
                HttpMethod.Post,
                controlApiBaseUrl,
                nodeKey,
                nodeSecret,
                $"api/commerce/tasks/{taskPublicId:D}/retry",
                request,
                cancellationToken);
        }

        private async Task<CommerceNodeTaskClientResult<TPayload>> SendAsync<TPayload>(
            HttpMethod method,
            string controlApiBaseUrl,
            string nodeKey,
            string nodeSecret,
            string path,
            object? body,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(controlApiBaseUrl, UriKind.Absolute, out var baseUri))
            {
                return Failure<TPayload>(null, "Control API URL is not an absolute URL.", "invalid_url");
            }

            try
            {
                using var request = new HttpRequestMessage(method, AppendPath(baseUri, path));
                request.Headers.TryAddWithoutValidation("X-Node-Key", nodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", nodeSecret);

                if (body is not null)
                {
                    request.Content = JsonContent.Create(body, options: SerializerOptions);
                }

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return Failure<TPayload>(
                        (int)response.StatusCode,
                        $"Commerce Node returned HTTP {(int)response.StatusCode} with an empty response.",
                        "empty_response");
                }

                var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
                if (envelope is null)
                {
                    return Failure<TPayload>(
                        (int)response.StatusCode,
                        "Commerce Node returned a malformed response envelope.",
                        "malformed_response");
                }

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    return new CommerceNodeTaskClientResult<TPayload>(
                        false,
                        (int)response.StatusCode,
                        string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node task request failed." : envelope.Message,
                        envelope.Data,
                        "remote_failure");
                }

                return new CommerceNodeTaskClientResult<TPayload>(
                    true,
                    (int)response.StatusCode,
                    envelope.Message,
                    envelope.Data);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>(null, "Commerce Node task request timed out.", "timeout");
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(null, ex.Message, "request_failed");
            }
            catch (JsonException)
            {
                return Failure<TPayload>(null, "Commerce Node returned malformed JSON.", "malformed_json");
            }
        }

        private static Uri AppendPath(Uri baseUri, string path)
        {
            var value = baseUri.ToString().TrimEnd('/') + "/" + path.TrimStart('/');
            return new Uri(value, UriKind.Absolute);
        }

        private static CommerceNodeTaskClientResult<TPayload> Failure<TPayload>(
            int? httpStatusCode,
            string message,
            string errorCode)
        {
            return new CommerceNodeTaskClientResult<TPayload>(
                false,
                httpStatusCode,
                message,
                ErrorCode: errorCode);
        }

        private sealed record CommerceNodeEnvelope<TPayload>(
            bool Success,
            string Message,
            TPayload? Data);
    }
}

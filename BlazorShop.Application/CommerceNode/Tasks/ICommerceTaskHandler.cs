namespace BlazorShop.Application.CommerceNode.Tasks
{
    public interface ICommerceTaskHandler
    {
        string TaskType { get; }

        Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken);
    }

    public sealed class CommerceTaskHandlerContext
    {
        private readonly Func<CancellationToken, Task<bool>> isCancellationRequestedAsync;

        public CommerceTaskHandlerContext(
            Guid taskId,
            Guid publicId,
            string taskType,
            string payloadSchemaVersion,
            string payloadJson,
            int attemptNumber,
            Func<CancellationToken, Task<bool>> isCancellationRequestedAsync)
        {
            this.TaskId = taskId;
            this.PublicId = publicId;
            this.TaskType = taskType;
            this.PayloadSchemaVersion = payloadSchemaVersion;
            this.PayloadJson = payloadJson;
            this.AttemptNumber = attemptNumber;
            this.isCancellationRequestedAsync = isCancellationRequestedAsync;
        }

        public Guid TaskId { get; }

        public Guid PublicId { get; }

        public string TaskType { get; }

        public string PayloadSchemaVersion { get; }

        public string PayloadJson { get; }

        public int AttemptNumber { get; }

        public Task<bool> IsCancellationRequestedAsync(CancellationToken cancellationToken)
        {
            return this.isCancellationRequestedAsync(cancellationToken);
        }
    }

    public sealed record CommerceTaskHandlerResult(
        bool Success,
        string? Message = null,
        string? ResultJson = null,
        string? ErrorCode = null,
        bool Retryable = false)
    {
        public static CommerceTaskHandlerResult Succeeded(string? message = null, string? resultJson = null)
        {
            return new CommerceTaskHandlerResult(true, message, resultJson);
        }

        public static CommerceTaskHandlerResult Failed(
            string message,
            string? errorCode = null,
            bool retryable = false,
            string? resultJson = null)
        {
            return new CommerceTaskHandlerResult(false, message, resultJson, errorCode, retryable);
        }
    }
}

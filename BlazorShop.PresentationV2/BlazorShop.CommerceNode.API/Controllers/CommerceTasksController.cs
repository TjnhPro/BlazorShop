namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/tasks")]
    public sealed class CommerceTasksController : ControllerBase
    {
        private readonly ICommerceTaskService taskService;

        public CommerceTasksController(ICommerceTaskService taskService)
        {
            this.taskService = taskService;
        }

        [HttpPost]
        public async Task<IActionResult> Enqueue(
            [FromBody] EnqueueCommerceTaskRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.taskService.EnqueueAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] string? taskType,
            [FromQuery] DateTimeOffset? createdFrom,
            [FromQuery] DateTimeOffset? createdTo,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var result = await this.taskService.ListAsync(
                new CommerceTaskListQuery(status, taskType, createdFrom, createdTo, skip, take),
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> GetByPublicId(
            Guid publicId,
            CancellationToken cancellationToken)
        {
            var result = await this.taskService.GetByPublicIdAsync(publicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/cancel")]
        public async Task<IActionResult> Cancel(
            Guid publicId,
            [FromBody] CancelCommerceTaskRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await this.taskService.CancelAsync(
                publicId,
                request ?? new CancelCommerceTaskRequest(),
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/retry")]
        public async Task<IActionResult> Retry(
            Guid publicId,
            [FromBody] RetryCommerceTaskRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await this.taskService.RetryAsync(
                publicId,
                request ?? new RetryCommerceTaskRequest(),
                cancellationToken);

            return ToActionResult(result);
        }

        private static IActionResult ToActionResult<TPayload>(ApplicationResult<TPayload> result)
        {
            if (IsAlreadyExists(result) && result.Value is not null)
            {
                return new ObjectResult(CommerceNodeApiResponse<TPayload>.Succeeded(result.Value, NormalizeMessage(result.Message)))
                {
                    StatusCode = StatusCodes.Status200OK,
                };
            }

            return result.ToCommerceNodeActionResult();
        }

        private static bool IsAlreadyExists<TPayload>(ApplicationResult<TPayload> result)
        {
            return !result.Success
                && string.Equals(result.Error?.Code, "task.already_exists", StringComparison.Ordinal);
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Commerce Node task request could not be completed."
                : message;
        }
    }
}

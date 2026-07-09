namespace BlazorShop.CommerceNode.API.Controllers
{
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

        private static IActionResult ToActionResult<TPayload>(CommerceTaskOperationResult<TPayload> result)
        {
            var response = result.Success
                ? CommerceNodeApiResponse<TPayload>.Succeeded(result.Payload, NormalizeMessage(result.Message))
                : CommerceNodeApiResponse<TPayload>.Failed(NormalizeMessage(result.Message), result.Payload);

            return new ObjectResult(response)
            {
                StatusCode = result.Success ? StatusCodes.Status200OK : ToStatusCode(result.Failure),
            };
        }

        private static int ToStatusCode(CommerceTaskOperationFailure? failure)
        {
            return failure switch
            {
                CommerceTaskOperationFailure.Validation => StatusCodes.Status400BadRequest,
                CommerceTaskOperationFailure.NotFound => StatusCodes.Status404NotFound,
                CommerceTaskOperationFailure.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The Commerce Node task request could not be completed."
                : message;
        }
    }
}

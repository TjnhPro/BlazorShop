namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/internal/store")]
    public sealed class StorefrontStoreController : StorefrontApiControllerBase
    {
        private readonly ICommerceStoreContext storeContext;

        public StorefrontStoreController(ICommerceStoreContext storeContext)
        {
            this.storeContext = storeContext;
        }

        [HttpGet("current")]
        public async Task<IActionResult> Current(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> Maintenance(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return ToActionResult(result);
            }

            var maintenance = new
            {
                result.Payload.PublicId,
                result.Payload.StoreKey,
                result.Payload.Name,
                result.Payload.MaintenanceModeEnabled,
                result.Payload.MaintenanceMessage,
            };

            return this.Ok(CommerceNodeApiResponse<object>.Succeeded(maintenance, "Store maintenance state retrieved."));
        }

        private static IActionResult ToActionResult<TPayload>(CommerceStoreOperationResult<TPayload> result)
        {
            var response = result.Success
                ? CommerceNodeApiResponse<TPayload>.Succeeded(result.Payload, NormalizeMessage(result.Message))
                : CommerceNodeApiResponse<TPayload>.Failed(NormalizeMessage(result.Message), result.Payload);

            return new ObjectResult(response)
            {
                StatusCode = result.Success ? StatusCodes.Status200OK : ToStatusCode(result.Failure),
            };
        }

        private static int ToStatusCode(CommerceStoreOperationFailure? failure)
        {
            return failure switch
            {
                CommerceStoreOperationFailure.Validation => StatusCodes.Status400BadRequest,
                CommerceStoreOperationFailure.NotFound => StatusCodes.Status404NotFound,
                CommerceStoreOperationFailure.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The current store could not be resolved."
                : message;
        }
    }
}

namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/message-templates")]
    public sealed class CommerceMessageTemplatesController : CommerceAdminControllerBase
    {
        private readonly ITransactionalMessageAdminService messageAdminService;

        public CommerceMessageTemplatesController(ITransactionalMessageAdminService messageAdminService)
        {
            this.messageAdminService = messageAdminService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<MessageTemplateAdminSummary>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<MessageTemplateAdminSummary>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<IReadOnlyList<MessageTemplateAdminSummary>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.ListTemplatesAsync(cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{publicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.GetTemplateAsync(publicId, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{publicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            Guid publicId,
            [FromBody] UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.UpdateTemplateAsync(publicId, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("{publicId:guid}/reset")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Reset(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.ResetTemplateAsync(publicId, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("preview")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplatePreviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplatePreviewResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplatePreviewResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<MessageTemplatePreviewResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Preview(
            [FromBody] PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.PreviewTemplateAsync(request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }

    [ApiController]
    [Route("api/commerce/admin/queued-messages")]
    public sealed class CommerceQueuedMessagesController : CommerceAdminControllerBase
    {
        private readonly ITransactionalMessageAdminService messageAdminService;

        public CommerceQueuedMessagesController(ITransactionalMessageAdminService messageAdminService)
        {
            this.messageAdminService = messageAdminService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminListResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminListResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminListResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] string? templateSystemName,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await this.messageAdminService.ListQueuedMessagesAsync(
                status,
                templateSystemName,
                skip,
                take,
                cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{publicId:guid}")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.GetQueuedMessageAsync(publicId, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("{publicId:guid}/retry")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Retry(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.RetryQueuedMessageAsync(publicId, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("{publicId:guid}/cancel")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cancel(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.messageAdminService.CancelQueuedMessageAsync(publicId, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}

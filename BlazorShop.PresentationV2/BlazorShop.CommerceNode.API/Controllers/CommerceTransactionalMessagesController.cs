namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/commerce/admin/email-settings")]
    public sealed class CommerceStoreEmailSettingsController : CommerceAdminControllerBase
    {
        private readonly ICommerceStoreContext storeContext;
        private readonly IStoreEmailSettingsService settingsService;
        private readonly IStoreEmailTestSendService testSendService;
        private readonly StoreEmailTransportOptions transportOptions;

        public CommerceStoreEmailSettingsController(
            ICommerceStoreContext storeContext,
            IStoreEmailSettingsService settingsService,
            IStoreEmailTestSendService testSendService,
            IOptions<StoreEmailTransportOptions> transportOptions)
        {
            this.storeContext = storeContext;
            this.settingsService = settingsService;
            this.testSendService = testSendService;
            this.transportOptions = transportOptions.Value;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.Failure<StoreEmailSettingsResponse>(ServiceResponseType.ValidationError, storeResult.Message ?? "Store scope is required.");
            }

            return this.FromServiceResponse(await this.settingsService.GetAsync(storeResult.Payload, cancellationToken));
        }

        [HttpPut]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromBody] UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.Failure<StoreEmailSettingsResponse>(ServiceResponseType.ValidationError, storeResult.Message ?? "Store scope is required.");
            }

            return this.FromServiceResponse(await this.settingsService.UpdateAsync(
                storeResult.Payload,
                request,
                this.User.Identity?.Name,
                this.transportOptions.CaptureModeAllowed,
                cancellationToken));
        }

        [HttpPost("password/rotate")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RotatePassword(
            [FromBody] RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.Failure<StoreEmailSettingsResponse>(ServiceResponseType.ValidationError, storeResult.Message ?? "Store scope is required.");
            }

            var current = await this.settingsService.GetAsync(storeResult.Payload, cancellationToken);
            if (!current.Success || current.Payload is null)
            {
                return this.FromServiceResponse(current);
            }

            return this.FromServiceResponse(await this.settingsService.UpdateAsync(
                storeResult.Payload,
                ToUpdateRequest(current.Payload, request.Password, clearPassword: false, disable: false),
                this.User.Identity?.Name,
                this.transportOptions.CaptureModeAllowed,
                cancellationToken));
        }

        [HttpPost("password/clear")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ClearPassword(CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.Failure<StoreEmailSettingsResponse>(ServiceResponseType.ValidationError, storeResult.Message ?? "Store scope is required.");
            }

            var current = await this.settingsService.GetAsync(storeResult.Payload, cancellationToken);
            if (!current.Success || current.Payload is null)
            {
                return this.FromServiceResponse(current);
            }

            return this.FromServiceResponse(await this.settingsService.UpdateAsync(
                storeResult.Payload,
                ToUpdateRequest(current.Payload, password: null, clearPassword: true, disable: true),
                this.User.Identity?.Name,
                this.transportOptions.CaptureModeAllowed,
                cancellationToken));
        }

        [HttpPost("test-send")]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<SendStoreEmailTestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<SendStoreEmailTestResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<SendStoreEmailTestResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CommerceNodeApiResponse<SendStoreEmailTestResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTest(
            [FromBody] SendStoreEmailTestRequest request,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.Failure<SendStoreEmailTestResponse>(ServiceResponseType.ValidationError, storeResult.Message ?? "Store scope is required.");
            }

            return this.FromServiceResponse(await this.testSendService.SendAsync(storeResult.Payload, request, cancellationToken));
        }

        private static UpdateStoreEmailSettingsRequest ToUpdateRequest(
            StoreEmailSettingsResponse current,
            string? password,
            bool clearPassword,
            bool disable)
        {
            return new UpdateStoreEmailSettingsRequest
            {
                Enabled = !disable && current.Enabled,
                SmtpHost = current.SmtpHost,
                SmtpPort = current.SmtpPort,
                UseSsl = current.UseSsl,
                Username = current.Username,
                Password = password,
                ClearPassword = clearPassword,
                UseExistingPassword = !clearPassword,
                FromEmail = current.FromEmail,
                FromDisplayName = current.FromDisplayName,
                ReplyToEmail = current.ReplyToEmail,
                DeliveryMode = current.DeliveryMode,
                CaptureRedirectToEmail = current.CaptureRedirectToEmail,
            };
        }
    }

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

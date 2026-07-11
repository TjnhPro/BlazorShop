namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/variation-templates")]
    public sealed class CommerceVariationTemplatesController : CommerceAdminControllerBase
    {
        private readonly IVariationTemplateService variationTemplateService;

        public CommerceVariationTemplatesController(IVariationTemplateService variationTemplateService)
        {
            this.variationTemplateService = variationTemplateService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await this.variationTemplateService.ListAsync(
                new VariationTemplateListQuery(pageNumber, pageSize),
                cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVariationTemplateRequest request, CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.CreateAsync(request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.GetByIdAsync(id, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVariationTemplateRequest request, CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.UpdateAsync(id, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.DeleteAsync(id, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("{id:guid}/options")]
        public async Task<IActionResult> CreateOption(
            Guid id,
            [FromBody] CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.CreateOptionAsync(id, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/options/{optionId:guid}")]
        public async Task<IActionResult> UpdateOption(
            Guid id,
            Guid optionId,
            [FromBody] UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.UpdateOptionAsync(id, optionId, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPost("{id:guid}/options/{optionId:guid}/values")]
        public async Task<IActionResult> CreateValue(
            Guid id,
            Guid optionId,
            [FromBody] CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.CreateValueAsync(id, optionId, request, cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpPut("{id:guid}/options/{optionId:guid}/values/{valueId:guid}")]
        public async Task<IActionResult> UpdateValue(
            Guid id,
            Guid optionId,
            Guid valueId,
            [FromBody] UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.variationTemplateService.UpdateValueAsync(id, optionId, valueId, request, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }
}

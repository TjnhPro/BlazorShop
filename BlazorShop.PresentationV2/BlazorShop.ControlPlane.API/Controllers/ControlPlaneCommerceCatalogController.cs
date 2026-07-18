namespace BlazorShop.ControlPlane.API.Controllers
{
    using BlazorShop.Application.ControlPlane.Security;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/stores/{storePublicId:guid}/catalog")]
    [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
    public sealed class ControlPlaneCommerceCatalogController : ControllerBase
    {
    }
}

using System.Security.Claims;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/owners/me/dashboard")]
    [Authorize(Roles = Role.EvOwner)]
    public sealed class OwnersDashboardController : ControllerBase
    {
        private readonly IBookingService _svc;
        public OwnersDashboardController(IBookingService svc) { _svc = svc; }

        private string Nic() =>
            User.FindFirstValue("nic") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            throw new UnauthorizedAccessException("NIC missing from token.");

        [HttpGet]
        public async Task<ActionResult> Get() => Ok(await _svc.OwnerDashboardAsync(Nic()));
    }
}

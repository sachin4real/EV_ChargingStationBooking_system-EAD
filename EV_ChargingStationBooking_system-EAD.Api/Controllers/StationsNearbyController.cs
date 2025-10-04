using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/stations/nearby")]
    public sealed class StationsNearbyController : ControllerBase
    {
        private readonly IChargingStationService _svc;
        public StationsNearbyController(IChargingStationService svc) => _svc = svc;

        // allow any authenticated user (EvOwner/Operator/Backoffice)
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Get([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusKm = 5)
            => Ok(await _svc.NearbyAsync(lat, lng, radiusKm <= 0 ? 5 : radiusKm));
    }
}

using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/owners/me/bookings")]
    [Authorize(Roles = Role.EvOwner)]
    public sealed class OwnerBookingsController : ControllerBase
    {
        private readonly IBookingService _svc;
        public OwnerBookingsController(IBookingService svc) => _svc = svc;

        private string Nic() =>
            User.FindFirstValue("nic") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            throw new UnauthorizedAccessException("NIC missing from token.");

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] BookingCreateDto dto)
        {
            var b = await _svc.OwnerCreateAsync(Nic(), dto);
            return CreatedAtAction(nameof(GetMine), new { id = b.Id }, b);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update([FromRoute] string id, [FromBody] BookingUpdateDto dto)
            => Ok(await _svc.OwnerUpdateAsync(Nic(), id, dto));

        [HttpDelete("{id}")]
        public async Task<ActionResult> Cancel([FromRoute] string id)
        {
            await _svc.OwnerCancelAsync(Nic(), id);
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _svc.OwnerListAsync(Nic(), page, pageSize);
            return Ok(new { total, items });
        }
    }
}

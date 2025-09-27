using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/admin/owners")]
    [Authorize(Roles = Role.Backoffice)]
    public sealed class AdminOwnersController : ControllerBase
    {
        private readonly IEvOwnerService _svc;
        public AdminOwnersController(IEvOwnerService svc) => _svc = svc;

        /// <summary>Create an EV owner (admin)</summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] EvOwnerCreateDto dto)
        {
            var created = await _svc.AdminCreateAsync(dto, actorUserId: "admin");
            return CreatedAtAction(nameof(GetByNic), new { nic = created.Nic }, created);
        }

        /// <summary>Get a specific EV owner by NIC</summary>
        [HttpGet("{nic}")]
        public async Task<ActionResult> GetByNic([FromRoute] string nic)
            => Ok(await _svc.AdminGetAsync(nic));

        /// <summary>Search owners (q by name/email/nic, status, paging)</summary>
        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] OwnerListQuery q)
        {
            var (items, total) = await _svc.AdminSearchAsync(q);
            return Ok(new { total, items });
        }

        /// <summary>Update owner profile (admin)</summary>
        [HttpPut("{nic}")]
        public async Task<ActionResult> Update([FromRoute] string nic, [FromBody] EvOwnerUpdateDto dto)
            => Ok(await _svc.AdminUpdateAsync(nic, dto, actorUserId: "admin"));

        /// <summary>Activate/Deactivate owner (admin)</summary>
        [HttpPatch("{nic}/status")]
        public async Task<ActionResult> SetStatus([FromRoute] string nic, [FromBody] OwnerStatusDto dto)
        {
            await _svc.AdminSetActiveAsync(nic, dto.IsActive, actorUserId: "admin", reason: dto.Reason);
            return NoContent();
        }

        /// <summary>Hard delete owner (only if no bookings exist)</summary>
        [HttpDelete("{nic}")]
        public async Task<ActionResult> HardDelete([FromRoute] string nic)
        {
            await _svc.AdminHardDeleteAsync(nic, actorUserId: "admin");
            return NoContent();
        }
    }
}

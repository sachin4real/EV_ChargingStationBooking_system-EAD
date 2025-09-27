using System.Security.Claims;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/owners/me")]
    [Authorize(Roles = Role.EvOwner)]
    public sealed class OwnersSelfController : ControllerBase
    {
        private readonly IEvOwnerService _svc;
        public OwnersSelfController(IEvOwnerService svc) => _svc = svc;

        private string GetNicFromToken()
        {
            // Prefer explicit "nic" claim if present
            var nic = User.FindFirstValue("nic")
                    ?? User.FindFirstValue("sub")
                    ?? User.Claims.FirstOrDefault(c => c.Type.EndsWith("unique_name"))?.Value
                    ?? User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(nic))
                throw new UnauthorizedAccessException("NIC missing from token.");
            return nic;
        }

        /// <summary>My profile (owner)</summary>
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile() => Ok(await _svc.SelfGetAsync(GetNicFromToken()));

        /// <summary>Update my profile (owner)</summary>
        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile([FromBody] EvOwnerUpdateDto dto)
        {
            var (profile, _) = await _svc.SelfUpdateAsync(GetNicFromToken(), dto);
            return Ok(new { profile });
        }

        /// <summary>Self-deactivate (owner)</summary>
        [HttpPost("deactivate")]
        public async Task<ActionResult> Deactivate()
        {
            await _svc.SelfDeactivateAsync(GetNicFromToken());
            return NoContent();
        }
    }
}

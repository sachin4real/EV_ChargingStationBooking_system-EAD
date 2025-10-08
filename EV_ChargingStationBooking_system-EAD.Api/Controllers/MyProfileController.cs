using System.Security.Claims;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/users/me")]
    [Authorize(Roles = $"{Role.Backoffice},{Role.Operator},{Role.EvOwner}")]
    public sealed class MyProfileController : ControllerBase
    {
        private readonly IMyProfileService _svc;
        private readonly IJwtTokenService _jwt;
        private readonly Infrastructure.Repositories.IAuthUserRepository _usersRepo;

        public MyProfileController(
            IMyProfileService svc,
            IJwtTokenService jwt,
            Infrastructure.Repositories.IAuthUserRepository usersRepo)
        {
            _svc = svc;
            _jwt = jwt;
            _usersRepo = usersRepo;
        }

        private string GetUsernameFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.Claims.FirstOrDefault(c => c.Type.EndsWith("unique_name"))?.Value
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("No username in token.");
        }

        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            var username = GetUsernameFromClaims();
            var user = await _usersRepo.GetByUsernameAsync(username)
                       ?? throw new KeyNotFoundException("User not found.");
            var profile = await _svc.GetAsync(user.Id);
            return Ok(profile);
        }

        /// <summary>
        /// Update my profile; if 'currentPassword' and 'newPassword' are provided, password is changed in the same call.
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile([FromBody] MyProfileUpdateDto dto)
        {
            var username = GetUsernameFromClaims();
            var user = await _usersRepo.GetByUsernameAsync(username)
                       ?? throw new KeyNotFoundException("User not found.");

            var (profile, emailChanged) = await _svc.UpdateAsync(user.Id, dto);

            if (emailChanged)
            {
                var newToken = _jwt.CreateToken(user.Id, profile.Username, profile.Role);
                return Ok(new { profile, newToken });
            }
            return Ok(new { profile });
        }

        // Remove this route, or keep it as obsolete if clients already use it.
        [Obsolete("Use PUT /api/users/me/profile with currentPassword/newPassword instead.")]
        [HttpPut("password")]
        public IActionResult ObsoleteChangePassword() => NotFound();
    }
}

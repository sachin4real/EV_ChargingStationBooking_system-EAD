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

        // Return a flat shape the web app expects, and INCLUDE stationIds
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            var username = GetUsernameFromClaims();

            var user = await _usersRepo.GetByUsernameAsync(username)
                       ?? throw new KeyNotFoundException("User not found.");

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                role = user.Role,
                fullName = user.FullName,
                phone = user.Phone,
                ownerNic = user.OwnerNic,               // from AuthUser
                stationIds = user.StationIds ?? new List<string>() // ← important for operator
            });
        }

        /// <summary>
        /// Update my profile; if 'currentPassword' and 'newPassword' are provided, password is changed in the same call.
        /// Returns the updated profile (including stationIds) and a new token when email/username changes.
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile([FromBody] MyProfileUpdateDto dto)
        {
            var username = GetUsernameFromClaims();
            var user = await _usersRepo.GetByUsernameAsync(username)
                       ?? throw new KeyNotFoundException("User not found.");

            var (profile, emailChanged) = await _svc.UpdateAsync(user.Id, dto);

            // Re-read the auth user to include stationIds (and ownerNic) in the response
            var updated = await _usersRepo.GetByIdAsync(user.Id)
                          ?? throw new KeyNotFoundException("User not found.");

            var payload = new
            {
                id = updated.Id,
                username = profile.Username,   // MyProfileDto has these
                role = profile.Role,
                fullName = profile.FullName,
                phone = profile.Phone,
                ownerNic = updated.OwnerNic,   // ← use AuthUser; MyProfileDto doesn't have OwnerNic
                stationIds = updated.StationIds ?? new List<string>()
            };

            if (emailChanged)
            {
                var newToken = _jwt.CreateToken(updated.Id, profile.Username, profile.Role);
                return Ok(new { profile = payload, newToken });
            }

            return Ok(new { profile = payload });
        }

        [Obsolete("Use PUT /api/users/me/profile with currentPassword/newPassword instead.")]
        [HttpPut("password")]
        public IActionResult ObsoleteChangePassword() => NotFound();
    }
}

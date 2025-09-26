
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
    // only backoffice and operator can use web-profile endpoints
    [Authorize(Roles = $"{Role.Backoffice},{Role.Operator}")]
    public sealed class MyProfileController : ControllerBase
    {
        private readonly IMyProfileService _svc;
        private readonly IJwtTokenService _jwt;

        public MyProfileController(IMyProfileService svc, IJwtTokenService jwt)
        {
            _svc = svc;
            _jwt = jwt;
        }

        private string GetUserIdOrUsernameFromClaims()
        {
            var username = User.FindFirstValue(ClaimTypes.Name)
                           ?? User.Claims.FirstOrDefault(c => c.Type.EndsWith("unique_name"))?.Value
                           ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub");
            return username ?? throw new UnauthorizedAccessException("No username in token.");
        }

        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile([FromServices] EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories.IAuthUserRepository usersRepo)
        {
            var username = GetUserIdOrUsernameFromClaims();
            var user = await usersRepo.GetByUsernameAsync(username) ?? throw new KeyNotFoundException("User Not FOund");
            var profile = await _svc.GetAsync(user.Id);
            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile(
            [FromBody] MyProfileUpdateDto dto,
            [FromServices] EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories.IAuthUserRepository usersRepo)
        {
            var username = GetUserIdOrUsernameFromClaims();
            var user = await usersRepo.GetByUsernameAsync(username) ?? throw new KeyNotFoundException("User not found.");
            var (profile, emailChanged) = await _svc.UpdateAsync(user.Id, dto);

            if (emailChanged)
            {
                var newToken = _jwt.CreateToken(user.Id, profile.Username, profile.Role);
                return Ok(new { profile, newToken });
            }
            return Ok(new { profile });
        }
        [HttpPut("password")]
        public async Task<ActionResult> ChangePassword(
            [FromBody] ChangePasswordDto dto,
            [FromServices] EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories.IAuthUserRepository usersRepo)
        {
            var username = GetUserIdOrUsernameFromClaims();
            var user = await usersRepo.GetByUsernameAsync(username) ?? throw new KeyNotFoundException("User not found.");

            await _svc.ChangePasswordAsync(user.Id, dto);
            return NoContent();
        }
    }
}
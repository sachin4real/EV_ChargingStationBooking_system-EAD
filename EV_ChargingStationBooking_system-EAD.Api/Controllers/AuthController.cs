 
using System.Diagnostics;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IJwtTokenService _jwt;

        public AuthController(IAuthService auth, IJwtTokenService jwt)
        {
            _auth = auth;
            _jwt = jwt;
        }

        /// <summary> Login for all roles (username = email for staff, NIC for owners) </summary>

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _auth.ValidateCredentialsAsync(dto.Username, dto.Password);
            var token = _jwt.CreateToken(user.Id, user.Username, user.Role);
            return Ok(new { token, role = user.Role, username = user.Username });
        }

        // POST /api/auth/register/backoffice  (BACKOFFICE can create another BACKOFFICE)
        [HttpPost("register/backoffice")]
        [Authorize(Roles = Role.Backoffice)]
        public async Task<ActionResult> RegisterBackoffice([FromBody] RegisterStaffDto dto)
        {
            var forced = new RegisterStaffDto { Email = dto.Email, Password = dto.Password, Role = Role.Backoffice };
            var u = await _auth.RegisterStaffAsync(forced);
            return CreatedAtAction(nameof(Me), new { }, new { u.Username, u.Role });
        }

        // POST /api/auth/register/operator  (BACKOFFICE creates OPERATOR)
        [HttpPost("register/operator")]
        [Authorize(Roles = Role.Backoffice)]
        public async Task<ActionResult> RegisterOperator([FromBody] RegisterStaffDto dto)
        {
            var forced = new RegisterStaffDto { Email = dto.Email, Password = dto.Password, Role = Role.Operator };
            var u = await _auth.RegisterStaffAsync(forced);
            return CreatedAtAction(nameof(Me), new { }, new { u.Username, u.Role });
        }

        /// <summary> Register EV owner (public) </summary>
        [HttpPost("register/owner")]
        [AllowAnonymous]
        public async Task<ActionResult> RegisterOwner([FromBody] RegisterOwnerDto dto)
        {
            var u = await _auth.RegisterOwnerAsync(dto);
            return CreatedAtAction(nameof(Me), new { }, new { u.Username, u.Role });
        }

        /// <summary> Return info from JWT </summary>
        [HttpGet("me")]
        [Authorize]
        public ActionResult Me()
        {
            var name = User.Identity?.Name;
            var username = User.Claims.FirstOrDefault(c => c.Type.EndsWith("unique_name"))?.Value;
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            return Ok(new { username, role });
        }
    }
}
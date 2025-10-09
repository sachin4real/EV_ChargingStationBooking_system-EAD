using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/admin/staff")]
    [Authorize(Roles = Role.Backoffice)]
    public sealed class AdminStaffController : ControllerBase
    {
        private readonly IAdminStaffService _svc;
        private readonly IAuthService _auth;
        private readonly IAuthUserRepository _users;

        public AdminStaffController(IAdminStaffService svc, IAuthService auth, IAuthUserRepository users)
        {
            _svc = svc;
            _auth = auth;
            _users = users;
        }

        // List/search
        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] StaffListQuery q)
        {
            var (items, total) = await _svc.SearchAsync(q);
            return Ok(new { total, items });
        }

        // Get by id
        [HttpGet("{id}")]
        public async Task<ActionResult> GetById([FromRoute] string id)
            => Ok(await _svc.GetAsync(id));

        // CREATE: delegate hashing to AuthService, then assign station(s) if Operator
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] StaffCreateDto dto)
        {
            // 1) Create via AuthService (BCrypt inside)
            var created = await _auth.RegisterStaffAsync(new RegisterStaffDto
            {
                Email    = dto.Email,
                Password = dto.Password,
                Role     = dto.Role
            });

            // 2) Save optional profile fields + station assignment
            bool dirty = false;

            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                created.FullName = dto.FullName.Trim();
                dirty = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                created.Phone = dto.Phone.Trim();
                dirty = true;
            }

            var isOperator = string.Equals(created.Role, Role.Operator, StringComparison.OrdinalIgnoreCase);
            if (isOperator && dto.StationIds != null && dto.StationIds.Count > 0)
            {
                created.Role = Role.Operator; // ensure title-case
                created.StationIds = dto.StationIds.Distinct().ToList();
                dirty = true;
            }

            if (dirty) await _users.UpdateAsync(created);

            // 3) Return a view DTO
            var view = new StaffUserViewDto
            {
                Id           = created.Id,
                Email        = created.Username,
                Role         = created.Role,
                FullName     = created.FullName,
                Phone        = created.Phone,
                CreatedAtUtc = created.CreatedAtUtc,
                StationIds   = created.StationIds?.ToList() ?? new List<string>()
            };
            return Ok(view);
        }

        // UPDATE: profile/role/stations
        [HttpPatch("{id}")]
        public async Task<ActionResult> Update([FromRoute] string id, [FromBody] StaffUpdateDto dto)
            => Ok(await _svc.UpdateAsync(id, dto));
    }
}

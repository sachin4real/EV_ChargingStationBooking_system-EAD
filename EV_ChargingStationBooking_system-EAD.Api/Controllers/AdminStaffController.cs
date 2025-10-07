using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
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
        public AdminStaffController(IAdminStaffService svc) => _svc = svc;

        /// <summary>
        /// List staff users (Backoffice/Operator) with optional search/paging.
        /// Examples:
        ///   /api/admin/staff
        ///   /api/admin/staff?role=Backoffice
        ///   /api/admin/staff?q=fernando&page=2&pageSize=10
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] StaffListQuery q)
        {
            var (items, total) = await _svc.SearchAsync(q);
            return Ok(new { total, items });
        }

        /// <summary>Get a single staff user by id</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult> GetById([FromRoute] string id)
            => Ok(await _svc.GetAsync(id));
    }
}

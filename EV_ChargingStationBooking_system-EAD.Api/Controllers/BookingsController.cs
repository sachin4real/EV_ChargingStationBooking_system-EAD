using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public sealed class BookingsController : ControllerBase
    {
        private readonly IBookingService _svc;
        public BookingsController(IBookingService svc) => _svc = svc;

        // Search/admin list
        [HttpGet]
        [Authorize(Roles = $"{Role.Backoffice},{Role.Operator}")]
        public async Task<ActionResult> Search([FromQuery] BookingListQuery q)
        {
            var (items, total) = await _svc.AdminSearchAsync(q);
            return Ok(new { total, items });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{Role.Backoffice},{Role.Operator}")]
        public async Task<ActionResult> GetById(string id) => Ok(await _svc.GetAsync(id));

        // ✅ Operator ONLY – approve a booking
        [HttpPost("{id}/approve")]
        [Authorize(Roles = Role.Operator)]
        public async Task<ActionResult> Approve(string id)
            => Ok(await _svc.ApproveAsync(id, actorUserId: User.Identity?.Name ?? "operator"));

        // ✅ Operator ONLY – finalize/complete after session ends
        [HttpPost("{id}/finalize")]
        [Authorize(Roles = Role.Operator)]
        public async Task<ActionResult> Finalize(string id)
            => Ok(await _svc.FinalizeAsync(id, actorUserId: User.Identity?.Name ?? "operator"));
    }
}

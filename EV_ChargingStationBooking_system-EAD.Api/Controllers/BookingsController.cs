using System.Security.Claims;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;
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
        private readonly IBookingRepository _repo;          // NEW
        private readonly IAuthUserRepository _users;        // NEW

        public BookingsController(IBookingService svc, IBookingRepository repo, IAuthUserRepository users)
        {
            _svc = svc;
            _repo = repo;
            _users = users;
        }

        // Search/admin list
        [HttpGet]
        [Authorize(Roles = $"{Role.Backoffice},{Role.Operator}")]
        public async Task<ActionResult> Search([FromQuery] BookingListQuery q)
        {
            // Backoffice = unchanged
            if (User.IsInRole(Role.Backoffice))
            {
                var (itemsBackoffice, totalBackoffice) = await _svc.AdminSearchAsync(q);
                return Ok(new { total = totalBackoffice, items = itemsBackoffice });
            }

            // Operator = enforce station scope on server
            if (!User.IsInRole(Role.Operator))
                return Forbid();

            // Get current staff user
            var username = User.Identity?.Name
                           ?? User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(username))
                return Forbid();

            var me = await _users.GetByUsernameAsync(username);
            var scope = me?.StationIds ?? new List<string>();
            if (scope.Count == 0)
                return Ok(new { total = 0, items = Array.Empty<object>() });

            // If a specific stationId is requested, ensure it's within scope; otherwise use whole scope
            IReadOnlyCollection<string> stationIdsToQuery;
            if (!string.IsNullOrWhiteSpace(q.StationId))
            {
                stationIdsToQuery = scope.Contains(q.StationId) ? new[] { q.StationId } : Array.Empty<string>();
                if (stationIdsToQuery.Count == 0)
                    return Ok(new { total = 0, items = Array.Empty<object>() });
            }
            else
            {
                stationIdsToQuery = scope;
            }

            // Translate common paging/options from your query DTO
            var page = q.Page <= 0 ? 1 : q.Page;
            var pageSize = q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 200);
            var skip = (page - 1) * pageSize;

            DateTime? fromUtc = q.FutureOnly == true ? DateTime.UtcNow : null;
            DateTime? toUtc = null; // extend if your DTO exposes a To/End filter

            var (items, total) = await _repo.SearchManyAsync(
                stationIdsToQuery,
                q.Status,
                fromUtc,
                toUtc,
                skip,
                pageSize);

            return Ok(new { total, items });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{Role.Backoffice},{Role.Operator}")]
        public async Task<ActionResult> GetById(string id)
        {
            if (User.IsInRole(Role.Backoffice))
                return Ok(await _svc.GetAsync(id));

            // Operator: guard by scope
            var booking = await _repo.GetByIdAsync(id);
            if (booking is null) return NotFound();

            var username = User.Identity?.Name
                           ?? User.FindFirstValue(ClaimTypes.Name);
            var me = await _users.GetByUsernameAsync(username ?? string.Empty);
            var scope = me?.StationIds ?? new List<string>();
            if (!scope.Contains(booking.StationId))
                return Forbid();

            return Ok(await _svc.GetAsync(id));
        }

        // ✅ Operator ONLY – approve a booking
        [HttpPost("{id}/approve")]
        [Authorize(Roles = Role.Operator)]
        public async Task<ActionResult> Approve(string id)
        {
            // Guard by station scope
            var booking = await _repo.GetByIdAsync(id);
            if (booking is null) return NotFound();

            var username = User.Identity?.Name
                           ?? User.FindFirstValue(ClaimTypes.Name);
            var me = await _users.GetByUsernameAsync(username ?? string.Empty);
            var scope = me?.StationIds ?? new List<string>();
            if (!scope.Contains(booking.StationId))
                return Forbid();

            return Ok(await _svc.ApproveAsync(id, actorUserId: username ?? "operator"));
        }

        // ✅ Operator ONLY – finalize/complete after session ends
        [HttpPost("{id}/finalize")]
        [Authorize(Roles = Role.Operator)]
        public async Task<ActionResult> Finalize(string id)
        {
            // Guard by station scope
            var booking = await _repo.GetByIdAsync(id);
            if (booking is null) return NotFound();

            var username = User.Identity?.Name
                           ?? User.FindFirstValue(ClaimTypes.Name);
            var me = await _users.GetByUsernameAsync(username ?? string.Empty);
            var scope = me?.StationIds ?? new List<string>();
            if (!scope.Contains(booking.StationId))
                return Forbid();

            return Ok(await _svc.FinalizeAsync(id, actorUserId: username ?? "operator"));
        }
    }
}

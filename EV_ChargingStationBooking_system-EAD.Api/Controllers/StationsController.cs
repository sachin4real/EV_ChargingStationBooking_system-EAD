using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers
{
    [ApiController]
    [Route("api/stations")]
    public sealed class StationsController : ControllerBase
    {
        private readonly IChargingStationService _svc;
        public StationsController(IChargingStationService svc) => _svc = svc;

        // POST /stations  → Backoffice only
        [HttpPost]
        [Authorize(Roles = $"{Role.Backoffice}")]
        public async Task<ActionResult> Create([FromBody] StationCreateDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // GET (optional, handy in Swagger)
        [HttpGet]
        [Authorize] // any authenticated (adjust if you want public list)
        public async Task<ActionResult> List([FromQuery] string? q, [FromQuery] bool? isActive,
                                             [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _svc.ListAsync(q, isActive, page, pageSize);
            return Ok(new { total, items });
        }

        // GET /stations/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetById([FromRoute] string id)
            => Ok(await _svc.GetAsync(id));

        [HttpPatch("{id}/activate")]
        [Authorize(Roles = Role.Backoffice)]
        public async Task<ActionResult> Activate(string id)
        {
            await _svc.ActivateAsync(id);
            return NoContent(); // 204
        }

        // PUT /stations/{id} → Backoffice
        [HttpPut("{id}")]
        [Authorize(Roles = $"{Role.Backoffice}")]
        public async Task<ActionResult> Update([FromRoute] string id, [FromBody] StationUpdateDto dto)
            => Ok(await _svc.UpdateAsync(id, dto));

        // PUT /stations/{id}/schedule → Backoffice + Operator
        [HttpPut("{id}/schedule")]
        [Authorize(Roles = $"{Role.Backoffice},{Role.Operator}")]
        public async Task<ActionResult> UpdateSchedule([FromRoute] string id, [FromBody] StationScheduleUpdateDto dto)
            => Ok(await _svc.UpdateScheduleAsync(id, dto));

        // PATCH /stations/{id}/deactivate → Backoffice (service enforces “no active bookings”)
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = $"{Role.Backoffice}")]
        public async Task<ActionResult> Deactivate([FromRoute] string id)
        {
            await _svc.DeactivateAsync(id);
            return NoContent();
        }
    }
}

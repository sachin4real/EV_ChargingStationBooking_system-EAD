using System.Security.Claims;
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers;

[ApiController]
[Authorize(Roles = Role.EvOwner)]
public sealed class OwnerSelfController : ControllerBase
{
    private readonly IOwnerRepository _owners;
    public OwnerSelfController(IOwnerRepository owners) => _owners = owners;

    [HttpPatch("/me/deactivate")]
    public async Task<ActionResult> DeactivateMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var owner = await _owners.Col.Find(x => x.UserId == userId).FirstOrDefaultAsync();
        if (owner is null) return NotFound();

        var upd = Builders<EvOwner>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.DeactivatedBySelf, true)
            .Set(x => x.DeactivatedAtUtc, DateTime.UtcNow);

        await _owners.Col.UpdateOneAsync(x => x.UserId == userId, upd);
        return NoContent();
    }
}

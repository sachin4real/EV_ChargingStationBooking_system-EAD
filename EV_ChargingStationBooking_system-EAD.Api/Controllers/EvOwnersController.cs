using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers;

[ApiController]
[Route("users/owners")]
[Authorize(Roles = Role.Backoffice)]
public sealed class EvOwnersController : ControllerBase
{
    private readonly IOwnerRepository _owners;
    private readonly IAuthUserRepository _users;

    public EvOwnersController(IOwnerRepository owners, IAuthUserRepository users)
    {
        _owners = owners; _users = users;
    }

    // (optional admin create) POST /users/owners
    [HttpPost]
    public async Task<ActionResult> CreateOwner([FromBody] CreateOwnerBackofficeDto dto)
    {
        if (await _owners.Col.Find(x => x.Nic == dto.Nic).AnyAsync())
            return Conflict(new { message = "NIC already exists" });
        if (await _users.GetByUsernameAsync(dto.Email) is not null)
            return Conflict(new { message = "Email already exists" });

        var user = new AuthUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Username = dto.Email,                          // username = email
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Role.EvOwner,
            OwnerNic = dto.Nic
        };
        await _users.CreateAsync(user);

        var owner = new EvOwner
        {
            Nic = dto.Nic,
            UserId = user.Id,
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            IsActive = true
        };
        await _owners.Col.InsertOneAsync(owner);

        return Created($"/users/owners/{owner.Nic}", owner);
    }

    // GET /users/owners?q=&status=&page=&pageSize=
    [HttpGet]
    public async Task<ActionResult> List([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = Builders<EvOwner>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var r = new MongoDB.Bson.BsonRegularExpression(q, "i");
            filter &= Builders<EvOwner>.Filter.Or(
                Builders<EvOwner>.Filter.Regex(x => x.Nic, r),
                Builders<EvOwner>.Filter.Regex(x => x.FullName, r),
                Builders<EvOwner>.Filter.Regex(x => x.Phone, r));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase)) filter &= Builders<EvOwner>.Filter.Eq(x => x.IsActive, true);
            else if (status.Equals("disabled", StringComparison.OrdinalIgnoreCase)) filter &= Builders<EvOwner>.Filter.Eq(x => x.IsActive, false);
        }

        var total = await _owners.Col.CountDocumentsAsync(filter);
        var items = await _owners.Col.Find(filter).SortByDescending(x => x.CreatedAtUtc).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
        return Ok(new { page, pageSize, total, items });
    }

    // PATCH /users/owners/{nic}/activate
    [HttpPatch("{nic}/activate")]
    public async Task<ActionResult> Activate(string nic)
    {
        var owner = await _owners.Col.Find(x => x.Nic == nic).FirstOrDefaultAsync();
        if (owner is null) return NotFound();

        var upd = Builders<EvOwner>.Update.Set(x => x.IsActive, true).Set(x => x.DeactivatedBySelf, false).Set(x => x.DeactivatedAtUtc, null);
        await _owners.Col.UpdateOneAsync(x => x.Nic == nic, upd);
        return NoContent();
    }

    // PATCH /users/owners/{nic}/deactivate
    [HttpPatch("{nic}/deactivate")]
    public async Task<ActionResult> Deactivate(string nic)
    {
        var owner = await _owners.Col.Find(x => x.Nic == nic).FirstOrDefaultAsync();
        if (owner is null) return NotFound();

        var upd = Builders<EvOwner>.Update.Set(x => x.IsActive, false).Set(x => x.DeactivatedBySelf, false).Set(x => x.DeactivatedAtUtc, DateTime.UtcNow);
        await _owners.Col.UpdateOneAsync(x => x.Nic == nic, upd);
        return NoContent();
    }

    // PATCH /users/owners/{nic}/reactivate
    [HttpPatch("{nic}/reactivate")]
    public async Task<ActionResult> Reactivate(string nic) => await Activate(nic);
}

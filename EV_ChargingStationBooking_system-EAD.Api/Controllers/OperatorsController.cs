using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Controllers;

[ApiController]
[Route("users/operators")]
[Authorize(Roles = Role.Backoffice)]
public sealed class OperatorsController : ControllerBase
{
    private readonly IOperatorRepository _ops;
    private readonly IAuthUserRepository _users;

    public OperatorsController(IOperatorRepository ops, IAuthUserRepository users)
    {
        _ops = ops;
        _users = users;
    }

    // POST /users/operators
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateOperatorDto dto)
    {
        // 1) login user
        var exists = await _users.GetByUsernameAsync(dto.Email);
        if (exists is not null) return Conflict(new { message = "Email already exists" });

        var user = new AuthUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Username = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Role.Operator,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _users.CreateAsync(user);

        // 2) operator profile
        var op = new Operator
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            Name = dto.Name,
            Phone = dto.Phone,
            IsActive = true
        };
        await _ops.Col.InsertOneAsync(op);

        return Created($"/users/operators/{op.Id}",
            new { op.Id, Email = user.Username, op.Name, op.Phone, op.IsActive });
    }

    // GET /users/operators?q=&status=&page=&pageSize=
    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = Builders<Operator>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var r = new MongoDB.Bson.BsonRegularExpression(q, "i");
            filter &= Builders<Operator>.Filter.Or(
                Builders<Operator>.Filter.Regex(x => x.Name, r),
                Builders<Operator>.Filter.Regex(x => x.Phone, r));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                filter &= Builders<Operator>.Filter.Eq(x => x.IsActive, true);
            else if (status.Equals("disabled", StringComparison.OrdinalIgnoreCase))
                filter &= Builders<Operator>.Filter.Eq(x => x.IsActive, false);
        }

        var total = await _ops.Col.CountDocumentsAsync(filter);
        var items = await _ops.Col.Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        // join emails WITHOUT touching the raw users collection
        var userIds = items.Select(i => i.UserId)
                           .Where(id => !string.IsNullOrWhiteSpace(id))
                           .Distinct();
        var emailById = await _users.GetUsernamesByIdsAsync(userIds);

        return Ok(new
        {
            page,
            pageSize,
            total,
            items = items.Select(i => new
            {
                i.Id,
                Email = emailById.TryGetValue(i.UserId, out var email) ? email : "",
                i.Name,
                i.Phone,
                i.IsActive,
                i.CreatedAtUtc
            })
        });
    }

    // PATCH /users/{id}/enable
    [HttpPatch("{id}/enable")]
    public async Task<ActionResult> Enable(string id)
    {
        var op = await _ops.Col.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (op is null) return NotFound();

        await _ops.Col.UpdateOneAsync(
            x => x.Id == id,
            Builders<Operator>.Update.Set(x => x.IsActive, true));

        // ensure auth user is active (don’t change role here)
        await _users.UpdateIsActiveAsync(op.UserId, true);

        return NoContent();
    }

    // PATCH /users/{id}/disable
    [HttpPatch("{id}/disable")]
    public async Task<ActionResult> Disable(string id)
    {
        var op = await _ops.Col.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (op is null) return NotFound();

        await _ops.Col.UpdateOneAsync(
            x => x.Id == id,
            Builders<Operator>.Update.Set(x => x.IsActive, false));

        await _users.UpdateIsActiveAsync(op.UserId, false);
        return NoContent();
    }

    // PATCH /users/{id}/reset-password
    [HttpPatch("{id}/reset-password")]
    public async Task<ActionResult> ResetPassword(string id, [FromBody] ResetPasswordDto body)
    {
        var op = await _ops.Col.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (op is null) return NotFound();

        var hash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        await _users.UpdatePasswordHashAsync(op.UserId, hash);

        return NoContent();
    }
}

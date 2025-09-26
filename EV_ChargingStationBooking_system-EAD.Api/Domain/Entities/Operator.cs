using MongoDB.Bson.Serialization.Attributes;

namespace EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
public sealed class Operator
{
    [BsonId] public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;    // FK -> users_auth.Id
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public bool   IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

}
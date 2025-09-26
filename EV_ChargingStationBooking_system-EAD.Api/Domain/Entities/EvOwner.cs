using MongoDB.Bson.Serialization.Attributes;

namespace EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
public sealed class EvOwner
{
    // NIC is the document _id
    [BsonId] public string Nic { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool   IsActive { get; set; } = true;

    public bool     DeactivatedBySelf { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
    public DateTime   CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
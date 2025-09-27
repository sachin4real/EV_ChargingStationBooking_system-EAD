namespace EV_ChargingStationBooking_system_EAD.Api.Domain.Entities
{
    public sealed class EvOwner
    {
        public string Id { get; set; } = default!;
        public string Nic { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
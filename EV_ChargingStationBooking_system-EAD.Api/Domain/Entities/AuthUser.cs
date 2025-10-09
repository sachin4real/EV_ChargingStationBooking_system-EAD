namespace EV_ChargingStationBooking_system_EAD.Api.Domain.Entities
{
    public sealed class AuthUser
    {
        public string Id { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? OwnerNic { get; set; }

        public string? FullName { get; set; }
        public string? Phone { get; set; }

        public List<string> StationIds { get; set; } = new();
        public bool IsActive { get; set; } = true;

    }
}
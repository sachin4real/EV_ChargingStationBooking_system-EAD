namespace EV_ChargingStationBooking_system_EAD.Api.Domain.Entities
{
    public static class BookingStatus
    {
        public const string Pending   = "Pending";
        public const string Approved  = "Approved";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";
    }

    public sealed class Booking
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Nic { get; set; } = default!;        // EV owner NIC
        public string StationId { get; set; } = default!;

        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc   { get; set; }

        public string Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

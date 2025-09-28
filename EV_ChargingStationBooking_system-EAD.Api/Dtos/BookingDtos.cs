namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    // Owner creates/updates own booking
    public sealed class BookingCreateDto
    {
        public string StationId { get; set; } = default!;
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc   { get; set; }
    }

    public sealed class BookingUpdateDto
    {
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc   { get; set; }
    }

    public sealed class BookingViewDto
    {
        public string Id { get; set; } = default!;
        public string Nic { get; set; } = default!;
        public string StationId { get; set; } = default!;
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc   { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public sealed class BookingListQuery
    {
        public string? Nic { get; set; }
        public string? StationId { get; set; }
        public string? Status { get; set; } // Pending/Approved/Cancelled/Completed
        public bool? FutureOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

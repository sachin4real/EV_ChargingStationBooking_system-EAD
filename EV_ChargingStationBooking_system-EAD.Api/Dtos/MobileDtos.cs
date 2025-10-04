namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    // --- QR (owner -> operator) ---
    public sealed class OwnerBookingQrDto
    {
        public string Qr { get; set; } = default!; // compact string for QR image
        public DateTime ExpiresAtUtc { get; set; }
    }

    public sealed class OperatorScanRequest
    {
        public string Qr { get; set; } = default!;
    }

    public sealed class OperatorScanResponse
    {
        public BookingViewDto Booking { get; set; } = default!;
        public string OwnerNic { get; set; } = default!;
        public string OwnerName { get; set; } = "";
        public string StationId { get; set; } = default!;
        public string StationName { get; set; } = default!;
        public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
    }

    // --- Owner dashboard ---
    public sealed class OwnerDashboardDto
    {
        public int Pending { get; set; }
        public int ApprovedFuture { get; set; }
        public BookingViewDto? NextBooking { get; set; }
    }

    // --- Nearby stations ---
    public sealed class NearbyStationDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Location { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public bool IsActive { get; set; }
        public double DistanceKm { get; set; }
    }
}

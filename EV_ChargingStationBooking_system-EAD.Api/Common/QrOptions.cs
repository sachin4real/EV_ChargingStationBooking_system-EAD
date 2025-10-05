namespace EV_ChargingStationBooking_system_EAD.Api.Common
{
    public sealed class QrOptions
    {
        // long random string (>=32 chars)
        public string Secret { get; set; } = default!;

        // QR validity (minutes)
        public int ExpiryMinutes { get; set; } = 5;
    }
}

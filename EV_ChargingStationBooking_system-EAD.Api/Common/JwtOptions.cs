namespace EV_ChargingStationBooking_system_EAD.Api.Common
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string Key { get; set; } = "";
        public int ExpiresMinutes { get; set; } = 120;
    }
}
namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    public sealed class MyProfileDto
    {
        public string Username { get; set; } = default!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = default!;
    }

    public sealed class MyProfileUpdateDto
    {
        public string Email { get; set; } = default!;   // can change email
        public string? FullName { get; set; }
        public string? Phone { get; set; }

        // OPTIONAL: if both are present, password will be changed
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}

namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    public sealed class MyProfileDto
    {
        public string Username { get;set;} = default!;
        public string? FullName { get; set;}
        public string? Phone { get; set;}
        public string Role { get;set;} = default!;
    }

    public sealed class MyProfileUpdateDto
    {
        public string Email { get; set; } = default!;     // allow changing email
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }

    public sealed class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
    }
}
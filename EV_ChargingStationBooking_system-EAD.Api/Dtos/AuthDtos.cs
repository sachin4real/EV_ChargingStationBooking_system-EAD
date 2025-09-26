
namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    public sealed class LoginDto
    {
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public sealed class RegisterStaffDto
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? Role { get; set; } = default!;
    }    

    public sealed class RegisterOwnerDto
    {
        public string Nic { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Email { get; set; } = default!;
    }
}
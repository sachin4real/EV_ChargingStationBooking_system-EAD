namespace EV_ChargingStationBooking_system_EAD.Api.Dtos;

public sealed class CreateOperatorDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
}

public sealed class ResetPasswordDto
{
    public string NewPassword { get; set; } = default!;
}

public sealed class CreateOwnerBackofficeDto
{
    public string Nic { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Email { get; set; } = default!;
}

namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    public sealed class StaffListQuery
    {
        public string? Q { get; set; }           // search by email/full name
        public string? Role { get; set; }        // "Backoffice" | "Operator" (optional)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class StaffUserViewDto
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;   // Username for staff = email
        public string Role { get; set; } = default!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    public sealed class EvOwnerCreateDto
    {
        public string Nic { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public sealed class EvOwnerUpdateDto
    {
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;

    }

    public sealed class OwnerListQuery
    {
        public string? Q { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class EvOwnerViewDto
    {
        public string Nic { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public sealed class OwnerStatusDto
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }
}
namespace EV_ChargingStationBooking_system_EAD.Api.Dtos
{
    public sealed class StationCreateDto
    {
        public string Name { get; set; } = default!;
        public string Type { get; set; } = "AC"; // "AC" | "DC"
        public int TotalSlots { get; set; }
        public string Location { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public sealed class StationUpdateDto
    {
        public string Name { get; set; } = default!;
        public string Type { get; set; } = "AC";
        public int TotalSlots { get; set; }
         public string Location { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public sealed class StationScheduleItemDto
    {
        public string Date { get; set; } = default!;   // "yyyy-MM-dd"
        public int AvailableSlots { get; set; }
    }

    public sealed class StationScheduleUpdateDto
    {
        public List<StationScheduleItemDto> Days { get; set; } = new();
        public bool ReplaceAll { get; set; } = false;  // if true, replace; else merge+upsert per day
    }

    public sealed class StationViewDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public int TotalSlots { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Location { get; set; } = "";
        public bool IsActive { get; set; }
        public List<StationScheduleItemDto> Schedule { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}

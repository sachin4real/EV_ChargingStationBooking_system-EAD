namespace EV_ChargingStationBooking_system_EAD.Api.Domain.Entities
{

    public sealed class ChargingStation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N"); // Mongo _id (string)
        public string Name { get; set; } = default!;
        public string Type { get; set; } = "AC";        // "AC" | "DC"
        public int TotalSlots { get; set; }             // physical connectors or bays

        public string Location { get; set; } = "";


        public double Lat { get; set; }                 // location
        public double Lng { get; set; }

        public bool IsActive { get; set; } = true;

        // Optional day-level availability plan (per day override)
        public List<StationDayAvailability> Schedule { get; set; } = new();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class StationDayAvailability
    {
         // ISO date (yyyy-MM-dd) to keep it simple for mobile/web
        public string Date { get; set; } = default!;   // e.g. "2025-09-26"
        public int AvailableSlots { get; set; }        // 0..TotalSlots
    }
}


using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IChargingStationService
    {
        Task<StationViewDto> CreateAsync(StationCreateDto dto);
        Task<StationViewDto> UpdateAsync(string id, StationUpdateDto dto);
        Task<StationViewDto> UpdateScheduleAsync(string id, StationScheduleUpdateDto dto);
        Task DeactivateAsync(string id);   // enforce: deny if active bookings
        Task ActivateAsync(string id);
        Task<(IReadOnlyList<StationViewDto> items, long total)> ListAsync(string? q, bool? isActive, int page, int pageSize);
        Task<StationViewDto> GetAsync(string id);
    }

    public sealed class ChargingStationService : IChargingStationService
    {
        private readonly IChargingStationRepository _stations;
        private readonly IBookingRepository _bookings;

        public ChargingStationService(IChargingStationRepository stations, IBookingRepository bookings)
        {
            _stations = stations;
            _bookings = bookings;
        }

        // ---- helpers ----
        private static void ValidateLatLng(double lat, double lng)
        {
            if (lat < -90 || lat > 90) throw new InvalidOperationException("Latitude must be between -90 and 90.");
            if (lng < -180 || lng > 180) throw new InvalidOperationException("Longitude must be between -180 and 180.");
        }

        private static string NormalizeType(string? t)
            => (t ?? "AC").Trim().ToUpperInvariant() == "DC" ? "DC" : "AC";

        // ---- CRUD ----
        public async Task<StationViewDto> CreateAsync(StationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new InvalidOperationException("Name is required.");
            if (dto.TotalSlots < 1) throw new InvalidOperationException("TotalSlots must be >= 1.");
            ValidateLatLng(dto.Lat, dto.Lng);

            var s = new ChargingStation
            {
                Name = dto.Name.Trim(),
                Type = NormalizeType(dto.Type),
                TotalSlots = Math.Clamp(dto.TotalSlots, 1, 200),
                Location = dto.Location?.Trim() ?? "",   // ← you were missing this
                Lat = dto.Lat,
                Lng = dto.Lng,
                IsActive = true
            };
            await _stations.CreateAsync(s);
            return Map(s);
        }

        public async Task<StationViewDto> UpdateAsync(string id, StationUpdateDto dto)
        {
            var s = await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found.");

            if (string.IsNullOrWhiteSpace(dto.Name)) throw new InvalidOperationException("Name is required.");
            if (dto.TotalSlots < 1) throw new InvalidOperationException("TotalSlots must be >= 1.");
            ValidateLatLng(dto.Lat, dto.Lng);

            s.Name = dto.Name.Trim();
            s.Type = NormalizeType(dto.Type);
            s.TotalSlots = Math.Clamp(dto.TotalSlots, 1, 200);
            s.Location = dto.Location?.Trim() ?? "";
            s.Lat = dto.Lat;
            s.Lng = dto.Lng;

            await _stations.UpdateAsync(s);
            return Map(s);
        }

        public async Task<StationViewDto> UpdateScheduleAsync(string id, StationScheduleUpdateDto dto)
        {
            var s = await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found.");

            foreach (var d in dto.Days)
            {
                if (string.IsNullOrWhiteSpace(d.Date)) throw new InvalidOperationException("Date is required.");
                if (d.AvailableSlots < 0 || d.AvailableSlots > s.TotalSlots)
                    throw new InvalidOperationException($"AvailableSlots must be between 0 and {s.TotalSlots}.");
            }

            if (dto.ReplaceAll)
            {
                s.Schedule = dto.Days.Select(x => new StationDayAvailability
                {
                    Date = x.Date,
                    AvailableSlots = x.AvailableSlots
                }).ToList();
            }
            else
            {
                var map = s.Schedule.ToDictionary(x => x.Date, x => x);
                foreach (var incoming in dto.Days)
                {
                    map[incoming.Date] = new StationDayAvailability
                    {
                        Date = incoming.Date,
                        AvailableSlots = incoming.AvailableSlots
                    };
                }
                s.Schedule = map.Values.OrderBy(x => x.Date).ToList();
            }

            await _stations.UpdateAsync(s);
            return Map(s);
        }

        public async Task ActivateAsync(string id)
        {
            var s = await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found.");
            s.IsActive = true;                            // no special rule on activation
            await _stations.UpdateAsync(s);
        }

        public async Task DeactivateAsync(string id)
        {
            var s = await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found.");

            // cannot deactivate if there are active bookings (Pending/Approved)
            var hasActive = await _bookings.ExistsActiveForStationAsync(id);
            if (hasActive)
                throw new InvalidOperationException("Station cannot be deactivated while it has active bookings.");

            s.IsActive = false;
            await _stations.UpdateAsync(s);
        }

        public async Task<(IReadOnlyList<StationViewDto> items, long total)> ListAsync(string? q, bool? isActive, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, total) = await _stations.ListAsync(q, isActive, (page - 1) * pageSize, pageSize);
            return (items.Select(Map).ToList(), total);
        }

        public async Task<StationViewDto> GetAsync(string id)
            => Map(await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found."));

        private static StationViewDto Map(ChargingStation s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Type = s.Type,
            TotalSlots = s.TotalSlots,
            Location = s.Location,
            Lat = s.Lat,
            Lng = s.Lng,
            IsActive = s.IsActive,
            Schedule = s.Schedule
                .Select(x => new StationScheduleItemDto { Date = x.Date, AvailableSlots = x.AvailableSlots })
                .ToList(),
            CreatedAtUtc = s.CreatedAtUtc,
            UpdatedAtUtc = s.UpdatedAtUtc
        };
    }
}

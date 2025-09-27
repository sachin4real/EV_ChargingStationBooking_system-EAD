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

        public async Task<StationViewDto> CreateAsync(StationCreateDto dto)
        {
            var s = new ChargingStation
            {
                Name = dto.Name.Trim(),
                Type = (dto.Type ?? "AC").ToUpperInvariant() == "DC" ? "DC" : "AC",
                TotalSlots = Math.Clamp(dto.TotalSlots, 1, 200),
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

            s.Name = dto.Name.Trim();
            s.Type = (dto.Type ?? "AC").ToUpperInvariant() == "DC" ? "DC" : "AC";
            s.TotalSlots = Math.Clamp(dto.TotalSlots, 1, 200);
            s.Lat = dto.Lat;
            s.Lng = dto.Lng;

            await _stations.UpdateAsync(s);
            return Map(s);
        }

        public async Task<StationViewDto> UpdateScheduleAsync(string id, StationScheduleUpdateDto dto)
        {
            var s = await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found.");

            // Validate each day
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
                // merge by date
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

        public async Task DeactivateAsync(string id)
        {
            var s = await _stations.GetAsync(id) ?? throw new KeyNotFoundException("Station not found.");

            // Rule: cannot deactivate if there are active bookings (Pending or Approved)
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

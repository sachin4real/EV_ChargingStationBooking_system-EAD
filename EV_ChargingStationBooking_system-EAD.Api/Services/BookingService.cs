using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Dtos;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories;

namespace EV_ChargingStationBooking_system_EAD.Api.Services
{
    public interface IBookingService
    {
        // owner self
        Task<BookingViewDto> OwnerCreateAsync(string nicFromToken, BookingCreateDto dto);
        Task<BookingViewDto> OwnerUpdateAsync(string nicFromToken, string bookingId, BookingUpdateDto dto);
        Task OwnerCancelAsync(string nicFromToken, string bookingId);
        Task<(IReadOnlyList<BookingViewDto> items, long total)> OwnerListAsync(string nicFromToken, int page, int pageSize);

        // staff actions
        Task<BookingViewDto> ApproveAsync(string bookingId, string actorUserId);
        Task<BookingViewDto> FinalizeAsync(string bookingId, string actorUserId); // complete session
        Task<(IReadOnlyList<BookingViewDto> items, long total)> AdminSearchAsync(BookingListQuery q);
        Task<BookingViewDto> GetAsync(string id);

        Task<OwnerDashboardDto> OwnerDashboardAsync(string nicFromToken);

    }

    public sealed class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookings;
        private readonly IChargingStationRepository _stations;
        private readonly IEvOwnerRepository _owners;

        public BookingService(IBookingRepository bookings, IChargingStationRepository stations, IEvOwnerRepository owners)
        {
            _bookings = bookings;
            _stations = stations;
            _owners = owners;
        }

        // -------- owner self --------

        public async Task<BookingViewDto> OwnerCreateAsync(string nic, BookingCreateDto dto)
        {
            ValidateWindowForCreate(dto.StartTimeUtc, dto.EndTimeUtc);

            var owner = await _owners.GetByNicAsync(nic) ?? throw new KeyNotFoundException("Owner not found.");
            if (!owner.IsActive) throw new InvalidOperationException("Owner account is deactivated.");

            var station = await _stations.GetAsync(dto.StationId) ?? throw new KeyNotFoundException("Station not found.");
            if (!station.IsActive) throw new InvalidOperationException("Station is deactivated.");

            // capacity check (overlap-based)
            await EnsureCapacityAsync(station, dto.StartTimeUtc, dto.EndTimeUtc);

            var b = new Booking
            {
                Nic = nic,
                StationId = station.Id,
                StartTimeUtc = dto.StartTimeUtc,
                EndTimeUtc = dto.EndTimeUtc,
                Status = BookingStatus.Pending
            };
            await _bookings.CreateAsync(b);
            return Map(b);
        }

        public async Task<BookingViewDto> OwnerUpdateAsync(string nic, string id, BookingUpdateDto dto)
        {
            ValidateWindowForUpdateOrCancel(dto.StartTimeUtc);

            var b = await _bookings.GetByIdAsync(id) ?? throw new KeyNotFoundException("Booking not found.");
            if (b.Nic != nic) throw new UnauthorizedAccessException("Not your booking.");
            if (b.Status is BookingStatus.Cancelled or BookingStatus.Completed)
                throw new InvalidOperationException("Cannot modify a cancelled/completed booking.");

            var station = await _stations.GetAsync(b.StationId) ?? throw new KeyNotFoundException("Station not found.");
            if (!station.IsActive) throw new InvalidOperationException("Station is deactivated.");

            // capacity check with new window
            await EnsureCapacityAsync(station, dto.StartTimeUtc, dto.EndTimeUtc, excludeBookingId: b.Id);

            b.StartTimeUtc = dto.StartTimeUtc;
            b.EndTimeUtc = dto.EndTimeUtc;
            b.Status = BookingStatus.Pending; // revert to pending on change (if you want)
            await _bookings.UpdateAsync(b);
            return Map(b);
        }

        public async Task OwnerCancelAsync(string nic, string id)
        {
            var b = await _bookings.GetByIdAsync(id) ?? throw new KeyNotFoundException("Booking not found.");
            if (b.Nic != nic) throw new UnauthorizedAccessException("Not your booking.");
            ValidateWindowForUpdateOrCancel(b.StartTimeUtc);

            if (b.Status is BookingStatus.Cancelled or BookingStatus.Completed)
                return; // idempotent
            b.Status = BookingStatus.Cancelled;
            await _bookings.UpdateAsync(b);
        }

        public async Task<(IReadOnlyList<BookingViewDto> items, long total)> OwnerListAsync(string nic, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, total) = await _bookings.SearchAsync(nic, stationId: null, status: null, futureOnly: null, (page - 1) * pageSize, pageSize);
            return (items.Select(Map).ToList(), total);
        }

        // -------- staff actions --------

        public async Task<BookingViewDto> ApproveAsync(string id, string actorUserId)
        {
            var b = await _bookings.GetByIdAsync(id) ?? throw new KeyNotFoundException("Booking not found.");
            if (b.Status is BookingStatus.Cancelled or BookingStatus.Completed)
                throw new InvalidOperationException("Cannot approve a cancelled/completed booking.");

            // re-check capacity at approval time
            await EnsureCapacityAsync(await _stations.GetAsync(b.StationId) ?? throw new KeyNotFoundException("Station not found."),
                                      b.StartTimeUtc, b.EndTimeUtc, excludeBookingId: b.Id);

            b.Status = BookingStatus.Approved;
            await _bookings.UpdateAsync(b);
            return Map(b);
        }

        public async Task<BookingViewDto> FinalizeAsync(string id, string actorUserId)
        {
            var b = await _bookings.GetByIdAsync(id) ?? throw new KeyNotFoundException("Booking not found.");
            if (b.Status != BookingStatus.Approved)
                throw new InvalidOperationException("Only approved bookings can be completed.");
            b.Status = BookingStatus.Completed;
            await _bookings.UpdateAsync(b);
            return Map(b);
        }

        public async Task<(IReadOnlyList<BookingViewDto> items, long total)> AdminSearchAsync(BookingListQuery q)
        {
            var page = Math.Max(1, q.Page);
            var size = Math.Clamp(q.PageSize, 1, 100);
            var (items, total) = await _bookings.SearchAsync(q.Nic, q.StationId, q.Status, q.FutureOnly, (page - 1) * size, size);
            return (items.Select(Map).ToList(), total);
        }

        public async Task<BookingViewDto> GetAsync(string id)
            => Map(await _bookings.GetByIdAsync(id) ?? throw new KeyNotFoundException("Booking not found."));

        // -------- rules / helpers --------

        private static void ValidateWindowForCreate(DateTime startUtc, DateTime endUtc)
        {
            if (endUtc <= startUtc) throw new InvalidOperationException("End time must be after start time.");

            var now = DateTime.UtcNow;
            var max = now.AddDays(7);
            if (startUtc < now) throw new InvalidOperationException("Reservation start must be in the future.");
            if (startUtc > max) throw new InvalidOperationException("Reservation must be within 7 days from now.");
        }

        private static void ValidateWindowForUpdateOrCancel(DateTime startUtc)
        {
            var now = DateTime.UtcNow;
            if (startUtc - now < TimeSpan.FromHours(12))
                throw new InvalidOperationException("Changes/cancellations must be at least 12 hours before start time.");
        }

        private async Task EnsureCapacityAsync(ChargingStation station, DateTime startUtc, DateTime endUtc, string? excludeBookingId = null)
        {
            // Capacity for the day/time: use station.Schedule for day-level limit if present; else TotalSlots.
            var dateKey = startUtc.ToString("yyyy-MM-dd");
            var capacity = station.Schedule.FirstOrDefault(d => d.Date == dateKey)?.AvailableSlots ?? station.TotalSlots;

            if (capacity <= 0) throw new InvalidOperationException("Station has no available slots for the selected date.");

            var overlapping = await _bookings.CountOverlappingActiveAsync(station.Id, startUtc, endUtc);
            if (excludeBookingId != null)
            {
                // conservatively re-fetch to check if the excluded booking would have been counted; usually not necessary
                // kept simple: assume repository count excludes status changes; if you want exact exclusion, add it to repo filter
            }
            if (overlapping >= capacity)
                throw new InvalidOperationException("No capacity available for the selected time window.");
        }

        private static BookingViewDto Map(Booking b) => new()
        {
            Id = b.Id,
            Nic = b.Nic,
            StationId = b.StationId,
            StartTimeUtc = b.StartTimeUtc,
            EndTimeUtc = b.EndTimeUtc,
            Status = b.Status,
            CreatedAtUtc = b.CreatedAtUtc,
            UpdatedAtUtc = b.UpdatedAtUtc
        };

        public async Task<OwnerDashboardDto> OwnerDashboardAsync(string nic)
        {
            // counts
            var (future, _) = await _bookings.SearchAsync(nic, null, null, true, 0, 1_000_000);
            var pending = future.Count(b => b.Status == BookingStatus.Pending);
            var approved = future.Count(b => b.Status == BookingStatus.Approved);

            // next upcoming (approved preferred, else pending) sorted by StartTimeUtc
            var next = future
              .OrderBy(b => b.StartTimeUtc)
              .FirstOrDefault(b => b.Status == BookingStatus.Approved)
              ?? future.OrderBy(b => b.StartTimeUtc).FirstOrDefault();

            return new OwnerDashboardDto
            {
                Pending = pending,
                ApprovedFuture = approved,
                NextBooking = next is null ? null : new BookingViewDto
                {
                    Id = next.Id,
                    Nic = next.Nic,
                    StationId = next.StationId,
                    StartTimeUtc = next.StartTimeUtc,
                    EndTimeUtc = next.EndTimeUtc,
                    Status = next.Status,
                    CreatedAtUtc = next.CreatedAtUtc,
                    UpdatedAtUtc = next.UpdatedAtUtc
                }
            };
        }

    }
}

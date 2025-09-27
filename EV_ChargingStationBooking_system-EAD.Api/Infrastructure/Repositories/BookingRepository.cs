namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IBookingRepository
    {
        Task<bool> ExistsForOwnerAsync(string nic);
        Task<bool> ExistsFutureForOwnerAsync(string nic, DateTime utcNow);

          // NEW: used by station deactivate rule
        Task<bool> ExistsActiveForStationAsync(string stationId);
    }

    public sealed class BookingRepository : IBookingRepository
    {
        public Task<bool> ExistsForOwnerAsync(string nic) => Task.FromResult(false);
        public Task<bool> ExistsFutureForOwnerAsync(string nic, DateTime utcNow) => Task.FromResult(false);
        // If you don’t have bookings yet, stub false for now.
        public Task<bool> ExistsActiveForStationAsync(string stationId) => Task.FromResult(false);
    }
}
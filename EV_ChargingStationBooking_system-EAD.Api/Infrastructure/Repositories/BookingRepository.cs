namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IBookingRepository
    {
        Task<bool> ExistsForOwnerAsync(string nic);
        Task<bool> ExistsFutureForOwnerAsync(string nic, DateTime utcNow);
    }

    public sealed class BookingRepository : IBookingRepository
    {
         public Task<bool> ExistsForOwnerAsync(string nic) => Task.FromResult(false);
        public Task<bool> ExistsFutureForOwnerAsync(string nic, DateTime utcNow) => Task.FromResult(false);
    }
}
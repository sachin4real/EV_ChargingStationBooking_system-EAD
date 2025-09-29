using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IBookingRepository
    {
        Task CreateIndexesAsync();

        Task CreateAsync(Booking b);
        Task<Booking?> GetByIdAsync(string id);
        Task UpdateAsync(Booking b);

        Task<(IReadOnlyList<Booking> items, long total)> SearchAsync(
            string? nic, string? stationId, string? status, bool? futureOnly,
            int skip, int take);

        // Owner constraints
        Task<bool> ExistsForOwnerAsync(string nic);
        Task<bool> ExistsFutureForOwnerAsync(string nic, DateTime utcNow);

        // Station capacity / deactivation
        Task<bool> ExistsActiveForStationAsync(string stationId);

        // Count overlapping active bookings for a station/time window
        Task<long> CountOverlappingActiveAsync(string stationId, DateTime startUtc, DateTime endUtc);
    }

    public sealed class BookingRepository : IBookingRepository
    {
        private readonly IMongoCollection<Booking> _col;

        public BookingRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<Booking>("bookings");
        }

        public async Task CreateIndexesAsync()
        {
            var models = new List<CreateIndexModel<Booking>>
            {
                new CreateIndexModel<Booking>(
                    Builders<Booking>.IndexKeys
                        .Ascending(b => b.StationId)
                        .Ascending(b => b.Status)
                        .Ascending(b => b.StartTimeUtc)
                        .Ascending(b => b.EndTimeUtc),
                    new CreateIndexOptions { Name = "ix_station_status_time" }),

                new CreateIndexModel<Booking>(
                    Builders<Booking>.IndexKeys
                        .Ascending(b => b.Nic)
                        .Ascending(b => b.StartTimeUtc),
                    new CreateIndexOptions { Name = "ix_owner_start" })
            };
            await _col.Indexes.CreateManyAsync(models);
        }

        public Task CreateAsync(Booking b) => _col.InsertOneAsync(b);

        public Task<Booking?> GetByIdAsync(string id)
            => _col.Find(b => b.Id == id).FirstOrDefaultAsync();

        public Task UpdateAsync(Booking b)
        {
            b.UpdatedAtUtc = DateTime.UtcNow;
            return _col.ReplaceOneAsync(x => x.Id == b.Id, b, new ReplaceOptions { IsUpsert = false });
        }

        public async Task<(IReadOnlyList<Booking> items, long total)> SearchAsync(
            string? nic, string? stationId, string? status, bool? futureOnly,
            int skip, int take)
        {
            var f = Builders<Booking>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(nic))
                f &= Builders<Booking>.Filter.Eq(x => x.Nic, nic);

            if (!string.IsNullOrWhiteSpace(stationId))
                f &= Builders<Booking>.Filter.Eq(x => x.StationId, stationId);

            if (!string.IsNullOrWhiteSpace(status))
                f &= Builders<Booking>.Filter.Eq(x => x.Status, status);

            if (futureOnly == true)
                f &= Builders<Booking>.Filter.Gte(x => x.StartTimeUtc, DateTime.UtcNow);

            var total = await _col.CountDocumentsAsync(f);
            var items = await _col.Find(f)
                                  .SortBy(x => x.StartTimeUtc)
                                  .Skip(skip).Limit(take)
                                  .ToListAsync();
            return (items, total);
        }

        public Task<bool> ExistsForOwnerAsync(string nic)
            => _col.Find(b => b.Nic == nic).Limit(1).AnyAsync();

        public Task<bool> ExistsFutureForOwnerAsync(string nic, DateTime utcNow)
            => _col.Find(b => b.Nic == nic && b.StartTimeUtc >= utcNow &&
                              (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved))
                   .Limit(1).AnyAsync();

        public Task<bool> ExistsActiveForStationAsync(string stationId)
        {
            var now = DateTime.UtcNow;
            var f = Builders<Booking>.Filter.Eq(b => b.StationId, stationId) &
                    Builders<Booking>.Filter.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved }) &
                    Builders<Booking>.Filter.Gte(b => b.EndTimeUtc, now);
            return _col.Find(f).Limit(1).AnyAsync();
        }

        public Task<long> CountOverlappingActiveAsync(string stationId, DateTime startUtc, DateTime endUtc)
        {
            var overlap = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved }),
                // overlap condition: (start < existing.End) && (end > existing.Start)
                Builders<Booking>.Filter.Lt(b => b.StartTimeUtc, endUtc),
                Builders<Booking>.Filter.Gt(b => b.EndTimeUtc, startUtc)
            );
            return _col.CountDocumentsAsync(overlap);
        }
    }
}

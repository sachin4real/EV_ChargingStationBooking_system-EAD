using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IChargingStationRepository
    {
        Task CreateIndexesAsync();

        Task CreateAsync(ChargingStation s);
        Task<ChargingStation?> GetAsync(string id);
        Task UpdateAsync(ChargingStation s);
        Task<(IReadOnlyList<ChargingStation> items, long total)>
            ListAsync(string? q, bool? isActive, int skip, int take);

             Task<IReadOnlyList<ChargingStation>> ListAllActiveAsync();
            
    }

    public sealed class ChargingStationRepository : IChargingStationRepository
    {
        private readonly IMongoCollection<ChargingStation> _col;
        public ChargingStationRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<ChargingStation>("stations");
        }

        public async Task CreateIndexesAsync()
        {
            var models = new List<CreateIndexModel<ChargingStation>>
            {
                new CreateIndexModel<ChargingStation>(
                    Builders<ChargingStation>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true, Name = "ux_station_name" }),
                new CreateIndexModel<ChargingStation>(
                    Builders<ChargingStation>.IndexKeys.Ascending(x => x.IsActive))
            };
            await _col.Indexes.CreateManyAsync(models);
        }

        

        public Task CreateAsync(ChargingStation s) => _col.InsertOneAsync(s);

        public Task<ChargingStation?> GetAsync(string id)
            => _col.Find(x => x.Id == id).FirstOrDefaultAsync();
        
 public async Task<IReadOnlyList<ChargingStation>> ListAllActiveAsync()
        => await _col.Find(s => s.IsActive).ToListAsync();

        public Task UpdateAsync(ChargingStation s)
        {
            s.UpdatedAtUtc = DateTime.UtcNow;
            return _col.ReplaceOneAsync(x => x.Id == s.Id, s, new ReplaceOptions { IsUpsert = false });
        }

        public async Task<(IReadOnlyList<ChargingStation> items, long total)>
            ListAsync(string? q, bool? isActive, int skip, int take)
        {
            var filter = Builders<ChargingStation>.Filter.Empty;
            if (!string.IsNullOrWhiteSpace(q))
            {
                filter &= Builders<ChargingStation>.Filter.Regex(
                    x => x.Name, new MongoDB.Bson.BsonRegularExpression(q, "i"));
            }
            if (isActive is not null)
            {
                filter &= Builders<ChargingStation>.Filter.Eq(x => x.IsActive, isActive.Value);
            }

            var total = await _col.CountDocumentsAsync(filter);
            var items = await _col.Find(filter)
                                  .SortBy(x => x.Name)
                                  .Skip(skip).Limit(take)
                                  .ToListAsync();
            return (items, total);
        }
        
        
    }
}

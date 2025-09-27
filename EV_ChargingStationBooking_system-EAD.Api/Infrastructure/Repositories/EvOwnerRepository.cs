using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using Microsoft.AspNetCore.Http.Features;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IEvOwnerRepository
    {
        Task CreateIndexesAsync();
        Task<bool> ExistsNicAsync(string nic);
        Task<bool> ExistsEmailAsync(string email, string? excludeNic = null);

        Task CreateAsync(EvOwner o);
        Task<EvOwner?> GetByNicAsync(string nic);
        Task UpdateAsync(EvOwner o);
        Task DeleteAsync(string nic);

        Task<(IReadOnlyList<EvOwner> items, long total)> SearchAsync(string? q, bool? isActive, int skip, int take);
    }

    public sealed class EvOwnerRepository : IEvOwnerRepository
    {
        private readonly IMongoCollection<EvOwner> _col;

        public EvOwnerRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<EvOwner>("ev_owners");
        }
        public async Task CreateIndexesAsync()
        {
            var idxModels = new List<CreateIndexModel<EvOwner>>
            {
                new CreateIndexModel<EvOwner>(
                    Builders<EvOwner>.IndexKeys.Ascending(x => x.Nic),
                    new CreateIndexOptions {Unique = true, Name = "ux_nic"}),
                new CreateIndexModel<EvOwner>(
                    Builders<EvOwner>.IndexKeys.Ascending(x => x.Email),
                    new CreateIndexOptions { Unique = true, Name = "ux_email" }),

                new CreateIndexModel<EvOwner>(
                    Builders<EvOwner>.IndexKeys.Ascending(x => x.IsActive)
                ),

                new CreateIndexModel<EvOwner>(
                    Builders<EvOwner>.IndexKeys.Text(x => x.FullName).Text(x => x.Email).Text(x => x.Nic),
                    new CreateIndexOptions { Name = "txt_owner" })

            };
            await _col.Indexes.CreateManyAsync(idxModels);
        }

        public async Task<bool> ExistsNicAsync(string nic) => (await _col.CountDocumentsAsync(x => x.Nic == nic)) > 0;

        public async Task<bool> ExistsEmailAsync(string email, string? excludeNic = null)
        {
            var filter = Builders<EvOwner>.Filter.Eq(x => x.Email, email);
            if (!string.IsNullOrEmpty(excludeNic))
                filter &= Builders<EvOwner>.Filter.Ne(x => x.Nic, excludeNic);
            return (await _col.CountDocumentsAsync(filter)) > 0;
        }
        public async Task CreateAsync(EvOwner o)
        {
            o.Id = o.Nic;
            o.CreatedAtUtc = o.UpdatedAtUtc = DateTime.UtcNow;
            await _col.InsertOneAsync(o);
        }
        public Task<EvOwner?> GetByNicAsync(string nic) => _col.Find(x => x.Nic == nic).FirstOrDefaultAsync();

        public Task UpdateAsync(EvOwner o)
        {
            o.UpdatedAtUtc = DateTime.UtcNow;
            return _col.ReplaceOneAsync(x => x.Nic == o.Nic, o, new ReplaceOptions { IsUpsert = false });
        }
        public Task DeleteAsync(string nic) => _col.DeleteOneAsync(x => x.Nic == nic);

        public async Task<(IReadOnlyList<EvOwner> items, long total)> SearchAsync(string? q, bool? isActive, int skip, int take)
        {
            var filter = Builders<EvOwner>.Filter.Empty;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = Builders<EvOwner>.Filter.Or(
                    Builders<EvOwner>.Filter.Regex(x => x.FullName, new MongoDB.Bson.BsonRegularExpression(q, "i")),
                    Builders<EvOwner>.Filter.Regex(x => x.Email, new MongoDB.Bson.BsonRegularExpression(q, "i")),
                    Builders<EvOwner>.Filter.Regex(x => x.Nic, new MongoDB.Bson.BsonRegularExpression(q, "i")));
                filter &= like;
            }

            if (isActive is not null)
                filter &= Builders<EvOwner>.Filter.Eq(x => x.IsActive, isActive.Value);

            var total = await _col.CountDocumentsAsync(filter);
            var items = await _col.Find(filter)
                                  .SortByDescending(x => x.UpdatedAtUtc)
                                  .Skip(skip)
                                  .Limit(take)
                                  .ToListAsync();

            return (items, total);
        }
        
    }
}
using EV_ChargingStationBooking_system_EAD.Api.Domain;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public interface IAuthUserRepository
    {
        Task<AuthUser?> GetByUsernameAsync(string username);
        Task<AuthUser?> GetByIdAsync(string id);
        Task CreateAsync(AuthUser u);
        Task<long> CountBackofficeAsync();
        Task UpdateAsync(AuthUser u);
        Task<bool> EmailInUseAsync(string email, string excludeUserId);
        Task DeleteAsync(string id);

        // NEW: search staff (Backoffice/Operator) with optional role filter + paging
        Task<(IReadOnlyList<AuthUser> items, long total)>
            SearchStaffAsync(string? q, string? role, int skip, int take);

        Task SetActiveAsync(string id, bool isActive); 
    }

    public sealed class AuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<AuthUser> _col;

        public AuthUserRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<AuthUser>("users_auth");
        }

        public Task<AuthUser?> GetByUsernameAsync(string username)
            => _col.Find(x => x.Username == username).FirstOrDefaultAsync();

        public Task<AuthUser?> GetByIdAsync(string id)
            => _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public Task CreateAsync(AuthUser u) => _col.InsertOneAsync(u);

        public async Task<long> CountBackofficeAsync()
            => await _col.CountDocumentsAsync(x => x.Role == Role.Backoffice);

        public Task UpdateAsync(AuthUser u)
            => _col.ReplaceOneAsync(x => x.Id == u.Id, u, new ReplaceOptions { IsUpsert = false });

        public async Task<bool> EmailInUseAsync(string email, string excludeUserId)
        {
            var count = await _col.CountDocumentsAsync(x => x.Username == email && x.Id != excludeUserId);
            return count > 0;
        }

        public Task DeleteAsync(string id)
            => _col.DeleteOneAsync(x => x.Id == id);

        // -------- NEW: staff listing --------
        public async Task<(IReadOnlyList<AuthUser> items, long total)>
            SearchStaffAsync(string? q, string? role, int skip, int take)
        {
            // Only staff roles (Backoffice, Operator)
            var filter = Builders<AuthUser>.Filter.In(x => x.Role, new[] { Role.Backoffice, Role.Operator });

            if (!string.IsNullOrWhiteSpace(role))
            {
                var normalized = role.Equals(Role.Backoffice, StringComparison.OrdinalIgnoreCase) ? Role.Backoffice
                               : role.Equals(Role.Operator, StringComparison.OrdinalIgnoreCase) ? Role.Operator
                               : null;
                if (normalized != null)
                    filter &= Builders<AuthUser>.Filter.Eq(x => x.Role, normalized);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var rx = new MongoDB.Bson.BsonRegularExpression(q, "i");
                filter &= Builders<AuthUser>.Filter.Or(
                    Builders<AuthUser>.Filter.Regex(x => x.Username, rx),  // email
                    Builders<AuthUser>.Filter.Regex(x => x.FullName, rx)
                );
            }

            var total = await _col.CountDocumentsAsync(filter);
            var items = await _col.Find(filter)
                                  .SortBy(x => x.Username)
                                  .Skip(skip).Limit(take)
                                  .ToListAsync();
            return (items, total);
        }
        
            public async Task SetActiveAsync(string id, bool isActive)
            {
                var update = Builders<AuthUser>.Update
                    .Set(x => x.IsActive, isActive)
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
                await _col.UpdateOneAsync(x => x.Id == id, update);
            }
    }
}

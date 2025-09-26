using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EV_ChargingStationBooking_system_EAD.Api.Domain.Entities;
using EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Mongo;
using MongoDB.Driver;

namespace EV_ChargingStationBooking_system_EAD.Api.Infrastructure.Repositories
{
    public sealed class AuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<AuthUser> _col;

        public AuthUserRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<AuthUser>("users_auth");
        }

       public async Task<AuthUser?> GetByUsernameAsync(string username)
{
    return await _col.Find(x => x.Username == username)
                     .FirstOrDefaultAsync();
}

public async Task<AuthUser?> GetByIdAsync(string id)
{
    return await _col.Find(x => x.Id == id)
                     .FirstOrDefaultAsync();
}


        public Task CreateAsync(AuthUser u) =>
            _col.InsertOneAsync(u);

        public Task<long> CountBackofficeAsync() =>
            _col.CountDocumentsAsync(x => x.Role == Domain.Role.Backoffice);

        // helpers used by controllers
        public async Task<Dictionary<string, string>> GetUsernamesByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
                        ?? new List<string>();
            if (idList.Count == 0) return new Dictionary<string, string>();

            var items = await _col.Find(u => idList.Contains(u.Id))
                                  .Project(u => new { u.Id, u.Username })
                                  .ToListAsync();

            return items.ToDictionary(x => x.Id, x => x.Username);
        }

        public Task UpdatePasswordHashAsync(string userId, string newHash)
        {
            var update = Builders<AuthUser>.Update.Set(u => u.PasswordHash, newHash);
            return _col.UpdateOneAsync(u => u.Id == userId, update);
        }

        public Task UpdateIsActiveAsync(string userId, bool isActive)
        {
            // If AuthUser doesn't have an IsActive property, use the string version:
            // var update = Builders<AuthUser>.Update.Set("IsActive", isActive);
            var update = Builders<AuthUser>.Update.Set("IsActive", isActive);
            return _col.UpdateOneAsync(u => u.Id == userId, update);
        }
    }
}

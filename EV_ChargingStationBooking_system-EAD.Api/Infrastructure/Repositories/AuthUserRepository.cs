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
    }

    public sealed class AuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<AuthUser> _col;
        public AuthUserRepository(MongoContext ctx)
        {
            _col = ctx.GetCollection<AuthUser>("users_auth");
        }
        public Task<AuthUser?> GetByUsernameAsync(string username) => _col.Find(x =>
        x.Username == username).FirstOrDefaultAsync();

        public Task<AuthUser?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public Task CreateAsync(AuthUser u) => _col.InsertOneAsync(u);

        public async Task<long> CountBackofficeAsync() => await _col.CountDocumentsAsync(x =>
        x.Role == Domain.Role.Backoffice);

        public Task UpdateAsync(AuthUser u)
        => _col.ReplaceOneAsync(x => x.Id == u.Id, u, new ReplaceOptions { IsUpsert = false });

        public async Task<bool> EmailInUseAsync(string email, string excludeUserId)
        {
            var count = await _col.CountDocumentsAsync(x => x.Username == email && x.Id != excludeUserId);
            return count > 0;
        }

        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);
    }
}